# Editor Enhancement Guide - Retro Style

Complete guide to enhancing the editors with retro aesthetics and advanced features.

---

## Overview

The editors now have access to:
- **RetroUI.cs** - Complete DOS/EGA-style UI toolkit
- **EditorActions.cs** - Undo/redo system with history management
- Full copy/paste infrastructure
- Prefab system foundation

---

## Part 1: Retro Visual Style

### Using RetroUI Components

The `RetroUI` class provides Commander Keen / DOS-era styled UI components.

#### 1. **Color Palette**

```csharp
using PlatformerEngine.Core.Editor;

// 16-color EGA palette
RetroUI.Black, RetroUI.DarkBlue, RetroUI.DarkGreen, RetroUI.DarkCyan
RetroUI.DarkRed, RetroUI.DarkMagenta, RetroUI.Brown, RetroUI.Gray
RetroUI.DarkGray, RetroUI.Blue, RetroUI.Green, RetroUI.Cyan
RetroUI.Red, RetroUI.Magenta, RetroUI.Yellow, RetroUI.White

// Themed colors
RetroUI.BorderColor     // Cyan borders
RetroUI.TitleColor      // Yellow titles
RetroUI.ActiveColor     // Yellow highlights
RetroUI.WindowColor     // Dark blue windows
```

#### 2. **Drawing Windows**

```csharp
// In your editor's Draw() method:
spriteBatch.Begin();

// Draw retro window with title
RetroUI.DrawWindow(spriteBatch, pixelTexture,
    new Rectangle(100, 100, 400, 300),
    "LEVEL EDITOR v1.0", font);

spriteBatch.End();
```

#### 3. **Buttons**

```csharp
Rectangle saveButton = new Rectangle(10, 10, 100, 30);
bool isHovered = saveButton.Contains(mousePos);
bool isPressed = isHovered && mouseState.LeftButton == ButtonState.Pressed;

RetroUI.DrawButton(spriteBatch, pixelTexture, saveButton,
    "SAVE", font, isPressed, isHovered);
```

#### 4. **Checkboxes**

```csharp
RetroUI.DrawCheckbox(spriteBatch, pixelTexture,
    new Vector2(10, 50), showGrid, "Show Grid", font, isHovered);
```

#### 5. **Progress Bars**

```csharp
// Save progress indicator
RetroUI.DrawProgressBar(spriteBatch, pixelTexture,
    new Rectangle(10, 100, 200, 20),
    saveProgress, RetroUI.Green);
```

#### 6. **CRT Effects**

```csharp
// Add at end of Draw() for full-screen effects
spriteBatch.Begin();

// Scanlines (horizontal lines like old CRT monitors)
RetroUI.DrawScanlines(spriteBatch, pixelTexture, screenBounds, 0.15f);

// Vignette (darkens edges for curved screen effect)
RetroUI.DrawCRTVignette(spriteBatch, pixelTexture, screenBounds);

spriteBatch.End();
```

#### 7. **Text Effects**

```csharp
// Text with shadow
RetroUI.DrawTextWithShadow(spriteBatch, font, "KEENER EDITOR",
    new Vector2(10, 10), RetroUI.Yellow);

// Blinking text (for warnings)
RetroUI.DrawBlinkingText(spriteBatch, font, "UNSAVED CHANGES",
    new Vector2(10, 30), gameTime, RetroUI.Red, RetroUI.Yellow);
```

---

## Part 2: Undo/Redo System

### Implementation Example

```csharp
using PlatformerEngine.Core.Editor;

public class EnhancedLevelEditor : Scene
{
    private EditorHistory history = new EditorHistory();
    private KeyboardState previousKeyboard;

    public override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        // Undo (Ctrl+Z)
        if (keyboard.IsKeyDown(Keys.LeftControl) &&
            keyboard.IsKeyDown(Keys.Z) &&
            !previousKeyboard.IsKeyDown(Keys.Z))
        {
            history.Undo();
        }

        // Redo (Ctrl+Y)
        if (keyboard.IsKeyDown(Keys.LeftControl) &&
            keyboard.IsKeyDown(Keys.Y) &&
            !previousKeyboard.IsKeyDown(Keys.Y))
        {
            history.Redo();
        }

        previousKeyboard = keyboard;
    }

    // When painting tiles:
    private void PaintTile(int x, int y, int tileId)
    {
        var action = new PaintTileAction(currentLayer, x, y, tileId);
        history.ExecuteAction(action);
    }

    // When placing entities:
    private void PlaceEntity(EntityData entity)
    {
        var action = new PlaceEntityAction(entities, entity);
        history.ExecuteAction(action);
    }
}
```

