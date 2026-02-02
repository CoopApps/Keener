using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Input;
using System;

namespace PlatformerEngine.Core.Scenes
{
    /// <summary>
    /// Editor menu - Choose which editor to launch
    /// </summary>
    public class EditorMenuScene : Scene
    {
        private enum EditorOption
        {
            LevelEditor,
            WorldMapEditor,
            Back
        }

        private EditorOption selectedOption;
        private SpriteFont font;
        private Texture2D pixelTexture;

        public EditorMenuScene(SpriteFont font)
        {
            this.font = font;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Create pixel texture
            pixelTexture = new Texture2D(SceneManager.Instance.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;

            // Navigate
            if (input.IsActionPressed(InputAction.MoveUp))
            {
                selectedOption = (EditorOption)(((int)selectedOption - 1 + 3) % 3);
            }
            else if (input.IsActionPressed(InputAction.MoveDown))
            {
                selectedOption = (EditorOption)(((int)selectedOption + 1) % 3);
            }

            // Select
            if (input.IsActionPressed(InputAction.Jump) || input.IsActionPressed(InputAction.Interact))
            {
                HandleSelection();
            }

            // Back
            if (input.IsActionPressed(InputAction.Pause))
            {
                SceneManager.Instance.PopScene();
            }
        }

        private void HandleSelection()
        {
            switch (selectedOption)
            {
                case EditorOption.LevelEditor:
                    SceneManager.Instance.ChangeScene("LevelEditor");
                    break;

                case EditorOption.WorldMapEditor:
                    SceneManager.Instance.ChangeScene("WorldMapEditor");
                    break;

                case EditorOption.Back:
                    SceneManager.Instance.PopScene();
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.GraphicsDevice.Clear(new Color(20, 20, 40));

            spriteBatch.Begin();

            // Title
            string title = "EDITOR MENU";
            Vector2 titleSize = font != null ? font.MeasureString(title) : Vector2.Zero;
            Vector2 titlePos = new Vector2(320 - titleSize.X / 2, 100);
            if (font != null)
            {
                spriteBatch.DrawString(font, title, titlePos, Color.Yellow);
            }

            // Menu options
            string[] options = { "Level Editor", "World Map Editor", "Back" };
            float yPos = 250;

            for (int i = 0; i < options.Length; i++)
            {
                bool isSelected = i == (int)selectedOption;
                Color color = isSelected ? Color.Yellow : Color.White;
                string text = (isSelected ? "> " : "  ") + options[i];

                if (font != null)
                {
                    Vector2 textSize = font.MeasureString(text);
                    Vector2 position = new Vector2(320 - textSize.X / 2, yPos);
                    spriteBatch.DrawString(font, text, position, color);
                }

                yPos += 40;
            }

            // Instructions
            string instruction = "Press ESC to go back";
            if (font != null)
            {
                Vector2 instrSize = font.MeasureString(instruction);
                spriteBatch.DrawString(font, instruction,
                    new Vector2(320 - instrSize.X / 2, 450), Color.Gray);
            }

            // Description
            string description = selectedOption switch
            {
                EditorOption.LevelEditor => "Create and edit platformer levels with tiles, entities, and collision",
                EditorOption.WorldMapEditor => "Design world maps with connected level nodes",
                EditorOption.Back => "Return to main menu",
                _ => ""
            };

            if (font != null && !string.IsNullOrEmpty(description))
            {
                // Wrap text
                var lines = WrapText(description, 500);
                float descY = 380;
                foreach (var line in lines)
                {
                    Vector2 lineSize = font.MeasureString(line);
                    spriteBatch.DrawString(font, line,
                        new Vector2(320 - lineSize.X / 2, descY), Color.LightGray);
                    descY += 25;
                }
            }

            spriteBatch.End();
        }

        private string[] WrapText(string text, float maxWidth)
        {
            if (font == null) return new[] { text };

            var lines = new System.Collections.Generic.List<string>();
            var words = text.Split(' ');
            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = currentLine + (currentLine.Length > 0 ? " " : "") + word;
                Vector2 size = font.MeasureString(testLine);

                if (size.X > maxWidth && currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine);
            }

            return lines.ToArray();
        }

        public override void OnExit()
        {
            base.OnExit();
            pixelTexture?.Dispose();
        }
    }
}
