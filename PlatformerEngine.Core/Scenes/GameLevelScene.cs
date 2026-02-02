using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Audio;
using PlatformerEngine.Core.Combat;
using PlatformerEngine.Core.Entities;
using PlatformerEngine.Core.Events;
using PlatformerEngine.Core.Graphics;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Persistence;
using PlatformerEngine.Core.Replay;
using PlatformerEngine.Core.Screens;
using PlatformerEngine.Core.UI;
using System.Collections.Generic;
using System.Linq;

namespace PlatformerEngine.Core.Scenes
{
    /// <summary>
    /// Main gameplay scene - GameLoop equivalent from Keen
    /// </summary>
    public class GameLevelScene : Scene
    {
        // Core systems
        private TileMap tileMap;
        private Camera camera;
        private Player player;
        private HUD hud;

        // Entity lists
        private List<Entity> entities;
        private List<Enemy> enemies;
        private List<Collectible> collectibles;
        private InteractiveObjectManager interactiveObjects;

        // Managers
        private PowerUpManager powerUpManager;
        private ReplaySystem replaySystem;
        private EventManager eventManager;

        // Game state
        private string levelName;
        private Vector2 spawnPoint;
        private bool isPaused;
        private bool isRecordingDemo;
        private LevelStats currentStats;

        // Resources
        private SpriteFont font;
        private Texture2D playerTexture;
        private Texture2D tilesetTexture;

        public GameLevelScene(string levelName, SpriteFont font = null)
        {
            this.levelName = levelName;
            this.font = font;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Initialize systems
            entities = new List<Entity>();
            enemies = new List<Enemy>();
            collectibles = new List<Collectible>();
            interactiveObjects = new InteractiveObjectManager();
            powerUpManager = PowerUpManager.Instance;
            replaySystem = ReplaySystem.Instance;
            eventManager = EventManager.Instance;

            // Initialize stats
            currentStats = new LevelStats
            {
                LevelName = levelName
            };

            // Load level
            LoadLevel();

            // Create player
            CreatePlayer();

            // Create camera
            camera = new Camera
            {
                FollowSpeed = 5f,
                DeadzoneWidth = 100f,
                DeadzoneHeight = 80f
            };
            camera.Follow(player.Position);

            // Create HUD
            hud = new HUD(font)
            {
                Player = player
            };

            // Start background music
            AudioManager.Instance.PlayMusic("gameplay_music", loop: true, fadeInDuration: 1.0f);

            // Start demo recording if needed
            if (isRecordingDemo)
            {
                replaySystem.StartRecording(levelName);
            }
        }

        private void LoadLevel()
        {
            // Try to load level data from file
            try
            {
                var levelData = LevelData.Load($"Levels/{levelName}.json");

                // Create tilemap from data
                tileMap = new TileMap(levelData.TileWidth, levelData.TileHeight, levelData.TileSize);

                // Load layers
                foreach (var layerData in levelData.Layers)
                {
                    var layer = new TileLayer(layerData.Width, layerData.Height)
                    {
                        IsVisible = layerData.IsVisible,
                        Depth = layerData.Depth
                    };

                    // Decompress tile data
                    int[] tiles = LevelData.DecompressLayer(layerData.CompressedTiles, layerData.Width * layerData.Height);
                    for (int i = 0; i < tiles.Length; i++)
                    {
                        int x = i % layerData.Width;
                        int y = i / layerData.Width;
                        layer.SetTile(x, y, tiles[i]);
                    }

                    tileMap.Layers.Add(layer);
                }

                // Load parallax layers
                foreach (var parallaxData in levelData.ParallaxLayers)
                {
                    var parallax = new ParallaxLayer(parallaxData.TextureName, parallaxData.ScrollSpeed)
                    {
                        Position = parallaxData.Position,
                        IsVisible = parallaxData.IsVisible,
                        Depth = parallaxData.Depth
                    };
                    tileMap.ParallaxLayers.Add(parallax);
                }

                // Load entities
                SpawnEntities(levelData.Entities);

                // Set collision map
                if (levelData.CollisionLayer != null && levelData.CollisionLayer.Length > 0)
                {
                    tileMap.SetCollisionMap(levelData.CollisionLayer);
                }
            }
            catch
            {
                // Create default level if file doesn't exist
                CreateDefaultLevel();
            }
        }

