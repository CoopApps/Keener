# Commander Keen Architecture Analysis

This document explains how Commander Keen 4-6 handled application launch, menus, and game flow, and how it's been implemented in this modern C# platformer engine.

## Commander Keen Launch Sequence

### 1. Main Entry Point (CK_MAIN.C)

```c
void main()
{
    // 1. Check for EGAGRAPH files
    // 2. Parse command-line arguments (DEMO, JOYPAD)
    InitGame();      // Initialize all subsystems
    CheckMemory();   // Validate RAM/EMS/XMS requirements
    DemoLoop();      // Enter attract mode loop (never returns)
}
```

### 2. Subsystem Initialization (InitGame)

Commander Keen initialized subsystems in this specific order:

```c
void InitGame()
{
    MM_Startup();    // Memory Manager - Heap allocation, EMS/XMS
    VW_Startup();    // Video/View Manager - Graphics mode setup
    RF_Startup();    // Refresh/Rendering - Scrolling system
    IN_Startup();    // Input Manager - Keyboard/Joystick
    SD_Startup();    // Sound Driver - AdLib/PC Speaker
    US_Startup();    // User Services - UI/Menus
    CA_Startup();    // Cache Manager - Asset loading

    // Lock essential graphics in memory (fonts, tiles)
}
```

**Why this order matters:**
- Memory must be available before allocating buffers
- Graphics must be initialized before loading textures
- Input needed before menus can respond
- Sound can fail gracefully if hardware unavailable
- Cache depends on memory manager

### 3. Demo Loop (Attract Mode)

The `DemoLoop()` function cycles through:

```
┌──────────────────────────────────────────────┐
│  ATTRACT MODE CYCLE                          │
└──────────────────────────────────────────────┘
         │
         ├─> ShowTitle()         (6 second display)
         │   Wait for input...
         │
         ├─> [Key Press] ──────> US_ControlPanel()
         │                       (Main Menu)
         │                              │
         ├─> [Timeout] ────────> RunDemo(n)         │
         │                       (Playback demo)     │
         │                                           │
         └─> ShowHighScores()                        │
             (Scores overlaid on demo)               │
                 │                                   │
                 └───────────────────────────────────┘
                         Loop Forever

[From Menu] ────> NewGame() ──> GameLoop() ──> [Return to Demo Loop]
```

### 4. Main Menu (US_ControlPanel)

The Control Panel provided these options:

```
┌─────────────────────────────┐
│     MAIN MENU OPTIONS       │
├─────────────────────────────┤
│  > New Game                 │  → NewGame() → GameLoop()
│    Load Game                │  → Load save, GameLoop()
│    Save Game                │  → Save current state
│    Configure Controls       │  → Keyboard/Joystick mapping
│    Sound Options            │  → AdLib/PC Speaker/Off
│    View High Scores         │  → Display leaderboard
│    Paddle War (Keen 4)      │  → Bonus breakout game
│    Quit                     │  → Exit to DOS
└─────────────────────────────┘
```

**Key Implementation Details:**
- **Blocking function** - Didn't return until user selected option
- **State-based** - Used function return value to indicate choice
- **Reusable** - Could be called from title screen OR in-game (pause menu)

### 5. Game State Flow

```c
enum exittype {
    ex_stillplaying,  // Continue current level
    ex_completed,     // Level finished - advance
    ex_died,          // Player death - lose life
    ex_warped,        // Teleported to new level
    ex_resetgame,     // Start over
    ex_loadedgame,    // Loaded from save
    ex_victorious,    // Game completed
    ex_abort          // Quit to menu
};
```

The `GameLoop()` would return one of these states, and the calling code would decide what to do:

```c
void GameLoop() {
    while(1) {
        SetupGameLevel(true);      // Load level data
        exittype result = PlayLoop(); // Run gameplay

        switch(result) {
            case ex_died:
                if (--Lives == 0) return; // Game over
                break;
            case ex_completed:
                currentLevel++;
                break;
            case ex_abort:
                return; // Back to demo loop
            // ...
        }
    }
}
```

---

## Modern C# Implementation

The engine replicates Keen's architecture using modern patterns:

