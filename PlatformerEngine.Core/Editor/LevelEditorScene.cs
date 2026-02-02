using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlatformerEngine.Core.Data;
using PlatformerEngine.Core.Entities;
using PlatformerEngine.Core.Graphics;
using PlatformerEngine.Core.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlatformerEngine.Core.Editor
{
    /// <summary>
    /// Comprehensive level editor for creating platformer levels
    /// </summary>
    public class LevelEditorScene : Scenes.Scene
    {
        private enum EditorTool
        {
            Pencil,      // Draw single tiles
            Brush,       // Draw multiple tiles
            Fill,        // Flood fill
            Eraser,      // Erase tiles
            Eyedropper,  // Pick tile from level
            Select,      // Select area
            Entity       // Place entities
        }

        private enum EditorMode
        {
            Tiles,        // Editing tiles
            Collision,    // Editing collision
            Entities      // Placing entities
        }

        // Editor state
        private EditorTool currentTool = EditorTool.Pencil;
        private EditorMode currentMode = EditorMode.Tiles;
        private string currentFileName = "NewLevel";

        // Level data
        private TileMap tileMap;
        private int currentTileId = 1;
        private int currentLayer = 0;
        private TileCollision currentCollision = TileCollision.Solid;
        private List<EntityData> entities = new List<EntityData>();
        private string currentEntityType = "Coin";

        // Camera
        private Vector2 cameraPosition;
        private float cameraZoom = 1.0f;
        private bool isDraggingCamera;
        private Vector2 cameraDragStart;

        // UI
        private SpriteFont font;
        private Texture2D pixelTexture;
        private Rectangle screenBounds;
        private bool showGrid = true;
        private bool showCollision = true;

        // Tile palette
        private readonly int[] tilePalette = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        private int paletteScroll = 0;

        // Entity types
        private readonly string[] entityTypes = {
            "PlayerSpawn", "Coin", "HealthPickup", "ExtraLife", "Key",
            "PowerUp_JumpBoost", "PowerUp_SpeedBoost", "PowerUp_Invincibility",
            "WalkerEnemy", "FlyerEnemy", "ShooterEnemy", "ChargerEnemy",
            "Checkpoint", "Switch", "Bridge", "Teleporter"
        };

        // Selection
        private Rectangle? selectionStart;
        private Rectangle? selectionEnd;
        private int[]? clipboard;

        // Level properties
        private int levelWidth = 64;
        private int levelHeight = 32;
        private int tileSize = 16;

        public LevelEditorScene(SpriteFont font)
        {
            this.font = font;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Create pixel texture
            pixelTexture = new Texture2D(Scenes.SceneManager.Instance.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            screenBounds = Scenes.SceneManager.Instance.GraphicsDevice.Viewport.Bounds;

            // Initialize empty level
            if (tileMap == null)
            {
                InitializeNewLevel();
            }
        }

        private void InitializeNewLevel()
        {
            tileMap = new TileMap(levelWidth, levelHeight, tileSize);

            // Create main layer
            var mainLayer = new TileLayer(levelWidth, levelHeight);
            tileMap.Layers.Add(mainLayer);

            // Create empty collision map
            var collisionMap = new TileCollision[levelWidth * levelHeight];
            tileMap.SetCollisionMap(collisionMap);
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();

            // ESC to exit
            if (input.IsActionPressed(InputAction.Pause))
            {
                Scenes.SceneManager.Instance.ChangeScene("MainMenu");
                return;
            }

            // Mode switching (Tab)
            if (keyboardState.IsKeyDown(Keys.Tab))
            {
                static bool tabPressed = false;
                if (!tabPressed)
                {
                    tabPressed = true;
                    currentMode = (EditorMode)(((int)currentMode + 1) % 3);
                }
            }
            else
            {
                tabPressed = false;
            }

            // Tool selection (1-7)
            if (keyboardState.IsKeyDown(Keys.D1)) currentTool = EditorTool.Pencil;
            if (keyboardState.IsKeyDown(Keys.D2)) currentTool = EditorTool.Brush;
            if (keyboardState.IsKeyDown(Keys.D3)) currentTool = EditorTool.Fill;
            if (keyboardState.IsKeyDown(Keys.D4)) currentTool = EditorTool.Eraser;
            if (keyboardState.IsKeyDown(Keys.D5)) currentTool = EditorTool.Eyedropper;
            if (keyboardState.IsKeyDown(Keys.D6)) currentTool = EditorTool.Select;
            if (keyboardState.IsKeyDown(Keys.D7)) currentTool = EditorTool.Entity;

            // Camera pan (middle mouse or space + drag)
            if (mouseState.MiddleButton == ButtonState.Pressed ||
                (keyboardState.IsKeyDown(Keys.Space) && mouseState.LeftButton == ButtonState.Pressed))
            {
                if (!isDraggingCamera)
                {
                    isDraggingCamera = true;
                    cameraDragStart = new Vector2(mouseState.X, mouseState.Y);
                }
                else
                {
                    Vector2 delta = new Vector2(mouseState.X, mouseState.Y) - cameraDragStart;
                    cameraPosition -= delta / cameraZoom;
                    cameraDragStart = new Vector2(mouseState.X, mouseState.Y);
                }
            }
            else
            {
                isDraggingCamera = false;
            }

            // Zoom (mouse wheel)
            int scrollDelta = mouseState.ScrollWheelValue;
            static int previousScroll = 0;
            if (scrollDelta != previousScroll)
            {
                float zoomDelta = (scrollDelta - previousScroll) * 0.001f;
                cameraZoom = MathHelper.Clamp(cameraZoom + zoomDelta, 0.25f, 4.0f);
                previousScroll = scrollDelta;
            }

            // Toggle grid (G)
            if (keyboardState.IsKeyDown(Keys.G))
            {
                static bool gPressed = false;
                if (!gPressed)
                {
                    gPressed = true;
                    showGrid = !showGrid;
                }
            }
            else
            {
                gPressed = false;
            }

            // Toggle collision view (C)
            if (keyboardState.IsKeyDown(Keys.C))
            {
                static bool cPressed = false;
                if (!cPressed)
                {
                    cPressed = true;
                    showCollision = !showCollision;
                }
            }
            else
            {
                cPressed = false;
            }

            // Save/Load
            if (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl))
            {
                if (keyboardState.IsKeyDown(Keys.S))
                {
                    static bool sPressed = false;
                    if (!sPressed)
                    {
                        sPressed = true;
                        SaveLevel();
                    }
                }
                else
                {
                    sPressed = false;
                }

                if (keyboardState.IsKeyDown(Keys.O))
                {
                    static bool oPressed = false;
                    if (!oPressed)
                    {
                        oPressed = true;
                        LoadLevel();
                    }
                }
                else
                {
                    oPressed = false;
                }
            }

            // Get mouse tile position
            Vector2 mouseScreenPos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 mouseWorldPos = ScreenToWorld(mouseScreenPos);
            int mouseTileX = (int)(mouseWorldPos.X / tileSize);
            int mouseTileY = (int)(mouseWorldPos.Y / tileSize);

            // Handle editing based on mode and tool
            if (!isDraggingCamera)
            {
                if (currentMode == EditorMode.Tiles)
                {
                    HandleTileEditing(mouseState, mouseTileX, mouseTileY);
                }
                else if (currentMode == EditorMode.Collision)
                {
                    HandleCollisionEditing(mouseState, mouseTileX, mouseTileY);
                }
                else if (currentMode == EditorMode.Entities)
                {
                    HandleEntityPlacement(mouseState, mouseWorldPos);
                }
            }

            // Tile palette scrolling
            if (mouseState.X > screenBounds.Width - 100)
            {
                if (mouseState.ScrollWheelValue > previousScroll)
                {
                    paletteScroll = Math.Max(0, paletteScroll - 1);
                }
                else if (mouseState.ScrollWheelValue < previousScroll)
                {
                    paletteScroll = Math.Min(tilePalette.Length - 8, paletteScroll + 1);
                }
            }
        }

        private void HandleTileEditing(MouseState mouseState, int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= levelWidth || tileY < 0 || tileY >= levelHeight)
                return;

            var layer = tileMap.Layers[currentLayer];

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                switch (currentTool)
                {
                    case EditorTool.Pencil:
                    case EditorTool.Brush:
                        layer.SetTile(tileX, tileY, currentTileId);
                        break;

                    case EditorTool.Eraser:
                        layer.SetTile(tileX, tileY, 0);
                        break;

                    case EditorTool.Fill:
                        static bool fillPressed = false;
                        if (!fillPressed)
                        {
                            fillPressed = true;
                            FloodFill(layer, tileX, tileY, layer.GetTile(tileX, tileY), currentTileId);
                        }
                        break;

                    case EditorTool.Eyedropper:
                        static bool eyedropperPressed = false;
                        if (!eyedropperPressed)
                        {
                            eyedropperPressed = true;
                            currentTileId = layer.GetTile(tileX, tileY);
                        }
                        break;
                }
            }
            else
            {
                fillPressed = false;
                eyedropperPressed = false;
            }
        }

        private void HandleCollisionEditing(MouseState mouseState, int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= levelWidth || tileY < 0 || tileY >= levelHeight)
                return;

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                tileMap.SetTileCollision(tileX, tileY, currentCollision);
            }
            else if (mouseState.RightButton == ButtonState.Pressed)
            {
                tileMap.SetTileCollision(tileX, tileY, TileCollision.None);
            }
        }

        private void HandleEntityPlacement(MouseState mouseState, Vector2 worldPos)
        {
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                static bool entityPlaced = false;
                if (!entityPlaced)
                {
                    entityPlaced = true;
                    entities.Add(new EntityData
                    {
                        Type = currentEntityType,
                        Position = worldPos,
                        Id = $"{currentEntityType}_{entities.Count}"
                    });
                }
            }
            else
            {
                entityPlaced = false;
            }

            // Right click to delete entity
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                static bool entityDeleted = false;
                if (!entityDeleted)
                {
                    entityDeleted = true;
                    for (int i = entities.Count - 1; i >= 0; i--)
                    {
                        if (Vector2.Distance(entities[i].Position, worldPos) < 20)
                        {
                            entities.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            else
            {
                entityDeleted = false;
            }
        }

        private void FloodFill(TileLayer layer, int x, int y, int targetTile, int replacementTile)
        {
            if (x < 0 || x >= levelWidth || y < 0 || y >= levelHeight)
                return;
            if (layer.GetTile(x, y) != targetTile || targetTile == replacementTile)
                return;

            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((x, y));
            var visited = new HashSet<(int, int)>();

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                if (visited.Contains((cx, cy)))
                    continue;
                if (cx < 0 || cx >= levelWidth || cy < 0 || cy >= levelHeight)
                    continue;
                if (layer.GetTile(cx, cy) != targetTile)
                    continue;

                visited.Add((cx, cy));
                layer.SetTile(cx, cy, replacementTile);

                queue.Enqueue((cx + 1, cy));
                queue.Enqueue((cx - 1, cy));
                queue.Enqueue((cx, cy + 1));
                queue.Enqueue((cx, cy - 1));
            }
        }

        private void SaveLevel()
        {
            try
            {
                Directory.CreateDirectory("Levels");

                var levelData = new LevelData
                {
                    LevelName = currentFileName,
                    TileWidth = levelWidth,
                    TileHeight = levelHeight,
                    TileSize = tileSize
                };

                // Save layers
                foreach (var layer in tileMap.Layers)
                {
                    var layerData = new TileLayerData
                    {
                        Width = levelWidth,
                        Height = levelHeight,
                        IsVisible = layer.IsVisible,
                        Depth = layer.Depth
                    };

                    // Get tile data
                    int[] tiles = new int[levelWidth * levelHeight];
                    for (int y = 0; y < levelHeight; y++)
                    {
                        for (int x = 0; x < levelWidth; x++)
                        {
                            tiles[y * levelWidth + x] = layer.GetTile(x, y);
                        }
                    }

                    layerData.CompressedTiles = LevelData.CompressLayer(tiles);
                    levelData.Layers.Add(layerData);
                }

                // Save collision
                var collisionData = new TileCollision[levelWidth * levelHeight];
                for (int i = 0; i < collisionData.Length; i++)
                {
                    int x = i % levelWidth;
                    int y = i / levelWidth;
                    collisionData[i] = tileMap.GetTileCollision(x, y);
                }
                levelData.CollisionLayer = collisionData;

                // Save entities
                levelData.Entities = entities;

                // Save to file
                levelData.Save($"Levels/{currentFileName}.json");
                Console.WriteLine($"Saved level to Levels/{currentFileName}.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving level: {ex.Message}");
            }
        }

        private void LoadLevel()
        {
            try
            {
                string path = $"Levels/{currentFileName}.json";
                if (File.Exists(path))
                {
                    var levelData = LevelData.Load(path);

                    levelWidth = levelData.TileWidth;
                    levelHeight = levelData.TileHeight;
                    tileSize = levelData.TileSize;

                    tileMap = new TileMap(levelWidth, levelHeight, tileSize);

                    // Load layers
                    foreach (var layerData in levelData.Layers)
                    {
                        var layer = new TileLayer(levelWidth, levelHeight)
                        {
                            IsVisible = layerData.IsVisible,
                            Depth = layerData.Depth
                        };

                        int[] tiles = LevelData.DecompressLayer(layerData.CompressedTiles, levelWidth * levelHeight);
                        for (int i = 0; i < tiles.Length; i++)
                        {
                            int x = i % levelWidth;
                            int y = i / levelWidth;
                            layer.SetTile(x, y, tiles[i]);
                        }

                        tileMap.Layers.Add(layer);
                    }

                    // Load collision
                    if (levelData.CollisionLayer != null)
                    {
                        tileMap.SetCollisionMap(levelData.CollisionLayer);
                    }

                    // Load entities
                    entities = levelData.Entities ?? new List<EntityData>();

                    Console.WriteLine($"Loaded level from {path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading level: {ex.Message}");
            }
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return (screenPos / cameraZoom) + cameraPosition;
        }

        private Vector2 WorldToScreen(Vector2 worldPos)
        {
            return (worldPos - cameraPosition) * cameraZoom;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.GraphicsDevice.Clear(new Color(40, 40, 60));

            // Draw world (with camera)
            spriteBatch.Begin(transformMatrix: GetCameraMatrix(), samplerState: SamplerState.PointClamp);

            // Draw grid
            if (showGrid)
            {
                DrawGrid(spriteBatch);
            }

            // Draw tiles
            Rectangle viewport = new Rectangle(
                (int)(cameraPosition.X - 100),
                (int)(cameraPosition.Y - 100),
                (int)(screenBounds.Width / cameraZoom + 200),
                (int)(screenBounds.Height / cameraZoom + 200)
            );
            tileMap?.Draw(spriteBatch, viewport);

            // Draw collision overlay
            if (showCollision && currentMode == EditorMode.Collision)
            {
                DrawCollisionOverlay(spriteBatch, viewport);
            }

            // Draw entities
            foreach (var entity in entities)
            {
                DrawEntity(spriteBatch, entity);
            }

            spriteBatch.End();

            // Draw UI (screen space)
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawUI(spriteBatch);
            DrawTilePalette(spriteBatch);
            spriteBatch.End();
        }

        private Matrix GetCameraMatrix()
        {
            return Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0) *
                   Matrix.CreateScale(cameraZoom);
        }

        private void DrawGrid(SpriteBatch spriteBatch)
        {
            Vector2 topLeft = ScreenToWorld(Vector2.Zero);
            Vector2 bottomRight = ScreenToWorld(new Vector2(screenBounds.Width, screenBounds.Height));

            int startX = Math.Max(0, (int)(topLeft.X / tileSize));
            int endX = Math.Min(levelWidth, (int)(bottomRight.X / tileSize) + 1);
            int startY = Math.Max(0, (int)(topLeft.Y / tileSize));
            int endY = Math.Min(levelHeight, (int)(bottomRight.Y / tileSize) + 1);

            Color gridColor = new Color(80, 80, 100, 128);

            for (int x = startX; x <= endX; x++)
            {
                DrawLine(spriteBatch,
                    new Vector2(x * tileSize, startY * tileSize),
                    new Vector2(x * tileSize, endY * tileSize),
                    gridColor, 1);
            }

            for (int y = startY; y <= endY; y++)
            {
                DrawLine(spriteBatch,
                    new Vector2(startX * tileSize, y * tileSize),
                    new Vector2(endX * tileSize, y * tileSize),
                    gridColor, 1);
            }
        }

        private void DrawCollisionOverlay(SpriteBatch spriteBatch, Rectangle viewport)
        {
            int startX = Math.Max(0, viewport.X / tileSize);
            int endX = Math.Min(levelWidth, (viewport.X + viewport.Width) / tileSize + 1);
            int startY = Math.Max(0, viewport.Y / tileSize);
            int endY = Math.Min(levelHeight, (viewport.Y + viewport.Height) / tileSize + 1);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    var collision = tileMap.GetTileCollision(x, y);
                    if (collision != TileCollision.None)
                    {
                        Color color = GetCollisionColor(collision);
                        Rectangle rect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                        DrawRectangle(spriteBatch, rect, color * 0.5f);
                    }
                }
            }
        }

        private Color GetCollisionColor(TileCollision collision)
        {
            return collision switch
            {
                TileCollision.Solid => Color.Red,
                TileCollision.Platform => Color.Yellow,
                TileCollision.Ladder => Color.Green,
                TileCollision.Deadly => Color.Purple,
                TileCollision.Water => Color.Blue,
                TileCollision.Ice => Color.Cyan,
                TileCollision.Slope => Color.Orange,
                _ => Color.White
            };
        }

        private void DrawEntity(SpriteBatch spriteBatch, EntityData entity)
        {
            Color color = entity.Type.Contains("Enemy") ? Color.Red :
                         entity.Type.Contains("PowerUp") ? Color.Yellow :
                         entity.Type == "PlayerSpawn" ? Color.Green :
                         Color.Cyan;

            DrawCircle(spriteBatch, entity.Position, 8, color, filled: true);
            DrawCircle(spriteBatch, entity.Position, 8, Color.White, filled: false, thickness: 1);
        }

        private void DrawUI(SpriteBatch spriteBatch)
        {
            if (font == null) return;

            // Top bar
            DrawRectangle(spriteBatch, new Rectangle(0, 0, screenBounds.Width, 180), new Color(0, 0, 0, 200));

            int y = 10;
            int lineHeight = 20;

            DrawText(spriteBatch, "LEVEL EDITOR", new Vector2(10, y), Color.Yellow);
            y += lineHeight * 2;

            DrawText(spriteBatch, $"Mode: {currentMode} (Tab to switch)", new Vector2(10, y), Color.White);
            y += lineHeight;
            DrawText(spriteBatch, $"Tool: {currentTool} (1-7)", new Vector2(10, y), Color.White);
            y += lineHeight;
            DrawText(spriteBatch, $"Zoom: {cameraZoom:F2}x", new Vector2(10, y), Color.White);
            y += lineHeight;

            if (currentMode == EditorMode.Tiles)
            {
                DrawText(spriteBatch, $"Selected Tile: {currentTileId}", new Vector2(10, y), Color.Cyan);
            }
            else if (currentMode == EditorMode.Collision)
            {
                DrawText(spriteBatch, $"Collision: {currentCollision}", new Vector2(10, y), Color.Cyan);
            }
            else if (currentMode == EditorMode.Entities)
            {
                DrawText(spriteBatch, $"Entity: {currentEntityType}", new Vector2(10, y), Color.Cyan);
            }
            y += lineHeight * 2;

            // Controls
            DrawText(spriteBatch, "Controls: Tab=Mode  1-7=Tools  G=Grid  C=Collision", new Vector2(10, y), Color.Gray);
            y += lineHeight;
            DrawText(spriteBatch, "          Ctrl+S=Save  Ctrl+O=Load  ESC=Exit", new Vector2(10, y), Color.Gray);
        }

        private void DrawTilePalette(SpriteBatch spriteBatch)
        {
            if (currentMode != EditorMode.Tiles) return;

            int paletteX = screenBounds.Width - 90;
            int paletteY = 200;
            int tileDisplaySize = 32;

            DrawRectangle(spriteBatch, new Rectangle(paletteX - 5, paletteY - 5, 70, 400), new Color(0, 0, 0, 200));

            for (int i = 0; i < 10; i++)
            {
                int tileIndex = i + paletteScroll;
                if (tileIndex >= tilePalette.Length) break;

                int tileId = tilePalette[tileIndex];
                int x = paletteX;
                int y = paletteY + i * (tileDisplaySize + 5);

                Color tileColor = tileId == 0 ? Color.DarkGray : Color.LightBlue;
                if (tileId == currentTileId) tileColor = Color.Yellow;

                DrawRectangle(spriteBatch, new Rectangle(x, y, tileDisplaySize, tileDisplaySize), tileColor);
                DrawText(spriteBatch, tileId.ToString(), new Vector2(x + 5, y + 5), Color.Black);
            }
        }

        private void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
        {
            if (font != null)
            {
                spriteBatch.DrawString(font, text, position, color);
            }
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, bool filled = true)
        {
            spriteBatch.Draw(pixelTexture, rect, color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();

            spriteBatch.Draw(pixelTexture,
                new Rectangle((int)start.X, (int)start.Y, (int)length, thickness),
                null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }

        private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, bool filled = true, int thickness = 1)
        {
            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)i / segments * MathHelper.TwoPi;
                float angle2 = (float)(i + 1) / segments * MathHelper.TwoPi;

                Vector2 p1 = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * radius;
                Vector2 p2 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * radius;

                if (filled)
                {
                    DrawLine(spriteBatch, center, p1, color, (int)radius / 2);
                }
                else
                {
                    DrawLine(spriteBatch, p1, p2, color, thickness);
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            pixelTexture?.Dispose();
        }
    }
}
