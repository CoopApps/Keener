# Platformer Engine - VGA-Style 2D Game Engine

A complete, modern C# game engine built with MonoGame for creating classic-style platformer games with VGA-era aesthetics and parallax scrolling.

## Features

✨ **Complete platformer engine** with player physics, collision detection, and animation
🎨 **Multi-layer parallax scrolling** for depth and atmosphere
🎮 **Full input system** supporting keyboard, gamepad, and action mappings
📦 **JSON-based level format** for easy level design
🔧 **Extensible entity system** for players, enemies, and items
⚡ **Optimized rendering** with viewport culling
🎬 **Sprite animation system** with sprite sheet support

---

## Architecture Overview

### Core Systems

#### 1. **TileMap System** (`TileMap.cs`) - 500+ lines
The rendering and collision foundation:
- Multi-layer tile rendering (background, main, foreground)
- Parallax scrolling with configurable scroll speeds
- Viewport culling (only draws visible tiles)
- Collision detection with 7 tile types
- Debug visualization

```csharp
// Collision types available
TileCollision.Solid      // Blocks all movement
TileCollision.Platform   // One-way platforms (jump through)
TileCollision.Ladder     // Climbable surfaces
TileCollision.Deadly     // Spikes, lava, hazards
TileCollision.Water      // Affects physics
TileCollision.Ice        // Slippery surfaces
TileCollision.Slope      // Angled surfaces
```

#### 2. **Level Data System** (`LevelData.cs`)
JSON-based level format:
- Serialization/deserialization
- Run-length encoding compression
- Entity definitions (spawns, enemies, items)
- Multiple layers with properties
- Parallax layer configuration

**Example Level JSON:**
```json
{
  "name": "Level 1-1",
  "width": 100,
  "height": 20,
  "tileSize": 16,
  "tileset": "grassland.png",
  "backgroundColor": "#87CEEB",
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
      "depth": 0.5,
      "data": [1, 2, 3, ...]
    },
    {
      "name": "Collision",
      "visible": false,
      "data": [1, 1, 0, ...]
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

#### 3. **Camera System** (`Camera.cs`)
Professional 2D camera:
- Smooth following with lerp
- Screen shake effects
- Deadzone support
- Bounds clamping
- World ↔ Screen coordinate conversion
- Zoom and rotation

```csharp
camera.Follow(player.Center);
camera.Shake(intensity: 5f, duration: 0.3f);
Vector2 worldPos = camera.ScreenToWorld(mousePosition);
```

#### 4. **Input System** (`InputManager.cs`)
Comprehensive input management:
- Keyboard support
- Gamepad support (with vibration)
- Mouse input
- Action mapping system
- Analog stick support with deadzone
- Customizable key bindings

```csharp
// Action-based input (platform-agnostic)
if (input.IsActionPressed(InputAction.Jump))
    player.Jump();

float horizontal = input.GetHorizontalAxis(); // -1 to 1

// Or raw input
if (input.IsKeyPressed(Keys.Space))
    DoSomething();
```

#### 5. **Entity System** (`Entity.cs`, `Player.cs`)
Base entity framework:
- Position, velocity, acceleration
- Collision bounds with offsets
- Sprite rendering
- Ground/wall/ceiling detection
- Virtual methods for overriding behavior

**Player Physics:**
- Variable jump height (hold jump for higher jumps)
- Smooth acceleration/deceleration
- Coyote time (can jump slightly after leaving ground)
- Jump buffering (can press jump before landing)
- Optional air jumps (double/triple jump)
- Platform collision (one-way platforms)
- Adjustable physics constants

```csharp
// Create player
var player = new Player
{
    WalkSpeed = 120f,
    RunSpeed = 200f,
    JumpForce = 350f,
    MaxAirJumps = 1  // Enable double jump
};

// Update player (handles input and physics automatically)
player.Update(gameTime, tileMap);
```

#### 6. **Animation System** (`Animation.cs`)
Sprite sheet animation:
- Frame-based animation
- Looping and non-looping clips
- Playback speed control
- Animation events (OnComplete callback)
- Helper methods for common patterns

```csharp
// Create animation controller
var anim = new AnimationController(spriteSheet);

// Add animations
anim.AddClip(AnimationClip.FromSpriteSheet("Walk", 0, 0, 16, 24, 6, 0.1f));
anim.AddClip(AnimationClip.FromSpriteSheet("Jump", 96, 0, 16, 24, 1));

// Play animation
anim.Play("Walk");
anim.Update(gameTime);
anim.Draw(spriteBatch, position, Color.White);
```

---

## Complete Usage Example

```csharp
public class MyPlatformer : Game
{
    private SpriteBatch spriteBatch;
    private Camera camera;
    private TileMap level;
    private Player player;
    private InputManager input;