### File Structure

```
PlatformerEngine.Core/
├── PlatformerGame.cs              # Main game class (CK_MAIN.C equivalent)
├── Scenes/
│   ├── SceneManager.cs            # Scene stack manager
│   ├── MenuScenes.cs              # All menu screens
│   │   ├── TitleScreenScene       (ShowTitle)
│   │   ├── MainMenuScene          (US_ControlPanel)
│   │   ├── LoadGameMenuScene      (Load game screen)
│   │   ├── ControlsMenuScene      (Control config)
│   │   └── SoundMenuScene         (Sound options)
│   └── AttractModeManager         (DemoLoop)
├── Screens/
│   ├── TextScreen.cs              # Full-screen text
│   ├── HighScoreScreen.cs         # Leaderboard
│   ├── LevelCompleteScreen.cs    # Stats screen
│   └── WorldMapScreen.cs          # Level select
├── Input/
│   └── InputManager.cs            (ID_IN equivalent)
├── Audio/
│   └── AudioManager.cs            (ID_SD equivalent)
├── Persistence/
│   └── SaveManager.cs             (Save/Load system)
└── Replay/
    └── ReplaySystem.cs            (Demo recording)
```

### Initialization Sequence

**PlatformerGame.cs:**

```csharp
protected override void Initialize()
{
    // Following Keen's subsystem order:
    InitializeMemoryManagement();  // MM_Startup()
    InitializeGraphics();           // VW_Startup(), RF_Startup()
    InitializeInput();              // IN_Startup()
    InitializeAudio();              // SD_Startup()
    InitializeUserServices();       // US_Startup()
    InitializeCaching();            // CA_Startup()

    CheckSystemRequirements();      // CheckMemory()
    RegisterScenes();               // Register all scenes
    StartAttractMode();             // DemoLoop()
}
```

### Scene Management

Instead of Keen's blocking functions, we use a **scene stack**:

```csharp
// Keen way (blocking):
US_ControlPanel();  // Blocks until user chooses

// Modern way (scene-based):
SceneManager.Instance.PushScene("MainMenu");
// Returns immediately, menu updates/draws each frame
```

**Benefits:**
- Non-blocking for smooth rendering
- Easy transitions and effects
- Can overlay scenes (pause menu over game)
- Better separation of concerns

### Attract Mode Flow

**AttractModeManager.cs** replicates `DemoLoop()`:

```csharp
public class AttractModeManager
{
    private enum AttractState {
        TitleScreen,    // ShowTitle()
        DemoPlayback,   // RunDemo(n)
        HighScores,     // ShowHighScores()
        Credits         // Credits screen
    }

    public void Update(float deltaTime) {
        switch (currentState) {
            case AttractState.TitleScreen:
                // Timeout → DemoPlayback
                // User input → MainMenu
                break;
            // ... cycle through states
        }
    }
}
```

### Menu Navigation

**MainMenuScene.cs** implements `US_ControlPanel()`:

```csharp
public class MainMenuScene : Scene
{
    private enum MenuOption {
        NewGame,      // → Start new game
        LoadGame,     // → Push LoadGameMenu scene
        Controls,     // → Push ControlsMenu scene
        Sound,        // → Push SoundMenu scene
        HighScores,   // → Push HighScores scene
        Quit          // → Exit
    }

    private void HandleMenuSelection() {
        switch (selectedOption) {
            case MenuOption.NewGame:
                SceneManager.Instance.ChangeScene("GameLevel",
                    new FadeTransition(0.5f));
                break;
            // ...
        }
    }
}
```

---

## Key Differences: Keen vs Modern

| Aspect | Commander Keen | Modern Engine |
|--------|---------------|---------------|
| **Architecture** | Procedural C | Object-Oriented C# |
| **Screen Flow** | Blocking functions | Scene stack |
| **Graphics** | EGA 320×200 16-color | Any resolution, millions of colors |
| **Memory** | Manual (EMS/XMS) | Garbage collected |
| **Input** | Polled keyboard/joystick | Event-based, multi-device |
| **Audio** | AdLib/PC Speaker | XAudio2 (modern sound) |
| **Assets** | Custom compression | Content Pipeline |
| **Saves** | Raw file writes | JSON serialization |
| **Demos** | Input recording | ReplaySystem with JSON |