### Drawing Undo/Redo UI

```csharp
public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
{
    // ... existing drawing ...

    spriteBatch.Begin();

    // Show undo/redo status in retro style
    Color undoColor = history.CanUndo ? RetroUI.Yellow : RetroUI.DarkGray;
    Color redoColor = history.CanRedo ? RetroUI.Yellow : RetroUI.DarkGray;

    RetroUI.DrawTextWithShadow(spriteBatch, font,
        $"UNDO (Ctrl+Z): {history.GetUndoDescription()}",
        new Vector2(10, screenBounds.Height - 60), undoColor);

    RetroUI.DrawTextWithShadow(spriteBatch, font,
        $"REDO (Ctrl+Y): {history.GetRedoDescription()}",
        new Vector2(10, screenBounds.Height - 40), redoColor);

    spriteBatch.End();
}
```

---

## Part 3: Copy/Paste System

### Implementation

```csharp
public class EnhancedLevelEditor : Scene
{
    private EditorClipboard clipboard = new EditorClipboard();
    private Rectangle? selectionRect;

    // Copy selection (Ctrl+C)
    private void CopySelection()
    {
        if (selectionRect == null) return;

        var rect = selectionRect.Value;
        clipboard.Width = rect.Width;
        clipboard.Height = rect.Height;
        clipboard.Tiles = new int[rect.Width, rect.Height];

        // Copy tiles
        for (int y = 0; y < rect.Height; y++)
        {
            for (int x = 0; x < rect.Width; x++)
            {
                int worldX = rect.X + x;
                int worldY = rect.Y + y;
                clipboard.Tiles[x, y] = currentLayer.GetTile(worldX, worldY);
            }
        }
    }

    // Paste (Ctrl+V)
    private void PasteClipboard(int targetX, int targetY)
    {
        if (clipboard.IsEmpty) return;

        var compound = new CompoundAction("Paste");

        for (int y = 0; y < clipboard.Height; y++)
        {
            for (int x = 0; x < clipboard.Width; x++)
            {
                int worldX = targetX + x;
                int worldY = targetY + y;

                if (worldX >= 0 && worldX < levelWidth &&
                    worldY >= 0 && worldY < levelHeight)
                {
                    var action = new PaintTileAction(
                        currentLayer, worldX, worldY,
                        clipboard.Tiles[x, y]
                    );
                    compound.AddAction(action);
                }
            }
        }

        history.ExecuteAction(compound);
    }

    // Draw selection rectangle
    private void DrawSelection(SpriteBatch spriteBatch)
    {
        if (selectionRect == null) return;

        var rect = selectionRect.Value;
        var screenRect = new Rectangle(
            (int)((rect.X * tileSize - cameraPosition.X) * cameraZoom),
            (int)((rect.Y * tileSize - cameraPosition.Y) * cameraZoom),
            (int)(rect.Width * tileSize * cameraZoom),
            (int)(rect.Height * tileSize * cameraZoom)
        );

        // Animated dashed border
        DrawDashedRect(spriteBatch, screenRect, RetroUI.Yellow);
    }
}
```

---

## Part 4: Tileset Browser

### Retro-Style Tile Picker

```csharp
public class TilesetBrowser
{
    private Rectangle bounds;
    private int[] tileIds;
    private int selectedTileId;
    private int scrollOffset;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        // Window background
        RetroUI.DrawWindow(spriteBatch, pixel, bounds, "TILESET", font);

        // Draw tiles in grid
        int tilesPerRow = 8;
        int tileDisplaySize = 32;
        int padding = 4;

        for (int i = 0; i < tileIds.Length; i++)
        {
            int row = i / tilesPerRow;
            int col = i % tilesPerRow;

            int x = bounds.X + 10 + col * (tileDisplaySize + padding);
            int y = bounds.Y + 30 + (row - scrollOffset) * (tileDisplaySize + padding);

            if (y < bounds.Y + 30 || y > bounds.Bottom - 40) continue;

            Rectangle tileRect = new Rectangle(x, y, tileDisplaySize, tileDisplaySize);

            // Draw tile (or colored square if no texture)
            Color tileColor = GetTileColor(tileIds[i]);
            spriteBatch.Draw(pixel, tileRect, tileColor);

            // Selection highlight
            if (tileIds[i] == selectedTileId)
            {
                DrawRect(spriteBatch, pixel, tileRect, RetroUI.Yellow, 2);
            }
            else
            {
                DrawRect(spriteBatch, pixel, tileRect, RetroUI.Cyan, 1);
            }

            // Tile ID
            string idText = tileIds[i].ToString();
            spriteBatch.DrawString(font, idText,
                new Vector2(x + 2, y + 2), RetroUI.White);
        }

        // Scrollbar
        DrawScrollbar(spriteBatch, pixel);
    }

    private Color GetTileColor(int tileId)
    {
        // Generate color from ID for visual distinction
        return new Color(
            (tileId * 37) % 255,
            (tileId * 73) % 255,
            (tileId * 131) % 255
        );
    }
}
```

