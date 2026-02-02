using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Replay;
using System;

namespace PlatformerEngine.Core.Scenes
{
    /// <summary>
    /// Demo playback scene (RunDemo equivalent from Keen)
    /// </summary>
    public class DemoScene : Scene
    {
        private GameLevelScene gameLevel;
        private ReplaySystem replaySystem;
        private ReplayData replayData;
        private SpriteFont font;
        private bool showDemoText = true;
        private float displayTime;

        public DemoScene(string demoFileName, SpriteFont font = null)
        {
            this.font = font;
            replaySystem = ReplaySystem.Instance;

            // Load demo
            try
            {
                replayData = ReplayData.Load(demoFileName);
            }
            catch
            {
                // Demo file not found - will exit
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (replayData == null)
            {
                // No demo - return to attract mode
                SceneManager.Instance.ChangeScene("TitleScreen");
                return;
            }

            // Create game level for the demo
            gameLevel = new GameLevelScene(replayData.LevelName, font);
            gameLevel.OnEnter();

            // Start playback
            replaySystem.StartPlayback(replayData);
        }

        public override void Update(GameTime gameTime)
        {
            displayTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Check for user input to skip demo
            var input = InputManager.Instance;
            if (input.IsActionPressed(InputAction.Jump) ||
                input.IsActionPressed(InputAction.Pause) ||
                input.IsActionPressed(InputAction.Interact))
            {
                // User pressed key - go to main menu
                ExitDemo();
                return;
            }

            // Update replay system
            replaySystem.Update(gameTime);

            // Check if demo finished
            if (!replaySystem.IsPlaying)
            {
                ExitDemo();
                return;
            }

            // Update the game level (it will receive input from replay system)
            gameLevel?.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Draw the game level
            gameLevel?.Draw(spriteBatch, gameTime);

            // Draw "DEMO" text overlay
            if (showDemoText && font != null)
            {
                spriteBatch.Begin();

                string demoText = "- DEMO -";
                Vector2 textSize = font.MeasureString(demoText);
                Vector2 position = new Vector2(320 - textSize.X / 2, 20);

                // Blinking effect
                float blink = (float)Math.Sin(displayTime * 3);
                if (blink > 0)
                {
                    spriteBatch.DrawString(font, demoText, position, Color.Yellow);
                }

                spriteBatch.End();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            // Stop playback
            replaySystem.StopPlayback();

            // Clean up game level
            gameLevel?.OnExit();
        }

        private void ExitDemo()
        {
            // Return to main menu or next attract mode screen
            SceneManager.Instance.ChangeScene("MainMenu", new FadeTransition(0.5f));
        }
    }
}
