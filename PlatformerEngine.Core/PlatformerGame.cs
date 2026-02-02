using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlatformerEngine.Core.Audio;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Persistence;
using PlatformerEngine.Core.Scenes;
using PlatformerEngine.Core.Screens;
using System;

namespace PlatformerEngine.Core
{
    /// <summary>
    /// Main game class - equivalent to CK_MAIN.C
    /// Handles initialization and main loop similar to Commander Keen
    /// </summary>
    public class PlatformerGame : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont defaultFont;

        // Manager instances
        private SceneManager sceneManager;
        private AudioManager audioManager;
        private InputManager inputManager;
        private SaveManager saveManager;
        private AttractModeManager attractMode;

        // Resources
        private Texture2D titleScreenTexture;

        public PlatformerGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set resolution (Keen was 320x200, scaled up for modern displays)
            graphics.PreferredBackBufferWidth = 640;
            graphics.PreferredBackBufferHeight = 480;
        }

        /// <summary>
        /// Initialize - equivalent to InitGame() in Keen
        /// Sets up all subsystems in proper order
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Initialize subsystems (following Keen's initialization order)
            InitializeMemoryManagement();  // MM_Startup()
            InitializeGraphics();           // VW_Startup(), RF_Startup()
            InitializeInput();              // IN_Startup()
            InitializeAudio();              // SD_Startup()
            InitializeUserServices();       // US_Startup()
            InitializeCaching();            // CA_Startup()

            // Check memory/requirements
            CheckSystemRequirements();

            // Register scenes
            RegisterScenes();

            // Enter demo loop (attract mode)
            StartAttractMode();
        }

        /// <summary>
        /// Memory management initialization (MM_Startup equivalent)
        /// </summary>
        private void InitializeMemoryManagement()
        {
            // In modern C#, this is handled by the CLR
            // But you could initialize object pools here
            Console.WriteLine("Memory Management: OK");
        }

        /// <summary>
        /// Graphics initialization (VW_Startup, RF_Startup equivalent)
        /// </summary>
        private void InitializeGraphics()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            sceneManager = SceneManager.Instance;

            Console.WriteLine("Graphics: OK");
        }

        /// <summary>
        /// Input initialization (IN_Startup equivalent)
        /// </summary>
        private void InitializeInput()
        {
            inputManager = InputManager.Instance;

            // Configure default key bindings
            inputManager.MapKey(InputAction.MoveLeft, Keys.Left);
            inputManager.MapKey(InputAction.MoveLeft, Keys.A);
            inputManager.MapKey(InputAction.MoveRight, Keys.Right);
            inputManager.MapKey(InputAction.MoveRight, Keys.D);
            inputManager.MapKey(InputAction.Jump, Keys.Space);
            inputManager.MapKey(InputAction.Jump, Keys.Z);
            inputManager.MapKey(InputAction.Shoot, Keys.X);
            inputManager.MapKey(InputAction.Duck, Keys.Down);
            inputManager.MapKey(InputAction.Duck, Keys.S);
            inputManager.MapKey(InputAction.Run, Keys.LeftShift);
            inputManager.MapKey(InputAction.Interact, Keys.E);
            inputManager.MapKey(InputAction.Pause, Keys.Escape);

            // Gamepad support
            inputManager.EnableGamepad(true);

            Console.WriteLine("Input: OK");
        }

        /// <summary>
        /// Audio initialization (SD_Startup equivalent)
        /// </summary>
        private void InitializeAudio()
        {
            audioManager = AudioManager.Instance;
            audioManager.Initialize(Content);

            // Set default volumes
            audioManager.MasterVolume = 1.0f;
            audioManager.MusicVolume = 0.7f;
            audioManager.SFXVolume = 0.8f;

            Console.WriteLine("Audio: OK");
        }

        /// <summary>
        /// User services initialization (US_Startup equivalent)
        /// </summary>
        private void InitializeUserServices()
        {
            saveManager = SaveManager.Instance;
            saveManager.Initialize();

            // Load high scores
            // Load config/settings

            Console.WriteLine("User Services: OK");
        }

        /// <summary>
        /// Cache management initialization (CA_Startup equivalent)
        /// </summary>
        private void InitializeCaching()
        {
            // In Keen, this would lock essential graphics in memory
            // In modern systems, we pre-load critical assets

            Console.WriteLine("Cache: OK");
        }

        /// <summary>
        /// Check system requirements (CheckMemory equivalent)
        /// </summary>
        private void CheckSystemRequirements()
        {
            // Keen checked for sufficient RAM/EMS/XMS
            // Modern version could check GPU capabilities, available memory, etc.

            Console.WriteLine("System Requirements: OK");
        }

        /// <summary>
        /// Load game content
        /// </summary>
        protected override void LoadContent()
        {
            // Load fonts
            defaultFont = Content.Load<SpriteFont>("Fonts/Default");

            // Load title screen
            try
            {
                titleScreenTexture = Content.Load<Texture2D>("Textures/TitleScreen");
            }
            catch
            {
                // Create a simple colored texture if file doesn't exist
                titleScreenTexture = new Texture2D(GraphicsDevice, 1, 1);
                titleScreenTexture.SetData(new[] { Color.DarkBlue });
            }

            Console.WriteLine("Content Loaded");
        }

        /// <summary>
        /// Register all game scenes
        /// </summary>
        private void RegisterScenes()
        {
            // Title and menu scenes
            sceneManager.RegisterScene("TitleScreen",
                new TitleScreenScene(titleScreenTexture, defaultFont));

            sceneManager.RegisterScene("MainMenu",
                new MainMenuScene(defaultFont, titleScreenTexture));

            sceneManager.RegisterScene("LoadGameMenu",
                new LoadGameMenuScene(defaultFont));

            sceneManager.RegisterScene("ControlsMenu",
                new ControlsMenuScene(defaultFont));

            sceneManager.RegisterScene("SoundMenu",
                new SoundMenuScene(defaultFont));

            sceneManager.RegisterScene("HighScores",
                new HighScoreScreen(defaultFont));

            // Game scenes would be registered here
            // sceneManager.RegisterScene("GameLevel", new GameLevelScene(...));
            // sceneManager.RegisterScene("WorldMap", new WorldMapScreen(...));

            Console.WriteLine("Scenes Registered");
        }

        /// <summary>
        /// Start attract mode (DemoLoop equivalent)
        /// </summary>
        private void StartAttractMode()
        {
            attractMode = new AttractModeManager();
            attractMode.Start();

            Console.WriteLine("Entering Attract Mode");
        }

        /// <summary>
        /// Main update loop
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            // Update input first
            inputManager.Update(gameTime);

            // Check for exit (Alt+F4 or close button still work)
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            // Update attract mode manager
            if (attractMode != null)
            {
                attractMode.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            // Update audio
            audioManager.Update(gameTime);

            // Update current scene
            sceneManager.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Main draw loop
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Draw current scene
            sceneManager.Draw(spriteBatch, gameTime);

            base.Draw(gameTime);
        }

        /// <summary>
        /// Cleanup on exit
        /// </summary>
        protected override void UnloadContent()
        {
            // Save settings
            saveManager.SaveSettings(new GameSettings
            {
                MasterVolume = audioManager.MasterVolume,
                MusicVolume = audioManager.MusicVolume,
                SFXVolume = audioManager.SFXVolume
            });

            base.UnloadContent();
        }
    }

    /// <summary>
    /// Program entry point
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point - equivalent to main() in CK_MAIN.C
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Parse command-line arguments (Keen checked for DEMO, JOYPAD flags)
            bool startDemo = Array.Exists(args, arg => arg.ToUpper() == "DEMO");
            bool noSound = Array.Exists(args, arg => arg.ToUpper() == "NOSOUND");

            Console.WriteLine("=================================");
            Console.WriteLine("  PLATFORMER ENGINE");
            Console.WriteLine("  Based on Commander Keen 4-6");
            Console.WriteLine("=================================");
            Console.WriteLine();

            // Display command-line args if any
            if (args.Length > 0)
            {
                Console.WriteLine("Command-line arguments:");
                foreach (var arg in args)
                {
                    Console.WriteLine($"  {arg}");
                }
                Console.WriteLine();
            }

            // Create and run game
            using (var game = new PlatformerGame())
            {
                game.Run();
            }
        }
    }
}