---

## Part 5: Entity Property Editor

### Retro Property Panel

```csharp
public class EntityPropertyPanel
{
    private EntityData selectedEntity;
    private Rectangle bounds;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (selectedEntity == null) return;

        RetroUI.DrawWindow(spriteBatch, pixel, bounds, "ENTITY PROPERTIES", font);

        int y = bounds.Y + 30;
        int lineHeight = 25;

        // Entity type
        RetroUI.DrawTextWithShadow(spriteBatch, font,
            $"Type: {selectedEntity.Type}",
            new Vector2(bounds.X + 10, y), RetroUI.White);
        y += lineHeight;

        // Position
        RetroUI.DrawTextWithShadow(spriteBatch, font,
            $"Position: ({(int)selectedEntity.Position.X}, {(int)selectedEntity.Position.Y})",
            new Vector2(bounds.X + 10, y), RetroUI.White);
        y += lineHeight;

        // ID
        RetroUI.DrawTextWithShadow(spriteBatch, font,
            $"ID: {selectedEntity.Id}",
            new Vector2(bounds.X + 10, y), RetroUI.White);
        y += lineHeight;

        // Custom properties
        if (selectedEntity.CustomData != null)
        {
            RetroUI.DrawSeparator(spriteBatch, pixel,
                new Vector2(bounds.X + 10, y),
                new Vector2(bounds.Right - 10, y), true);
            y += 10;

            foreach (var kvp in selectedEntity.CustomData)
            {
                RetroUI.DrawTextWithShadow(spriteBatch, font,
                    $"{kvp.Key}: {kvp.Value}",
                    new Vector2(bounds.X + 10, y), RetroUI.Gray);
                y += lineHeight;
            }
        }

        // Edit buttons
        y = bounds.Bottom - 50;
        DrawEditButtons(spriteBatch, pixel, font, y);
    }
}
```

---

## Part 6: Minimap

### Retro-Style Level Overview

```csharp
public class Minimap
{
    private Rectangle bounds;
    private TileMap tileMap;
    private Vector2 cameraPosition;
    private float cameraZoom;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        // Background
        RetroUI.DrawPanel(spriteBatch, pixel, bounds, inset: true);

        // Calculate minimap scale
        float scaleX = (float)bounds.Width / (tileMap.TileWidth * tileMap.TileSize);
        float scaleY = (float)bounds.Height / (tileMap.TileHeight * tileMap.TileSize);
        float scale = Math.Min(scaleX, scaleY) * 0.9f;

        // Draw simplified level
        for (int y = 0; y < tileMap.TileHeight; y++)
        {
            for (int x = 0; x < tileMap.TileWidth; x++)
            {
                int tileId = tileMap.Layers[0].GetTile(x, y);
                if (tileId == 0) continue;

                int miniX = bounds.X + (int)(x * tileMap.TileSize * scale);
                int miniY = bounds.Y + (int)(y * tileMap.TileSize * scale);
                int miniSize = Math.Max(1, (int)(tileMap.TileSize * scale));

                Color color = GetMinimapColor(tileId);
                spriteBatch.Draw(pixel,
                    new Rectangle(miniX, miniY, miniSize, miniSize),
                    color);
            }
        }

        // Draw viewport indicator
        DrawViewportRect(spriteBatch, pixel, scale);
    }

    private void DrawViewportRect(SpriteBatch spriteBatch, Texture2D pixel, float scale)
    {
        // Calculate visible area on minimap
        Rectangle viewport = new Rectangle(
            bounds.X + (int)(cameraPosition.X * scale),
            bounds.Y + (int)(cameraPosition.Y * scale),
            (int)(640 / cameraZoom * scale),
            (int)(480 / cameraZoom * scale)
        );

        // Draw with animated border
        DrawDashedRect(spriteBatch, pixel, viewport, RetroUI.Yellow);
    }
}
```

