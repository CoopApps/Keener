using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace PlatformerEngine.Core.Editor
{
    /// <summary>
    /// Retro UI helper - DOS/CRT-style interface components
    /// </summary>
    public static class RetroUI
    {
        // Retro color palette (EGA-inspired)
        public static readonly Color Black = new Color(0, 0, 0);
        public static readonly Color DarkBlue = new Color(0, 0, 170);
        public static readonly Color DarkGreen = new Color(0, 170, 0);
        public static readonly Color DarkCyan = new Color(0, 170, 170);
        public static readonly Color DarkRed = new Color(170, 0, 0);
        public static readonly Color DarkMagenta = new Color(170, 0, 170);
        public static readonly Color Brown = new Color(170, 85, 0);
        public static readonly Color Gray = new Color(170, 170, 170);
        public static readonly Color DarkGray = new Color(85, 85, 85);
        public static readonly Color Blue = new Color(85, 85, 255);
        public static readonly Color Green = new Color(85, 255, 85);
        public static readonly Color Cyan = new Color(85, 255, 255);
        public static readonly Color Red = new Color(255, 85, 85);
        public static readonly Color Magenta = new Color(255, 85, 255);
        public static readonly Color Yellow = new Color(255, 255, 85);
        public static readonly Color White = new Color(255, 255, 255);

        // UI element colors
        public static readonly Color BorderColor = Cyan;
        public static readonly Color TitleColor = Yellow;
        public static readonly Color ActiveColor = Yellow;
        public static readonly Color InactiveColor = Gray;
        public static readonly Color BackgroundColor = Black;
        public static readonly Color WindowColor = new Color(0, 0, 128); // Dark blue
        public static readonly Color SelectedColor = new Color(0, 255, 255, 128); // Cyan highlight

        /// <summary>
        /// Draw a retro-style window with border
        /// </summary>
        public static void DrawWindow(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds,
            string title = null, SpriteFont font = null)
        {
            // Background
            spriteBatch.Draw(pixel, bounds, WindowColor);

            // Double-line border (DOS-style)
            int thickness = 2;

            // Outer border
            DrawRect(spriteBatch, pixel, bounds, BorderColor, thickness);

            // Inner border (inset by 4 pixels)
            Rectangle innerBorder = new Rectangle(
                bounds.X + 4, bounds.Y + 4,
                bounds.Width - 8, bounds.Height - 8
            );
            DrawRect(spriteBatch, pixel, innerBorder, BorderColor, 1);

            // Title bar
            if (!string.IsNullOrEmpty(title) && font != null)
            {
                Rectangle titleBar = new Rectangle(bounds.X, bounds.Y, bounds.Width, 24);
                spriteBatch.Draw(pixel, titleBar, DarkCyan);

                Vector2 titleSize = font.MeasureString(title);
                Vector2 titlePos = new Vector2(
                    bounds.X + (bounds.Width - titleSize.X) / 2,
                    bounds.Y + (24 - titleSize.Y) / 2
                );
                spriteBatch.DrawString(font, title, titlePos, TitleColor);
            }
        }

        /// <summary>
        /// Draw a retro-style button
        /// </summary>
        public static void DrawButton(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds,
            string text, SpriteFont font, bool isPressed, bool isHovered)
        {
            Color bgColor = isPressed ? DarkCyan : (isHovered ? DarkBlue : WindowColor);
            Color borderColor = isHovered ? Yellow : Cyan;
            Color textColor = isPressed ? Yellow : White;

            // Background
            spriteBatch.Draw(pixel, bounds, bgColor);

            // 3D border effect
            if (!isPressed)
            {
                // Light edges (top, left)
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Y),
                    new Vector2(bounds.Right, bounds.Y), White, 2);
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Y),
                    new Vector2(bounds.X, bounds.Bottom), White, 2);

                // Dark edges (bottom, right)
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Bottom),
                    new Vector2(bounds.Right, bounds.Bottom), DarkGray, 2);
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.Right, bounds.Y),
                    new Vector2(bounds.Right, bounds.Bottom), DarkGray, 2);
            }

            // Border
            DrawRect(spriteBatch, pixel, bounds, borderColor, 1);

            // Text
            if (font != null && !string.IsNullOrEmpty(text))
            {
                Vector2 textSize = font.MeasureString(text);
                Vector2 textPos = new Vector2(
                    bounds.X + (bounds.Width - textSize.X) / 2,
                    bounds.Y + (bounds.Height - textSize.Y) / 2
                );
                if (isPressed)
                    textPos += new Vector2(1, 1);

                spriteBatch.DrawString(font, text, textPos, textColor);
            }
        }

        /// <summary>
        /// Draw a retro-style panel
        /// </summary>
        public static void DrawPanel(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds,
            bool inset = false)
        {
            // Background
            spriteBatch.Draw(pixel, bounds, WindowColor);

            if (inset)
            {
                // Inset 3D effect (dark top/left, light bottom/right)
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Y),
                    new Vector2(bounds.Right, bounds.Y), DarkGray, 2);
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Y),
                    new Vector2(bounds.X, bounds.Bottom), DarkGray, 2);

                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Bottom),
                    new Vector2(bounds.Right, bounds.Bottom), White, 2);
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.Right, bounds.Y),
                    new Vector2(bounds.Right, bounds.Bottom), White, 2);
            }
            else
            {
                // Raised 3D effect
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Y),
                    new Vector2(bounds.Right, bounds.Y), White, 2);
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Y),
                    new Vector2(bounds.X, bounds.Bottom), White, 2);

                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.X, bounds.Bottom),
                    new Vector2(bounds.Right, bounds.Bottom), DarkGray, 2);
                DrawLine(spriteBatch, pixel,
                    new Vector2(bounds.Right, bounds.Y),
                    new Vector2(bounds.Right, bounds.Bottom), DarkGray, 2);
            }
        }

        /// <summary>
        /// Draw a retro-style checkbox
        /// </summary>
        public static void DrawCheckbox(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position,
            bool isChecked, string label, SpriteFont font, bool isHovered)
        {
            int size = 16;
            Rectangle box = new Rectangle((int)position.X, (int)position.Y, size, size);

            // Box background
            spriteBatch.Draw(pixel, box, Black);
            DrawRect(spriteBatch, pixel, box, isHovered ? Yellow : Cyan, 1);

            // Check mark (X)
            if (isChecked)
            {
                DrawLine(spriteBatch, pixel,
                    new Vector2(box.X + 2, box.Y + 2),
                    new Vector2(box.Right - 2, box.Bottom - 2), Yellow, 2);
                DrawLine(spriteBatch, pixel,
                    new Vector2(box.Right - 2, box.Y + 2),
                    new Vector2(box.X + 2, box.Bottom - 2), Yellow, 2);
            }

            // Label
            if (font != null && !string.IsNullOrEmpty(label))
            {
                Vector2 labelPos = new Vector2(position.X + size + 8, position.Y);
                spriteBatch.DrawString(font, label, labelPos, isHovered ? Yellow : White);
            }
        }

        /// <summary>
        /// Draw a retro progress bar
        /// </summary>
        public static void DrawProgressBar(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds,
            float progress, Color fillColor)
        {
            // Background
            spriteBatch.Draw(pixel, bounds, Black);
            DrawRect(spriteBatch, pixel, bounds, Cyan, 1);

            // Fill
            int fillWidth = (int)((bounds.Width - 4) * MathHelper.Clamp(progress, 0, 1));
            if (fillWidth > 0)
            {
                Rectangle fillRect = new Rectangle(bounds.X + 2, bounds.Y + 2, fillWidth, bounds.Height - 4);
                spriteBatch.Draw(pixel, fillRect, fillColor);
            }
        }

        /// <summary>
        /// Draw a scanline effect (CRT simulation)
        /// </summary>
        public static void DrawScanlines(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds,
            float intensity = 0.15f)
        {
            for (int y = bounds.Y; y < bounds.Bottom; y += 2)
            {
                Rectangle line = new Rectangle(bounds.X, y, bounds.Width, 1);
                spriteBatch.Draw(pixel, line, Color.Black * intensity);
            }
        }

        /// <summary>
        /// Draw CRT screen curve effect (vignette)
        /// </summary>
        public static void DrawCRTVignette(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds)
        {
            int vignetteSize = 50;

            // Top gradient
            for (int i = 0; i < vignetteSize; i++)
            {
                float alpha = (float)i / vignetteSize * 0.5f;
                Rectangle rect = new Rectangle(bounds.X, bounds.Y + i, bounds.Width, 1);
                spriteBatch.Draw(pixel, rect, Color.Black * alpha);
            }

            // Bottom gradient
            for (int i = 0; i < vignetteSize; i++)
            {
                float alpha = (float)i / vignetteSize * 0.5f;
                Rectangle rect = new Rectangle(bounds.X, bounds.Bottom - vignetteSize + i, bounds.Width, 1);
                spriteBatch.Draw(pixel, rect, Color.Black * alpha);
            }

            // Left gradient
            for (int i = 0; i < vignetteSize; i++)
            {
                float alpha = (float)i / vignetteSize * 0.5f;
                Rectangle rect = new Rectangle(bounds.X + i, bounds.Y, 1, bounds.Height);
                spriteBatch.Draw(pixel, rect, Color.Black * alpha);
            }

            // Right gradient
            for (int i = 0; i < vignetteSize; i++)
            {
                float alpha = (float)i / vignetteSize * 0.5f;
                Rectangle rect = new Rectangle(bounds.Right - vignetteSize + i, bounds.Y, 1, bounds.Height);
                spriteBatch.Draw(pixel, rect, Color.Black * alpha);
            }
        }

        /// <summary>
        /// Draw a retro tooltip
        /// </summary>
        public static void DrawTooltip(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position,
            string text, SpriteFont font)
        {
            if (string.IsNullOrEmpty(text) || font == null) return;

            Vector2 textSize = font.MeasureString(text);
            Rectangle bounds = new Rectangle(
                (int)position.X,
                (int)position.Y,
                (int)textSize.X + 16,
                (int)textSize.Y + 12
            );

            // Background
            spriteBatch.Draw(pixel, bounds, new Color(0, 0, 0, 240));

            // Border
            DrawRect(spriteBatch, pixel, bounds, Yellow, 2);

            // Text
            Vector2 textPos = new Vector2(bounds.X + 8, bounds.Y + 6);
            spriteBatch.DrawString(font, text, textPos, Yellow);
        }

        // Helper methods
        private static void DrawRect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect,
            Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start,
            Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();

            spriteBatch.Draw(pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)length, thickness),
                null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }

        /// <summary>
        /// Draw a retro-style separator line
        /// </summary>
        public static void DrawSeparator(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start,
            Vector2 end, bool doubleLine = false)
        {
            DrawLine(spriteBatch, pixel, start, end, Cyan, 1);
            if (doubleLine)
            {
                DrawLine(spriteBatch, pixel, start + new Vector2(0, 2), end + new Vector2(0, 2), Cyan, 1);
            }
        }

        /// <summary>
        /// Draw text with shadow (retro effect)
        /// </summary>
        public static void DrawTextWithShadow(SpriteBatch spriteBatch, SpriteFont font, string text,
            Vector2 position, Color color)
        {
            if (font == null || string.IsNullOrEmpty(text)) return;

            // Shadow
            spriteBatch.DrawString(font, text, position + new Vector2(2, 2), new Color(0, 0, 0, 128));
            // Text
            spriteBatch.DrawString(font, text, position, color);
        }

        /// <summary>
        /// Draw blinking text (for warnings/highlights)
        /// </summary>
        public static void DrawBlinkingText(SpriteBatch spriteBatch, SpriteFont font, string text,
            Vector2 position, GameTime gameTime, Color color1, Color color2)
        {
            if (font == null || string.IsNullOrEmpty(text)) return;

            double time = gameTime.TotalGameTime.TotalSeconds;
            Color color = ((int)(time * 2) % 2 == 0) ? color1 : color2;

            spriteBatch.DrawString(font, text, position, color);
        }
    }

    /// <summary>
    /// Undo/Redo system for editors
    /// </summary>
    public interface IEditorAction
    {
        void Execute();
        void Undo();
        string Description { get; }
    }

    public class EditorHistory
    {
        private Stack<IEditorAction> undoStack = new Stack<IEditorAction>();
        private Stack<IEditorAction> redoStack = new Stack<IEditorAction>();
        private const int MaxHistorySize = 100;

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public void ExecuteAction(IEditorAction action)
        {
            action.Execute();
            undoStack.Push(action);
            redoStack.Clear();

            // Limit history size
            if (undoStack.Count > MaxHistorySize)
            {
                var tempStack = new Stack<IEditorAction>();
                for (int i = 0; i < MaxHistorySize; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }
                undoStack = tempStack;
            }
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                var action = undoStack.Pop();
                action.Undo();
                redoStack.Push(action);
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                var action = redoStack.Pop();
                action.Execute();
                undoStack.Push(action);
            }
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        public string GetUndoDescription()
        {
            return undoStack.Count > 0 ? undoStack.Peek().Description : "Nothing to undo";
        }

        public string GetRedoDescription()
        {
            return redoStack.Count > 0 ? redoStack.Peek().Description : "Nothing to redo";
        }
    }
}
