using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlatformerEngine.Core.Data;
using PlatformerEngine.Core.Entities;
using PlatformerEngine.Core.Graphics;
using PlatformerEngine.Core.Input;
using System;

namespace PlatformerEngine.Example
{
    /// <summary>
    /// Complete example game demonstrating all engine features
    /// </summary>
    public class ExampleGame : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Core systems
        private Camera camera;
        private TileMap level;
        private LevelData levelData;
        private InputManager input;

        // Entities
        private Player player;

        // Debug
        private bool showDebug = false;
        private Texture2D pixelTexture;

        // Display settings
        private const int GAME_WIDTH = 320;
        private const int GAME_HEIGHT = 200;
        private const int SCALE = 3; // 960x600 window

        public ExampleGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set window size (VGA resolution scaled up)
            graphics.PreferredBackBufferWidth = GAME_WIDTH * SCALE;
            graphics.PreferredBackBufferHeight = GAME_HEIGHT * SCALE;
            graphics.ApplyChanges();

            Window.Title = "Platformer Engine Example";
        }

        protected override void Initialize()
        {
            // Initialize input manager
            input = InputManager.Instance;

            // Initialize camera
            camera = new Camera(GraphicsDevice.Viewport)
            {
                SmoothFollow = true,
                FollowSpeed = 5.0f,
                ClampToBounds = true
            };

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create 1x1 white pixel texture for debug drawing
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Load or create level
            LoadLevel();

            // Create player
            CreatePlayer();
        }

        /// <summary>
        /// Load level from JSON or create a test level
        /// </summary>
        private void LoadLevel()
        {
            // Try to load from file, otherwise create test level
            try
            {
                levelData = LevelData.Load("Content/Levels/level1.json");
            }
            catch
            {
                // Create a test level programmatically
                levelData = CreateTestLevel();
            }

            // Create tilemap
            level = new TileMap(levelData.Width, levelData.Height, levelData.TileSize);

            // Load tileset (you'll need to create this texture)
            // For now, we'll use a colored pixel as placeholder
            Texture2D tileset = CreatePlaceholderTileset();
            level.LoadTileset(tileset);

            // Add parallax layers
            if (levelData.ParallaxLayers != null)
            {
                foreach (var parallaxData in levelData.ParallaxLayers)
                {
                    try
                    {
                        // Try to load parallax texture
                        // Texture2D texture = Content.Load<Texture2D>(parallaxData.TexturePath);
                        // level.AddParallaxLayer(texture, parallaxData.ScrollSpeedX, parallaxData.ScrollSpeedY, parallaxData.RepeatX);
                    }
                    catch
                    {
                        // Skip if texture not found
                    }
                }
            }

            // Add tile layers
            foreach (var layerData in levelData.Layers)
            {
                TileLayer layer = level.AddLayer(layerData.Name, layerData.Depth);
                layer.Visible = layerData.Visible;
                layer.Opacity = layerData.Opacity;

                // Copy tile data
                if (layerData.Data != null)
                {
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

            // Setup collision from collision layer
            var collisionLayer = levelData.GetLayer(levelData.CollisionLayerName);
            if (collisionLayer != null && collisionLayer.Data != null)
            {
                for (int y = 0; y < levelData.Height; y++)
                {
                    for (int x = 0; x < levelData.Width; x++)
                    {
                        int collisionValue = collisionLayer.Data[y * levelData.Width + x];
                        if (collisionValue == 1)
                        {
                            level.SetCollision(x, y, TileCollision.Solid);
                        }
                        else if (collisionValue == 2)
                        {
                            level.SetCollision(x, y, TileCollision.Platform);
                        }
                    }
                }
            }

            // Set camera bounds to level size
            camera.SetBounds(level.PixelWidth, level.PixelHeight);
        }

        /// <summary>
        /// Create placeholder tileset for testing
        /// </summary>
        private Texture2D CreatePlaceholderTileset()
        {
            int tileSize = 16;
            int tilesPerRow = 16;
            int tileRows = 16;

            Texture2D tileset = new Texture2D(GraphicsDevice, tilesPerRow * tileSize, tileRows * tileSize);
            Color[] data = new Color[tileset.Width * tileset.Height];

            // Create colored tiles
            for (int tileY = 0; tileY < tileRows; tileY++)
            {
                for (int tileX = 0; tileX < tilesPerRow; tileX++)
                {
                    int tileIndex = tileY * tilesPerRow + tileX;
                    Color tileColor = GetTileColor(tileIndex);

                    // Fill tile with color
                    for (int py = 0; py < tileSize; py++)
                    {
                        for (int px = 0; px < tileSize; px++)
                        {
                            int x = tileX * tileSize + px;
                            int y = tileY * tileSize + py;
                            data[y * tileset.Width + x] = tileColor;
                        }
                    }
                }
            }

            tileset.SetData(data);
            return tileset;
        }

        private Color GetTileColor(int index)
        {
            return index switch
            {
                0 => Color.Transparent,
                1 => new Color(139, 69, 19),    // Brown (ground)
                2 => new Color(34, 139, 34),    // Green (grass)
                3 => new Color(128, 128, 128),  // Gray (stone)
                4 => new Color(255, 215, 0),    // Gold (coin/item)
                _ => new Color(100 + index * 10, 50, 150) // Various colors
            };
        }

        /// <summary>
        /// Create test level programmatically
        /// </summary>
        private LevelData CreateTestLevel()
        {
            var testLevel = new LevelData
            {
                Name = "Test Level",
                Width = 100,
                Height = 20,
                TileSize = 16,
                BackgroundColor = "#87CEEB" // Sky blue
            };

            // Main layer
            var mainLayer = new TileLayerData
            {
                Name = "Main",
                Depth = 0.5f,
                Data = new int[testLevel.Width * testLevel.Height]
            };

            // Create ground
            for (int x = 0; x < testLevel.Width; x++)
            {
                // Ground tiles
                for (int y = 15; y < testLevel.Height; y++)
                {
                    mainLayer.Data[y * testLevel.Width + x] = 1;
                }

                // Grass on top of ground
                mainLayer.Data[14 * testLevel.Width + x] = 2;
            }

            // Create some platforms
            for (int x = 10; x < 15; x++)
            {
                mainLayer.Data[12 * testLevel.Width + x] = 3;
            }

            for (int x = 20; x < 27; x++)
            {
                mainLayer.Data[10 * testLevel.Width + x] = 3;
            }

            for (int x = 35; x < 42; x++)
            {
                mainLayer.Data[8 * testLevel.Width + x] = 3;
            }

            // Add some obstacles
            for (int y = 13; y < 15; y++)
            {
                mainLayer.Data[y * testLevel.Width + 30] = 3;
                mainLayer.Data[y * testLevel.Width + 50] = 3;
            }

            testLevel.Layers.Add(mainLayer);

            // Collision layer (1 = solid, 2 = platform)
            var collisionLayer = new TileLayerData
            {
                Name = "Collision",
                Visible = false,
                Data = new int[testLevel.Width * testLevel.Height]
            };

            // Mark ground as solid
            for (int x = 0; x < testLevel.Width; x++)
            {
                for (int y = 14; y < testLevel.Height; y++)
                {
                    collisionLayer.Data[y * testLevel.Width + x] = 1;
                }
            }

            // Mark platforms as platform collision
            for (int x = 10; x < 15; x++)
                collisionLayer.Data[12 * testLevel.Width + x] = 2;
            for (int x = 20; x < 27; x++)
                collisionLayer.Data[10 * testLevel.Width + x] = 2;
            for (int x = 35; x < 42; x++)
                collisionLayer.Data[8 * testLevel.Width + x] = 2;

            // Mark obstacles as solid
            for (int y = 13; y < 15; y++)
            {
                collisionLayer.Data[y * testLevel.Width + 30] = 1;
                collisionLayer.Data[y * testLevel.Width + 50] = 1;
            }

            testLevel.Layers.Add(collisionLayer);

            // Add player spawn
            testLevel.Entities.Add(new EntityData
            {
                Type = "PlayerSpawn",
                X = 32,
                Y = 200
            });

            return testLevel;
        }

        /// <summary>
        /// Create player
        /// </summary>
        private void CreatePlayer()
        {
            player = new Player();

            // Create simple player sprite (colored rectangle)
            player.Sprite = CreatePlayerSprite();

            // Find player spawn point
            var spawnData = levelData.GetEntity("PlayerSpawn");
            Vector2 spawnPos = spawnData != null ? spawnData.Position : new Vector2(32, 200);

            player.Respawn(spawnPos);

            // Focus camera on player
            camera.FocusOn(player.Position);
        }

        private Texture2D CreatePlayerSprite()
        {
            Texture2D sprite = new Texture2D(GraphicsDevice, 16, 24);
            Color[] data = new Color[16 * 24];

            // Simple player sprite (blue rectangle with white center)
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x >= 4 && x < 12 && y >= 8 && y < 16)
                        data[y * 16 + x] = Color.White;
                    else if (x >= 2 && x < 14 && y >= 2 && y < 22)
                        data[y * 16 + x] = Color.Blue;
                    else
                        data[y * 16 + x] = Color.Transparent;
                }
            }

            sprite.SetData(data);
            return sprite;
        }

        protected override void Update(GameTime gameTime)
        {
            // Update input first
            input.Update();

            // Debug toggle
            if (input.IsKeyPressed(Keys.F3))
                showDebug = !showDebug;

            // Exit
            if (input.IsActionPressed(InputAction.Pause))
                Exit();

            // Update player
            player.Update(gameTime, level);

            // Update camera to follow player
            camera.Follow(player.Center);
            camera.Update(gameTime);

            // Update parallax layers
            level.Update(camera.Position);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(levelData.BackgroundColorValue);

            // Draw game world
            spriteBatch.Begin(
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,  // Point clamp for pixel-perfect rendering
                null,
                null,
                null,
                camera.GetTransformMatrix()
            );

            // Draw level (includes parallax and tile layers)
            level.Draw(spriteBatch, camera.GetViewRectangle());

            // Draw player
            player.Draw(spriteBatch);

            // Draw debug info
            if (showDebug)
            {
                level.DrawCollisionDebug(spriteBatch, camera.GetViewRectangle(), pixelTexture);
                player.DrawDebug(spriteBatch, pixelTexture);
            }

            spriteBatch.End();

            // Draw UI (no camera transform)
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            if (showDebug)
            {
                DrawDebugText(spriteBatch);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawDebugText(SpriteBatch spriteBatch)
        {
            // You would need a SpriteFont loaded to display text
            // For now, this is a placeholder showing what info you'd display:
            /*
            string debugText = $"FPS: {1.0f / gameTime.ElapsedGameTime.TotalSeconds:F0}\n" +
                             $"Player Pos: ({player.Position.X:F1}, {player.Position.Y:F1})\n" +
                             $"Player Vel: ({player.Velocity.X:F1}, {player.Velocity.Y:F1})\n" +
                             $"On Ground: {player.IsOnGround}\n" +
                             $"Camera Pos: ({camera.Position.X:F1}, {camera.Position.Y:F1})\n" +
                             $"F3: Toggle Debug";
            spriteBatch.DrawString(font, debugText, new Vector2(10, 10), Color.White);
            */
        }
    }
}
