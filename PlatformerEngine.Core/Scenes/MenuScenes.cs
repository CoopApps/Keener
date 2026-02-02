using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Persistence;
using PlatformerEngine.Core.Screens;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Scenes
{
    /// <summary>
    /// Main menu scene (US_ControlPanel equivalent)
    /// </summary>
    public class MainMenuScene : Scene
    {
        private enum MenuOption
        {
            NewGame,
            LoadGame,
            Controls,
            Sound,
            HighScores,
            Quit
        }

        private MenuOption selectedOption;
        private Dictionary<MenuOption, string> menuText = new Dictionary<MenuOption, string>
        {
            { MenuOption.NewGame, "New Game" },
            { MenuOption.LoadGame, "Load Game" },
            { MenuOption.Controls, "Controls" },
            { MenuOption.Sound, "Sound Options" },
            { MenuOption.HighScores, "High Scores" },
            { MenuOption.Quit, "Quit" }
        };

        private SpriteFont font;
        private Texture2D titleBackground;
        private float menuYStart = 250f;
        private float menuSpacing = 40f;

        public MainMenuScene(SpriteFont font, Texture2D titleBackground = null)
        {
            this.font = font;
            this.titleBackground = titleBackground;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            selectedOption = MenuOption.NewGame;
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;

            // Navigate menu
            if (input.IsActionPressed(InputAction.MoveUp))
            {
                selectedOption = (MenuOption)(((int)selectedOption - 1 + 6) % 6);
            }
            else if (input.IsActionPressed(InputAction.MoveDown))
            {
                selectedOption = (MenuOption)(((int)selectedOption + 1) % 6);
            }

            // Select option
            if (input.IsActionPressed(InputAction.Jump) || input.IsActionPressed(InputAction.Interact))
            {
                HandleMenuSelection();
            }

            // Back/Escape
            if (input.IsActionPressed(InputAction.Pause))
            {
                selectedOption = MenuOption.Quit;
                HandleMenuSelection();
            }
        }

        private void HandleMenuSelection()
        {
            switch (selectedOption)
            {
                case MenuOption.NewGame:
                    // Start new game
                    SceneManager.Instance.ChangeScene("GameLevel", new FadeTransition(0.5f));
                    break;

                case MenuOption.LoadGame:
                    SceneManager.Instance.PushScene("LoadGameMenu");
                    break;

                case MenuOption.Controls:
                    SceneManager.Instance.PushScene("ControlsMenu");
                    break;

                case MenuOption.Sound:
                    SceneManager.Instance.PushScene("SoundMenu");
                    break;

                case MenuOption.HighScores:
                    SceneManager.Instance.PushScene("HighScores");
                    break;

                case MenuOption.Quit:
                    // Exit game
                    Environment.Exit(0);
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin();

            // Draw title background
            if (titleBackground != null)
            {
                spriteBatch.Draw(titleBackground, Vector2.Zero, Color.White);
            }

            // Draw menu options
            for (int i = 0; i < 6; i++)
            {
                var option = (MenuOption)i;
                string text = menuText[option];
                Vector2 position = new Vector2(320, menuYStart + i * menuSpacing);

                Color color = (option == selectedOption) ? Color.Yellow : Color.White;

                // Draw selection arrow
                if (option == selectedOption)
                {
                    string arrow = ">";
                    Vector2 arrowPos = position - new Vector2(30, 0);
                    spriteBatch.DrawString(font, arrow, arrowPos, Color.Yellow);
                }

                // Center text
                Vector2 textSize = font.MeasureString(text);
                position.X -= textSize.X / 2;

                spriteBatch.DrawString(font, text, position, color);
            }

            spriteBatch.End();
        }
    }

    /// <summary>
    /// Title screen with auto-demo (ShowTitle equivalent)
    /// </summary>
    public class TitleScreenScene : Scene
    {
        private Texture2D titleImage;
        private SpriteFont font;
        private float displayTime;
        private float timeoutDuration = 6.0f;
        private bool skipToMenu;

        public TitleScreenScene(Texture2D titleImage, SpriteFont font)
        {
            this.titleImage = titleImage;
            this.font = font;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            displayTime = 0;
            skipToMenu = false;
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            displayTime += deltaTime;

            // Check for any key press to skip to menu
            var input = InputManager.Instance;
            if (input.IsActionPressed(InputAction.Jump) ||
                input.IsActionPressed(InputAction.Interact) ||
                input.IsActionPressed(InputAction.Pause))
            {
                skipToMenu = true;
            }

            // Timeout to main menu or demo
            if (displayTime >= timeoutDuration || skipToMenu)
            {
                if (skipToMenu)
                {
                    // Go to main menu
                    SceneManager.Instance.ChangeScene("MainMenu", new FadeTransition(0.3f));
                }
                else
                {
                    // Play demo
                    SceneManager.Instance.ChangeScene("Demo", new FadeTransition(0.3f));
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin();

            if (titleImage != null)
            {
                spriteBatch.Draw(titleImage, Vector2.Zero, Color.White);
            }

            // "Press any key" text (blinking)
            float blink = (float)Math.Sin(displayTime * 4);
            if (blink > 0)
            {
                string text = "Press Any Key";
                Vector2 textSize = font.MeasureString(text);
                Vector2 position = new Vector2(320 - textSize.X / 2, 450);
                spriteBatch.DrawString(font, text, position, Color.White);
            }

            spriteBatch.End();
        }
    }

    /// <summary>
    /// Load game menu
    /// </summary>
    public class LoadGameMenuScene : Scene
    {
        private SpriteFont font;
        private string[] saveSlots;
        private int selectedSlot;

        public LoadGameMenuScene(SpriteFont font)
        {
            this.font = font;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            saveSlots = SaveManager.Instance.GetAllSaveSlots();
            selectedSlot = 0;
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;

            // Navigate slots
            if (input.IsActionPressed(InputAction.MoveUp))
            {
                selectedSlot = Math.Max(0, selectedSlot - 1);
            }
            else if (input.IsActionPressed(InputAction.MoveDown))
            {
                selectedSlot = Math.Min(saveSlots.Length - 1, selectedSlot + 1);
            }

            // Load selected save
            if (input.IsActionPressed(InputAction.Jump) || input.IsActionPressed(InputAction.Interact))
            {
                if (selectedSlot >= 0 && selectedSlot < saveSlots.Length)
                {
                    var saveData = SaveManager.Instance.LoadGame(saveSlots[selectedSlot]);
                    if (saveData != null)
                    {
                        // Load the saved level
                        SceneManager.Instance.ChangeScene("GameLevel", new FadeTransition(0.5f));
                    }
                }
            }

            // Back to main menu
            if (input.IsActionPressed(InputAction.Pause))
            {
                SceneManager.Instance.PopScene();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin();

            // Title
            string title = "Load Game";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(320 - titleSize.X / 2, 100);
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            // Draw save slots
            float yPos = 200;
            for (int i = 0; i < saveSlots.Length; i++)
            {
                string slotText = $"Slot {i + 1}: {saveSlots[i]}";
                Color color = (i == selectedSlot) ? Color.Yellow : Color.Gray;

                if (i == selectedSlot)
                {
                    spriteBatch.DrawString(font, ">", new Vector2(250, yPos), Color.Yellow);
                }

                spriteBatch.DrawString(font, slotText, new Vector2(280, yPos), color);
                yPos += 40;
            }

            // Instructions
            string instruction = "Press ESC to go back";
            Vector2 instrSize = font.MeasureString(instruction);
            spriteBatch.DrawString(font, instruction, new Vector2(320 - instrSize.X / 2, 500), Color.Gray);

            spriteBatch.End();
        }
    }

    /// <summary>
    /// Controls configuration menu
    /// </summary>
    public class ControlsMenuScene : Scene
    {
        private SpriteFont font;

        public ControlsMenuScene(SpriteFont font)
        {
            this.font = font;
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;

            // Back to main menu
            if (input.IsActionPressed(InputAction.Pause))
            {
                SceneManager.Instance.PopScene();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin();

            // Title
            string title = "Controls";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(320 - titleSize.X / 2, 100);
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            // Control list
            string[] controls = new string[]
            {
                "Arrow Keys / WASD - Move",
                "Space / Z - Jump",
                "X - Shoot",
                "Down + Space - Duck Jump",
                "Shift - Run",
                "E - Interact",
                "ESC - Pause"
            };

            float yPos = 200;
            foreach (var control in controls)
            {
                Vector2 textSize = font.MeasureString(control);
                spriteBatch.DrawString(font, control, new Vector2(320 - textSize.X / 2, yPos), Color.White);
                yPos += 35;
            }

            // Instructions
            string instruction = "Press ESC to go back";
            Vector2 instrSize = font.MeasureString(instruction);
            spriteBatch.DrawString(font, instruction, new Vector2(320 - instrSize.X / 2, 500), Color.Gray);

            spriteBatch.End();
        }
    }

    /// <summary>
    /// Sound options menu
    /// </summary>
    public class SoundMenuScene : Scene
    {
        private SpriteFont font;
        private int selectedOption;
        private string[] options = new string[] { "Master Volume", "Music Volume", "SFX Volume", "Back" };

        public SoundMenuScene(SpriteFont font)
        {
            this.font = font;
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;

            // Navigate
            if (input.IsActionPressed(InputAction.MoveUp))
            {
                selectedOption = Math.Max(0, selectedOption - 1);
            }
            else if (input.IsActionPressed(InputAction.MoveDown))
            {
                selectedOption = Math.Min(options.Length - 1, selectedOption + 1);
            }

            // Adjust volume or go back
            if (selectedOption < 3)
            {
                // Adjust volume with left/right
                // (Implementation would adjust AudioManager volumes)
            }

            if (input.IsActionPressed(InputAction.Pause) ||
                (selectedOption == 3 && input.IsActionPressed(InputAction.Jump)))
            {
                SceneManager.Instance.PopScene();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin();

            // Title
            string title = "Sound Options";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(320 - titleSize.X / 2, 100);
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            // Options
            float yPos = 200;
            for (int i = 0; i < options.Length; i++)
            {
                Color color = (i == selectedOption) ? Color.Yellow : Color.White;

                if (i == selectedOption)
                {
                    spriteBatch.DrawString(font, ">", new Vector2(200, yPos), Color.Yellow);
                }

                spriteBatch.DrawString(font, options[i], new Vector2(230, yPos), color);
                yPos += 40;
            }

            spriteBatch.End();
        }
    }

    /// <summary>
    /// Attract mode manager (DemoLoop equivalent)
    /// </summary>
    public class AttractModeManager
    {
        private enum AttractState
        {
            TitleScreen,
            DemoPlayback,
            HighScores,
            Credits
        }

        private AttractState currentState;
        private float stateTimer;
        private int demoIndex;
        private string[] demoFiles = new string[] { "demo1", "demo2", "demo3" };

        public void Start()
        {
            currentState = AttractState.TitleScreen;
            stateTimer = 0;
            demoIndex = 0;

            // Start with title screen
            SceneManager.Instance.ChangeScene("TitleScreen");
        }

        public void Update(float deltaTime)
        {
            stateTimer += deltaTime;

            switch (currentState)
            {
                case AttractState.TitleScreen:
                    // Title screen handles its own timeout
                    break;

                case AttractState.DemoPlayback:
                    // Demo playback scene would handle this
                    if (stateTimer >= 30f) // 30 seconds per demo
                    {
                        NextState();
                    }
                    break;

                case AttractState.HighScores:
                    if (stateTimer >= 10f)
                    {
                        NextState();
                    }
                    break;

                case AttractState.Credits:
                    if (stateTimer >= 15f)
                    {
                        NextState();
                    }
                    break;
            }
        }

        private void NextState()
        {
            stateTimer = 0;

            switch (currentState)
            {
                case AttractState.TitleScreen:
                    currentState = AttractState.DemoPlayback;
                    PlayNextDemo();
                    break;

                case AttractState.DemoPlayback:
                    currentState = AttractState.HighScores;
                    SceneManager.Instance.ChangeScene("HighScores");
                    break;

                case AttractState.HighScores:
                    currentState = AttractState.Credits;
                    SceneManager.Instance.ChangeScene("Credits");
                    break;

                case AttractState.Credits:
                    // Loop back to title
                    currentState = AttractState.TitleScreen;
                    SceneManager.Instance.ChangeScene("TitleScreen");
                    break;
            }
        }

        private void PlayNextDemo()
        {
            if (demoFiles.Length > 0)
            {
                string demoFile = demoFiles[demoIndex];
                demoIndex = (demoIndex + 1) % demoFiles.Length;

                // Load and play demo
                // ReplaySystem.Instance.LoadAndPlayDemo(demoFile);
            }
        }

        public void InterruptForMenu()
        {
            // User pressed key - go to main menu
            SceneManager.Instance.ChangeScene("MainMenu", new FadeTransition(0.3f));
        }
    }
}
