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
    /// Enhanced level editor with retro UI and advanced features
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

        private enum UIPanel
        {
            None,
            TilesetBrowser,
            EntityProperties,
            LayerVisibility,
            PrefabBrowser,
            Settings
        }

        // Editor state
        private EditorTool currentTool = EditorTool.Pencil;
        private EditorMode currentMode = EditorMode.Tiles;
        private UIPanel activePanel = UIPanel.None;
        private string currentFileName = "NewLevel";

        // Level data
        private TileMap tileMap;
        private int currentTileId = 1;
        private int currentLayer = 0;
        private TileCollision currentCollision = TileCollision.Solid;
        private List<EntityData> entities = new List<EntityData>();
        private string currentEntityType = "Coin";
        private EntityData selectedEntity;

        // Undo/Redo
        private EditorHistory history = new EditorHistory();

        // Clipboard
        private EditorClipboard clipboard = new EditorClipboard();
        private Rectangle? selectionStart;
        private Rectangle? selectionArea;

        // Prefabs
        private List<Prefab> prefabs = new List<Prefab>();
        private Prefab selectedPrefab;

        // Camera
        private Vector2 cameraPosition;
        private float cameraZoom = 2.0f;
        private bool isDraggingCamera;
        private Vector2 cameraDragStart;

        // UI
        private SpriteFont font;
        private Texture2D pixelTexture;
        private Rectangle screenBounds;
        private bool showGrid = true;
        private bool showCollision = true;
        private bool showMinimap = true;
        private bool enableCRT = false;
        private bool showScanlines = false;
        private GameTime lastGameTime;

        // Tile palette
        private readonly int[] tilePalette = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
        private int paletteScroll = 0;
        private int tilesetBrowserScroll = 0;

        // Entity types and properties
        private readonly string[] entityTypes = {
            "PlayerSpawn", "Coin", "HealthPickup", "ExtraLife", "Key",
            "PowerUp_JumpBoost", "PowerUp_SpeedBoost", "PowerUp_Invincibility",
            "WalkerEnemy", "FlyerEnemy", "ShooterEnemy", "ChargerEnemy",
            "Checkpoint", "Switch", "Bridge", "Teleporter"
        };

        // Entity property editing
        private Dictionary<string, string> editingProperties = new Dictionary<string, string>();
        private string editingPropertyKey = null;

        // Animation state
        private float animationTime = 0;
        private int animationFrame = 0;

        // Level properties
        private int levelWidth = 64;
        private int levelHeight = 32;
        private int tileSize = 16;

        // Input state tracking
        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;

        // UI Rectangles
        private Rectangle topBar;
        private Rectangle sidePanel;
        private Rectangle minimapRect;
        private Rectangle tilePaletteRect;

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

            // Setup UI rectangles
            topBar = new Rectangle(0, 0, screenBounds.Width, 80);
            sidePanel = new Rectangle(screenBounds.Width - 250, 80, 250, screenBounds.Height - 80);
            minimapRect = new Rectangle(screenBounds.Width - 240, 90, 220, 160);
            tilePaletteRect = new Rectangle(screenBounds.Width - 240, 260, 220, 400);

            // Initialize empty level
            if (tileMap == null)
            {
                InitializeNewLevel();
            }

            // Load default prefabs
            LoadDefaultPrefabs();

            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
        }

        private void InitializeNewLevel()
        {
            tileMap = new TileMap(levelWidth, levelHeight, tileSize);

            // Create main layer
            var mainLayer = new TileLayer(levelWidth, levelHeight);
            tileMap.Layers.Add(mainLayer);

            // Create background layer
            var bgLayer = new TileLayer(levelWidth, levelHeight) { Depth = 0.9f };
            tileMap.Layers.Add(bgLayer);

            // Create foreground layer
            var fgLayer = new TileLayer(levelWidth, levelHeight) { Depth = 0.1f };
            tileMap.Layers.Add(fgLayer);

            // Create empty collision map
            var collisionMap = new TileCollision[levelWidth * levelHeight];
            tileMap.SetCollisionMap(collisionMap);
        }

        private void LoadDefaultPrefabs()
        {
            // Enemy patrol pattern
            prefabs.Add(new Prefab
            {
                Name = "Enemy Patrol",
                Entities = new List<EntityData>
                {
                    new EntityData { Type = "WalkerEnemy", Position = Vector2.Zero },
                    new EntityData { Type = "WalkerEnemy", Position = new Vector2(64, 0) }
                }
            });

            // Coin row
            prefabs.Add(new Prefab
            {
                Name = "Coin Row",
                Entities = new List<EntityData>
                {
                    new EntityData { Type = "Coin", Position = Vector2.Zero },
                    new EntityData { Type = "Coin", Position = new Vector2(16, 0) },
                    new EntityData { Type = "Coin", Position = new Vector2(32, 0) },
                    new EntityData { Type = "Coin", Position = new Vector2(48, 0) },
                    new EntityData { Type = "Coin", Position = new Vector2(64, 0) }
                }
            });
        }

        public override void Update(GameTime gameTime)
        {
            lastGameTime = gameTime;
            animationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (animationTime >= 0.15f)
            {
                animationTime = 0;
                animationFrame = (animationFrame + 1) % 4;
            }

            var input = InputManager.Instance;
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();

            // ESC to exit
            if (input.IsActionPressed(InputAction.Pause))
            {
                if (activePanel != UIPanel.None)
                {
                    activePanel = UIPanel.None;
                }
                else
                {
                    Scenes.SceneManager.Instance.ChangeScene("MainMenu");
                    return;
                }
            }

            // Handle keyboard shortcuts
            HandleKeyboardShortcuts(keyboardState);

            // Camera pan (middle mouse or space + drag)
            HandleCameraControls(mouseState, keyboardState);

            // Get mouse position
            Vector2 mouseScreenPos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 mouseWorldPos = ScreenToWorld(mouseScreenPos);
            int mouseTileX = (int)(mouseWorldPos.X / tileSize);
            int mouseTileY = (int)(mouseWorldPos.Y / tileSize);

            // Check if mouse is over UI panels
            bool isOverUI = topBar.Contains(mouseScreenPos) || sidePanel.Contains(mouseScreenPos);

            // Handle panel interactions
            if (isOverUI)
            {
                HandleUIInput(mouseState, mouseScreenPos);
            }
            // Handle editing in world space
            else if (!isDraggingCamera)
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

            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;
        }

        private void HandleKeyboardShortcuts(KeyboardState keyboardState)
        {
            bool ctrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            bool shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

            // Undo/Redo
            if (ctrl && IsKeyPressed(keyboardState, Keys.Z) && !shift)
            {
                history.Undo();
            }
            if (ctrl && ((IsKeyPressed(keyboardState, Keys.Z) && shift) || IsKeyPressed(keyboardState, Keys.Y)))
            {
                history.Redo();
            }

            // Copy/Paste
            if (ctrl && IsKeyPressed(keyboardState, Keys.C))
            {
                CopySelection();
            }
            if (ctrl && IsKeyPressed(keyboardState, Keys.V))
            {
                PasteSelection();
            }

            // Save/Load
            if (ctrl && IsKeyPressed(keyboardState, Keys.S))
            {
                SaveLevel();
            }
            if (ctrl && IsKeyPressed(keyboardState, Keys.O))
            {
                LoadLevel();
            }

            // Test play
            if (IsKeyPressed(keyboardState, Keys.F5))
            {
                TestPlayLevel();
            }

            // Mode switching (Tab)
            if (IsKeyPressed(keyboardState, Keys.Tab))
            {
                currentMode = (EditorMode)(((int)currentMode + 1) % 3);
            }

            // Tool selection (1-7)
            if (IsKeyPressed(keyboardState, Keys.D1)) currentTool = EditorTool.Pencil;
            if (IsKeyPressed(keyboardState, Keys.D2)) currentTool = EditorTool.Brush;
            if (IsKeyPressed(keyboardState, Keys.D3)) currentTool = EditorTool.Fill;
            if (IsKeyPressed(keyboardState, Keys.D4)) currentTool = EditorTool.Eraser;
            if (IsKeyPressed(keyboardState, Keys.D5)) currentTool = EditorTool.Eyedropper;
            if (IsKeyPressed(keyboardState, Keys.D6)) currentTool = EditorTool.Select;
            if (IsKeyPressed(keyboardState, Keys.D7)) currentTool = EditorTool.Entity;

            // Toggle overlays
            if (IsKeyPressed(keyboardState, Keys.G)) showGrid = !showGrid;
            if (IsKeyPressed(keyboardState, Keys.C)) showCollision = !showCollision;
            if (IsKeyPressed(keyboardState, Keys.M)) showMinimap = !showMinimap;

            // Panel toggles
            if (IsKeyPressed(keyboardState, Keys.T)) activePanel = activePanel == UIPanel.TilesetBrowser ? UIPanel.None : UIPanel.TilesetBrowser;
            if (IsKeyPressed(keyboardState, Keys.P)) activePanel = activePanel == UIPanel.PrefabBrowser ? UIPanel.None : UIPanel.PrefabBrowser;
            if (IsKeyPressed(keyboardState, Keys.L)) activePanel = activePanel == UIPanel.LayerVisibility ? UIPanel.None : UIPanel.LayerVisibility;
        }

        private bool IsKeyPressed(KeyboardState current, Keys key)
        {
            return current.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
        }

        private void HandleCameraControls(MouseState mouseState, KeyboardState keyboardState)
        {
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
            if (mouseState.ScrollWheelValue != previousMouseState.ScrollWheelValue)
            {
                float zoomDelta = (mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue) * 0.001f;
                cameraZoom = MathHelper.Clamp(cameraZoom + zoomDelta, 0.5f, 4.0f);
            }
        }

        private void HandleUIInput(MouseState mouseState, Vector2 mousePos)
        {
            // Handle tile palette clicks
            if (currentMode == EditorMode.Tiles && tilePaletteRect.Contains(mousePos))
            {
                if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                {
                    int tileIndex = (int)((mousePos.Y - tilePaletteRect.Y) / 45) + tilesetBrowserScroll;
                    if (tileIndex >= 0 && tileIndex < tilePalette.Length)
                    {
                        currentTileId = tilePalette[tileIndex];
                    }
                }

                // Scroll in palette
                if (mouseState.ScrollWheelValue != previousMouseState.ScrollWheelValue)
                {
                    int scrollDelta = (previousMouseState.ScrollWheelValue - mouseState.ScrollWheelValue) / 120;
                    tilesetBrowserScroll = MathHelper.Clamp(tilesetBrowserScroll + scrollDelta, 0, Math.Max(0, tilePalette.Length - 8));
                }
            }

            // Handle minimap clicks
            if (showMinimap && minimapRect.Contains(mousePos))
            {
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    float relX = (mousePos.X - minimapRect.X) / minimapRect.Width;
                    float relY = (mousePos.Y - minimapRect.Y) / minimapRect.Height;
                    cameraPosition = new Vector2(
                        relX * levelWidth * tileSize - screenBounds.Width / (2 * cameraZoom),
                        relY * levelHeight * tileSize - screenBounds.Height / (2 * cameraZoom)
                    );
                }
            }
        }

        private void HandleTileEditing(MouseState mouseState, int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= levelWidth || tileY < 0 || tileY >= levelHeight)
                return;

            var layer = tileMap.Layers[Math.Min(currentLayer, tileMap.Layers.Count - 1)];

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                switch (currentTool)
                {
                    case EditorTool.Pencil:
                    case EditorTool.Brush:
                        if (layer.GetTile(tileX, tileY) != currentTileId)
                        {
                            var action = new PaintTileAction(layer, tileX, tileY, currentTileId);
                            history.ExecuteAction(action);
                        }
                        break;

                    case EditorTool.Eraser:
                        if (layer.GetTile(tileX, tileY) != 0)
                        {
                            var action = new PaintTileAction(layer, tileX, tileY, 0);
                            history.ExecuteAction(action);
                        }
                        break;

                    case EditorTool.Fill:
                        if (previousMouseState.LeftButton == ButtonState.Released)
                        {
                            FloodFill(layer, tileX, tileY, layer.GetTile(tileX, tileY), currentTileId);
                        }
                        break;

                    case EditorTool.Eyedropper:
                        if (previousMouseState.LeftButton == ButtonState.Released)
                        {
                            currentTileId = layer.GetTile(tileX, tileY);
                        }
                        break;

                    case EditorTool.Select:
                        if (previousMouseState.LeftButton == ButtonState.Released)
                        {
                            selectionStart = new Rectangle(tileX, tileY, 1, 1);
                        }
                        else if (selectionStart.HasValue)
                        {
                            int minX = Math.Min(selectionStart.Value.X, tileX);
                            int minY = Math.Min(selectionStart.Value.Y, tileY);
                            int maxX = Math.Max(selectionStart.Value.X, tileX);
                            int maxY = Math.Max(selectionStart.Value.Y, tileY);
                            selectionArea = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
                        }
                        break;
                }
            }
            else
            {
                if (currentTool == EditorTool.Select && selectionStart.HasValue)
                {
                    selectionStart = null;
                }
            }
        }

        private void HandleCollisionEditing(MouseState mouseState, int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= levelWidth || tileY < 0 || tileY >= levelHeight)
                return;

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (tileMap.GetTileCollision(tileX, tileY) != currentCollision)
                {
                    var action = new PaintCollisionAction(tileMap, tileX, tileY, currentCollision);
                    history.ExecuteAction(action);
                }
            }
            else if (mouseState.RightButton == ButtonState.Pressed)
            {
                if (tileMap.GetTileCollision(tileX, tileY) != TileCollision.None)
                {
                    var action = new PaintCollisionAction(tileMap, tileX, tileY, TileCollision.None);
                    history.ExecuteAction(action);
                }
            }

            // Cycle collision type with scroll wheel
            if (mouseState.ScrollWheelValue != previousMouseState.ScrollWheelValue)
            {
                int collisionTypes = Enum.GetValues(typeof(TileCollision)).Length;
                int current = (int)currentCollision;
                int delta = (mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue) > 0 ? 1 : -1;
                currentCollision = (TileCollision)(((current + delta) % collisionTypes + collisionTypes) % collisionTypes);
            }
        }

        private void HandleEntityPlacement(MouseState mouseState, Vector2 worldPos)
        {
            // Left click to place
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                var newEntity = new EntityData
                {
                    Type = currentEntityType,
                    Position = worldPos,
                    Id = $"{currentEntityType}_{entities.Count}",
                    Properties = new Dictionary<string, string>()
                };

                var action = new PlaceEntityAction(entities, newEntity);
                history.ExecuteAction(action);
                selectedEntity = newEntity;
                activePanel = UIPanel.EntityProperties;
            }

            // Right click to delete/select
            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    if (Vector2.Distance(entities[i].Position, worldPos) < 20)
                    {
                        if (mouseState.LeftButton == ButtonState.Pressed)
                        {
                            selectedEntity = entities[i];
                            activePanel = UIPanel.EntityProperties;
                        }
                        else
                        {
                            var action = new DeleteEntityAction(entities, entities[i]);
                            history.ExecuteAction(action);
                        }
                        break;
                    }
                }
            }
        }

        private void FloodFill(TileLayer layer, int x, int y, int targetTile, int replacementTile)
        {
            if (x < 0 || x >= levelWidth || y < 0 || y >= levelHeight)
                return;
            if (targetTile == replacementTile)
                return;

            var compound = new CompoundAction($"Fill with tile {replacementTile}");
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
                compound.AddAction(new PaintTileAction(layer, cx, cy, replacementTile));

                queue.Enqueue((cx + 1, cy));
                queue.Enqueue((cx - 1, cy));
                queue.Enqueue((cx, cy + 1));
                queue.Enqueue((cx, cy - 1));
            }

            history.ExecuteAction(compound);
        }

        private void CopySelection()
        {
            if (selectionArea.HasValue)
            {
                var sel = selectionArea.Value;
                clipboard.Width = sel.Width;
                clipboard.Height = sel.Height;
                clipboard.Tiles = new int[sel.Width, sel.Height];
                clipboard.Collisions = new TileCollision[sel.Width, sel.Height];

                var layer = tileMap.Layers[currentLayer];
                for (int y = 0; y < sel.Height; y++)
                {
                    for (int x = 0; x < sel.Width; x++)
                    {
                        int worldX = sel.X + x;
                        int worldY = sel.Y + y;
                        if (worldX >= 0 && worldX < levelWidth && worldY >= 0 && worldY < levelHeight)
                        {
                            clipboard.Tiles[x, y] = layer.GetTile(worldX, worldY);
                            clipboard.Collisions[x, y] = tileMap.GetTileCollision(worldX, worldY);
                        }
                    }
                }
            }
        }

        private void PasteSelection()
        {
            if (clipboard.IsEmpty || !selectionArea.HasValue)
                return;

            var compound = new CompoundAction("Paste selection");
            var sel = selectionArea.Value;
            var layer = tileMap.Layers[currentLayer];

            for (int y = 0; y < clipboard.Height; y++)
            {
                for (int x = 0; x < clipboard.Width; x++)
                {
                    int worldX = sel.X + x;
                    int worldY = sel.Y + y;
                    if (worldX >= 0 && worldX < levelWidth && worldY >= 0 && worldY < levelHeight)
                    {
                        if (currentMode == EditorMode.Tiles)
                        {
                            compound.AddAction(new PaintTileAction(layer, worldX, worldY, clipboard.Tiles[x, y]));
                        }
                        else if (currentMode == EditorMode.Collision)
                        {
                            compound.AddAction(new PaintCollisionAction(tileMap, worldX, worldY, clipboard.Collisions[x, y]));
                        }
                    }
                }
            }

            history.ExecuteAction(compound);
        }

        private void TestPlayLevel()
        {
            // Save level temporarily
            SaveLevel();
            // Switch to gameplay scene with this level
            // Note: This requires GameLevelScene to accept a level path parameter
            Console.WriteLine("Test play mode - Switch to GameLevelScene with current level");
            // Scenes.SceneManager.Instance.ChangeScene("GameLevel", $"Levels/{currentFileName}.json");
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

                    history.Clear();
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
            spriteBatch.GraphicsDevice.Clear(RetroUI.Black);

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

            // Draw selection area
            if (selectionArea.HasValue)
            {
                Rectangle selRect = new Rectangle(
                    selectionArea.Value.X * tileSize,
                    selectionArea.Value.Y * tileSize,
                    selectionArea.Value.Width * tileSize,
                    selectionArea.Value.Height * tileSize
                );
                DrawRectangleOutline(spriteBatch, selRect, RetroUI.Yellow, 2);
            }

            // Draw entities
            foreach (var entity in entities)
            {
                DrawEntity(spriteBatch, entity, entity == selectedEntity);
            }

            spriteBatch.End();

            // Draw UI (screen space)
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Top bar
            DrawTopBar(spriteBatch);

            // Side panel
            DrawSidePanel(spriteBatch);

            // Active panels
            DrawActivePanel(spriteBatch);

            // CRT effects
            if (enableCRT)
            {
                if (showScanlines)
                {
                    RetroUI.DrawScanlines(spriteBatch, pixelTexture, screenBounds, 0.2f);
                }
                RetroUI.DrawCRTVignette(spriteBatch, pixelTexture, screenBounds);
            }

            spriteBatch.End();
        }

        private void DrawTopBar(SpriteBatch spriteBatch)
        {
            RetroUI.DrawPanel(spriteBatch, pixelTexture, topBar, inset: false);

            int x = 10;
            int y = 10;

            RetroUI.DrawTextWithShadow(spriteBatch, font, "LEVEL EDITOR v2.0", new Vector2(x, y), RetroUI.Yellow);
            y += 25;

            string modeText = $"Mode: {currentMode} [TAB]  Tool: {currentTool} [1-7]  Layer: {currentLayer}";
            spriteBatch.DrawString(font, modeText, new Vector2(x, y), RetroUI.White);
            y += 20;

            string infoText = $"Zoom: {cameraZoom:F1}x  Tile: {currentTileId}  Entities: {entities.Count}";
            spriteBatch.DrawString(font, infoText, new Vector2(x, y), RetroUI.Gray);

            // Undo/Redo status
            x = screenBounds.Width / 2;
            y = 15;
            string undoText = $"Undo: {history.GetUndoDescription()}";
            string redoText = $"Redo: {history.GetRedoDescription()}";
            spriteBatch.DrawString(font, undoText, new Vector2(x, y), history.CanUndo ? RetroUI.Cyan : RetroUI.DarkGray);
            spriteBatch.DrawString(font, redoText, new Vector2(x, y + 20), history.CanRedo ? RetroUI.Cyan : RetroUI.DarkGray);
        }

        private void DrawSidePanel(SpriteBatch spriteBatch)
        {
            RetroUI.DrawPanel(spriteBatch, pixelTexture, sidePanel, inset: true);

            // Minimap
            if (showMinimap)
            {
                DrawMinimap(spriteBatch);
            }

            // Tile palette
            if (currentMode == EditorMode.Tiles)
            {
                DrawTilePalette(spriteBatch);
            }
            // Entity list
            else if (currentMode == EditorMode.Entities)
            {
                DrawEntityList(spriteBatch);
            }
            // Collision types
            else if (currentMode == EditorMode.Collision)
            {
                DrawCollisionTypes(spriteBatch);
            }
        }

        private void DrawMinimap(SpriteBatch spriteBatch)
        {
            RetroUI.DrawWindow(spriteBatch, pixelTexture, minimapRect, "MINIMAP", font);

            Rectangle contentArea = new Rectangle(
                minimapRect.X + 10,
                minimapRect.Y + 30,
                minimapRect.Width - 20,
                minimapRect.Height - 40
            );

            // Draw level overview
            float scaleX = (float)contentArea.Width / (levelWidth * tileSize);
            float scaleY = (float)contentArea.Height / (levelHeight * tileSize);
            float scale = Math.Min(scaleX, scaleY);

            // Draw tiles (simplified)
            for (int y = 0; y < levelHeight; y += 4)
            {
                for (int x = 0; x < levelWidth; x += 4)
                {
                    var layer = tileMap.Layers[0];
                    if (layer.GetTile(x, y) != 0)
                    {
                        Rectangle tileRect = new Rectangle(
                            contentArea.X + (int)(x * tileSize * scale),
                            contentArea.Y + (int)(y * tileSize * scale),
                            Math.Max(1, (int)(4 * tileSize * scale)),
                            Math.Max(1, (int)(4 * tileSize * scale))
                        );
                        spriteBatch.Draw(pixelTexture, tileRect, RetroUI.Gray);
                    }
                }
            }

            // Draw viewport indicator
            Rectangle viewportRect = new Rectangle(
                contentArea.X + (int)(cameraPosition.X * scale),
                contentArea.Y + (int)(cameraPosition.Y * scale),
                (int)(screenBounds.Width / cameraZoom * scale),
                (int)(screenBounds.Height / cameraZoom * scale)
            );
            DrawRectangleOutline(spriteBatch, viewportRect, RetroUI.Yellow, 1);
        }

        private void DrawTilePalette(SpriteBatch spriteBatch)
        {
            RetroUI.DrawWindow(spriteBatch, pixelTexture, tilePaletteRect, "TILESET", font);

            int contentX = tilePaletteRect.X + 10;
            int contentY = tilePaletteRect.Y + 35;
            int tileDisplaySize = 40;

            for (int i = 0; i < 8; i++)
            {
                int tileIndex = i + tilesetBrowserScroll;
                if (tileIndex >= tilePalette.Length) break;

                int tileId = tilePalette[tileIndex];
                Rectangle tileRect = new Rectangle(contentX, contentY + i * 45, tileDisplaySize, tileDisplaySize);

                bool isSelected = (tileId == currentTileId);
                Color bgColor = isSelected ? RetroUI.SelectedColor : RetroUI.WindowColor;

                RetroUI.DrawPanel(spriteBatch, pixelTexture, tileRect, inset: true);
                spriteBatch.Draw(pixelTexture, tileRect, bgColor);

                // Draw tile ID
                Vector2 textPos = new Vector2(tileRect.Right + 10, tileRect.Y + 10);
                Color textColor = isSelected ? RetroUI.Yellow : RetroUI.White;
                spriteBatch.DrawString(font, $"#{tileId}", textPos, textColor);

                // Draw border for selected
                if (isSelected)
                {
                    DrawRectangleOutline(spriteBatch, tileRect, RetroUI.Yellow, 2);
                }
            }

            // Scroll indicator
            if (tilePalette.Length > 8)
            {
                float scrollPercent = (float)tilesetBrowserScroll / (tilePalette.Length - 8);
                RetroUI.DrawProgressBar(spriteBatch, pixelTexture,
                    new Rectangle(tilePaletteRect.X + 10, tilePaletteRect.Bottom - 25, tilePaletteRect.Width - 20, 15),
                    scrollPercent, RetroUI.Cyan);
            }
        }

        private void DrawEntityList(SpriteBatch spriteBatch)
        {
            RetroUI.DrawWindow(spriteBatch, pixelTexture, tilePaletteRect, "ENTITIES", font);

            int contentX = tilePaletteRect.X + 10;
            int contentY = tilePaletteRect.Y + 35;
            int lineHeight = 22;

            for (int i = 0; i < Math.Min(15, entityTypes.Length); i++)
            {
                string entityType = entityTypes[i];
                bool isSelected = (entityType == currentEntityType);

                Rectangle itemRect = new Rectangle(contentX, contentY + i * lineHeight, tilePaletteRect.Width - 20, lineHeight - 2);

                if (isSelected)
                {
                    spriteBatch.Draw(pixelTexture, itemRect, RetroUI.SelectedColor);
                }

                Color textColor = isSelected ? RetroUI.Yellow : RetroUI.White;
                spriteBatch.DrawString(font, entityType, new Vector2(contentX + 5, contentY + i * lineHeight + 2), textColor);

                // Make clickable
                if (Mouse.GetState().LeftButton == ButtonState.Pressed &&
                    previousMouseState.LeftButton == ButtonState.Released &&
                    itemRect.Contains(Mouse.GetState().Position))
                {
                    currentEntityType = entityType;
                }
            }
        }

        private void DrawCollisionTypes(SpriteBatch spriteBatch)
        {
            RetroUI.DrawWindow(spriteBatch, pixelTexture, tilePaletteRect, "COLLISION", font);

            int contentX = tilePaletteRect.X + 10;
            int contentY = tilePaletteRect.Y + 35;
            int lineHeight = 30;

            var collisionTypes = Enum.GetValues(typeof(TileCollision)).Cast<TileCollision>().ToArray();

            for (int i = 0; i < collisionTypes.Length; i++)
            {
                TileCollision collision = collisionTypes[i];
                bool isSelected = (collision == currentCollision);

                Rectangle colorBox = new Rectangle(contentX, contentY + i * lineHeight, 20, 20);
                spriteBatch.Draw(pixelTexture, colorBox, GetCollisionColor(collision));
                DrawRectangleOutline(spriteBatch, colorBox, isSelected ? RetroUI.Yellow : RetroUI.Cyan, 1);

                Color textColor = isSelected ? RetroUI.Yellow : RetroUI.White;
                Vector2 textPos = new Vector2(colorBox.Right + 10, colorBox.Y + 2);
                spriteBatch.DrawString(font, collision.ToString(), textPos, textColor);

                // Make clickable
                Rectangle clickRect = new Rectangle(contentX, contentY + i * lineHeight, tilePaletteRect.Width - 20, lineHeight);
                if (Mouse.GetState().LeftButton == ButtonState.Pressed &&
                    previousMouseState.LeftButton == ButtonState.Released &&
                    clickRect.Contains(Mouse.GetState().Position))
                {
                    currentCollision = collision;
                }
            }
        }

        private void DrawActivePanel(SpriteBatch spriteBatch)
        {
            if (activePanel == UIPanel.EntityProperties && selectedEntity != null)
            {
                DrawEntityPropertiesPanel(spriteBatch);
            }
            else if (activePanel == UIPanel.LayerVisibility)
            {
                DrawLayerVisibilityPanel(spriteBatch);
            }
            else if (activePanel == UIPanel.PrefabBrowser)
            {
                DrawPrefabBrowserPanel(spriteBatch);
            }
            else if (activePanel == UIPanel.TilesetBrowser)
            {
                DrawTilesetBrowserPanel(spriteBatch);
            }
        }

        private void DrawEntityPropertiesPanel(SpriteBatch spriteBatch)
        {
            Rectangle panelRect = new Rectangle(screenBounds.Width / 2 - 200, screenBounds.Height / 2 - 150, 400, 300);
            RetroUI.DrawWindow(spriteBatch, pixelTexture, panelRect, "ENTITY PROPERTIES", font);

            int x = panelRect.X + 15;
            int y = panelRect.Y + 40;
            int lineHeight = 25;

            spriteBatch.DrawString(font, $"Type: {selectedEntity.Type}", new Vector2(x, y), RetroUI.White);
            y += lineHeight;
            spriteBatch.DrawString(font, $"ID: {selectedEntity.Id}", new Vector2(x, y), RetroUI.White);
            y += lineHeight;
            spriteBatch.DrawString(font, $"Position: ({selectedEntity.Position.X:F0}, {selectedEntity.Position.Y:F0})", new Vector2(x, y), RetroUI.White);
            y += lineHeight * 2;

            spriteBatch.DrawString(font, "Properties:", new Vector2(x, y), RetroUI.Cyan);
            y += lineHeight;

            // Show/edit properties
            if (selectedEntity.Properties != null)
            {
                foreach (var kvp in selectedEntity.Properties.ToList())
                {
                    spriteBatch.DrawString(font, $"{kvp.Key}: {kvp.Value}", new Vector2(x, y), RetroUI.Gray);
                    y += lineHeight;
                }
            }

            // Close button
            Rectangle closeButton = new Rectangle(panelRect.Right - 80, panelRect.Bottom - 40, 60, 25);
            bool isHovered = closeButton.Contains(Mouse.GetState().Position);
            bool isPressed = isHovered && Mouse.GetState().LeftButton == ButtonState.Pressed;
            RetroUI.DrawButton(spriteBatch, pixelTexture, closeButton, "CLOSE", font, isPressed, isHovered);

            if (isHovered && Mouse.GetState().LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                activePanel = UIPanel.None;
            }
        }

        private void DrawLayerVisibilityPanel(SpriteBatch spriteBatch)
        {
            Rectangle panelRect = new Rectangle(screenBounds.Width / 2 - 150, screenBounds.Height / 2 - 100, 300, 200);
            RetroUI.DrawWindow(spriteBatch, pixelTexture, panelRect, "LAYER VISIBILITY", font);

            int x = panelRect.X + 15;
            int y = panelRect.Y + 40;
            int lineHeight = 30;

            for (int i = 0; i < tileMap.Layers.Count; i++)
            {
                var layer = tileMap.Layers[i];
                Vector2 checkPos = new Vector2(x, y + i * lineHeight);
                string label = $"Layer {i} (Depth: {layer.Depth:F1})";
                bool isHovered = new Rectangle((int)checkPos.X, (int)checkPos.Y, 200, 16).Contains(Mouse.GetState().Position);

                RetroUI.DrawCheckbox(spriteBatch, pixelTexture, checkPos, layer.IsVisible, label, font, isHovered);

                // Toggle on click
                if (isHovered && Mouse.GetState().LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                {
                    layer.IsVisible = !layer.IsVisible;
                }
            }
        }

        private void DrawPrefabBrowserPanel(SpriteBatch spriteBatch)
        {
            Rectangle panelRect = new Rectangle(50, 100, 300, 400);
            RetroUI.DrawWindow(spriteBatch, pixelTexture, panelRect, "PREFAB BROWSER", font);

            int x = panelRect.X + 15;
            int y = panelRect.Y + 40;
            int lineHeight = 35;

            for (int i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                Rectangle itemRect = new Rectangle(x, y + i * lineHeight, panelRect.Width - 30, lineHeight - 5);

                bool isHovered = itemRect.Contains(Mouse.GetState().Position);
                bool isSelected = prefab == selectedPrefab;

                if (isSelected)
                {
                    spriteBatch.Draw(pixelTexture, itemRect, RetroUI.SelectedColor);
                }
                else if (isHovered)
                {
                    spriteBatch.Draw(pixelTexture, itemRect, RetroUI.DarkBlue);
                }

                Color textColor = isSelected ? RetroUI.Yellow : (isHovered ? RetroUI.Cyan : RetroUI.White);
                spriteBatch.DrawString(font, prefab.Name, new Vector2(x + 5, y + i * lineHeight + 5), textColor);
                spriteBatch.DrawString(font, $"({prefab.Entities.Count} entities)",
                    new Vector2(x + 5, y + i * lineHeight + 18), RetroUI.Gray);

                // Click to select/place
                if (isHovered && Mouse.GetState().LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                {
                    selectedPrefab = prefab;
                    // Place prefab at cursor (simplified)
                    Console.WriteLine($"Placing prefab: {prefab.Name}");
                }
            }
        }

        private void DrawTilesetBrowserPanel(SpriteBatch spriteBatch)
        {
            Rectangle panelRect = new Rectangle(100, 100, 500, 400);
            RetroUI.DrawWindow(spriteBatch, pixelTexture, panelRect, "TILESET BROWSER", font);

            int contentX = panelRect.X + 20;
            int contentY = panelRect.Y + 40;
            int tileSize = 48;
            int tilesPerRow = 8;

            for (int i = 0; i < Math.Min(40, tilePalette.Length); i++)
            {
                int tileId = tilePalette[i];
                int row = i / tilesPerRow;
                int col = i % tilesPerRow;

                Rectangle tileRect = new Rectangle(
                    contentX + col * (tileSize + 5),
                    contentY + row * (tileSize + 5),
                    tileSize,
                    tileSize
                );

                bool isSelected = (tileId == currentTileId);
                bool isHovered = tileRect.Contains(Mouse.GetState().Position);

                RetroUI.DrawPanel(spriteBatch, pixelTexture, tileRect, inset: true);

                if (isSelected)
                {
                    DrawRectangleOutline(spriteBatch, tileRect, RetroUI.Yellow, 2);
                }
                else if (isHovered)
                {
                    DrawRectangleOutline(spriteBatch, tileRect, RetroUI.Cyan, 1);
                }

                // Tile ID label
                Vector2 labelPos = new Vector2(tileRect.X + 2, tileRect.Y + 2);
                spriteBatch.DrawString(font, tileId.ToString(), labelPos, RetroUI.White);

                // Click to select
                if (isHovered && Mouse.GetState().LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
                {
                    currentTileId = tileId;
                }
            }
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
                        spriteBatch.Draw(pixelTexture, rect, color * 0.5f);
                    }
                }
            }
        }

        private Color GetCollisionColor(TileCollision collision)
        {
            return collision switch
            {
                TileCollision.Solid => RetroUI.Red,
                TileCollision.Platform => RetroUI.Yellow,
                TileCollision.Ladder => RetroUI.Green,
                TileCollision.Deadly => RetroUI.Magenta,
                TileCollision.Water => RetroUI.Blue,
                TileCollision.Ice => RetroUI.Cyan,
                TileCollision.Slope => new Color(255, 165, 0),
                _ => RetroUI.White
            };
        }

        private void DrawEntity(SpriteBatch spriteBatch, EntityData entity, bool isSelected)
        {
            Color color = entity.Type.Contains("Enemy") ? RetroUI.Red :
                         entity.Type.Contains("PowerUp") ? RetroUI.Yellow :
                         entity.Type == "PlayerSpawn" ? RetroUI.Green :
                         RetroUI.Cyan;

            // Animated pulsing for selected
            if (isSelected)
            {
                float pulse = (float)Math.Sin(animationTime * 4) * 0.3f + 0.7f;
                color = Color.Lerp(color, RetroUI.Yellow, pulse);
            }

            DrawCircle(spriteBatch, entity.Position, 8, color, filled: true);
            DrawCircle(spriteBatch, entity.Position, 8, RetroUI.White, filled: false, thickness: 1);

            // Draw type label if zoomed in
            if (cameraZoom > 1.5f && font != null)
            {
                string label = entity.Type.Length > 10 ? entity.Type.Substring(0, 10) : entity.Type;
                Vector2 labelSize = font.MeasureString(label);
                Vector2 labelPos = entity.Position - new Vector2(labelSize.X / 2, 20);
                spriteBatch.DrawString(font, label, labelPos, RetroUI.White);
            }
        }

        private Matrix GetCameraMatrix()
        {
            return Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0) *
                   Matrix.CreateScale(cameraZoom);
        }

        private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
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