    protected override void Initialize()
    {
        input = InputManager.Instance;
        camera = new Camera(GraphicsDevice.Viewport);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load level from JSON
        var levelData = LevelData.Load("Content/Levels/level1.json");

        // Create tilemap
        level = new TileMap(levelData.Width, levelData.Height, levelData.TileSize);
        level.LoadTileset(Content.Load<Texture2D>(levelData.TilesetPath));

        // Add parallax layers
        foreach (var parallax in levelData.ParallaxLayers)
        {
            var texture = Content.Load<Texture2D>(parallax.TexturePath);
            level.AddParallaxLayer(texture, parallax.ScrollSpeedX,
                                   parallax.ScrollSpeedY, parallax.RepeatX);
        }

        // Add tile layers
        foreach (var layerData in levelData.Layers)
        {
            var layer = level.AddLayer(layerData.Name, layerData.Depth);
            layer.Visible = layerData.Visible;

            // Copy tile data
            for (int y = 0; y < levelData.Height; y++)
                for (int x = 0; x < levelData.Width; x++)
                    layer.SetTile(x, y, layerData.Data[y * levelData.Width + x]);
        }

        // Setup collision
        var collisionLayer = levelData.GetLayer("Collision");
        for (int y = 0; y < levelData.Height; y++)
        {
            for (int x = 0; x < levelData.Width; x++)
            {
                if (collisionLayer.Data[y * levelData.Width + x] == 1)
                    level.SetCollision(x, y, TileCollision.Solid);
            }
        }

        // Create player
        player = new Player();
        player.Sprite = Content.Load<Texture2D>("player");
        var spawn = levelData.GetEntity("PlayerSpawn");
        player.Respawn(spawn.Position);

        // Setup camera
        camera.SetBounds(level.PixelWidth, level.PixelHeight);
        camera.FocusOn(player.Position);
    }

    protected override void Update(GameTime gameTime)
    {
        input.Update();

        player.Update(gameTime, level);

        camera.Follow(player.Center);
        camera.Update(gameTime);

        level.Update(camera.Position);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Draw world with camera
        spriteBatch.Begin(
            SpriteSortMode.BackToFront,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,  // Pixel-perfect rendering
            null, null, null,
            camera.GetTransformMatrix()
        );

        level.Draw(spriteBatch, camera.GetViewRectangle());
        player.Draw(spriteBatch);

        spriteBatch.End();

        base.Draw(gameTime);
    }
}
```

---

## Project Structure

```
Keener/
├── PlatformerEngine.Core/           # Core engine library
│   ├── Graphics/
│   │   ├── TileMap.cs              # Rendering & collision (500+ lines)
│   │   ├── Camera.cs               # Camera system
│   │   └── Animation.cs            # Sprite animation
│   ├── Data/
│   │   └── LevelData.cs            # JSON level format
│   ├── Input/
│   │   └── InputManager.cs         # Input system
│   ├── Entities/
│   │   ├── Entity.cs               # Base entity class
│   │   └── Player.cs               # Player with physics
│   └── Physics/
│       └── (future collision resolution)
├── PlatformerEngine.Example/        # Example implementation
│   └── ExampleGame.cs              # Complete working example
└── README.md
```

---

## Key Design Decisions

### 1. **Data-Driven Design**
- Levels are JSON files (easy to edit, version control friendly)
- Designers can modify levels without touching code
- Level editor can generate JSON directly

### 2. **Separation of Concerns**
- `TileMap` handles rendering and collision
- `LevelData` handles serialization
- `Camera` handles viewport and scrolling
- `InputManager` handles all input
- `Player` handles character physics
- Each system is independent and testable

### 3. **Performance Optimized**
- Viewport culling (only draws visible tiles)
- Caches visible tile list between frames
- Efficient collision detection (only checks nearby tiles)
- Parallax updates only when camera moves

### 4. **Modern C# Features**
- LINQ for clean queries
- Properties for encapsulation
- Nullable types for optional features
- System.Text.Json for serialization (no dependencies)
- Enums with [Flags] for collision types

### 5. **Flexible Input System**
- Action-based input (cross-platform)
- Supports keyboard, gamepad, mouse
- Customizable mappings
- Analog stick support with deadzone
- Gamepad vibration support

---

## Physics Features

### Player Movement
- **Smooth acceleration** - No instant movement, feels natural
- **Air control** - Reduced but present
- **Friction** - Different for ground vs air
- **Variable jump height** - Hold jump longer = jump higher
- **Coyote time** - Can jump briefly after leaving ledge
- **Jump buffering** - Can press jump before landing
- **Air jumps** - Optional double/triple jump
- **Platform collision** - One-way platforms you can jump through

### Adjustable Constants
All physics values are public properties you can tweak:
```csharp
player.WalkSpeed = 120f;
player.RunSpeed = 200f;
player.JumpForce = 350f;
player.JumpHoldGravity = 600f;
player.FallGravity = 900f;
player.MaxFallSpeed = 400f;
player.Acceleration = 800f;
player.Friction = 600f;
player.CoyoteTime = 0.15f;
player.JumpBufferTime = 0.1f;
player.MaxAirJumps = 1;
```

---

## Building the Project

### Requirements
- .NET 6.0 or higher
- MonoGame 3.8.1+
- Visual Studio 2022 or VS Code

### Quick Start
1. Install .NET SDK and MonoGame
2. Clone this repository
3. Open in Visual Studio
4. Build and run `ExampleGame.cs`

### Creating Your Own Game
1. Reference `PlatformerEngine.Core` in your project
2. Create a `Game` class that inherits from MonoGame's `Game`
3. Load a level JSON or create one programmatically
4. Create a `Player` instance
5. Update and draw in your game loop

See `ExampleGame.cs` for a complete working implementation.

---

## Next Steps

To extend this engine, consider adding:

1. **Enemy AI** - Patrol patterns, chase behavior
2. **Combat System** - Health, damage, projectiles
3. **Power-ups** - Collectibles that modify player abilities
4. **Audio System** - Music and sound effects
5. **Particle Effects** - Dust, explosions, trails
6. **Level Editor** - Visual tool for creating levels
7. **Save System** - Checkpoints, progress persistence
8. **Menus & UI** - Title screen, pause menu, HUD

---

## Contributing

This is an educational project demonstrating game engine architecture. Feel free to fork and extend!

## License

Educational/learning project. Use as you wish for learning game development.

---

**Ready to build your platformer!** 🎮

All core systems are implemented and working. Add your art, design your levels, and create your game!
