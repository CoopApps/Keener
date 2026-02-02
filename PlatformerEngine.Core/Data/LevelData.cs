using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformerEngine.Core.Data
{
    /// <summary>
    /// Serializable parallax layer data
    /// </summary>
    public class ParallaxLayerData
    {
        [JsonPropertyName("texture")]
        public string TexturePath { get; set; }

        [JsonPropertyName("scrollSpeedX")]
        public float ScrollSpeedX { get; set; } = 1.0f;

        [JsonPropertyName("scrollSpeedY")]
        public float ScrollSpeedY { get; set; } = 1.0f;

        [JsonPropertyName("repeatX")]
        public bool RepeatX { get; set; } = true;

        [JsonPropertyName("repeatY")]
        public bool RepeatY { get; set; } = false;

        [JsonPropertyName("offsetX")]
        public float OffsetX { get; set; } = 0f;

        [JsonPropertyName("offsetY")]
        public float OffsetY { get; set; } = 0f;
    }

    /// <summary>
    /// Serializable tile layer data
    /// </summary>
    public class TileLayerData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; } = true;

        [JsonPropertyName("opacity")]
        public float Opacity { get; set; } = 1.0f;

        [JsonPropertyName("depth")]
        public float Depth { get; set; } = 0.5f;

        [JsonPropertyName("data")]
        public int[] Data { get; set; }

        [JsonPropertyName("compressed")]
        public bool Compressed { get; set; } = false;

        // For run-length encoding compression
        [JsonPropertyName("compressedData")]
        public List<CompressedRun> CompressedData { get; set; }
    }

    /// <summary>
    /// Run-length encoding for tile data compression
    /// </summary>
    public class CompressedRun
    {
        [JsonPropertyName("tile")]
        public int Tile { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Serializable entity data (player spawn, enemies, items, etc.)
    /// </summary>
    public class EntityData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        // Helper to get Vector2 position
        [JsonIgnore]
        public Vector2 Position => new Vector2(X, Y);
    }

    /// <summary>
    /// Complete level data - serializes to/from JSON
    /// </summary>
    public class LevelData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("tileSize")]
        public int TileSize { get; set; } = 16;

        [JsonPropertyName("tileset")]
        public string TilesetPath { get; set; }

        [JsonPropertyName("backgroundColor")]
        public string BackgroundColor { get; set; } = "#87CEEB"; // Sky blue

        [JsonPropertyName("music")]
        public string MusicPath { get; set; }

        [JsonPropertyName("parallaxLayers")]
        public List<ParallaxLayerData> ParallaxLayers { get; set; } = new List<ParallaxLayerData>();

        [JsonPropertyName("layers")]
        public List<TileLayerData> Layers { get; set; } = new List<TileLayerData>();

        [JsonPropertyName("collisionLayer")]
        public string CollisionLayerName { get; set; } = "Collision";

        [JsonPropertyName("entities")]
        public List<EntityData> Entities { get; set; } = new List<EntityData>();

        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Parse background color from hex string to Color
        /// </summary>
        [JsonIgnore]
        public Color BackgroundColorValue
        {
            get
            {
                try
                {
                    string hex = BackgroundColor.TrimStart('#');
                    if (hex.Length == 6)
                    {
                        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                        return new Color(r, g, b);
                    }
                }
                catch { }
                return Color.CornflowerBlue;
            }
        }

        /// <summary>
        /// Save level to JSON file
        /// </summary>
        public void Save(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Load level from JSON file
        /// </summary>
        public static LevelData Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Level file not found: {filePath}");
            }

            string json = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var levelData = JsonSerializer.Deserialize<LevelData>(json, options);

            // Decompress any compressed layers
            foreach (var layer in levelData.Layers)
            {
                if (layer.Compressed && layer.CompressedData != null)
                {
                    layer.Data = DecompressLayer(layer.CompressedData, levelData.Width * levelData.Height);
                }
            }

            return levelData;
        }

        /// <summary>
        /// Decompress run-length encoded tile data
        /// </summary>
        private static int[] DecompressLayer(List<CompressedRun> compressedData, int expectedSize)
        {
            List<int> decompressed = new List<int>(expectedSize);

            foreach (var run in compressedData)
            {
                for (int i = 0; i < run.Count; i++)
                {
                    decompressed.Add(run.Tile);
                }
            }

            // Pad with zeros if needed
            while (decompressed.Count < expectedSize)
            {
                decompressed.Add(0);
            }

            return decompressed.ToArray();
        }

        /// <summary>
        /// Compress tile data using run-length encoding
        /// </summary>
        public static List<CompressedRun> CompressLayer(int[] data)
        {
            var compressed = new List<CompressedRun>();

            if (data == null || data.Length == 0)
                return compressed;

            int currentTile = data[0];
            int count = 1;

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] == currentTile)
                {
                    count++;
                }
                else
                {
                    compressed.Add(new CompressedRun { Tile = currentTile, Count = count });
                    currentTile = data[i];
                    count = 1;
                }
            }

            // Add the last run
            compressed.Add(new CompressedRun { Tile = currentTile, Count = count });

            return compressed;
        }

        /// <summary>
        /// Get entity by type
        /// </summary>
        public EntityData GetEntity(string type)
        {
            return Entities.Find(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all entities of a specific type
        /// </summary>
        public List<EntityData> GetEntitiesByType(string type)
        {
            return Entities.FindAll(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get layer by name
        /// </summary>
        public TileLayerData GetLayer(string name)
        {
            return Layers.Find(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Create a simple test level
        /// </summary>
        public static LevelData CreateTestLevel()
        {
            var level = new LevelData
            {
                Name = "Test Level",
                Width = 50,
                Height = 20,
                TileSize = 16,
                TilesetPath = "tilesets/test.png",
                BackgroundColor = "#87CEEB"
            };

            // Add parallax backgrounds
            level.ParallaxLayers.Add(new ParallaxLayerData
            {
                TexturePath = "backgrounds/mountains.png",
                ScrollSpeedX = 0.2f,
                ScrollSpeedY = 0.5f,
                RepeatX = true
            });

            level.ParallaxLayers.Add(new ParallaxLayerData
            {
                TexturePath = "backgrounds/clouds.png",
                ScrollSpeedX = 0.5f,
                ScrollSpeedY = 0.3f,
                RepeatX = true
            });

            // Add main tile layer
            var mainLayer = new TileLayerData
            {
                Name = "Main",
                Depth = 0.5f,
                Data = new int[level.Width * level.Height]
            };

            // Create a simple ground
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 15; y < level.Height; y++)
                {
                    mainLayer.Data[y * level.Width + x] = 1; // Ground tile
                }
            }

            // Add some platforms
            for (int x = 10; x < 15; x++)
            {
                mainLayer.Data[12 * level.Width + x] = 2; // Platform tile
            }

            level.Layers.Add(mainLayer);

            // Add collision layer
            var collisionLayer = new TileLayerData
            {
                Name = "Collision",
                Visible = false,
                Data = new int[level.Width * level.Height]
            };

            // Mark ground as solid (1 = solid)
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 15; y < level.Height; y++)
                {
                    collisionLayer.Data[y * level.Width + x] = 1;
                }
            }

            level.Layers.Add(collisionLayer);

            // Add entities
            level.Entities.Add(new EntityData
            {
                Type = "PlayerSpawn",
                X = 32,
                Y = 200
            });

            level.Entities.Add(new EntityData
            {
                Type = "Enemy",
                X = 200,
                Y = 200,
                Properties = new Dictionary<string, object>
                {
                    { "enemyType", "Goomba" },
                    { "patrolDistance", 64 }
                }
            });

            return level;
        }
    }
}
