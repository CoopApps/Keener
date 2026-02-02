using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Scenes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformerEngine.Core.Screens
{
    /// <summary>
    /// Single high score entry
    /// </summary>
    public class HighScoreEntry
    {
        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("name")]
        public string PlayerName { get; set; } = "PLAYER";

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; }

        [JsonPropertyName("time")]
        public double CompletionTime { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// High score table data
    /// </summary>
    public class HighScoreTable
    {
        [JsonPropertyName("entries")]
        public List<HighScoreEntry> Entries { get; set; } = new List<HighScoreEntry>();

        [JsonPropertyName("maxEntries")]
        public int MaxEntries { get; set; } = 10;

        /// <summary>
        /// Check if score qualifies for table
        /// </summary>
        public bool IsHighScore(int score)
        {
            if (Entries.Count < MaxEntries)
                return true;

            return score > Entries[Entries.Count - 1].Score;
        }

        /// <summary>
        /// Add entry to table
        /// </summary>
        public int AddEntry(HighScoreEntry entry)
        {
            Entries.Add(entry);
            Entries = Entries.OrderByDescending(e => e.Score).Take(MaxEntries).ToList();

            // Update ranks
            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Rank = i + 1;
            }

            return Entries.FindIndex(e => e == entry) + 1;
        }

        /// <summary>
        /// Save to file
        /// </summary>
        public void Save(string filePath = "highscores.json")
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(filePath, json);
            }
            catch { }
        }

        /// <summary>
        /// Load from file
        /// </summary>
        public static HighScoreTable Load(string filePath = "highscores.json")
        {
            try
            {
                if (!File.Exists(filePath))
                    return new HighScoreTable();

                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<HighScoreTable>(json, options);
            }
            catch
            {
                return new HighScoreTable();
            }
        }
    }

    /// <summary>
    /// High score display screen
    /// </summary>
    public class HighScoreScreen : Scene
    {
        private HighScoreTable highScores;
        private SpriteFont font;
        private SpriteFont titleFont;
        private Texture2D pixelTexture;
        private InputManager input;

        // New entry input
        private bool isEnteringName = false;
        private int newEntryIndex = -1;
        private string currentName = "";
        private int maxNameLength = 10;
        private float cursorBlinkTimer = 0f;

        // Animation
        private float fadeAlpha = 0f;
        private List<float> entryAnimationTimes = new List<float>();

        // Colors
        private Color backgroundColor = new Color(10, 10, 30);
        private Color titleColor = Color.Gold;
        private Color textColor = Color.White;
        private Color highlightColor = Color.Yellow;
        private Color newEntryColor = Color.Cyan;

        public Action OnComplete { get; set; }

        public HighScoreScreen(string name = "HighScores") : base(name)
        {
        }

        public void Initialize(SpriteFont font, SpriteFont titleFont)
        {
            this.font = font;
            this.titleFont = titleFont;
            input = InputManager.Instance;
            highScores = HighScoreTable.Load();

            // Setup animation times
            for (int i = 0; i < highScores.MaxEntries; i++)
            {
                entryAnimationTimes.Add(i * 0.1f);
            }
        }

        /// <summary>
        /// Add a new high score entry
        /// </summary>
        public void AddNewEntry(int score, string level, double time)
        {
            if (highScores.IsHighScore(score))
            {
                var entry = new HighScoreEntry
                {
                    Score = score,
                    Level = level,
                    CompletionTime = time,
                    Date = DateTime.Now,
                    PlayerName = ""
                };

                newEntryIndex = highScores.AddEntry(entry);
                isEnteringName = true;
                currentName = "";
            }
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
        }

        public override void Update(GameTime gameTime)
        {
            if (input == null)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Fade in
            fadeAlpha = Math.Min(fadeAlpha + deltaTime * 2f, 1f);
            cursorBlinkTimer += deltaTime;

            // Handle name input
            if (isEnteringName)
            {
                HandleNameInput();
            }
            else
            {
                // Exit on any key
                if (input.IsActionPressed(InputAction.Confirm) ||
                    input.IsActionPressed(InputAction.Cancel) ||
                    input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
                {
                    Exit();
                }
            }
        }

        private void HandleNameInput()
        {
            // Get keys pressed this frame
            var keys = Microsoft.Xna.Framework.Input.Keyboard.GetState().GetPressedKeys();

            foreach (var key in keys)
            {
                if (input.IsKeyPressed(key))
                {
                    // Enter - confirm name
                    if (key == Microsoft.Xna.Framework.Input.Keys.Enter)
                    {
                        ConfirmName();
                        return;
                    }

                    // Backspace - delete character
                    if (key == Microsoft.Xna.Framework.Input.Keys.Back && currentName.Length > 0)
                    {
                        currentName = currentName.Substring(0, currentName.Length - 1);
                        continue;
                    }

                    // Space
                    if (key == Microsoft.Xna.Framework.Input.Keys.Space && currentName.Length < maxNameLength)
                    {
                        currentName += " ";
                        continue;
                    }

                    // Letters and numbers
                    if (currentName.Length < maxNameLength)
                    {
                        string keyString = key.ToString();
                        if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0]))
                        {
                            currentName += keyString;
                        }
                        else if (keyString.StartsWith("D") && keyString.Length == 2 && char.IsDigit(keyString[1]))
                        {
                            // Number keys
                            currentName += keyString[1];
                        }
                    }
                }
            }
        }

        private void ConfirmName()
        {
            if (string.IsNullOrWhiteSpace(currentName))
                currentName = "PLAYER";

            if (newEntryIndex > 0 && newEntryIndex <= highScores.Entries.Count)
            {
                highScores.Entries[newEntryIndex - 1].PlayerName = currentName.ToUpper();
            }

            highScores.Save();
            isEnteringName = false;
            newEntryIndex = -1;
        }

        private void Exit()
        {
            OnComplete?.Invoke();
            sceneManager?.PopScene();
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (font == null)
                return;

            var graphics = game.GraphicsDevice;
            int screenWidth = graphics.Viewport.Width;
            int screenHeight = graphics.Viewport.Height;

            // Draw background
            spriteBatch.Draw(pixelTexture,
                new Rectangle(0, 0, screenWidth, screenHeight),
                backgroundColor * fadeAlpha);

            int yOffset = 50;

            // Title
            string title = "HIGH SCORES";
            Vector2 titleSize = titleFont.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2f, yOffset);
            spriteBatch.DrawString(titleFont, title, titlePos, titleColor * fadeAlpha);

            yOffset += (int)titleSize.Y + 40;

            // Column headers
            int rankX = screenWidth / 2 - 300;
            int nameX = screenWidth / 2 - 200;
            int scoreX = screenWidth / 2 + 50;
            int levelX = screenWidth / 2 + 200;

            spriteBatch.DrawString(font, "RANK", new Vector2(rankX, yOffset), textColor * fadeAlpha * 0.7f);
            spriteBatch.DrawString(font, "NAME", new Vector2(nameX, yOffset), textColor * fadeAlpha * 0.7f);
            spriteBatch.DrawString(font, "SCORE", new Vector2(scoreX, yOffset), textColor * fadeAlpha * 0.7f);
            spriteBatch.DrawString(font, "LEVEL", new Vector2(levelX, yOffset), textColor * fadeAlpha * 0.7f);

            yOffset += 40;

            // Entries
            for (int i = 0; i < highScores.MaxEntries; i++)
            {
                float entryAlpha = fadeAlpha;

                // Animate entry appearance
                float currentTime = (float)gameTime.TotalGameTime.TotalSeconds;
                if (currentTime < entryAnimationTimes[i])
                    entryAlpha = 0f;
                else if (currentTime < entryAnimationTimes[i] + 0.3f)
                    entryAlpha *= (currentTime - entryAnimationTimes[i]) / 0.3f;

                if (i < highScores.Entries.Count)
                {
                    var entry = highScores.Entries[i];
                    bool isNewEntry = (i + 1) == newEntryIndex;

                    Color entryColor = isNewEntry ? newEntryColor : textColor;

                    // Rank
                    spriteBatch.DrawString(font, $"#{entry.Rank}", new Vector2(rankX, yOffset), entryColor * entryAlpha);

                    // Name (with input cursor if entering)
                    string displayName = isEnteringName && isNewEntry ? currentName : entry.PlayerName;
                    if (isEnteringName && isNewEntry && (cursorBlinkTimer % 1f) < 0.5f)
                        displayName += "_";

                    spriteBatch.DrawString(font, displayName, new Vector2(nameX, yOffset), entryColor * entryAlpha);

                    // Score
                    spriteBatch.DrawString(font, entry.Score.ToString(), new Vector2(scoreX, yOffset), entryColor * entryAlpha);

                    // Level
                    spriteBatch.DrawString(font, entry.Level, new Vector2(levelX, yOffset), entryColor * entryAlpha);
                }
                else
                {
                    // Empty slot
                    spriteBatch.DrawString(font, $"#{i + 1}", new Vector2(rankX, yOffset), textColor * entryAlpha * 0.3f);
                    spriteBatch.DrawString(font, "---", new Vector2(nameX, yOffset), textColor * entryAlpha * 0.3f);
                    spriteBatch.DrawString(font, "0", new Vector2(scoreX, yOffset), textColor * entryAlpha * 0.3f);
                }

                yOffset += 35;
            }

            // Instructions
            string instructions = isEnteringName
                ? "Type your name and press ENTER"
                : "Press any key to continue...";

            Vector2 instructionsSize = font.MeasureString(instructions);
            Vector2 instructionsPos = new Vector2((screenWidth - instructionsSize.X) / 2f, screenHeight - 50);
            float blink = (float)Math.Sin(DateTime.Now.Millisecond / 200.0) * 0.5f + 0.5f;
            spriteBatch.DrawString(font, instructions, instructionsPos, textColor * blink * fadeAlpha);
        }
    }
}
