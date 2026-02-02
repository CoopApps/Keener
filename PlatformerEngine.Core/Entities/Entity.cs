using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Graphics;
using System;

namespace PlatformerEngine.Core.Entities
{
    /// <summary>
    /// Base class for all game entities (player, enemies, items, etc.)
    /// </summary>
    public abstract class Entity
    {
        // Position and movement
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Acceleration { get; set; }

        // Dimensions
        public int Width { get; set; }
        public int Height { get; set; }

        // Collision bounds (can be smaller than sprite for better feel)
        public Rectangle Bounds
        {
            get => new Rectangle(
                (int)(Position.X + BoundsOffset.X),
                (int)(Position.Y + BoundsOffset.Y),
                BoundsWidth,
                BoundsHeight
            );
        }

        public Vector2 BoundsOffset { get; set; } = Vector2.Zero;
        public int BoundsWidth { get; set; }
        public int BoundsHeight { get; set; }

        // Center point
        public Vector2 Center => Position + new Vector2(Width / 2f, Height / 2f);

        // Sprite rendering
        public Texture2D Sprite { get; set; }
        public Rectangle? SourceRectangle { get; set; }
        public Color TintColor { get; set; } = Color.White;
        public SpriteEffects SpriteEffect { get; set; } = SpriteEffects.None;
        public float Rotation { get; set; } = 0f;
        public Vector2 Origin { get; set; } = Vector2.Zero;
        public float LayerDepth { get; set; } = 0.5f;

        // State
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public bool IsSolid { get; set; } = true;

        // Gravity
        public bool UseGravity { get; set; } = true;
        public float GravityScale { get; set; } = 1.0f;

        // Ground detection
        public bool IsOnGround { get; set; }
        public bool IsOnPlatform { get; set; }
        public bool IsAgainstWall { get; set; }
        public bool IsAgainstCeiling { get; set; }

        // Direction
        public int FacingDirection { get; set; } = 1; // 1 = right, -1 = left

        protected Entity()
        {
            // Default to using full sprite as bounds
            BoundsWidth = Width;
            BoundsHeight = Height;
        }

        /// <summary>
        /// Update entity logic
        /// </summary>
        public virtual void Update(GameTime gameTime, TileMap tileMap)
        {
            if (!IsActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Apply acceleration to velocity
            Velocity += Acceleration * deltaTime;

            // Reset acceleration (needs to be reapplied each frame)
            Acceleration = Vector2.Zero;
        }

        /// <summary>
        /// Draw the entity
        /// </summary>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible || Sprite == null) return;

            spriteBatch.Draw(
                Sprite,
                Position,
                SourceRectangle,
                TintColor,
                Rotation,
                Origin,
                1.0f,
                SpriteEffect,
                LayerDepth
            );
        }

        /// <summary>
        /// Draw debug visualization
        /// </summary>
        public virtual void DrawDebug(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (!IsVisible) return;

            // Draw bounds
            spriteBatch.Draw(pixelTexture, Bounds, new Color(0, 255, 0, 100));

            // Draw position point
            spriteBatch.Draw(pixelTexture, new Rectangle((int)Position.X - 2, (int)Position.Y - 2, 4, 4), Color.Red);

            // Draw center point
            Vector2 center = Center;
            spriteBatch.Draw(pixelTexture, new Rectangle((int)center.X - 2, (int)center.Y - 2, 4, 4), Color.Blue);
        }

        /// <summary>
        /// Check if this entity overlaps with another
        /// </summary>
        public bool Overlaps(Entity other)
        {
            return Bounds.Intersects(other.Bounds);
        }

        /// <summary>
        /// Check if this entity overlaps with a rectangle
        /// </summary>
        public bool Overlaps(Rectangle rect)
        {
            return Bounds.Intersects(rect);
        }

        /// <summary>
        /// Get distance to another entity
        /// </summary>
        public float DistanceTo(Entity other)
        {
            return Vector2.Distance(Center, other.Center);
        }

        /// <summary>
        /// Get direction vector to another entity
        /// </summary>
        public Vector2 DirectionTo(Entity other)
        {
            Vector2 direction = other.Center - Center;
            if (direction != Vector2.Zero)
                direction.Normalize();
            return direction;
        }

        /// <summary>
        /// Called when entity collides with a tile
        /// </summary>
        public virtual void OnTileCollision(int tileX, int tileY, TileCollision collision)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when entity collides with another entity
        /// </summary>
        public virtual void OnEntityCollision(Entity other)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when entity takes damage
        /// </summary>
        public virtual void OnDamage(int damage, Entity source)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when entity is destroyed
        /// </summary>
        public virtual void OnDestroy()
        {
            IsActive = false;
            // Override in derived classes for cleanup
        }
    }
}
