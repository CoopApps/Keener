# Keener Level & World Map Editors

Complete documentation for the built-in level and world map editors.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Level Editor](#level-editor)
3. [World Map Editor](#world-map-editor)
4. [Workflow Tips](#workflow-tips)
5. [File Formats](#file-formats)
6. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Accessing the Editors

1. Launch the game
2. Select **"Editor"** from the main menu
3. Choose:
   - **Level Editor** - Create individual platformer levels
   - **World Map Editor** - Design overworld maps with level nodes

### Quick Tips

- **Save early, save often** - Use `Ctrl+S` to save your work
- **Camera controls** - Middle mouse button or Space+Drag to pan
- **Zoom** - Mouse wheel to zoom in/out (0.25x to 4x)
- **Exit** - Press `ESC` to return to menu (prompts to save if changes exist)

---

## Level Editor

### Overview

The Level Editor allows you to create complete platformer levels with tiles, collision, and entities.

### Interface

```
┌─────────────────────────────────────────────────┐
│ Top Bar: Mode, Tool, Info                      │
├─────────────────────────────────────────────────┤
│                                                 │
│                                                 │
│              Main Canvas                        │
│         (Infinite scrollable)                   │
│                                                 │
├─────────────────────────────────────────────────┤
│ Right Sidebar: Tile Palette / Entity List      │
└─────────────────────────────────────────────────┘
```

### Modes (Switch with TAB)

#### 1. **Tiles Mode**
Edit the visual tiles of your level.

**Tools:**
- **Pencil (1)** - Draw single tiles
- **Brush (2)** - Paint multiple tiles at once
- **Fill (3)** - Flood fill an area with selected tile
- **Eraser (4)** - Remove tiles
- **Eyedropper (5)** - Pick a tile from the level
- **Select (6)** - Select and copy/paste areas
- **Entity (7)** - (Switches to Entity mode)

**Usage:**
1. Select a tile from the palette (right side)
2. Choose a tool (1-6 keys)
3. Left-click to paint
4. Right-click to erase

#### 2. **Collision Mode**
Define collision properties for tiles.

**Collision Types:**
- **None** (0) - No collision
- **Solid** (1) - Blocks all movement (walls, floors)
- **Platform** (2) - One-way platforms (jump through from below)
- **Ladder** (3) - Climbable surfaces
- **Deadly** (4) - Spikes, lava, hazards
- **Water** (5) - Affects physics (slower movement)
- **Ice** (6) - Slippery surfaces
- **Slope** (7) - Angled surfaces

**Usage:**
1. Switch to Collision mode (Tab)
2. Select collision type from toolbar
3. Left-click to paint collision
4. Right-click to clear collision
5. Press `C` to toggle collision overlay

**Color Guide:**
- Red = Solid
- Yellow = Platform
- Green = Ladder
- Purple = Deadly
- Blue = Water
- Cyan = Ice
- Orange = Slope

#### 3. **Entities Mode**
Place gameplay elements like enemies, power-ups, and interactive objects.

**Entity Categories:**

**Player:**
- `PlayerSpawn` - Starting position

**Collectibles:**
- `Coin` - Currency/score
- `HealthPickup` - Restore HP
- `ExtraLife` - 1-up
- `Key` - Unlock doors

**Power-Ups:**
- `PowerUp_JumpBoost` - Pogo stick
- `PowerUp_SpeedBoost` - Run faster
- `PowerUp_Invincibility` - Temporary invulnerability
- `PowerUp_Shield` - Damage absorption
- `PowerUp_Magnet` - Attract collectibles

**Enemies:**
- `WalkerEnemy` - Ground patrol
- `FlyerEnemy` - Flying swooper
- `ShooterEnemy` - Ranged attacker
- `ChargerEnemy` - Charges at player
- `TurretEnemy` - Stationary shooter
- `JumperEnemy` - Hopping enemy

**Interactive Objects:**
- `Checkpoint` - Save point
- `Switch` - Activatable trigger
- `Bridge` - Extends when activated
- `Teleporter` - Warps player

**Usage:**
1. Switch to Entities mode (Tab)
2. Select entity type from list
3. Left-click to place entity
4. Right-click on entity to delete

### Controls

| Action | Key/Mouse |
|--------|-----------|
| Switch Mode | Tab |
| Select Tool | 1-7 |
| Paint/Place | Left Click |
| Erase/Delete | Right Click |
| Pan Camera | Middle Mouse or Space+Left Drag |
| Zoom In/Out | Mouse Wheel |
| Toggle Grid | G |
| Toggle Collision View | C |
| Save Level | Ctrl+S |
| Load Level | Ctrl+O |
| Exit Editor | ESC |

### Workflow

1. **Start with tiles**: Draw the basic layout in Tiles mode
2. **Add collision**: Switch to Collision mode and paint collision types
3. **Place entities**: Add player spawn, enemies, collectibles, and objects
4. **Test and iterate**: Save and load in-game to test
5. **Refine**: Adjust based on playtesting

### Tips

- **Grid snapping**: Grid is always enabled for precise placement
- **Layers**: Levels support multiple layers (future expansion)
- **Viewport culling**: Only visible area is drawn for performance
- **Tile palette**: Scroll with mouse wheel when hovering over palette
- **Flood fill**: Click once to fill connected tiles (use carefully!)
- **Entity placement**: Entities show as colored circles (Green=Player, Red=Enemy, Yellow=PowerUp, Cyan=Object)

---

## World Map Editor

### Overview

Create overworld maps with connected level nodes for game progression.

### Interface

```
┌─────────────────────────────────────────────────┐
│ Top Bar: Tool, Node Count, Zoom                │
├─────────────────────────────────────────────────┤
│                                                 │
│                Main Canvas                      │
│         (Nodes and Connections)                 │
│                                                 │
├─────────────────────────────────────────────────┤
│ Right Sidebar: Selected Node Properties        │
└─────────────────────────────────────────────────┘
```

### Tools (Select with 1-4)

1. **Select (1)** - Click and drag nodes to move them
2. **Add Node (2)** - Click to create new level nodes
3. **Connect (3)** - Click two nodes to connect them
4. **Delete (4)** - Click to remove nodes or connections

### Node Properties

Each level node has:
- **ID** - Unique identifier (auto-generated)
- **Level Name** - Display name
- **Position** - X,Y coordinates on map
- **Is Unlocked** - Can player access this level?
- **Is Completed** - Has player finished this level?
- **Stars Earned** - 0-3 stars based on performance
- **Connected Nodes** - List of connected level IDs

### Usage

#### Creating a World Map

1. **Add Nodes**:
   - Select Add Node tool (2)
   - Click on canvas to place level nodes
   - First node is automatically unlocked

2. **Connect Nodes**:
   - Select Connect tool (3)
   - Click first node
   - Click second node to create bidirectional path
   - Repeat for all connections

3. **Move Nodes**:
   - Select Select tool (1)
   - Drag nodes to desired positions
   - Grid snapping available

4. **Edit Properties**:
   - Select a node (tool 1)
   - Right-click to edit name
   - Type new name and press Enter
   - Properties panel shows all node data

5. **Delete Elements**:
   - Select Delete tool (4)
   - Click node to delete (removes all connections)
   - Connections are automatically cleaned up

### Controls

| Action | Key/Mouse |
|--------|-----------|
| Select Tool | 1 (Select) |
| Add Node | 2 (Add) |
| Connect Nodes | 3 (Connect) |
| Delete | 4 (Delete) |
| Move Node | Left Drag (Select tool) |
| Edit Name | Right Click (on node) |
| Pan Camera | Middle Mouse |
| Zoom | Mouse Wheel |
| Toggle Grid | G |
| Save Map | Ctrl+S |
| Load Map | Ctrl+O |
| Exit Editor | ESC |

### Visual Guide

**Node Appearance:**
- **Blue Circle** = Unlocked level
- **Gray Circle** = Locked level
- **Yellow Circle** = Currently selected
- **White Outline** = Node border
- **Yellow Stars** = Completion stars (above node)
- **White Lines** = Connections between nodes

### Example Workflow

1. **Main Hub**: Create central node (unlocked)
2. **Branch Paths**: Add connected nodes in different directions
3. **Progressive Unlock**: Connect nodes sequentially for linear progression
4. **Optional Branches**: Add side paths for optional levels
5. **Hub Return**: Connect later levels back to hub for shortcuts
6. **Test Navigation**: Verify all connections work

### Tips

- **Start Simple**: Begin with 3-5 nodes, expand later
- **Visual Layout**: Arrange nodes spatially to match game world
- **Progression Flow**: Left-to-right or bottom-to-top reads well
- **Shortcuts**: Add connections between distant nodes for late-game shortcuts
- **Grid Snapping**: Hold shift while dragging for grid alignment
- **Naming**: Use descriptive names (e.g., "Level 1 - Forest Entrance")
- **Connection Limit**: No hard limit, but 2-4 connections per node is typical

---

## Workflow Tips

### Complete Game Creation Workflow

1. **Design World Map**:
   - Open World Map Editor
   - Create all level nodes
   - Connect them logically
   - Save world map

2. **Create Levels**:
   - Open Level Editor
   - Create level matching each world map node
   - Save with ID matching node (e.g., "level1.json")
   - Repeat for all nodes

3. **Add Content**:
   - Place player spawn (required)
   - Add enemies and obstacles
   - Place collectibles and power-ups
   - Add checkpoints for longer levels
   - Set up interactive objects (switches, bridges)

4. **Test Integration**:
   - Load game
   - Navigate world map
   - Play each level
   - Verify progression works

5. **Iterate**:
   - Adjust difficulty
   - Refine level layouts
   - Balance enemy placement
   - Add secrets and optional paths

### Best Practices

**Level Design:**
- Start with player spawn in safe area
- Gradually increase difficulty
- Place checkpoints every 2-3 screens
- Hide secrets behind optional challenges
- Add power-ups before difficult sections
- Use varied enemy types
- Create multiple paths when possible

**World Map Design:**
- Group thematically similar levels
- Balance linear and branching paths
- Provide skill checks before difficult areas
- Add hub nodes for respite
- Consider backtracking opportunities
- Leave room for DLC/expansion nodes

**File Management:**
- Use consistent naming (level1, level2, etc.)
- Keep world map IDs and level files synced
- Version control saves (level1_v1, level1_v2)
- Back up regularly
- Document special mechanics

---

## File Formats

### Level Files

**Location**: `Levels/`
**Format**: JSON
**Extension**: `.json`

**Example**: `level1.json`
```json
{
  "LevelName": "Level 1",
  "TileWidth": 64,
  "TileHeight": 32,
  "TileSize": 16,
  "Layers": [...],
  "CollisionLayer": [...],
  "Entities": [...]
}
```

**Features:**
- Run-length encoding for tile data
- Compressed format for efficient storage
- Human-readable JSON structure
- Multiple layers supported

### World Map Files

**Location**: `WorldMaps/`
**Format**: JSON
**Extension**: `.json`

**Example**: `MainWorld.json`
```json
{
  "MapName": "Main World",
  "BackgroundTexture": "world_map_bg",
  "Nodes": [
    {
      "Id": "level1",
      "LevelName": "Level 1",
      "Position": { "X": 100, "Y": 300 },
      "IsUnlocked": true,
      "IsCompleted": false,
      "StarsEarned": 0,
      "ConnectedNodeIds": ["level2"]
    }
  ]
}
```

---

## Troubleshooting

### Common Issues

**Issue**: Can't see tiles in Level Editor
- **Solution**: Make sure you have textures loaded or use placeholder colors

**Issue**: Collision not working in-game
- **Solution**: Verify collision was painted in Collision mode, not just tiles

**Issue**: Entity not spawning
- **Solution**: Check entity type matches game's entity factory

**Issue**: World map nodes not connecting
- **Solution**: Use Connect tool (3), click both nodes in sequence

**Issue**: Level file not loading
- **Solution**: Check file is in `Levels/` folder with `.json` extension

**Issue**: Camera stuck/can't move
- **Solution**: Use middle mouse or Space+Left drag to pan

**Issue**: Can't place entity
- **Solution**: Make sure you're in Entities mode (Tab to switch)

**Issue**: Grid not visible
- **Solution**: Press G to toggle grid display

### Performance Tips

- Keep level size reasonable (64x32 tiles is good)
- Use fewer large sprites than many small ones
- Limit entity count per screen (~20-30 max)
- Use viewport culling (automatic in renderer)
- Test on target hardware regularly

### Getting Help

If you encounter issues:
1. Check console output for error messages
2. Verify file paths and naming
3. Test with default/example files
4. Review STATUS.md for system requirements
5. Check KEEN_ARCHITECTURE.md for implementation details

---

## Keyboard Reference

### Universal Controls

| Key | Action |
|-----|--------|
| Tab | Switch mode/section |
| 1-7 | Select tools |
| G | Toggle grid |
| C | Toggle collision overlay |
| Ctrl+S | Save |
| Ctrl+O | Load |
| ESC | Exit/Cancel |
| Space | Pan modifier |

### Mouse Controls

| Button | Action |
|--------|--------|
| Left Click | Paint/Place/Select |
| Right Click | Erase/Delete/Edit |
| Middle Click | Pan camera |
| Scroll Wheel | Zoom |

---

## Quick Start Examples

### Example 1: Simple Level

1. Open Level Editor
2. Select Pencil tool (1)
3. Draw ground tiles (tile ID 1)
4. Tab to Collision mode
5. Paint ground as Solid
6. Tab to Entities mode
7. Place PlayerSpawn
8. Place 5-10 Coins
9. Place 2-3 WalkerEnemies
10. Ctrl+S to save as "test_level.json"

### Example 2: Linear World Map

1. Open World Map Editor
2. Add Node tool (2)
3. Click to place 5 nodes in a line
4. Connect tool (3)
5. Connect each node to next
6. Select tool (1)
7. Drag to arrange
8. Right-click each to name (Level 1, Level 2, etc.)
9. Ctrl+S to save as "world.json"

### Example 3: Hub World Map

1. Place central hub node
2. Add 4 nodes around hub (cardinal directions)
3. Connect each to hub
4. Add 2-3 nodes branching from each
5. Connect branches
6. Optional: Add shortcut from end back to hub
7. Name all nodes descriptively
8. Save

---

## Advanced Features

### Level Editor

**Multi-layer Support** (Future):
- Background layers for parallax
- Multiple foreground layers
- Per-layer opacity

**Tile Animation** (Future):
- Animated tile support
- Frame-by-frame editing

**Prefabs** (Future):
- Save/load entity groups
- Reusable room templates

### World Map Editor

**Node Metadata** (Implemented):
- Custom properties per node
- Level requirements
- Unlock conditions

**Path Visualization** (Implemented):
- Shows all connections
- Bidirectional paths
- Visual feedback

---

## Conclusion

The Keener editors provide professional-grade tools for creating complete platformer games. With the Level Editor and World Map Editor working together, you can create complex, interconnected game worlds.

**Remember**:
- Save frequently (Ctrl+S)
- Test in-game regularly
- Iterate based on playtesting
- Keep backups of working versions

Happy level creating! 🎮

---

**For more information:**
- See STATUS.md for complete engine features
- See KEEN_ARCHITECTURE.md for technical details
- See README.md for general project information
