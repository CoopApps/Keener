using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlatformerEngine.Core.Data;
using PlatformerEngine.Core.Graphics;
using PlatformerEngine.Core.Input;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Editor
{
    /// <summary>
    /// Editor tool modes
    /// </summary>
    public enum EditorTool
    {
        Brush,      // Paint tiles
        Eraser,     // Remove tiles
        Fill,       // Flood fill
        Rectangle,  // Draw rectangles
        Entity,     // Place entities
        Select      // Select/move tiles
    }

    /// <summary>
    /// Complete level editor
    /// </summary>
    public class LevelEditor
    {
        // Level data
        private LevelData levelData;
        private TileMap tileMap;
        private int currentLayer = 0;

        // Editor state
        public EditorTool CurrentTool { get; set; } = EditorTool.Brush;
        public int SelectedTile { get; set; } = 1;
        public string SelectedEntityType { get; set; } = "PlayerSpawn";
        public bool ShowGrid { get; set; } = true;
        public bool ShowCollision { get; set; } = true;
        public bool ShowEntities { get; set; } = true;

        // Camera
        private Vector2 cameraPosition = Vector2.Zero;
        private float cameraZoom = 1.0f;
        private const float MIN_ZOOM = 0.5f;
        private const float MAX_ZOOM = 4.0f;

        // Input
        private InputManager input;
        private MouseState previousMouseState;
        private bool isDragging;
        private Vector2 dragStart;

        // UI
        private TilePalette tilePalette;
        private EntityPalette entityPalette;
        private LayerPanel layerPanel;

        // Rendering
        private SpriteBatch spriteBatch;
        private Texture2D pixelTexture;
        private SpriteFont font;

        public LevelEditor(GraphicsDevice graphicsDevice, SpriteFont font)
        {
            this.font = font;
            input = InputManager.Instance;

            spriteBatch = new SpriteBatch(graphicsDevice);

            // Create pixel texture
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Create UI panels
            tilePalette = new TilePalette(graphicsDevice, font, new Vector2(10, 10));
            entityPalette = new EntityPalette(font, new Vector2(10, 300));
            layerPanel = new LayerPanel(font, new Vector2(10, 500));
        }

        /// <summary>
        /// Create new level
        /// </summary>
        public void NewLevel(int width, int height, int tileSize = 16)
        {
            levelData = new LevelData
            {
                Name = "New Level",
                Width = width,
                Height = height,
                TileSize = tileSize
            };

            // Add default layers
            levelData.Layers.Add(new TileLayerData
            {
                Name = "Background",
                Depth = 0.3f,
                Data = new int[width * height]
            });

            levelData.Layers.Add(new TileLayerData
            {
                Name = "Main",
                Depth = 0.5f,
                Data = new int[width * height]
            });

            levelData.Layers.Add(new TileLayerData
            {
                Name = "Collision",
                Depth = 0.7f,
                Visible = false,
                Data = new int[width * height]
            });

            tileMap = new TileMap(width, height, tileSize);
        }

        /// <summary>
        /// Load existing level
        /// </summary>
        public void LoadLevel(string filePath)
        {
            levelData = LevelData.Load(filePath);
            tileMap = new TileMap(levelData.Width, levelData.Height, levelData.TileSize);

            // Setup tilemap from level data
            foreach (var layerData in levelData.Layers)
            {
                var layer = tileMap.AddLayer(layerData.Name, layerData.Depth);
                layer.Visible = layerData.Visible;

                for (int y = 0; y < levelData.Height; y++)
                {
                    for (int x = 0; x < levelData.Width; x++)
                    {
                        int tileIndex = layerData.Data[y * levelData.Width + x];
                        layer.SetTile(x, y, tileIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Save level
        /// </summary>
        public void SaveLevel(string filePath)
        {
            // Update level data from tilemap
            for (int i = 0; i < levelData.Layers.Count; i++)
            {
                var layer = tileMap.Layers[i];
                var layerData = levelData.Layers[i];

                for (int y = 0; y < levelData.Height; y++)
                {
                    for (int x = 0; x < levelData.Width; x++)
                    {
                        layerData.Data[y * levelData.Width + x] = layer.GetTile(x, y);
                    }
                }
            }

            levelData.Save(filePath);
        }

        /// <summary>
        /// Update editor
        /// </summary>
        public void Update(GameTime gameTime)
        {
            input.Update();

            HandleCameraInput();
            HandleToolInput();
            HandleUI();
        }

        private void HandleCameraInput()
        {
            // Pan camera with middle mouse or arrow keys
            if (input.IsButtonDown(Buttons.RightStick) || input.IsKeyDown(Keys.Space))
            {
                Vector2 mouseDelta = input.MouseDelta;
                cameraPosition -= mouseDelta / cameraZoom;
            }

            // Zoom with mouse wheel
            int scrollDelta = input.GetScrollWheelDelta();
            if (scrollDelta != 0)
            {
                float zoomChange = scrollDelta > 0 ? 1.1f : 0.9f;
                cameraZoom *= zoomChange;
                cameraZoom = MathHelper.Clamp(cameraZoom, MIN_ZOOM, MAX_ZOOM);
            }

            // Camera movement with WASD
            const float CAM_SPEED = 300f;
            if (input.IsKeyDown(Keys.W)) cameraPosition.Y -= CAM_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds / cameraZoom;
            if (input.IsKeyDown(Keys.S)) cameraPosition.Y += CAM_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds / cameraZoom;
            if (input.IsKeyDown(Keys.A)) cameraPosition.X -= CAM_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds / cameraZoom;
            if (input.IsKeyDown(Keys.D)) cameraPosition.X += CAM_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds / cameraZoom;
        }

        private void HandleToolInput()
        {
            // Tool selection with number keys
            if (input.IsKeyPressed(Keys.D1)) CurrentTool = EditorTool.Brush;
            if (input.IsKeyPressed(Keys.D2)) CurrentTool = EditorTool.Eraser;
            if (input.IsKeyPressed(Keys.D3)) CurrentTool = EditorTool.Fill;
            if (input.IsKeyPressed(Keys.D4)) CurrentTool = EditorTool.Rectangle;
            if (input.IsKeyPressed(Keys.D5)) CurrentTool = EditorTool.Entity;
            if (input.IsKeyPressed(Keys.D6)) CurrentTool = EditorTool.Select;

            // Get mouse position in world space
            Vector2 mousePos = input.MousePosition;
            Vector2 worldPos = ScreenToWorld(mousePos);
            int tileX = (int)(worldPos.X / levelData.TileSize);
            int tileY = (int)(worldPos.Y / levelData.TileSize);

            // Use tools
            if (input.IsLeftMouseDown() && IsInBounds(tileX, tileY))
            {
                switch (CurrentTool)
                {
                    case EditorTool.Brush:
                        PlaceTile(tileX, tileY, SelectedTile);
                        break;

                    case EditorTool.Eraser:
                        PlaceTile(tileX, tileY, 0);
                        break;

                    case EditorTool.Fill:
                        if (input.IsLeftMousePressed())
                            FloodFill(tileX, tileY, SelectedTile);
                        break;

                    case EditorTool.Entity:
                        if (input.IsLeftMousePressed())
                            PlaceEntity(worldPos);
                        break;
                }
            }
        }

        private void HandleUI()
        {
            // Layer selection
            if (input.IsKeyPressed(Keys.PageUp))
            {
                currentLayer = Math.Max(0, currentLayer - 1);
            }
            if (input.IsKeyPressed(Keys.PageDown))
            {
                currentLayer = Math.Min(levelData.Layers.Count - 1, currentLayer + 1);
            }

            // Toggle views
            if (input.IsKeyPressed(Keys.G)) ShowGrid = !ShowGrid;
            if (input.IsKeyPressed(Keys.C)) ShowCollision = !ShowCollision;
            if (input.IsKeyPressed(Keys.E)) ShowEntities = !ShowEntities;
        }

        private void PlaceTile(int x, int y, int tileIndex)
        {
            if (currentLayer < 0 || currentLayer >= tileMap.Layers.Count)
                return;

            tileMap.Layers[currentLayer].SetTile(x, y, tileIndex);
        }

        private void FloodFill(int startX, int startY, int newTile)
        {
            if (currentLayer < 0 || currentLayer >= tileMap.Layers.Count)
                return;

            var layer = tileMap.Layers[currentLayer];
            int originalTile = layer.GetTile(startX, startY);

            if (originalTile == newTile)
                return;

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();

                if (!IsInBounds(p.X, p.Y))
                    continue;

                if (layer.GetTile(p.X, p.Y) != originalTile)
                    continue;

                layer.SetTile(p.X, p.Y, newTile);

                queue.Enqueue(new Point(p.X + 1, p.Y));
                queue.Enqueue(new Point(p.X - 1, p.Y));
                queue.Enqueue(new Point(p.X, p.Y + 1));
                queue.Enqueue(new Point(p.X, p.Y - 1));
            }
        }

        private void PlaceEntity(Vector2 position)
        {
            levelData.Entities.Add(new EntityData
            {
                Type = SelectedEntityType,
                X = position.X,
                Y = position.Y
            });
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < levelData.Width && y >= 0 && y < levelData.Height;
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return screenPos / cameraZoom + cameraPosition;
        }

        private Vector2 WorldToScreen(Vector2 worldPos)
        {
            return (worldPos - cameraPosition) * cameraZoom;
        }

        /// <summary>
        /// Draw editor
        /// </summary>
        public void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.CornflowerBlue);

            // Draw level
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, null, GetCameraMatrix());

            // Draw tilemap
            if (tileMap != null)
            {
                Rectangle viewRect = new Rectangle(
                    (int)cameraPosition.X,
                    (int)cameraPosition.Y,
                    (int)(graphicsDevice.Viewport.Width / cameraZoom),
                    (int)(graphicsDevice.Viewport.Height / cameraZoom)
                );

                tileMap.Draw(spriteBatch, viewRect);
            }

            // Draw grid
            if (ShowGrid)
            {
                DrawGrid();
            }

            // Draw entities
            if (ShowEntities)
            {
                DrawEntities();
            }

            spriteBatch.End();

            // Draw UI
            spriteBatch.Begin();

            DrawUI();

            spriteBatch.End();
        }

        private void DrawGrid()
        {
            // Simplified grid drawing
            // Full implementation would draw grid lines
        }

        private void DrawEntities()
        {
            foreach (var entity in levelData.Entities)
            {
                Rectangle entityBounds = new Rectangle((int)entity.X - 8, (int)entity.Y - 8, 16, 16);
                spriteBatch.Draw(pixelTexture, entityBounds, new Color(255, 0, 0, 128));
            }
        }

        private void DrawUI()
        {
            // Tool indicator
            string toolText = $"Tool: {CurrentTool} | Layer: {(currentLayer >= 0 ? levelData.Layers[currentLayer].Name : "None")} | Tile: {SelectedTile}";
            spriteBatch.DrawString(font, toolText, new Vector2(10, 10), Color.White);

            // Instructions
            string instructions = "1-6: Tools | WASD: Pan | Scroll: Zoom | PgUp/PgDn: Layers | G/C/E: Toggle Grid/Collision/Entities";
            spriteBatch.DrawString(font, instructions, new Vector2(10, 30), Color.LightGray);
        }

        private Matrix GetCameraMatrix()
        {
            return Matrix.CreateTranslation(new Vector3(-cameraPosition, 0)) *
                   Matrix.CreateScale(cameraZoom);
        }
    }

    // Helper classes for UI panels
    class TilePalette
    {
        public Vector2 Position { get; set; }
        // Simplified - full implementation would show tile grid

        public TilePalette(GraphicsDevice gd, SpriteFont font, Vector2 pos)
        {
            Position = pos;
        }
    }

    class EntityPalette
    {
        public Vector2 Position { get; set; }
        // Shows entity types to place

        public EntityPalette(SpriteFont font, Vector2 pos)
        {
            Position = pos;
        }
    }

    class LayerPanel
    {
        public Vector2 Position { get; set; }
        // Shows/manages layers

        public LayerPanel(SpriteFont font, Vector2 pos)
        {
            Position = pos;
        }
    }
}