        private void CreateDefaultLevel()
        {
            // Create a simple test level
            tileMap = new TileMap(64, 32, 16);

            var layer = new TileLayer(64, 32);

            // Create ground
            for (int x = 0; x < 64; x++)
            {
                layer.SetTile(x, 30, 1); // Ground tile
                layer.SetTile(x, 31, 1);
                tileMap.SetTileCollision(x, 30, TileCollision.Solid);
                tileMap.SetTileCollision(x, 31, TileCollision.Solid);
            }

            // Create some platforms
            for (int x = 10; x < 15; x++)
            {
                layer.SetTile(x, 25, 2);
                tileMap.SetTileCollision(x, 25, TileCollision.Platform);
            }

            for (int x = 20; x < 28; x++)
            {
                layer.SetTile(x, 20, 2);
                tileMap.SetTileCollision(x, 20, TileCollision.Platform);
            }

            tileMap.Layers.Add(layer);

            // Set spawn point
            spawnPoint = new Vector2(100, 400);

            // Spawn some test entities
            SpawnTestEntities();
        }

        private void SpawnEntities(List<EntityData> entityDataList)
        {
            foreach (var entityData in entityDataList)
            {
                switch (entityData.Type)
                {
                    case "PlayerSpawn":
                        spawnPoint = entityData.Position;
                        break;

                    case "Coin":
                        var coin = new Coin { Position = entityData.Position };
                        collectibles.Add(coin);
                        entities.Add(coin);
                        break;

                    case "HealthPickup":
                        var health = new HealthPickup { Position = entityData.Position };
                        collectibles.Add(health);
                        entities.Add(health);
                        break;

                    case "WalkerEnemy":
                        var walker = new WalkerEnemy { Position = entityData.Position };
                        enemies.Add(walker);
                        entities.Add(walker);
                        break;

                    case "FlyerEnemy":
                        var flyer = new FlyerEnemy { Position = entityData.Position };
                        enemies.Add(flyer);
                        entities.Add(flyer);
                        break;

                    case "PowerUp_JumpBoost":
                        var jumpBoost = PowerUpFactory.CreateJumpBoost();
                        jumpBoost.Position = entityData.Position;
                        collectibles.Add(jumpBoost);
                        entities.Add(jumpBoost);
                        break;

                    case "Checkpoint":
                        var checkpoint = new Checkpoint(entityData.Id) { Position = entityData.Position };
                        interactiveObjects.AddCheckpoint(checkpoint);
                        entities.Add(checkpoint);
                        break;
                }
            }
        }

        private void SpawnTestEntities()
        {
            // Spawn some coins
            for (int i = 0; i < 5; i++)
            {
                var coin = new Coin
                {
                    Position = new Vector2(200 + i * 50, 400),
                    Value = 10
                };
                collectibles.Add(coin);
                entities.Add(coin);
            }

            // Spawn a health pickup
            var health = new HealthPickup
            {
                Position = new Vector2(300, 350),
                HealAmount = 25
            };
            collectibles.Add(health);
            entities.Add(health);

            // Spawn a walker enemy
            var walker = new WalkerEnemy
            {
                Position = new Vector2(400, 400)
            };
            enemies.Add(walker);
            entities.Add(walker);

            // Spawn a jump boost power-up
            var jumpBoost = PowerUpFactory.CreateJumpBoost(10f, 1.5f);
            jumpBoost.Position = new Vector2(500, 400);
            collectibles.Add(jumpBoost);
            entities.Add(jumpBoost);
        }

