# Platformer Engine - VGA-Style 2D Game Engine

A modern C# game engine built with MonoGame for creating classic-style platformer games with VGA-era aesthetics and parallax scrolling.

## Architecture Overview

This engine is designed around three core foundational systems:

### 1. **TileMap System** (`TileMap.cs`) - The Foundation
The most complex and critical component that handles:
- **Multi-layer tile rendering** - Background, main, foreground layers
- **Parallax scrolling** - Multiple background layers moving at different speeds for depth
- **Efficient rendering** - Only draws visible tiles (viewport culling)
- **Collision detection** - Tile-based physics with multiple collision types
- **Layer management** - Organize visual and collision layers

**Key Features:**
```csharp
// Multiple collision types
TileCollision.Solid      // Blocks all movement
TileCollision.Platform   // One-way platforms
TileCollision.Ladder     // Climbable
TileCollision.Deadly     // Spikes, lava
TileCollision.Water      // Affects physics
TileCollision.Ice        // Slippery surfaces

// Easy parallax setup
map.AddParallaxLayer(mountainsTexture, scrollSpeed: 0.2f);
map.AddParallaxLayer(cloudsTexture, scrollSpeed: 0.5f);
```

### 2. **Level Data System** (`LevelData.cs`)
JSON-based level format with:
- **Serialization/Deserialization** - Save and load levels from JSON
- **Compression** - Run-length encoding for tile data
- **Entity system** - Define spawn points, enemies, items with custom properties
- **Multiple layers** - Separate visual and collision layers
- **Parallax definitions** - Configure background layers in level file

**JSON Format:**
```json
{
  "name": "Level 1-1",
  "width": 100,
  "height": 20,
  "tileSize": 16,
  "tileset": "grassland.png",
  "parallaxLayers": [
    {
      "texture": "backgrounds/mountains.png",
      "scrollSpeedX": 0.2,
      "repeatX": true
    }
  ],
  "layers": [
    {
      "name": "Main",
      "data": [1, 2, 3, 4, ...]
    }
  ],
  "entities": [
    {
      "type": "PlayerSpawn",
      "x": 32,
      "y": 160
    }
  ]
}
```

### 3. **Camera System** (`Camera.cs`)
Smooth scrolling camera with:
- **Smooth following** - Lerp-based target tracking
- **Screen shake** - Impact effects
- **Deadzone** - Center area where camera doesn't move
- **Bounds clamping** - Prevent showing outside level
- **Coordinate conversion** - Screen ↔ World space
- **Zoom and rotation** support

## How It Works Together

```csharp
// In your Game class:
public class PlatformerGame : Game
{
    private SpriteBatch spriteBatch;
    private Camera camera;
    private TileMap level;
    private LevelData levelData;

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create camera
        camera = new Camera(GraphicsDevice.Viewport);

        // Load level data from JSON
        levelData = LevelData.Load("Content/Levels/level1.json");

        // Create tilemap
        level = new TileMap(levelData.Width, levelData.Height, levelData.TileSize);

        // Load tileset texture
        Texture2D tileset = Content.Load<Texture2D>(levelData.TilesetPath);
        level.LoadTileset(tileset);

        // Add parallax layers
        foreach (var parallaxData in levelData.ParallaxLayers)
        {
            Texture2D texture = Content.Load<Texture2D>(parallaxData.TexturePath);
            level.AddParallaxLayer(
                texture,
                parallaxData.ScrollSpeedX,
                parallaxData.ScrollSpeedY,
                parallaxData.RepeatX
            );
        }

        // Add tile layers
        foreach (var layerData in levelData.Layers)
        {
            TileLayer layer = level.AddLayer(layerData.Name, layerData.Depth);
            layer.Visible = layerData.Visible;
            layer.Opacity = layerData.Opacity;

            // Copy tile data
            for (int y = 0; y < levelData.Height; y++)
            {
                for (int x = 0; x < levelData.Width; x++)
                {
                    int tileIndex = layerData.Data[y * levelData.Width + x];
                    layer.SetTile(x, y, tileIndex);
                }
            }
        }

        // Setup collision from collision layer
        var collisionLayer = levelData.GetLayer("Collision");
        if (collisionLayer != null)
        {
            for (int y = 0; y < levelData.Height; y++)
            {
                for (int x = 0; x < levelData.Width; x++)
                {
                    int collisionValue = collisionLayer.Data[y * levelData.Width + x];
                    if (collisionValue > 0)
                    {
                        level.SetCollision(x, y, TileCollision.Solid);
                    }
                }
            }
        }

        // Set camera bounds to level size
        camera.SetBounds(level.PixelWidth, level.PixelHeight);

        // Focus on player spawn
        var playerSpawn = levelData.GetEntity("PlayerSpawn");
        if (playerSpawn != null)
        {
            camera.FocusOn(playerSpawn.Position);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        // Update camera (following player, screen shake, etc.)
        camera.Follow(playerPosition);
        camera.Update(gameTime);

        // Update parallax layers based on camera position
        level.Update(camera.Position);

        // Player collision example
        Rectangle playerBounds = new Rectangle((int)playerX, (int)playerY, 16, 16);
        if (level.CheckCollision(playerBounds, TileCollision.Solid))
        {
            // Handle collision
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(levelData.BackgroundColorValue);

        // Draw with camera transform
        spriteBatch.Begin(
            SpriteSortMode.BackToFront,
            BlendState.AlphaBlend,
            SamplerState.PointClamp, // Important for pixel art!
            null,
            null,
            null,
            camera.GetTransformMatrix()
        );

        // Draw level (includes parallax and tile layers)
        level.Draw(spriteBatch, camera.GetViewRectangle());

        // Draw player, enemies, etc.

        spriteBatch.End();

        base.Draw(gameTime);
    }
}
```

