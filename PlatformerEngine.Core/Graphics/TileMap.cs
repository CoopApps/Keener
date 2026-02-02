using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatformerEngine.Core.Graphics
{
    /// <summary>
    /// Represents a single parallax background layer that scrolls at a different speed than the main camera.
    /// This creates depth illusion in 2D platformers.
    /// </summary>
    public class ParallaxLayer
    {
        public Texture2D Texture { get; set; }
        public float ScrollSpeedX { get; set; } = 1.0f;  // 1.0 = matches camera, 0.5 = half speed, 2.0 = double speed
        public float ScrollSpeedY { get; set; } = 1.0f;
        public bool RepeatX { get; set; } = true;
        public bool RepeatY { get; set; } = false;
        public Vector2 Offset { get; set; } = Vector2.Zero;
        public Color Tint { get; set; } = Color.White;
        public float Depth { get; set; } = 0.0f; // 0.0 = back, 1.0 = front for layering

        private Vector2 currentOffset = Vector2.Zero;

        /// <summary>
        /// Update parallax offset based on camera position
        /// </summary>
        public void Update(Vector2 cameraPosition)
        {
            currentOffset = (cameraPosition * new Vector2(ScrollSpeedX, ScrollSpeedY)) + Offset;
        }

        /// <summary>
        /// Draw the parallax layer with proper tiling and offset
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle viewport)
        {
            if (Texture == null) return;

            if (RepeatX || RepeatY)
            {
                // Calculate how many tiles we need to draw
                int startX = RepeatX ? (int)(currentOffset.X / Texture.Width) - 1 : 0;
                int startY = RepeatY ? (int)(currentOffset.Y / Texture.Height) - 1 : 0;
                int endX = RepeatX ? startX + (viewport.Width / Texture.Width) + 3 : 1;
                int endY = RepeatY ? startY + (viewport.Height / Texture.Height) + 3 : 1;

                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        Vector2 position = new Vector2(
                            x * Texture.Width - (RepeatX ? currentOffset.X % Texture.Width : currentOffset.X),
                            y * Texture.Height - (RepeatY ? currentOffset.Y % Texture.Height : currentOffset.Y)
                        );

                        spriteBatch.Draw(
                            Texture,
                            position,
                            null,
                            Tint,
                            0f,
                            Vector2.Zero,
                            1.0f,
                            SpriteEffects.None,
                            Depth
                        );
                    }
                }
            }
            else
            {
                // Single image, no tiling
                spriteBatch.Draw(
                    Texture,
                    -currentOffset,
                    null,
                    Tint,
                    0f,
                    Vector2.Zero,
                    1.0f,
                    SpriteEffects.None,
                    Depth
                );
            }
        }
    }

    /// <summary>
    /// Represents a single layer of tiles in the level (background, main, foreground, collision, etc.)
    /// </summary>
    public class TileLayer
    {
        public string Name { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int[] Tiles { get; private set; }
        public bool Visible { get; set; } = true;
        public float Opacity { get; set; } = 1.0f;
        public float Depth { get; set; } = 0.5f; // Layer depth for rendering order

        public TileLayer(string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
            Tiles = new int[width * height];
        }

        /// <summary>
        /// Get tile at position (returns -1 if out of bounds)
        /// </summary>
        public int GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return -1;

            return Tiles[y * Width + x];
        }

        /// <summary>
        /// Set tile at position
        /// </summary>
        public void SetTile(int x, int y, int tileIndex)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            Tiles[y * Width + x] = tileIndex;
        }
    }

    /// <summary>
    /// Tile collision types for physics interaction
    /// </summary>
    [Flags]
    public enum TileCollision
    {
        None = 0,
        Solid = 1,           // Blocks movement in all directions
        Platform = 2,        // Only blocks downward movement (one-way platform)
        Ladder = 4,          // Allows climbing
        Deadly = 8,          // Hurts/kills player
        Water = 16,          // Changes movement physics
        Ice = 32,            // Slippery surface
        Slope = 64,          // Angled surface (requires additional data)
    }

    /// <summary>
    /// Main TileMap class - handles level rendering, parallax, and collision detection.
    /// This is the foundation of the platformer engine.
    /// </summary>
    public class TileMap
    {
        // Tileset properties
        public Texture2D Tileset { get; private set; }
        public int TileSize { get; private set; } = 16;
        public int TilesetColumns { get; private set; }
        public int TilesetRows { get; private set; }

        // Map dimensions
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int PixelWidth => Width * TileSize;
        public int PixelHeight => Height * TileSize;

        // Layers
        public List<TileLayer> Layers { get; private set; } = new List<TileLayer>();
        public List<ParallaxLayer> ParallaxLayers { get; private set; } = new List<ParallaxLayer>();

        // Collision data
        private TileCollision[] collisionMap;

        // Rendering optimization
        private Rectangle lastVisibleBounds;
        private List<Point> visibleTiles = new List<Point>();

        /// <summary>
        /// Create a new tilemap
        /// </summary>
        public TileMap(int width, int height, int tileSize = 16)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            collisionMap = new TileCollision[width * height];
        }

        /// <summary>
        /// Load tileset texture and calculate dimensions
        /// </summary>
        public void LoadTileset(Texture2D tileset)
        {
            Tileset = tileset;
            TilesetColumns = tileset.Width / TileSize;
            TilesetRows = tileset.Height / TileSize;
        }

        /// <summary>
        /// Add a tile layer to the map
        /// </summary>
        public TileLayer AddLayer(string name, float depth = 0.5f)
        {
            var layer = new TileLayer(name, Width, Height) { Depth = depth };
            Layers.Add(layer);

            // Sort layers by depth
            Layers = Layers.OrderBy(l => l.Depth).ToList();

            return layer;
        }

        /// <summary>
        /// Get layer by name
        /// </summary>
        public TileLayer GetLayer(string name)
        {
            return Layers.FirstOrDefault(l => l.Name == name);
        }

        /// <summary>
        /// Add a parallax background layer
        /// </summary>
        public ParallaxLayer AddParallaxLayer(Texture2D texture, float scrollSpeedX, float scrollSpeedY = 1.0f, bool repeatX = true, bool repeatY = false)
        {
            var layer = new ParallaxLayer
            {
                Texture = texture,
                ScrollSpeedX = scrollSpeedX,
                ScrollSpeedY = scrollSpeedY,
                RepeatX = repeatX,
                RepeatY = repeatY,
                Depth = ParallaxLayers.Count * 0.01f // Stack parallax layers behind tiles
            };

            ParallaxLayers.Add(layer);

            // Sort by depth
            ParallaxLayers = ParallaxLayers.OrderBy(l => l.Depth).ToList();

            return layer;
        }

        /// <summary>
        /// Set collision type for a tile
        /// </summary>
        public void SetCollision(int x, int y, TileCollision collision)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            collisionMap[y * Width + x] = collision;
        }

        /// <summary>
        /// Get collision type at world position
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return TileCollision.None;

            return collisionMap[y * Width + x];
        }

        /// <summary>
        /// Get collision at pixel position
        /// </summary>
        public TileCollision GetCollisionAtPixel(float x, float y)
        {
            return GetCollision((int)(x / TileSize), (int)(y / TileSize));
        }

        /// <summary>
        /// Check if a rectangle collides with solid tiles
        /// </summary>
        public bool CheckCollision(Rectangle bounds, TileCollision collisionType = TileCollision.Solid)
        {
            // Convert pixel bounds to tile coordinates
            int startX = Math.Max(0, bounds.Left / TileSize);
            int endX = Math.Min(Width - 1, bounds.Right / TileSize);
            int startY = Math.Max(0, bounds.Top / TileSize);
            int endY = Math.Min(Height - 1, bounds.Bottom / TileSize);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if ((GetCollision(x, y) & collisionType) != 0)
                    {
                        // Check if the tile actually intersects the bounds
                        Rectangle tileBounds = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                        if (bounds.Intersects(tileBounds))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get all tiles that intersect with a bounds (for detailed collision resolution)
        /// </summary>
        public IEnumerable<(int x, int y, TileCollision collision)> GetIntersectingTiles(Rectangle bounds, TileCollision collisionType = TileCollision.Solid)
        {
            int startX = Math.Max(0, bounds.Left / TileSize);
            int endX = Math.Min(Width - 1, bounds.Right / TileSize);
            int startY = Math.Max(0, bounds.Top / TileSize);
            int endY = Math.Min(Height - 1, bounds.Bottom / TileSize);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    TileCollision collision = GetCollision(x, y);
                    if ((collision & collisionType) != 0)
                    {
                        Rectangle tileBounds = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                        if (bounds.Intersects(tileBounds))
                        {
                            yield return (x, y, collision);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update parallax layers based on camera position
        /// </summary>
        public void Update(Vector2 cameraPosition)
        {
            foreach (var parallaxLayer in ParallaxLayers)
            {
                parallaxLayer.Update(cameraPosition);
            }
        }

        /// <summary>
        /// Calculate which tiles are visible in the viewport (optimization)
        /// </summary>
        private void CalculateVisibleTiles(Rectangle viewport)
        {
            // Only recalculate if viewport changed significantly
            if (Math.Abs(viewport.X - lastVisibleBounds.X) < TileSize &&
                Math.Abs(viewport.Y - lastVisibleBounds.Y) < TileSize &&
                viewport.Width == lastVisibleBounds.Width &&
                viewport.Height == lastVisibleBounds.Height)
            {
                return; // Use cached visible tiles
            }

            lastVisibleBounds = viewport;
            visibleTiles.Clear();

            // Add padding to see tiles just outside viewport (prevents pop-in)
            int padding = 2;
            int startX = Math.Max(0, (viewport.X / TileSize) - padding);
            int endX = Math.Min(Width - 1, ((viewport.X + viewport.Width) / TileSize) + padding);
            int startY = Math.Max(0, (viewport.Y / TileSize) - padding);
            int endY = Math.Min(Height - 1, ((viewport.Y + viewport.Height) / TileSize) + padding);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    visibleTiles.Add(new Point(x, y));
                }
            }
        }

        /// <summary>
        /// Draw the tilemap with parallax layers
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle viewport)
        {
            if (Tileset == null) return;

            // Calculate visible tiles for optimization
            CalculateVisibleTiles(viewport);

            // Draw parallax backgrounds first
            foreach (var parallaxLayer in ParallaxLayers)
            {
                parallaxLayer.Draw(spriteBatch, viewport);
            }

            // Draw tile layers
            foreach (var layer in Layers)
            {
                if (!layer.Visible) continue;

                Color tint = Color.White * layer.Opacity;

                // Only draw visible tiles
                foreach (var tilePos in visibleTiles)
                {
                    int tileIndex = layer.GetTile(tilePos.X, tilePos.Y);

                    // Skip empty tiles (index 0 or -1)
                    if (tileIndex <= 0) continue;

                    // Calculate source rectangle in tileset
                    int tileX = (tileIndex % TilesetColumns) * TileSize;
                    int tileY = (tileIndex / TilesetColumns) * TileSize;
                    Rectangle sourceRect = new Rectangle(tileX, tileY, TileSize, TileSize);

                    // Calculate destination position (world space)
                    Vector2 position = new Vector2(tilePos.X * TileSize, tilePos.Y * TileSize);

                    // Draw the tile
                    spriteBatch.Draw(
                        Tileset,
                        position,
                        sourceRect,
                        tint,
                        0f,
                        Vector2.Zero,
                        1.0f,
                        SpriteEffects.None,
                        layer.Depth
                    );
                }
            }
        }

        /// <summary>
        /// Draw collision debug overlay
        /// </summary>
        public void DrawCollisionDebug(SpriteBatch spriteBatch, Rectangle viewport, Texture2D pixelTexture)
        {
            CalculateVisibleTiles(viewport);

            foreach (var tilePos in visibleTiles)
            {
                TileCollision collision = GetCollision(tilePos.X, tilePos.Y);

                if (collision == TileCollision.None) continue;

                Rectangle tileBounds = new Rectangle(tilePos.X * TileSize, tilePos.Y * TileSize, TileSize, TileSize);

                // Choose color based on collision type
                Color debugColor = collision switch
                {
                    TileCollision.Solid => new Color(255, 0, 0, 100),      // Red
                    TileCollision.Platform => new Color(0, 255, 0, 100),   // Green
                    TileCollision.Ladder => new Color(255, 255, 0, 100),   // Yellow
                    TileCollision.Deadly => new Color(255, 0, 255, 100),   // Magenta
                    TileCollision.Water => new Color(0, 0, 255, 100),      // Blue
                    TileCollision.Ice => new Color(0, 255, 255, 100),      // Cyan
                    _ => new Color(128, 128, 128, 100)                      // Gray
                };

                spriteBatch.Draw(pixelTexture, tileBounds, debugColor);
            }
        }

        /// <summary>
        /// Clear all map data
        /// </summary>
        public void Clear()
        {
            Layers.Clear();
            ParallaxLayers.Clear();
            Array.Clear(collisionMap, 0, collisionMap.Length);
            visibleTiles.Clear();
        }
    }
}
