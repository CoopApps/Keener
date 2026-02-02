using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace PlatformerEngine.Core.Graphics
{
    /// <summary>
    /// 2D Camera for scrolling platformer levels with smooth following and screen shake
    /// </summary>
    public class Camera
    {
        private Vector2 position;
        private Vector2 targetPosition;
        private Vector2 shakeOffset;
        private float shakeIntensity;
        private float shakeDuration;
        private Random random = new Random();

        // Viewport properties
        public Viewport Viewport { get; private set; }
        public int ViewportWidth => Viewport.Width;
        public int ViewportHeight => Viewport.Height;

        // Camera properties
        public Vector2 Position
        {
            get => position + shakeOffset;
            set => position = value;
        }

        public float X
        {
            get => Position.X;
            set => position.X = value;
        }

        public float Y
        {
            get => Position.Y;
            set => position.Y = value;
        }

        public float Zoom { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0f;

        // Following behavior
        public bool SmoothFollow { get; set; } = true;
        public float FollowSpeed { get; set; } = 5.0f;
        public Vector2 FollowOffset { get; set; } = Vector2.Zero;

        // Camera bounds (to prevent showing areas outside the level)
        public Rectangle? Bounds { get; set; }
        public bool ClampToBounds { get; set; } = true;

        // Deadzone (area where target can move without camera following)
        public Rectangle? Deadzone { get; set; }
        public bool UseDeadzone { get; set; } = false;

        /// <summary>
        /// Create a new camera
        /// </summary>
        public Camera(Viewport viewport)
        {
            Viewport = viewport;
            position = Vector2.Zero;
            targetPosition = Vector2.Zero;
        }

        /// <summary>
        /// Get the transformation matrix for SpriteBatch
        /// </summary>
        public Matrix GetTransformMatrix()
        {
            return
                Matrix.CreateTranslation(new Vector3(-Position, 0)) *
                Matrix.CreateRotationZ(Rotation) *
                Matrix.CreateScale(Zoom, Zoom, 1) *
                Matrix.CreateTranslation(new Vector3(ViewportWidth * 0.5f, ViewportHeight * 0.5f, 0));
        }

        /// <summary>
        /// Get the view rectangle in world space
        /// </summary>
        public Rectangle GetViewRectangle()
        {
            Vector2 topLeft = ScreenToWorld(Vector2.Zero);
            Vector2 bottomRight = ScreenToWorld(new Vector2(ViewportWidth, ViewportHeight));

            return new Rectangle(
                (int)topLeft.X,
                (int)topLeft.Y,
                (int)(bottomRight.X - topLeft.X),
                (int)(bottomRight.Y - topLeft.Y)
            );
        }

        /// <summary>
        /// Convert screen coordinates to world coordinates
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(GetTransformMatrix()));
        }

        /// <summary>
        /// Convert world coordinates to screen coordinates
        /// </summary>
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, GetTransformMatrix());
        }

        /// <summary>
        /// Check if a world-space rectangle is visible to the camera
        /// </summary>
        public bool IsVisible(Rectangle worldBounds)
        {
            return GetViewRectangle().Intersects(worldBounds);
        }

        /// <summary>
        /// Focus camera on a position immediately (no smoothing)
        /// </summary>
        public void FocusOn(Vector2 worldPosition)
        {
            position = worldPosition + FollowOffset;
            targetPosition = position;
            ClampPosition();
        }

        /// <summary>
        /// Set camera target for smooth following
        /// </summary>
        public void Follow(Vector2 worldPosition)
        {
            Vector2 targetPos = worldPosition + FollowOffset;

            // Check deadzone
            if (UseDeadzone && Deadzone.HasValue)
            {
                Rectangle deadzone = Deadzone.Value;
                deadzone.Offset((int)position.X, (int)position.Y);

                // Only move camera if target is outside deadzone
                if (deadzone.Contains(worldPosition))
                {
                    return; // Target is in deadzone, don't move camera
                }
            }

            targetPosition = targetPos;
        }

        /// <summary>
        /// Create screen shake effect
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            shakeIntensity = Math.Max(shakeIntensity, intensity);
            shakeDuration = Math.Max(shakeDuration, duration);
        }

        /// <summary>
        /// Update camera position and effects
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update smooth following
            if (SmoothFollow)
            {
                position = Vector2.Lerp(position, targetPosition, FollowSpeed * deltaTime);
            }
            else
            {
                position = targetPosition;
            }

            // Update screen shake
            if (shakeDuration > 0)
            {
                shakeDuration -= deltaTime;

                if (shakeDuration <= 0)
                {
                    shakeOffset = Vector2.Zero;
                    shakeIntensity = 0;
                }
                else
                {
                    // Random offset based on intensity
                    shakeOffset = new Vector2(
                        (float)(random.NextDouble() * 2 - 1) * shakeIntensity,
                        (float)(random.NextDouble() * 2 - 1) * shakeIntensity
                    );
                }
            }

            ClampPosition();
        }

        /// <summary>
        /// Clamp camera position to bounds
        /// </summary>
        private void ClampPosition()
        {
            if (!ClampToBounds || !Bounds.HasValue)
                return;

            Rectangle bounds = Bounds.Value;

            // Calculate half viewport size in world space
            float halfWidth = (ViewportWidth / Zoom) * 0.5f;
            float halfHeight = (ViewportHeight / Zoom) * 0.5f;

            // Clamp position
            position.X = MathHelper.Clamp(position.X, bounds.Left + halfWidth, bounds.Right - halfWidth);
            position.Y = MathHelper.Clamp(position.Y, bounds.Top + halfHeight, bounds.Bottom - halfHeight);

            // Also clamp target position
            targetPosition.X = MathHelper.Clamp(targetPosition.X, bounds.Left + halfWidth, bounds.Right - halfWidth);
            targetPosition.Y = MathHelper.Clamp(targetPosition.Y, bounds.Top + halfHeight, bounds.Bottom - halfHeight);
        }

        /// <summary>
        /// Set camera bounds to match level size
        /// </summary>
        public void SetBounds(int levelWidth, int levelHeight)
        {
            Bounds = new Rectangle(0, 0, levelWidth, levelHeight);
        }

        /// <summary>
        /// Reset camera to default state
        /// </summary>
        public void Reset()
        {
            position = Vector2.Zero;
            targetPosition = Vector2.Zero;
            shakeOffset = Vector2.Zero;
            shakeIntensity = 0;
            shakeDuration = 0;
            Zoom = 1.0f;
            Rotation = 0f;
        }
    }
}