---

## Part 7: Prefab System

### Saving and Loading Prefabs

```csharp
public class PrefabManager
{
    private List<Prefab> prefabs = new List<Prefab>();

    public void SavePrefab(string name, List<EntityData> entities, Vector2 pivot)
    {
        var prefab = new Prefab
        {
            Name = name,
            PivotPoint = pivot,
            Entities = new List<EntityData>()
        };

        // Copy entities relative to pivot
        foreach (var entity in entities)
        {
            var copy = new EntityData
            {
                Type = entity.Type,
                Position = entity.Position - pivot,
                CustomData = entity.CustomData
            };
            prefab.Entities.Add(copy);
        }

        prefabs.Add(prefab);
        SaveToFile($"Prefabs/{name}.json");
    }

    public void PlacePrefab(Prefab prefab, Vector2 position,
                           List<EntityData> targetList, EditorHistory history)
    {
        var compound = new CompoundAction($"Place prefab: {prefab.Name}");

        foreach (var template in prefab.Entities)
        {
            var entity = new EntityData
            {
                Type = template.Type,
                Position = position + template.Position,
                Id = $"{template.Type}_{Guid.NewGuid()}"
            };

            var action = new PlaceEntityAction(targetList, entity);
            compound.AddAction(action);
        }

        history.ExecuteAction(compound);
    }
}

// Prefab browser UI
public class PrefabBrowser
{
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        RetroUI.DrawWindow(spriteBatch, pixel, bounds, "PREFABS", font);

        int y = bounds.Y + 30;
        foreach (var prefab in prefabs)
        {
            bool isHovered = HoverRect(new Rectangle(bounds.X + 10, y, bounds.Width - 20, 25));

            RetroUI.DrawButton(spriteBatch, pixel,
                new Rectangle(bounds.X + 10, y, bounds.Width - 20, 25),
                prefab.Name, font, false, isHovered);

            y += 30;
        }
    }
}
```

---

## Part 8: Test-Play Button

### Launch Level Directly from Editor

```csharp
public class EnhancedLevelEditor : Scene
{
    private void DrawToolbar(SpriteBatch spriteBatch)
    {
        // Test play button with retro style
        Rectangle playButton = new Rectangle(
            screenBounds.Width - 120, 10, 100, 30
        );

        bool playHovered = playButton.Contains(mousePos);

        RetroUI.DrawButton(spriteBatch, pixelTexture, playButton,
            "▶ TEST", font, false, playHovered);

        if (playHovered && mouseState.LeftButton == ButtonState.Pressed)
        {
            TestPlayLevel();
        }
    }

    private void TestPlayLevel()
    {
        // Save current level to temp file
        string tempPath = "Levels/__temp_test.json";
        SaveLevelToFile(tempPath);

        // Create game level scene with temp level
        var gameScene = new GameLevelScene("__temp_test", font);
        SceneManager.Instance.PushScene("__TestPlay");

        // Note: Press ESC in game to return to editor
    }
}
```

---

## Part 9: Layer Visibility Toggles

### Multi-Layer Management

```csharp
public class LayerPanel
{
    private List<bool> layerVisibility = new List<bool> { true, true, true };
    private int activeLayer = 0;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        RetroUI.DrawWindow(spriteBatch, pixel, bounds, "LAYERS", font);

        int y = bounds.Y + 30;

        for (int i = 0; i < tileMap.Layers.Count; i++)
        {
            bool isActive = (i == activeLayer);
            bool isHovered = HoverRect(new Rectangle(bounds.X + 10, y, bounds.Width - 20, 25));

            // Layer checkbox
            RetroUI.DrawCheckbox(spriteBatch, pixel,
                new Vector2(bounds.X + 10, y),
                layerVisibility[i],
                $"Layer {i}",
                font, isHovered);

            // Active layer indicator
            if (isActive)
            {
                spriteBatch.DrawString(font, "◄",
                    new Vector2(bounds.Right - 30, y), RetroUI.Yellow);
            }

            y += 30;
        }

        // Buttons
        DrawLayerButtons(spriteBatch, pixel, font, bounds.Bottom - 80);
    }
}
```

