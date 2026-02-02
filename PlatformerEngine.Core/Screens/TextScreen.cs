using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Scenes;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Screens
{
    /// <summary>
    /// Full-screen text display for story, help, credits, etc.
    /// </summary>
    public class TextScreen : Scene
    {
        private List<string> textPages = new List<string>();
        private int currentPage = 0;
        private SpriteFont font;
        private SpriteFont titleFont;
        private Texture2D backgroundTexture;
        private Texture2D pixelTexture;
        private Color backgroundColor = new Color(0, 0, 40);
        private Color textColor = Color.White;
        private Color titleColor = Color.Yellow;
        private InputManager input;

        // Animation
        private float fadeAlpha = 0f;
        private bool isFadingIn = true;
        private float fadeSpeed = 2f;

        // Page turn animation
        private float pageTransitionProgress = 0f;
        private bool isTransitioning = false;
        private int nextPage = 0;

        // Auto-advance
        public bool AutoAdvance { get; set; } = false;
        public float AutoAdvanceDelay { get; set; } = 5.0f;
        private float autoAdvanceTimer = 0f;

        // Events
        public Action OnComplete { get; set; }

        public TextScreen(string name = "TextScreen") : base(name)
        {
        }

        /// <summary>
        /// Initialize with fonts
        /// </summary>
        public void Initialize(SpriteFont font, SpriteFont titleFont = null)
        {
            this.font = font;
            this.titleFont = titleFont ?? font;
            input = InputManager.Instance;
        }

        /// <summary>
        /// Set background
        /// </summary>
        public void SetBackground(Texture2D texture)
        {
            backgroundTexture = texture;
        }

        /// <summary>
        /// Set colors
        /// </summary>
        public void SetColors(Color background, Color text, Color title)
        {
            backgroundColor = background;
            textColor = text;
            titleColor = title;
        }

        /// <summary>
        /// Add a text page
        /// </summary>
        public void AddPage(string text)
        {
            textPages.Add(text);
        }

        /// <summary>
        /// Add multiple pages
        /// </summary>
        public void AddPages(params string[] pages)
        {
            textPages.AddRange(pages);
        }

        /// <summary>
        /// Clear all pages
        /// </summary>
        public void ClearPages()
        {
            textPages.Clear();
            currentPage = 0;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // Create pixel texture if not set
            if (pixelTexture == null && game != null)
            {
                pixelTexture = new Texture2D(game.GraphicsDevice, 1, 1);
                pixelTexture.SetData(new[] { Color.White });
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            fadeAlpha = 0f;
            isFadingIn = true;
            currentPage = 0;
            autoAdvanceTimer = 0f;
        }

        public override void Update(GameTime gameTime)
        {
            if (input == null || textPages.Count == 0)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle fade in
            if (isFadingIn)
            {
                fadeAlpha += fadeSpeed * deltaTime;
                if (fadeAlpha >= 1f)
                {
                    fadeAlpha = 1f;
                    isFadingIn = false;
                }
            }

            // Handle page transition
            if (isTransitioning)
            {
                pageTransitionProgress += 3f * deltaTime;
                if (pageTransitionProgress >= 1f)
                {
                    pageTransitionProgress = 0f;
                    isTransitioning = false;
                    currentPage = nextPage;
                    autoAdvanceTimer = 0f;
                }
                return;
            }

            // Auto-advance
            if (AutoAdvance)
            {
                autoAdvanceTimer += deltaTime;
                if (autoAdvanceTimer >= AutoAdvanceDelay)
                {
                    NextPage();
                }
            }

            // Input handling
            if (input.IsActionPressed(InputAction.Confirm) ||
                input.IsActionPressed(InputAction.Jump) ||
                input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Space) ||
                input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter))
            {
                NextPage();
            }

            if (input.IsActionPressed(InputAction.Cancel) ||
                input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                Exit();
            }
        }

        private void NextPage()
        {
            if (isTransitioning)
                return;

            if (currentPage < textPages.Count - 1)
            {
                nextPage = currentPage + 1;
                isTransitioning = true;
                pageTransitionProgress = 0f;
            }
            else
            {
                Exit();
            }
        }

        private void Exit()
        {
            OnComplete?.Invoke();
            sceneManager?.PopScene();
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (textPages.Count == 0 || font == null)
                return;

            var graphics = game.GraphicsDevice;
            int screenWidth = graphics.Viewport.Width;
            int screenHeight = graphics.Viewport.Height;

            // Draw background
            if (backgroundTexture != null)
            {
                spriteBatch.Draw(backgroundTexture,
                    new Rectangle(0, 0, screenWidth, screenHeight),
                    Color.White * fadeAlpha);
            }
            else if (pixelTexture != null)
            {
                spriteBatch.Draw(pixelTexture,
                    new Rectangle(0, 0, screenWidth, screenHeight),
                    backgroundColor * fadeAlpha);
            }

            // Draw current page
            if (currentPage < textPages.Count)
            {
                float pageAlpha = fadeAlpha;

                // Page transition effect
                if (isTransitioning)
                {
                    pageAlpha *= 1f - pageTransitionProgress;
                }

                DrawPage(spriteBatch, textPages[currentPage], pageAlpha, screenWidth, screenHeight);
            }

            // Draw next page during transition
            if (isTransitioning && nextPage < textPages.Count)
            {
                float nextPageAlpha = fadeAlpha * pageTransitionProgress;
                DrawPage(spriteBatch, textPages[nextPage], nextPageAlpha, screenWidth, screenHeight);
            }

            // Draw page indicator
            if (!isTransitioning && currentPage < textPages.Count - 1)
            {
                DrawContinueIndicator(spriteBatch, screenWidth, screenHeight);
            }
        }

        private void DrawPage(SpriteBatch spriteBatch, string text, float alpha, int screenWidth, int screenHeight)
        {
            // Parse text for title (first line starting with #)
            string[] lines = text.Split('\n');
            string title = null;
            int contentStartLine = 0;

            if (lines.Length > 0 && lines[0].StartsWith("#"))
            {
                title = lines[0].Substring(1).Trim();
                contentStartLine = 1;
            }

            int padding = 50;
            int yOffset = padding;

            // Draw title
            if (!string.IsNullOrEmpty(title))
            {
                Vector2 titleSize = titleFont.MeasureString(title);
                Vector2 titlePos = new Vector2(
                    (screenWidth - titleSize.X) / 2f,
                    yOffset
                );
                spriteBatch.DrawString(titleFont, title, titlePos, titleColor * alpha);
                yOffset += (int)titleSize.Y + 40;
            }

            // Draw content
            string content = string.Join("\n", lines, contentStartLine, lines.Length - contentStartLine);
            string wrappedContent = WrapText(content, screenWidth - padding * 2);

            Vector2 contentPos = new Vector2(padding, yOffset);
            spriteBatch.DrawString(font, wrappedContent, contentPos, textColor * alpha);
        }

        private void DrawContinueIndicator(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            // Blinking continue prompt
            float blink = (float)Math.Sin(DateTime.Now.Millisecond / 200.0) * 0.5f + 0.5f;
            string prompt = "Press SPACE or ENTER to continue...";
            Vector2 promptSize = font.MeasureString(prompt);
            Vector2 promptPos = new Vector2(
                (screenWidth - promptSize.X) / 2f,
                screenHeight - 50
            );
            spriteBatch.DrawString(font, prompt, promptPos, textColor * blink * fadeAlpha);
        }

        private string WrapText(string text, float maxWidth)
        {
            if (font == null)
                return text;

            string[] words = text.Split(' ');
            string line = "";
            string result = "";

            foreach (string word in words)
            {
                // Handle newlines in text
                if (word.Contains("\n"))
                {
                    string[] parts = word.Split('\n');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                        {
                            result += line.TrimEnd() + "\n";
                            line = "";
                        }

                        string testLine = line + parts[i] + " ";
                        Vector2 size = font.MeasureString(testLine);

                        if (size.X > maxWidth && line.Length > 0)
                        {
                            result += line.TrimEnd() + "\n";
                            line = parts[i] + " ";
                        }
                        else
                        {
                            line = testLine;
                        }
                    }
                }
                else
                {
                    string testLine = line + word + " ";
                    Vector2 size = font.MeasureString(testLine);

                    if (size.X > maxWidth && line.Length > 0)
                    {
                        result += line.TrimEnd() + "\n";
                        line = word + " ";
                    }
                    else
                    {
                        line = testLine;
                    }
                }
            }

            result += line.TrimEnd();
            return result;
        }
    }

    /// <summary>
    /// Helper for creating common text screens
    /// </summary>
    public static class TextScreenFactory
    {
        /// <summary>
        /// Create story screen
        /// </summary>
        public static TextScreen CreateStoryScreen(SpriteFont font, SpriteFont titleFont, params string[] pages)
        {
            var screen = new TextScreen("Story");
            screen.Initialize(font, titleFont);
            screen.SetColors(new Color(0, 0, 20), Color.White, Color.Gold);
            screen.AddPages(pages);
            return screen;
        }

        /// <summary>
        /// Create help screen
        /// </summary>
        public static TextScreen CreateHelpScreen(SpriteFont font, SpriteFont titleFont, Dictionary<string, string> controls)
        {
            var screen = new TextScreen("Help");
            screen.Initialize(font, titleFont);
            screen.SetColors(new Color(20, 20, 40), Color.LightGray, Color.Yellow);

            string helpText = "# Controls\n\n";
            foreach (var control in controls)
            {
                helpText += $"{control.Key}: {control.Value}\n";
            }

            screen.AddPage(helpText);
            return screen;
        }

        /// <summary>
        /// Create credits screen
        /// </summary>
        public static TextScreen CreateCreditsScreen(SpriteFont font, SpriteFont titleFont, params string[] creditLines)
        {
            var screen = new TextScreen("Credits");
            screen.Initialize(font, titleFont);
            screen.SetColors(Color.Black, Color.White, Color.Gold);
            screen.AutoAdvance = true;
            screen.AutoAdvanceDelay = 3f;

            string credits = "# Credits\n\n" + string.Join("\n", creditLines);
            screen.AddPage(credits);

            return screen;
        }
    }
}