## Key Design Decisions

### Why This Architecture?

1. **Separation of Concerns**
   - `TileMap` handles rendering and collision
   - `LevelData` handles serialization
   - `Camera` handles viewport and scrolling
   - Each system is independent and testable

2. **Data-Driven Design**
   - Levels are JSON files (easy to edit, version control friendly)
   - Artists/designers can modify levels without touching code
   - Level editor can generate JSON directly

3. **Performance Optimized**
   - Viewport culling (only draws visible tiles)
   - Caches visible tile list between frames
   - Efficient collision detection (only checks nearby tiles)
   - Parallax updates only when camera moves

4. **Flexible Collision System**
   - Flag-based collision types (tiles can have multiple types)
   - Separate collision layer from visual layers
   - Helper methods for common collision checks
   - Returns detailed collision data for custom resolution

5. **Modern C# Features**
   - LINQ for clean queries
   - Properties for encapsulation
   - Nullable types for optional features
   - System.Text.Json for serialization (no external dependencies)

## What Makes `TileMap.cs` the Most Complex File?

**Lines of Code:** ~500+ lines
**Responsibilities:**
1. Manages multiple rendering systems (parallax, tiles, layers)
2. Implements spatial optimization (viewport culling)
3. Handles collision detection with multiple types
4. Supports debug visualization
5. Integrates with camera for scrolling
6. Efficiently manages large levels (1000+ tiles)

**Key Algorithms:**
- **Viewport Culling:** O(visible tiles) instead of O(all tiles)
- **Collision Detection:** Spatial partitioning to only check nearby tiles
- **Parallax Scrolling:** Multi-layer depth simulation
- **Layer Sorting:** Automatic depth-based rendering order

## Next Steps

To build on this foundation, you would add:

1. **Entity System** (`Entity.cs`, `Player.cs`, `Enemy.cs`)
2. **Physics System** (`PhysicsEngine.cs` for gravity, jumping, collision resolution)
3. **Input System** (`InputManager.cs`)
4. **Animation System** (`AnimatedSprite.cs`)
5. **Audio System** (`AudioManager.cs`)
6. **Level Editor** (separate application or in-game tool)

## Building the Project

**Requirements:**
- .NET 6.0 or higher
- MonoGame 3.8.1+

**Project Structure:**
```
Keener/
├── PlatformerEngine.Core/        # Engine library
│   ├── Graphics/
│   │   ├── TileMap.cs           ← Most complex foundational file
│   │   ├── Camera.cs
│   │   └── ParallaxLayer.cs
│   ├── Data/
│   │   └── LevelData.cs
│   └── Physics/
│       └── (future)
├── PlatformerEngine.Game/        # Game implementation
│   ├── Entities/
│   ├── States/
│   └── Program.cs
└── PlatformerEngine.Editor/      # Level editor
    └── (future)
```

## License

Educational project for learning game engine architecture.

---

**The foundation is built.** Ready to add player physics, enemies, and gameplay!