---

## Usage Example

### Starting the Game

```csharp
// Program.cs entry point
static void Main(string[] args)
{
    using (var game = new PlatformerGame())
    {
        game.Run();
    }
}

// Game automatically:
// 1. Initializes all subsystems
// 2. Shows title screen
// 3. Enters attract mode
// 4. Cycles demos/scores/credits
// 5. Waits for user to press key → Main menu
```

### Flow Example

```
User starts game
    ↓
PlatformerGame.Initialize()
    ↓
Title Screen (6s or key press)
    ↓
[User presses key]
    ↓
Main Menu
    ↓
[User selects "New Game"]
    ↓
World Map Screen
    ↓
[User selects level]
    ↓
Game Level Scene
    ↓
[Level complete]
    ↓
Level Complete Screen (stats)
    ↓
[Check for high score]
    ↓
High Score Entry (if qualified)
    ↓
Back to World Map
```

---

## Adding New Menu Options

To add a new menu item:

1. **Add to MenuOption enum:**
```csharp
private enum MenuOption {
    NewGame,
    LoadGame,
    Controls,
    Sound,
    HighScores,
    Credits,    // NEW
    Quit
}
```

2. **Add menu text:**
```csharp
menuText.Add(MenuOption.Credits, "Credits");
```

3. **Handle selection:**
```csharp
case MenuOption.Credits:
    SceneManager.Instance.PushScene("Credits");
    break;
```

4. **Create scene:**
```csharp
sceneManager.RegisterScene("Credits",
    TextScreenFactory.CreateCreditsScreen(
        "Game by...", "Music by...", "Thanks to..."
    ));
```

---

## Command-Line Arguments

Following Keen's tradition:

```bash
# Normal launch
./PlatformerGame.exe

# Start in demo mode
./PlatformerGame.exe DEMO

# Disable sound
./PlatformerGame.exe NOSOUND

# Multiple args
./PlatformerGame.exe DEMO NOSOUND
```

---

## Complete Engine Feature List

### Core Systems (from Keen)
- ✅ Application launch & initialization
- ✅ Subsystem management
- ✅ Title screen with timeout
- ✅ Main menu (New/Load/Controls/Sound/Scores/Quit)
- ✅ Attract mode cycling
- ✅ Demo recording/playback
- ✅ High score system
- ✅ Save/Load game
- ✅ Configuration persistence

### Gameplay Systems
- ✅ Platformer physics (gravity, jumping, coyote time)
- ✅ Tile-based collision (7 types)
- ✅ Parallax scrolling
- ✅ Camera system
- ✅ Entity system
- ✅ Player controller
- ✅ Enemy AI (6 types)
- ✅ Combat/Health system
- ✅ Power-ups (16 types)
- ✅ Collectibles/Inventory
- ✅ Interactive objects (switches, bridges, teleporters)
- ✅ Moving platforms
- ✅ Checkpoints

### UI Systems
- ✅ HUD (health, score, lives)
- ✅ Dialogue system
- ✅ Full-screen text screens
- ✅ Level complete screen with stats
- ✅ World map navigation
- ✅ Scene transitions

### Technical Features
- ✅ Input abstraction (keyboard/gamepad)
- ✅ Audio manager (music/SFX)
- ✅ Particle effects
- ✅ Animation system
- ✅ Level editor
- ✅ JSON level format
- ✅ Replay system

---

## Summary

This platformer engine faithfully recreates Commander Keen's architecture while leveraging modern programming practices:

- **Same flow:** Title → Demo Loop → Menu → Game
- **Same initialization:** Subsystems in specific order
- **Same features:** Demos, high scores, save/load, config
- **Modern tech:** C#, MonoGame, JSON, OOP patterns
- **Better structure:** Scene management, component system
- **More features:** 16 power-ups, 6 enemies, 8 interactive objects

The result is a **production-ready platformer engine** that honors the classic Keen architecture while being extensible and maintainable for modern game development.