---

## Part 10: Animation Preview

### Sprite Animation Viewer

```csharp
public class AnimationPreview
{
    private int currentFrame = 0;
    private float animationTimer = 0;
    private float frameDuration = 0.1f;

    public void Update(float deltaTime)
    {
        animationTimer += deltaTime;
        if (animationTimer >= frameDuration)
        {
            animationTimer = 0;
            currentFrame = (currentFrame + 1) % frameCount;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        RetroUI.DrawWindow(spriteBatch, pixel, bounds, "ANIMATION", font);

        // Draw current frame (large)
        Rectangle frameRect = new Rectangle(
            bounds.X + (bounds.Width - 128) / 2,
            bounds.Y + 40,
            128, 128
        );

        RetroUI.DrawPanel(spriteBatch, pixel, frameRect, inset: true);
        // Draw animated sprite here

        // Frame timeline
        DrawFrameTimeline(spriteBatch, pixel, font);

        // Controls
        RetroUI.DrawTextWithShadow(spriteBatch, font,
            $"Frame: {currentFrame + 1}/{frameCount}",
            new Vector2(bounds.X + 10, bounds.Bottom - 60), RetroUI.White);

        RetroUI.DrawProgressBar(spriteBatch, pixel,
            new Rectangle(bounds.X + 10, bounds.Bottom - 35, bounds.Width - 20, 20),
            (float)currentFrame / frameCount, RetroUI.Green);
    }
}
```

---

## Complete Integration Example

Here's how to integrate all features into your Level Editor:

```csharp
public class RetroLevelEditor : Scene
{
    // Core systems
    private EditorHistory history = new EditorHistory();
    private EditorClipboard clipboard = new EditorClipboard();
    private PrefabManager prefabManager = new PrefabManager();

    // UI panels
    private TilesetBrowser tilesetBrowser;
    private EntityPropertyPanel propertyPanel;
    private Minimap minimap;
    private LayerPanel layerPanel;
    private AnimationPreview animPreview;
    private PrefabBrowser prefabBrowser;

    // State
    private bool showScanlines = true;
    private bool showVignette = true;

    public override void OnEnter()
    {
        base.OnEnter();

        // Initialize UI panels
        tilesetBrowser = new TilesetBrowser(new Rectangle(10, 200, 200, 400));
        propertyPanel = new EntityPropertyPanel(new Rectangle(screenWidth - 210, 200, 200, 300));
        minimap = new Minimap(new Rectangle(screenWidth - 210, 10, 200, 180));
        // ... etc
    }

    public override void Update(GameTime gameTime)
    {
        HandleInput();
        UpdatePanels();
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        // Draw world (with camera)
        spriteBatch.Begin(transformMatrix: GetCameraMatrix());
        DrawLevel(spriteBatch);
        spriteBatch.End();

        // Draw UI (screen space)
        spriteBatch.Begin();
        DrawRetroUI(spriteBatch, gameTime);
        spriteBatch.End();

        // CRT effects (last)
        if (showScanlines || showVignette)
        {
            spriteBatch.Begin();
            if (showScanlines)
                RetroUI.DrawScanlines(spriteBatch, pixelTexture, screenBounds, 0.15f);
            if (showVignette)
                RetroUI.DrawCRTVignette(spriteBatch, pixelTexture, screenBounds);
            spriteBatch.End();
        }
    }
}
```

---

## Keyboard Shortcuts Summary

```
CTRL+Z      - Undo
CTRL+Y      - Redo
CTRL+C      - Copy selection
CTRL+V      - Paste
CTRL+X      - Cut
CTRL+S      - Save
CTRL+O      - Load
CTRL+N      - New
F5          - Test play
F11         - Toggle scanlines
F12         - Toggle vignette
```

---

## Summary

You now have:
✅ Complete retro UI toolkit (DOS/EGA style)
✅ Full undo/redo system
✅ Copy/paste infrastructure
✅ Prefab save/load system
✅ Tileset browser
✅ Entity property editor
✅ Minimap
✅ Layer management
✅ Animation preview
✅ Test-play functionality
✅ CRT scanline effects
✅ Vignette (curved screen)

All with authentic Commander Keen aesthetic! 🎮