        private void CreatePlayer()
        {
            player = new Player
            {
                Position = spawnPoint
            };
            entities.Add(player);

            // Subscribe to player events
            var playerInventory = new Inventory();
            foreach (var collectible in collectibles)
            {
                collectible.OnCollected += (c, p) =>
                {
                    currentStats.CoinsCollected++;
                    player.Score += c.Value;
                };
            }
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;

            // Handle pause
            if (input.IsActionPressed(InputAction.Pause))
            {
                isPaused = !isPaused;
                if (isPaused)
                {
                    SceneManager.Instance.PushScene("PauseMenu");
                    return;
                }
            }

            if (isPaused)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update replay system
            if (isRecordingDemo)
            {
                replaySystem.RecordFrame(gameTime);
            }

            // Update power-ups
            powerUpManager.Update(deltaTime, player);
            powerUpManager.UpdateMagnetEffects(player, collectibles, deltaTime);

            // Update all entities
            foreach (var entity in entities.ToList())
            {
                entity.Update(gameTime, tileMap);

                // Remove dead entities
                if (!entity.IsActive)
                {
                    entities.Remove(entity);
                    enemies.Remove(entity as Enemy);
                    collectibles.Remove(entity as Collectible);
                }
            }

            // Update interactive objects
            interactiveObjects.Update(gameTime, entities);

            // Check collectible pickups
            foreach (var collectible in collectibles.ToList())
            {
                if (collectible.CheckCollection(player))
                {
                    // Collectible handles its own OnCollected event
                }
            }

            // Enemy AI - set player as target
            foreach (var enemy in enemies)
            {
                enemy.SetTarget(player);
            }

            // Check enemy collisions with player
            foreach (var enemy in enemies)
            {
                if (enemy.Overlaps(player) && enemy.Attack != null && enemy.Attack.CanAttack())
                {
                    player.Health?.TakeDamage(enemy.Attack.Damage, DamageType.Physical, enemy);
                    enemy.Attack.UpdateCooldown(0); // Reset cooldown
                }
            }

            // Check if player can attack
            if (input.IsActionPressed(InputAction.Shoot))
            {
                player.PerformAttack();
                // Create projectile or melee attack hitbox
            }

            // Update camera to follow player
            camera.Follow(player.Position);
            camera.Update(gameTime);

            // Update HUD
            hud.Update(gameTime);

            // Update stats
            currentStats.CompletionTime = gameTime.TotalGameTime.TotalSeconds;

            // Check win/lose conditions
            if (!player.IsActive)
            {
                if (player.Lives > 0)
                {
                    // Respawn player
                    var checkpoint = interactiveObjects.GetLastActivatedCheckpoint();
                    if (checkpoint != null)
                    {
                        player.Respawn(checkpoint.RespawnPosition);
                    }
                    else
                    {
                        player.Respawn(spawnPoint);
                    }
                }
                else
                {
                    // Game over
                    OnGameOver();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Get camera transform
            Matrix cameraTransform = camera.GetTransformMatrix();

            // Draw world (with camera)
            spriteBatch.Begin(transformMatrix: cameraTransform, samplerState: SamplerState.PointClamp);

            // Draw tilemap
            Rectangle viewport = camera.GetViewport();
            tileMap?.Draw(spriteBatch, viewport);

            // Draw entities
            foreach (var entity in entities.OrderBy(e => e.Position.Y))
            {
                entity.Draw(spriteBatch);
            }

            spriteBatch.End();

            // Draw HUD (no camera transform)
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            hud.Draw(spriteBatch);
            spriteBatch.End();

            // Draw pause overlay if paused
            if (isPaused)
            {
                spriteBatch.Begin();
                if (font != null)
                {
                    string pauseText = "PAUSED";
                    Vector2 textSize = font.MeasureString(pauseText);
                    Vector2 position = new Vector2(320 - textSize.X / 2, 240 - textSize.Y / 2);
                    spriteBatch.DrawString(font, pauseText, position, Color.Yellow);
                }
                spriteBatch.End();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            // Stop demo recording
            if (isRecordingDemo)
            {
                replaySystem.StopRecording();
                replaySystem.SaveReplay($"Demos/{levelName}_demo.json");
            }

            // Stop music
            AudioManager.Instance.StopMusic(1.0f);

            // Clean up
            entities.Clear();
            enemies.Clear();
            collectibles.Clear();
        }

        private void OnLevelComplete()
        {
            // Calculate final stats
            currentStats.SecretsFound = 0; // Count secrets found
            currentStats.TotalSecrets = 0; // Total secrets in level

            // Stop recording
            if (isRecordingDemo)
            {
                replaySystem.StopRecording();
            }

            // Check for high score
            var highScoreTable = HighScoreTable.Load("highscores.json");
            if (highScoreTable.IsHighScore(player.Score))
            {
                // Go to high score entry
                SceneManager.Instance.ChangeScene("HighScoreEntry", new FadeTransition(0.5f));
            }
            else
            {
                // Show level complete screen
                var levelCompleteScene = new LevelCompleteScreen(currentStats, font);
                SceneManager.Instance.ChangeScene("LevelComplete", new FadeTransition(0.5f));
            }
        }

        private void OnGameOver()
        {
            // Game over - return to main menu
            AudioManager.Instance.PlaySound("game_over");
            SceneManager.Instance.ChangeScene("GameOver", new FadeTransition(1.0f));
        }

        public void StartDemoRecording()
        {
            isRecordingDemo = true;
        }
    }

    /// <summary>
    /// Pause menu scene
    /// </summary>
    public class PauseMenuScene : Scene
    {
        private SpriteFont font;
        private int selectedOption;
        private string[] options = { "Resume", "Restart Level", "Main Menu" };

        public PauseMenuScene(SpriteFont font = null)
        {
            this.font = font;
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;

            // Navigate
            if (input.IsActionPressed(InputAction.MoveUp))
            {
                selectedOption = (selectedOption - 1 + options.Length) % options.Length;
            }
            else if (input.IsActionPressed(InputAction.MoveDown))
            {
                selectedOption = (selectedOption + 1) % options.Length;
            }

            // Select
            if (input.IsActionPressed(InputAction.Jump) || input.IsActionPressed(InputAction.Interact))
            {
                HandleSelection();
            }

            // Resume with pause button
            if (input.IsActionPressed(InputAction.Pause))
            {
                SceneManager.Instance.PopScene();
            }
        }

        private void HandleSelection()
        {
            switch (selectedOption)
            {
                case 0: // Resume
                    SceneManager.Instance.PopScene();
                    break;
                case 1: // Restart
                    SceneManager.Instance.PopScene();
                    SceneManager.Instance.ChangeScene("GameLevel", new FadeTransition(0.3f));
                    break;
                case 2: // Main Menu
                    SceneManager.Instance.PopScene();
                    SceneManager.Instance.ChangeScene("MainMenu", new FadeTransition(0.5f));
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (font == null) return;

            spriteBatch.Begin();

            // Semi-transparent overlay
            // (Would need a 1x1 white texture to draw this properly)

            // Menu
            float yPos = 200;
            for (int i = 0; i < options.Length; i++)
            {
                Color color = (i == selectedOption) ? Color.Yellow : Color.White;
                string text = (i == selectedOption ? "> " : "  ") + options[i];
                Vector2 textSize = font.MeasureString(text);
                Vector2 position = new Vector2(320 - textSize.X / 2, yPos);
                spriteBatch.DrawString(font, text, position, color);
                yPos += 40;
            }

            spriteBatch.End();
        }
    }

    /// <summary>
    /// Game over scene
    /// </summary>
    public class GameOverScene : Scene
    {
        private SpriteFont font;
        private float displayTime;

        public GameOverScene(SpriteFont font = null)
        {
            this.font = font;
        }

        public override void Update(GameTime gameTime)
        {
            displayTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (displayTime > 3f || InputManager.Instance.IsActionPressed(InputAction.Jump))
            {
                SceneManager.Instance.ChangeScene("MainMenu", new FadeTransition(0.5f));
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (font == null) return;

            spriteBatch.Begin();

            string text = "GAME OVER";
            Vector2 textSize = font.MeasureString(text);
            Vector2 position = new Vector2(320 - textSize.X / 2, 240 - textSize.Y / 2);

            // Blinking effect
            Color color = ((int)(displayTime * 2) % 2 == 0) ? Color.Red : Color.White;
            spriteBatch.DrawString(font, text, position, color);

            spriteBatch.End();
        }
    }
}
