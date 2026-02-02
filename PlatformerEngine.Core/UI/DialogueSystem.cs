using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.UI
{
    /// <summary>
    /// Single dialogue line
    /// </summary>
    public class DialogueLine
    {
        public string Speaker { get; set; }
        public string Text { get; set; }
        public Texture2D Portrait { get; set; }
        public float DisplayDuration { get; set; } = 0f;  // 0 = wait for input
        public Action OnComplete { get; set; }
    }

    /// <summary>
    /// Dialogue conversation
    /// </summary>
    public class Dialogue
    {
        public string Id { get; set; }
        public List<DialogueLine> Lines { get; set; } = new List<DialogueLine>();
        public int CurrentLine { get; private set; }
        public bool IsComplete => CurrentLine >= Lines.Count;

        public DialogueLine GetCurrentLine()
        {
            return CurrentLine < Lines.Count ? Lines[CurrentLine] : null;
        }

        public void NextLine()
        {
            if (!IsComplete)
            {
                Lines[CurrentLine].OnComplete?.Invoke();
                CurrentLine++;
            }
        }

        public void Reset()
        {
            CurrentLine = 0;
        }
    }

    /// <summary>
    /// Dialogue UI box
    /// </summary>
    public class DialogueBox : UIElement
    {
        private Dialogue currentDialogue;
        private string displayedText = "";
        private float typewriterTimer;
        private float typewriterSpeed = 0.05f;  // Seconds per character
        private int charactersDisplayed;

        public SpriteFont Font { get; set; }
        public Color BoxColor { get; set; } = new Color(0, 0, 0, 200);
        public Color TextColor { get; set; } = Color.White;
        public Color SpeakerColor { get; set; } = Color.Yellow;
        public int Padding { get; set; } = 10;
        public int PortraitSize { get; set; } = 64;
        public bool TypewriterEffect { get; set; } = true;
        public bool IsDialogueActive => currentDialogue != null && !currentDialogue.IsComplete;

        private Texture2D pixelTexture;
        private float autoAdvanceTimer;

        public event Action OnDialogueComplete;

        public DialogueBox(GraphicsDevice graphicsDevice, SpriteFont font)
        {
            Font = font;
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Position at bottom of screen
            Size = new Vector2(700, 150);
            Position = new Vector2(50, 450);
        }

        public void StartDialogue(Dialogue dialogue)
        {
            currentDialogue = dialogue;
            currentDialogue.Reset();
            charactersDisplayed = 0;
            typewriterTimer = 0;
            IsVisible = true;
            IsEnabled = true;
        }

        public void AdvanceDialogue()
        {
            if (currentDialogue == null)
                return;

            var currentLine = currentDialogue.GetCurrentLine();
            if (currentLine == null)
                return;

            // If typewriter still in progress, skip to end
            if (TypewriterEffect && charactersDisplayed < currentLine.Text.Length)
            {
                charactersDisplayed = currentLine.Text.Length;
                displayedText = currentLine.Text;
                return;
            }

            // Move to next line
            currentDialogue.NextLine();
            charactersDisplayed = 0;
            typewriterTimer = 0;

            if (currentDialogue.IsComplete)
            {
                EndDialogue();
            }
        }

        public void EndDialogue()
        {
            currentDialogue = null;
            IsVisible = false;
            OnDialogueComplete?.Invoke();
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsDialogueActive)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var currentLine = currentDialogue.GetCurrentLine();

            if (currentLine == null)
                return;

            // Typewriter effect
            if (TypewriterEffect && charactersDisplayed < currentLine.Text.Length)
            {
                typewriterTimer += deltaTime;

                if (typewriterTimer >= typewriterSpeed)
                {
                    charactersDisplayed++;
                    typewriterTimer = 0;

                    if (charactersDisplayed <= currentLine.Text.Length)
                    {
                        displayedText = currentLine.Text.Substring(0, charactersDisplayed);
                    }
                }
            }
            else
            {
                displayedText = currentLine.Text;
            }

            // Auto-advance if duration is set
            if (currentLine.DisplayDuration > 0 && charactersDisplayed >= currentLine.Text.Length)
            {
                autoAdvanceTimer += deltaTime;

                if (autoAdvanceTimer >= currentLine.DisplayDuration)
                {
                    autoAdvanceTimer = 0;
                    AdvanceDialogue();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible || currentDialogue == null)
                return;

            var currentLine = currentDialogue.GetCurrentLine();
            if (currentLine == null)
                return;

            // Draw box background
            spriteBatch.Draw(pixelTexture, Bounds, BoxColor);

            // Draw border
            DrawBorder(spriteBatch, Bounds, Color.White, 2);

            Vector2 textPos = Position + new Vector2(Padding, Padding);

            // Draw portrait if present
            if (currentLine.Portrait != null)
            {
                Rectangle portraitBounds = new Rectangle(
                    (int)Position.X + Padding,
                    (int)Position.Y + Padding,
                    PortraitSize,
                    PortraitSize
                );
                spriteBatch.Draw(currentLine.Portrait, portraitBounds, Color.White);
                textPos.X += PortraitSize + Padding;
            }

            // Draw speaker name
            if (!string.IsNullOrEmpty(currentLine.Speaker))
            {
                spriteBatch.DrawString(Font, currentLine.Speaker, textPos, SpeakerColor);
                textPos.Y += Font.LineSpacing + 5;
            }

            // Draw dialogue text (wrapped)
            float maxWidth = Size.X - Padding * 2;
            if (currentLine.Portrait != null)
                maxWidth -= PortraitSize + Padding;

            string wrappedText = WrapText(displayedText, maxWidth);
            spriteBatch.DrawString(Font, wrappedText, textPos, TextColor);

            // Draw continue indicator if text fully displayed
            if (charactersDisplayed >= currentLine.Text.Length && currentLine.DisplayDuration == 0)
            {
                DrawContinueIndicator(spriteBatch);
            }
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawContinueIndicator(SpriteBatch spriteBatch)
        {
            // Blinking triangle or arrow to indicate player can continue
            float blink = (float)Math.Sin(DateTime.Now.Millisecond / 200.0) * 0.5f + 0.5f;
            Vector2 indicatorPos = Position + new Vector2(Size.X - 20, Size.Y - 20);
            spriteBatch.DrawString(Font, "▼", indicatorPos, TextColor * blink);
        }

        private string WrapText(string text, float maxWidth)
        {
            if (Font == null)
                return text;

            string[] words = text.Split(' ');
            string line = "";
            string result = "";

            foreach (string word in words)
            {
                string testLine = line + word + " ";
                Vector2 size = Font.MeasureString(testLine);

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

            result += line.TrimEnd();
            return result;
        }
    }

    /// <summary>
    /// Dialogue manager
    /// </summary>
    public class DialogueManager
    {
        private Dictionary<string, Dialogue> dialogues = new Dictionary<string, Dialogue>();
        private DialogueBox dialogueBox;

        public bool IsDialogueActive => dialogueBox != null && dialogueBox.IsDialogueActive;

        public DialogueManager(GraphicsDevice graphicsDevice, SpriteFont font)
        {
            dialogueBox = new DialogueBox(graphicsDevice, font);
        }

        public void RegisterDialogue(Dialogue dialogue)
        {
            dialogues[dialogue.Id] = dialogue;
        }

        public void StartDialogue(string dialogueId)
        {
            if (dialogues.TryGetValue(dialogueId, out Dialogue dialogue))
            {
                dialogueBox.StartDialogue(dialogue);
            }
        }

        public void AdvanceDialogue()
        {
            dialogueBox?.AdvanceDialogue();
        }

        public void Update(GameTime gameTime)
        {
            dialogueBox?.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            dialogueBox?.Draw(spriteBatch);
        }

        /// <summary>
        /// Create simple dialogue
        /// </summary>
        public static Dialogue CreateSimpleDialogue(string id, string speaker, params string[] lines)
        {
            var dialogue = new Dialogue { Id = id };

            foreach (var line in lines)
            {
                dialogue.Lines.Add(new DialogueLine
                {
                    Speaker = speaker,
                    Text = line
                });
            }

            return dialogue;
        }
    }
}
