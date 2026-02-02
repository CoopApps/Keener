using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.UI
{
    /// <summary>
    /// Base UI element
    /// </summary>
    public abstract class UIElement
    {
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public float Alpha { get; set; } = 1.0f;
        public Color TintColor { get; set; } = Color.White;

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);

        public abstract void Update(GameTime gameTime);
        public abstract void Draw(SpriteBatch spriteBatch);
    }

    /// <summary>
    /// Text label UI element
    /// </summary>
    public class Label : UIElement
    {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }
        public Color TextColor { get; set; } = Color.White;
        public Vector2 Scale { get; set; } = Vector2.One;

        public Label(SpriteFont font, string text = "")
        {
            Font = font;
            Text = text;
        }

        public override void Update(GameTime gameTime)
        {
            // Labels don't need updating
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible || Font == null)
                return;

            Color finalColor = TextColor * Alpha;
            spriteBatch.DrawString(Font, Text, Position, finalColor, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }

    /// <summary>
    /// Health bar UI element
    /// </summary>
    public class HealthBar : UIElement
    {
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;
        public Color BarColor { get; set; } = Color.Red;
        public Color BackgroundColor { get; set; } = Color.DarkGray;
        public Color BorderColor { get; set; } = Color.White;
        public int BorderThickness { get; set; } = 2;
        public bool ShowNumbers { get; set; } = true;
        public SpriteFont Font { get; set; }

        private Texture2D pixelTexture;

        public HealthBar(GraphicsDevice graphicsDevice, Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;

            // Create 1x1 white pixel texture
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
        }

        public override void Update(GameTime gameTime)
        {
            // Could add animations here (lerp between values, etc.)
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            // Draw background
            spriteBatch.Draw(pixelTexture, Bounds, BackgroundColor * Alpha);

            // Draw health bar
            float healthPercentage = MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
            int barWidth = (int)(Size.X * healthPercentage);

            if (barWidth > 0)
            {
                Rectangle barRect = new Rectangle((int)Position.X, (int)Position.Y, barWidth, (int)Size.Y);
                spriteBatch.Draw(pixelTexture, barRect, BarColor * Alpha);
            }

            // Draw border
            if (BorderThickness > 0)
            {
                DrawBorder(spriteBatch, Bounds, BorderColor * Alpha, BorderThickness);
            }

            // Draw numbers
            if (ShowNumbers && Font != null)
            {
                string text = $"{CurrentHealth}/{MaxHealth}";
                Vector2 textSize = Font.MeasureString(text);
                Vector2 textPos = Position + (Size / 2f) - (textSize / 2f);
                spriteBatch.DrawString(Font, text, textPos, Color.White * Alpha);
            }
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }

    /// <summary>
    /// Counter display (for coins, score, etc.)
    /// </summary>
    public class Counter : UIElement
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public SpriteFont Font { get; set; }
        public Color TextColor { get; set; } = Color.White;
        public Texture2D Icon { get; set; }
        public Vector2 IconScale { get; set; } = Vector2.One;

        public Counter(SpriteFont font, string label = "", int initialValue = 0)
        {
            Font = font;
            Label = label;
            Value = initialValue;
        }

        public override void Update(GameTime gameTime)
        {
            // Could add number rolling animation here
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible || Font == null)
                return;

            Vector2 currentPos = Position;

            // Draw icon if present
            if (Icon != null)
            {
                spriteBatch.Draw(Icon, currentPos, null, Color.White * Alpha, 0f, Vector2.Zero, IconScale, SpriteEffects.None, 0f);
                currentPos.X += Icon.Width * IconScale.X + 5;
            }

            // Draw label and value
            string text = string.IsNullOrEmpty(Label) ? Value.ToString() : $"{Label}: {Value}";
            spriteBatch.DrawString(Font, text, currentPos, TextColor * Alpha);
        }
    }

    /// <summary>
    /// HUD (Heads-Up Display) manager
    /// </summary>
    public class HUD
    {
        private List<UIElement> elements = new List<UIElement>();
        public bool IsVisible { get; set; } = true;

        // Common HUD elements
        public HealthBar HealthBar { get; private set; }
        public Counter CoinCounter { get; private set; }
        public Counter ScoreCounter { get; private set; }
        public Counter LivesCounter { get; private set; }
        public Label MessageLabel { get; private set; }

        private float messageTimer;
        private float messageDuration;

        public void Initialize(GraphicsDevice graphicsDevice, SpriteFont font)
        {
            elements.Clear();

            // Health bar (top-left)
            HealthBar = new HealthBar(graphicsDevice, new Vector2(10, 10), new Vector2(150, 20))
            {
                Font = font
            };
            elements.Add(HealthBar);

            // Coins (top-left, below health)
            CoinCounter = new Counter(font, "Coins", 0)
            {
                Position = new Vector2(10, 40)
            };
            elements.Add(CoinCounter);

            // Score (top-right)
            ScoreCounter = new Counter(font, "Score", 0)
            {
                Position = new Vector2(600, 10)
            };
            elements.Add(ScoreCounter);

            // Lives (top-right, below score)
            LivesCounter = new Counter(font, "Lives", 3)
            {
                Position = new Vector2(600, 40)
            };
            elements.Add(LivesCounter);

            // Message label (center-top)
            MessageLabel = new Label(font)
            {
                Position = new Vector2(400, 100),
                IsVisible = false
            };
            elements.Add(MessageLabel);
        }

        public void Update(GameTime gameTime)
        {
            if (!IsVisible)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var element in elements)
            {
                if (element.IsEnabled)
                    element.Update(gameTime);
            }

            // Update message timer
            if (messageTimer > 0)
            {
                messageTimer -= deltaTime;
                if (messageTimer <= 0)
                {
                    MessageLabel.IsVisible = false;
                }
                else if (messageTimer < 0.5f)
                {
                    // Fade out in last 0.5 seconds
                    MessageLabel.Alpha = messageTimer / 0.5f;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            foreach (var element in elements)
            {
                if (element.IsVisible)
                    element.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Show a temporary message
        /// </summary>
        public void ShowMessage(string message, float duration = 2f)
        {
            MessageLabel.Text = message;
            MessageLabel.IsVisible = true;
            MessageLabel.Alpha = 1f;
            messageTimer = duration;
            messageDuration = duration;
        }

        /// <summary>
        /// Add a custom UI element
        /// </summary>
        public void AddElement(UIElement element)
        {
            elements.Add(element);
        }

        /// <summary>
        /// Remove a UI element
        /// </summary>
        public void RemoveElement(UIElement element)
        {
            elements.Remove(element);
        }

        /// <summary>
        /// Clear all custom elements (keeps default HUD)
        /// </summary>
        public void ClearCustomElements()
        {
            elements.RemoveAll(e => e != HealthBar && e != CoinCounter &&
                                    e != ScoreCounter && e != LivesCounter && e != MessageLabel);
        }
    }

    /// <summary>
    /// Simple button UI element
    /// </summary>
    public class Button : UIElement
    {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }
        public Color NormalColor { get; set; } = Color.Gray;
        public Color HoverColor { get; set; } = Color.LightGray;
        public Color PressedColor { get; set; } = Color.DarkGray;
        public Color TextColor { get; set; } = Color.White;

        public event Action OnClick;

        private bool isHovered;
        private bool isPressed;
        private Texture2D pixelTexture;

        public Button(GraphicsDevice graphicsDevice, SpriteFont font, string text, Vector2 position, Vector2 size)
        {
            Font = font;
            Text = text;
            Position = position;
            Size = size;

            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
        }

        public override void Update(GameTime gameTime)
        {
            // Mouse input would be checked here
            // This is a simplified version - actual implementation needs Input.InputManager
        }

        public void CheckInput(Vector2 mousePosition, bool isMouseDown, bool wasMouseDown)
        {
            if (!IsEnabled)
                return;

            isHovered = Bounds.Contains(mousePosition);

            if (isHovered)
            {
                if (isMouseDown && !wasMouseDown)
                {
                    isPressed = true;
                }
                else if (!isMouseDown && wasMouseDown && isPressed)
                {
                    OnClick?.Invoke();
                    isPressed = false;
                }
            }
            else
            {
                isPressed = false;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            // Determine button color
            Color buttonColor = NormalColor;
            if (!IsEnabled)
                buttonColor = Color.DarkGray;
            else if (isPressed)
                buttonColor = PressedColor;
            else if (isHovered)
                buttonColor = HoverColor;

            // Draw button background
            spriteBatch.Draw(pixelTexture, Bounds, buttonColor * Alpha);

            // Draw button border
            DrawBorder(spriteBatch, Bounds, Color.White * Alpha, 2);

            // Draw text
            if (Font != null && !string.IsNullOrEmpty(Text))
            {
                Vector2 textSize = Font.MeasureString(Text);
                Vector2 textPos = Position + (Size / 2f) - (textSize / 2f);
                spriteBatch.DrawString(Font, Text, textPos, TextColor * Alpha);
            }
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}
