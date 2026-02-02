using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Scenes;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Screens
{
    /// <summary>
    /// Level statistics
    /// </summary>
    public class LevelStats
    {
        public string LevelName { get; set; }
        public int CoinsCollected { get; set; }
        public int TotalCoins { get; set; }
        public int SecretsFound { get; set; }
        public int TotalSecrets { get; set; }
        public int EnemiesDefeated { get; set; }
        public int TotalEnemies { get; set; }
        public double CompletionTime { get; set; }
        public int Score { get; set; }
        public int Lives { get; set; }
        public bool PerfectRun { get; set; }  // No damage taken
        public bool SpeedrunBonus { get; set; }  // Completed under target time
        public Dictionary<string, int> CustomStats { get; set; } = new Dictionary<string, int>();

        public int GetCoinPercentage() => TotalCoins > 0 ? (CoinsCollected * 100) / TotalCoins : 0;
        public int GetSecretPercentage() => TotalSecrets > 0 ? (SecretsFound * 100) / TotalSecrets : 0;
        public int GetEnemyPercentage() => TotalEnemies > 0 ? (EnemiesDefeated * 100) / TotalEnemies : 0;

        public string GetRank()
        {
            int totalPercent = (GetCoinPercentage() + GetSecretPercentage() + GetEnemyPercentage()) / 3;

            if (PerfectRun && totalPercent >= 95) return "S+";
            if (totalPercent >= 95) return "S";
            if (totalPercent >= 85) return "A";
            if (totalPercent >= 75) return "B";
            if (totalPercent >= 60) return "C";
            if (totalPercent >= 40) return "D";
            return "E";
        }
    }

    /// <summary>
    /// Level complete / results screen
    /// </summary>
    public class LevelCompleteScreen : Scene
    {
        private LevelStats stats;
        private SpriteFont font;
        private SpriteFont titleFont;
        private Texture2D pixelTexture;
        private InputManager input;

        // Animation
        private float fadeAlpha = 0f;
        private float statsRevealProgress = 0f;
        private float totalAnimationTime = 3f;
        private float currentAnimationTime = 0f;

        // Star rating
        private int starsEarned = 0;
        private List<float> starRevealTimes = new List<float> { 1f, 1.5f, 2f };

        // Colors
        private Color backgroundColor = new Color(20, 20, 60);
        private Color titleColor = Color.Gold;
        private Color textColor = Color.White;
        private Color statColor = Color.LightGreen;
        private Color perfectColor = Color.Cyan;

        // Events
        public Action OnContinue { get; set; }

        public LevelCompleteScreen(string name = "LevelComplete") : base(name)
        {
        }

        public void Initialize(SpriteFont font, SpriteFont titleFont)
        {
            this.font = font;
            this.titleFont = titleFont;
            input = InputManager.Instance;
        }

        public void SetStats(LevelStats levelStats)
        {
            stats = levelStats;
            CalculateStars();
        }

        private void CalculateStars()
        {
            starsEarned = 0;

            // Star 1: Complete the level
            starsEarned++;

            // Star 2: Collect 50%+ items
            int avgPercent = (stats.GetCoinPercentage() + stats.GetSecretPercentage()) / 2;
            if (avgPercent >= 50)
                starsEarned++;

            // Star 3: Perfect run (no damage + 80%+ collection)
            if (stats.PerfectRun && avgPercent >= 80)
                starsEarned++;
        }

        public override void LoadContent()
        {
            base.LoadContent();

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
            statsRevealProgress = 0f;
            currentAnimationTime = 0f;
        }

        public override void Update(GameTime gameTime)
        {
            if (stats == null || input == null)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Fade in
            fadeAlpha = Math.Min(fadeAlpha + deltaTime * 2f, 1f);

            // Animate stats reveal
            currentAnimationTime += deltaTime;
            statsRevealProgress = Math.Min(currentAnimationTime / totalAnimationTime, 1f);

            // Skip to end of animation
            if (input.IsActionPressed(InputAction.Confirm) ||
                input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                if (statsRevealProgress < 1f)
                {
                    statsRevealProgress = 1f;
                    currentAnimationTime = totalAnimationTime;
                }
                else
                {
                    Continue();
                }
            }
        }

        private void Continue()
        {
            OnContinue?.Invoke();
            sceneManager?.PopScene();
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (stats == null || font == null)
                return;

            var graphics = game.GraphicsDevice;
            int screenWidth = graphics.Viewport.Width;
            int screenHeight = graphics.Viewport.Height;

            // Draw background
            spriteBatch.Draw(pixelTexture,
                new Rectangle(0, 0, screenWidth, screenHeight),
                backgroundColor * fadeAlpha);

            int yOffset = 50;
            int lineHeight = 35;

            // Title
            string title = "LEVEL COMPLETE!";
            Vector2 titleSize = titleFont.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2f, yOffset);
            spriteBatch.DrawString(titleFont, title, titlePos, titleColor * fadeAlpha);

            yOffset += (int)titleSize.Y + 20;

            // Level name
            if (!string.IsNullOrEmpty(stats.LevelName))
            {
                Vector2 nameSize = font.MeasureString(stats.LevelName);
                Vector2 namePos = new Vector2((screenWidth - nameSize.X) / 2f, yOffset);
                spriteBatch.DrawString(font, stats.LevelName, namePos, textColor * fadeAlpha);
                yOffset += lineHeight;
            }

            yOffset += 20;

            // Stars
            DrawStars(spriteBatch, screenWidth / 2, yOffset);
            yOffset += 60;

            // Stats (animated reveal)
            if (statsRevealProgress > 0.1f)
            {
                DrawStat(spriteBatch, 150, yOffset, "Time:", FormatTime(stats.CompletionTime), fadeAlpha * Math.Min((statsRevealProgress - 0.1f) * 5f, 1f));
                yOffset += lineHeight;
            }

            if (statsRevealProgress > 0.25f)
            {
                DrawStat(spriteBatch, 150, yOffset, "Coins:", $"{stats.CoinsCollected}/{stats.TotalCoins} ({stats.GetCoinPercentage()}%)", fadeAlpha * Math.Min((statsRevealProgress - 0.25f) * 5f, 1f));
                yOffset += lineHeight;
            }

            if (statsRevealProgress > 0.4f)
            {
                DrawStat(spriteBatch, 150, yOffset, "Secrets:", $"{stats.SecretsFound}/{stats.TotalSecrets} ({stats.GetSecretPercentage()}%)", fadeAlpha * Math.Min((statsRevealProgress - 0.4f) * 5f, 1f));
                yOffset += lineHeight;
            }

            if (statsRevealProgress > 0.55f)
            {
                DrawStat(spriteBatch, 150, yOffset, "Enemies:", $"{stats.EnemiesDefeated}/{stats.TotalEnemies} ({stats.GetEnemyPercentage()}%)", fadeAlpha * Math.Min((statsRevealProgress - 0.55f) * 5f, 1f));
                yOffset += lineHeight;
            }

            if (statsRevealProgress > 0.7f)
            {
                DrawStat(spriteBatch, 150, yOffset, "Score:", stats.Score.ToString(), fadeAlpha * Math.Min((statsRevealProgress - 0.7f) * 5f, 1f));
                yOffset += lineHeight;
            }

            yOffset += 20;

            // Bonuses
            if (stats.PerfectRun && statsRevealProgress > 0.8f)
            {
                DrawBonus(spriteBatch, screenWidth / 2, yOffset, "PERFECT RUN!", fadeAlpha * Math.Min((statsRevealProgress - 0.8f) * 5f, 1f));
                yOffset += lineHeight;
            }

            if (stats.SpeedrunBonus && statsRevealProgress > 0.85f)
            {
                DrawBonus(spriteBatch, screenWidth / 2, yOffset, "SPEED BONUS!", fadeAlpha * Math.Min((statsRevealProgress - 0.85f) * 5f, 1f));
                yOffset += lineHeight;
            }

            // Rank
            if (statsRevealProgress >= 1f)
            {
                yOffset += 20;
                string rank = $"Rank: {stats.GetRank()}";
                Vector2 rankSize = titleFont.MeasureString(rank);
                Vector2 rankPos = new Vector2((screenWidth - rankSize.X) / 2f, yOffset);

                Color rankColor = stats.GetRank().StartsWith("S") ? Color.Gold :
                                 stats.GetRank() == "A" ? Color.Silver :
                                 textColor;

                spriteBatch.DrawString(titleFont, rank, rankPos, rankColor * fadeAlpha);
                yOffset += (int)rankSize.Y + 30;

                // Continue prompt
                string prompt = "Press SPACE to continue...";
                Vector2 promptSize = font.MeasureString(prompt);
                Vector2 promptPos = new Vector2((screenWidth - promptSize.X) / 2f, screenHeight - 50);
                float blink = (float)Math.Sin(DateTime.Now.Millisecond / 200.0) * 0.5f + 0.5f;
                spriteBatch.DrawString(font, prompt, promptPos, textColor * blink * fadeAlpha);
            }
        }

        private void DrawStat(SpriteBatch spriteBatch, int x, int y, string label, string value, float alpha)
        {
            spriteBatch.DrawString(font, label, new Vector2(x, y), textColor * alpha);
            spriteBatch.DrawString(font, value, new Vector2(x + 200, y), statColor * alpha);
        }

        private void DrawBonus(SpriteBatch spriteBatch, int centerX, int y, string text, float alpha)
        {
            Vector2 size = font.MeasureString(text);
            Vector2 pos = new Vector2(centerX - size.X / 2f, y);
            spriteBatch.DrawString(font, text, pos, perfectColor * alpha);
        }

        private void DrawStars(SpriteBatch spriteBatch, int centerX, int y)
        {
            int starSpacing = 60;
            int startX = centerX - (starSpacing * (3 - 1) / 2);

            for (int i = 0; i < 3; i++)
            {
                float starAlpha = 0f;

                if (i < starsEarned)
                {
                    // Earned star - animate reveal
                    float revealTime = starRevealTimes[i];
                    if (currentAnimationTime >= revealTime)
                    {
                        starAlpha = Math.Min((currentAnimationTime - revealTime) * 3f, 1f);
                    }
                }
                else
                {
                    // Unearned star - show faded
                    if (statsRevealProgress >= 1f)
                        starAlpha = 0.2f;
                }

                if (starAlpha > 0f)
                {
                    DrawStar(spriteBatch, startX + i * starSpacing, y, starAlpha * fadeAlpha, i < starsEarned);
                }
            }
        }

        private void DrawStar(SpriteBatch spriteBatch, int x, int y, float alpha, bool filled)
        {
            // Simple star using text (replace with actual star texture for better visuals)
            string star = filled ? "★" : "☆";
            Color starColor = filled ? Color.Gold : Color.Gray;
            Vector2 size = titleFont.MeasureString(star);
            Vector2 pos = new Vector2(x - size.X / 2f, y);
            spriteBatch.DrawString(titleFont, star, pos, starColor * alpha);
        }

        private string FormatTime(double seconds)
        {
            int minutes = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            int ms = (int)((seconds - (int)seconds) * 100);
            return $"{minutes:00}:{secs:00}.{ms:00}";
        }
    }
}
