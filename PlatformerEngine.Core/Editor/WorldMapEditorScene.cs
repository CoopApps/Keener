using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlatformerEngine.Core.Editor
{
    /// <summary>
    /// Enhanced World Map Editor with retro UI
    /// </summary>
    public class WorldMapEditorScene : Scenes.Scene
    {
        private enum EditorTool
        {
            Select,      // Select and move nodes
            AddNode,     // Add new level nodes
            Connect,     // Connect nodes with paths
            Delete       // Delete nodes/connections
        }

        private enum EditorMode
        {
            Normal,
            ConnectingNodes,  // Drawing connection between two nodes
            DraggingNode,     // Moving a node
            PropertyEdit      // Editing node properties
        }

        // Editor state
        private EditorTool currentTool = EditorTool.Select;
        private EditorMode currentMode = EditorMode.Normal;
        private WorldMapData worldMap;
        private string currentFileName = "NewWorldMap";

        // Undo/Redo
        private EditorHistory history = new EditorHistory();

        // Selection and interaction
        private LevelNode selectedNode;
        private LevelNode connectStartNode;
        private LevelNode hoverNode;
        private Vector2 dragOffset;
        private Vector2 nodeStartPosition; // For undo

        // Camera
        private Vector2 cameraPosition;
        private float cameraZoom = 1.0f;
        private Vector2 cameraDragStart;
        private bool isDraggingCamera;

        // UI
        private SpriteFont font;
        private Texture2D pixelTexture;
        private Rectangle screenBounds;
        private bool showGrid = true;
        private int gridSize = 32;
        private bool enableCRT = false;
        private bool showScanlines = false;

        // Input tracking
        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;

        // Property editing
        private bool isEditingNodeName;
        private string editingNodeName = "";

        // Animation
        private float animationTime = 0;
        private GameTime lastGameTime;

        // Node appearance
        private const int NODE_RADIUS = 20;
        private const int NODE_SELECT_RADIUS = 25;

        // UI Rectangles
        private Rectangle topBar;
        private Rectangle sidePanel;

        public WorldMapEditorScene(SpriteFont font)
        {
            this.font = font;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Create pixel texture for drawing
            pixelTexture = new Texture2D(Scenes.SceneManager.Instance.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            screenBounds = Scenes.SceneManager.Instance.GraphicsDevice.Viewport.Bounds;

            // Setup UI rectangles
            topBar = new Rectangle(0, 0, screenBounds.Width, 80);
            sidePanel = new Rectangle(screenBounds.Width - 250, 80, 250, screenBounds.Height - 80);

            // Initialize empty world map
            if (worldMap == null)
            {
                worldMap = new WorldMapData
                {
                    MapName = "New World Map",
                    BackgroundTexture = "world_map_bg"
                };
            }

            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            lastGameTime = gameTime;
            animationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            var input = InputManager.Instance;
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();
            Vector2 mouseWorldPos = ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));

            // ESC to exit
            if (input.IsActionPressed(InputAction.Pause))
            {
                if (isEditingNodeName)
                {
                    isEditingNodeName = false;
                }
                else
                {
                    Scenes.SceneManager.Instance.ChangeScene("MainMenu");
                    return;
                }
            }

            // Handle keyboard shortcuts
            HandleKeyboardShortcuts(keyboardState);

            // Find node under mouse
            hoverNode = FindNodeAtPosition(mouseWorldPos);

            // Handle property editing mode
            if (isEditingNodeName)
            {
                HandleNodeNameEditing(keyboardState);
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                return; // Don't process other input while editing
            }

            // Camera controls
            HandleCameraControls(mouseState, keyboardState);

            // Handle different editor modes
            Vector2 mouseScreenPos = new Vector2(mouseState.X, mouseState.Y);
            bool isOverUI = topBar.Contains(mouseScreenPos) || sidePanel.Contains(mouseScreenPos);

            if (!isOverUI && !isDraggingCamera)
            {
                switch (currentMode)
                {
                    case EditorMode.Normal:
                        HandleNormalMode(mouseState, mouseWorldPos);
                        break;

                    case EditorMode.DraggingNode:
                        HandleDraggingNode(mouseState, mouseWorldPos);
                        break;

                    case EditorMode.ConnectingNodes:
                        HandleConnectingNodes(mouseState, mouseWorldPos);
                        break;
                }
            }

            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;
        }

        private void HandleKeyboardShortcuts(KeyboardState keyboardState)
        {
            bool ctrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);

            // Undo/Redo
            if (ctrl && IsKeyPressed(keyboardState, Keys.Z))
            {
                history.Undo();
            }
            if (ctrl && IsKeyPressed(keyboardState, Keys.Y))
            {
                history.Redo();
            }

            // Save/Load
            if (ctrl && IsKeyPressed(keyboardState, Keys.S))
            {
                SaveWorldMap();
            }
            if (ctrl && IsKeyPressed(keyboardState, Keys.O))
            {
                LoadWorldMap();
            }

            // Tool selection (1-4 keys)
            if (IsKeyPressed(keyboardState, Keys.D1)) currentTool = EditorTool.Select;
            if (IsKeyPressed(keyboardState, Keys.D2)) currentTool = EditorTool.AddNode;
            if (IsKeyPressed(keyboardState, Keys.D3)) currentTool = EditorTool.Connect;
            if (IsKeyPressed(keyboardState, Keys.D4)) currentTool = EditorTool.Delete;

            // Toggle grid
            if (IsKeyPressed(keyboardState, Keys.G)) showGrid = !showGrid;
        }

        private bool IsKeyPressed(KeyboardState current, Keys key)
        {
            return current.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
        }

        private void HandleCameraControls(MouseState mouseState, KeyboardState keyboardState)
        {
            // Camera pan (middle mouse or space + drag)
            if (mouseState.MiddleButton == ButtonState.Pressed ||
                (keyboardState.IsKeyDown(Keys.Space) && mouseState.LeftButton == ButtonState.Pressed))
            {
                if (!isDraggingCamera)
                {
                    isDraggingCamera = true;
                    cameraDragStart = new Vector2(mouseState.X, mouseState.Y);
                }
                else
                {
                    Vector2 delta = new Vector2(mouseState.X, mouseState.Y) - cameraDragStart;
                    cameraPosition -= delta / cameraZoom;
                    cameraDragStart = new Vector2(mouseState.X, mouseState.Y);
                }
            }
            else
            {
                isDraggingCamera = false;
            }

            // Camera zoom (mouse wheel)
            if (mouseState.ScrollWheelValue != previousMouseState.ScrollWheelValue)
            {
                float zoomDelta = (mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue) * 0.001f;
                cameraZoom = MathHelper.Clamp(cameraZoom + zoomDelta, 0.25f, 4.0f);
            }
        }

        private void HandleNormalMode(MouseState mouseState, Vector2 mouseWorldPos)
        {
            // Left click actions
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                switch (currentTool)
                {
                    case EditorTool.Select:
                        if (hoverNode != null)
                        {
                            selectedNode = hoverNode;
                            dragOffset = selectedNode.Position - mouseWorldPos;
                            nodeStartPosition = selectedNode.Position; // Store for undo
                            currentMode = EditorMode.DraggingNode;
                        }
                        else
                        {
                            selectedNode = null;
                        }
                        break;

                    case EditorTool.AddNode:
                        AddNode(mouseWorldPos);
                        break;

                    case EditorTool.Connect:
                        if (hoverNode != null)
                        {
                            connectStartNode = hoverNode;
                            currentMode = EditorMode.ConnectingNodes;
                        }
                        break;

                    case EditorTool.Delete:
                        if (hoverNode != null)
                        {
                            DeleteNode(hoverNode);
                        }
                        break;
                }
            }

            // Right click to edit properties
            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released && hoverNode != null)
            {
                selectedNode = hoverNode;
                StartEditingNodeName();
            }
        }

        private void HandleDraggingNode(MouseState mouseState, Vector2 mouseWorldPos)
        {
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                selectedNode.Position = mouseWorldPos + dragOffset;

                // Snap to grid if enabled
                if (showGrid)
                {
                    selectedNode.Position = new Vector2(
                        MathHelper.Round(selectedNode.Position.X / gridSize) * gridSize,
                        MathHelper.Round(selectedNode.Position.Y / gridSize) * gridSize
                    );
                }
            }
            else
            {
                // Create undo action for node movement
                var action = new MoveNodeAction(selectedNode, nodeStartPosition, selectedNode.Position);
                history.ExecuteAction(action);
                currentMode = EditorMode.Normal;
            }
        }

        private void HandleConnectingNodes(MouseState mouseState, Vector2 mouseWorldPos)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                if (hoverNode != null && hoverNode != connectStartNode)
                {
                    ConnectNodes(connectStartNode, hoverNode);
                    currentMode = EditorMode.Normal;
                    connectStartNode = null;
                }
            }

            // Right click to cancel
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                currentMode = EditorMode.Normal;
                connectStartNode = null;
            }
        }

        private void HandleNodeNameEditing(KeyboardState keyboardState)
        {
            var pressedKeys = keyboardState.GetPressedKeys();
            var previousKeys = previousKeyboardState.GetPressedKeys();
            var newKeys = pressedKeys.Except(previousKeys).ToArray();

            foreach (var key in newKeys)
            {
                if (key == Keys.Enter)
                {
                    selectedNode.LevelName = editingNodeName;
                    selectedNode.Id = editingNodeName.ToLower().Replace(" ", "_");
                    isEditingNodeName = false;
                }
                else if (key == Keys.Escape)
                {
                    isEditingNodeName = false;
                }
                else if (key == Keys.Back && editingNodeName.Length > 0)
                {
                    editingNodeName = editingNodeName.Substring(0, editingNodeName.Length - 1);
                }
                else if (key == Keys.Space)
                {
                    editingNodeName += " ";
                }
                else
                {
                    string keyString = key.ToString();
                    if (keyString.Length == 1)
                    {
                        char c = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)
                            ? keyString[0]
                            : char.ToLower(keyString[0]);
                        editingNodeName += c;
                    }
                }
            }
        }

        private void AddNode(Vector2 position)
        {
            int nodeCount = worldMap.Nodes.Count;
            var newNode = new LevelNode
            {
                Id = $"level{nodeCount + 1}",
                LevelName = $"Level {nodeCount + 1}",
                Position = position,
                IsUnlocked = nodeCount == 0
            };

            var action = new AddNodeAction(worldMap.Nodes, newNode);
            history.ExecuteAction(action);
            selectedNode = newNode;
        }

        private void DeleteNode(LevelNode node)
        {
            var action = new DeleteNodeAction(worldMap.Nodes, node);
            history.ExecuteAction(action);
            if (selectedNode == node)
            {
                selectedNode = null;
            }
        }

        private void ConnectNodes(LevelNode nodeA, LevelNode nodeB)
        {
            // Check if connection already exists
            if (!nodeA.ConnectedNodeIds.Contains(nodeB.Id))
            {
                var action = new ConnectNodesAction(nodeA, nodeB);
                history.ExecuteAction(action);
            }
        }

        private void StartEditingNodeName()
        {
            isEditingNodeName = true;
            editingNodeName = selectedNode.LevelName;
        }

        private LevelNode FindNodeAtPosition(Vector2 worldPos)
        {
            foreach (var node in worldMap.Nodes)
            {
                float distance = Vector2.Distance(node.Position, worldPos);
                if (distance <= NODE_SELECT_RADIUS)
                {
                    return node;
                }
            }
            return null;
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return (screenPos / cameraZoom) + cameraPosition;
        }

        private Vector2 WorldToScreen(Vector2 worldPos)
        {
            return (worldPos - cameraPosition) * cameraZoom;
        }

        private void SaveWorldMap()
        {
            try
            {
                Directory.CreateDirectory("WorldMaps");
                string path = $"WorldMaps/{currentFileName}.json";
                worldMap.Save(path);
                Console.WriteLine($"Saved world map to {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving world map: {ex.Message}");
            }
        }

        private void LoadWorldMap()
        {
            try
            {
                string path = $"WorldMaps/{currentFileName}.json";
                if (File.Exists(path))
                {
                    worldMap = WorldMapData.Load(path);
                    history.Clear();
                    Console.WriteLine($"Loaded world map from {path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading world map: {ex.Message}");
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.GraphicsDevice.Clear(RetroUI.Black);

            // Draw world space (with camera)
            spriteBatch.Begin(transformMatrix: GetCameraMatrix(), samplerState: SamplerState.PointClamp);

            // Draw grid
            if (showGrid)
            {
                DrawGrid(spriteBatch);
            }

            // Draw connections between nodes
            DrawConnections(spriteBatch);

            // Draw connection being created
            if (currentMode == EditorMode.ConnectingNodes && connectStartNode != null)
            {
                var mouseState = Mouse.GetState();
                Vector2 mouseWorldPos = ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
                DrawLine(spriteBatch, connectStartNode.Position, mouseWorldPos, RetroUI.Yellow, 3);
            }

            // Draw nodes
            foreach (var node in worldMap.Nodes)
            {
                bool isSelected = node == selectedNode;
                bool isHovered = node == hoverNode;
                DrawNode(spriteBatch, node, isSelected, isHovered);
            }

            spriteBatch.End();

            // Draw UI (screen space)
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Top bar
            DrawTopBar(spriteBatch);

            // Side panel
            DrawSidePanel(spriteBatch);

            // Name editing overlay
            if (isEditingNodeName)
            {
                DrawNameEditingOverlay(spriteBatch);
            }

            // CRT effects
            if (enableCRT)
            {
                if (showScanlines)
                {
                    RetroUI.DrawScanlines(spriteBatch, pixelTexture, screenBounds, 0.2f);
                }
                RetroUI.DrawCRTVignette(spriteBatch, pixelTexture, screenBounds);
            }

            spriteBatch.End();
        }

        private void DrawTopBar(SpriteBatch spriteBatch)
        {
            RetroUI.DrawPanel(spriteBatch, pixelTexture, topBar, inset: false);

            int x = 10;
            int y = 10;

            RetroUI.DrawTextWithShadow(spriteBatch, font, "WORLD MAP EDITOR v2.0", new Vector2(x, y), RetroUI.Yellow);
            y += 25;

            string toolText = $"Tool: {currentTool} [1-4]  Nodes: {worldMap.Nodes.Count}  Zoom: {cameraZoom:F1}x";
            spriteBatch.DrawString(font, toolText, new Vector2(x, y), RetroUI.White);
            y += 20;

            string controlsText = "Ctrl+S=Save  Ctrl+O=Load  Right-Click=Edit  ESC=Exit";
            spriteBatch.DrawString(font, controlsText, new Vector2(x, y), RetroUI.Gray);

            // Undo/Redo status
            x = screenBounds.Width / 2;
            y = 15;
            string undoText = $"Undo: {history.GetUndoDescription()}";
            string redoText = $"Redo: {history.GetRedoDescription()}";
            spriteBatch.DrawString(font, undoText, new Vector2(x, y), history.CanUndo ? RetroUI.Cyan : RetroUI.DarkGray);
            spriteBatch.DrawString(font, redoText, new Vector2(x, y + 20), history.CanRedo ? RetroUI.Cyan : RetroUI.DarkGray);
        }

        private void DrawSidePanel(SpriteBatch spriteBatch)
        {
            RetroUI.DrawPanel(spriteBatch, pixelTexture, sidePanel, inset: true);

            // Tool selection
            Rectangle toolPanel = new Rectangle(sidePanel.X + 10, sidePanel.Y + 10, sidePanel.Width - 20, 180);
            RetroUI.DrawWindow(spriteBatch, pixelTexture, toolPanel, "TOOLS", font);

            int y = toolPanel.Y + 35;
            int x = toolPanel.X + 10;
            int lineHeight = 28;

            var tools = new[] { EditorTool.Select, EditorTool.AddNode, EditorTool.Connect, EditorTool.Delete };
            var toolLabels = new[] { "1. Select & Move", "2. Add Node", "3. Connect", "4. Delete" };

            for (int i = 0; i < tools.Length; i++)
            {
                bool isSelected = currentTool == tools[i];
                Rectangle toolRect = new Rectangle(x, y + i * lineHeight, toolPanel.Width - 20, lineHeight - 3);

                if (isSelected)
                {
                    spriteBatch.Draw(pixelTexture, toolRect, RetroUI.SelectedColor);
                }

                Color textColor = isSelected ? RetroUI.Yellow : RetroUI.White;
                spriteBatch.DrawString(font, toolLabels[i], new Vector2(x + 5, y + i * lineHeight + 4), textColor);
            }

            // Selected node properties
            if (selectedNode != null)
            {
                Rectangle propPanel = new Rectangle(sidePanel.X + 10, toolPanel.Bottom + 20, sidePanel.Width - 20, 200);
                RetroUI.DrawWindow(spriteBatch, pixelTexture, propPanel, "NODE PROPERTIES", font);

                int propY = propPanel.Y + 35;
                int propX = propPanel.X + 10;
                int propLineHeight = 22;

                spriteBatch.DrawString(font, $"ID: {selectedNode.Id}", new Vector2(propX, propY), RetroUI.White);
                propY += propLineHeight;
                spriteBatch.DrawString(font, $"Name: {selectedNode.LevelName}", new Vector2(propX, propY), RetroUI.White);
                propY += propLineHeight;
                spriteBatch.DrawString(font, $"Position:", new Vector2(propX, propY), RetroUI.Cyan);
                propY += propLineHeight;
                spriteBatch.DrawString(font, $"  X: {selectedNode.Position.X:F0}", new Vector2(propX, propY), RetroUI.Gray);
                propY += propLineHeight;
                spriteBatch.DrawString(font, $"  Y: {selectedNode.Position.Y:F0}", new Vector2(propX, propY), RetroUI.Gray);
                propY += propLineHeight;

                // Unlocked checkbox
                Vector2 checkPos = new Vector2(propX, propY);
                bool checkHovered = new Rectangle((int)checkPos.X, (int)checkPos.Y, 100, 16).Contains(Mouse.GetState().Position);
                RetroUI.DrawCheckbox(spriteBatch, pixelTexture, checkPos, selectedNode.IsUnlocked, "Unlocked", font, checkHovered);
                propY += propLineHeight;

                spriteBatch.DrawString(font, $"Connections: {selectedNode.ConnectedNodeIds.Count}", new Vector2(propX, propY), RetroUI.White);
            }
        }

        private void DrawNameEditingOverlay(SpriteBatch spriteBatch)
        {
            int boxWidth = 400;
            int boxHeight = 120;
            int boxX = (screenBounds.Width - boxWidth) / 2;
            int boxY = (screenBounds.Height - boxHeight) / 2;

            Rectangle panelRect = new Rectangle(boxX, boxY, boxWidth, boxHeight);
            RetroUI.DrawWindow(spriteBatch, pixelTexture, panelRect, "EDIT LEVEL NAME", font);

            int y = panelRect.Y + 40;

            // Input field
            Rectangle inputRect = new Rectangle(boxX + 20, y, boxWidth - 40, 30);
            RetroUI.DrawPanel(spriteBatch, pixelTexture, inputRect, inset: true);

            string displayText = editingNodeName + (((int)(animationTime * 2) % 2 == 0) ? "_" : " ");
            spriteBatch.DrawString(font, displayText, new Vector2(inputRect.X + 5, inputRect.Y + 5), RetroUI.Yellow);

            y += 45;
            spriteBatch.DrawString(font, "Press ENTER to confirm, ESC to cancel", new Vector2(boxX + 20, y), RetroUI.Gray);
        }

        private void DrawGrid(SpriteBatch spriteBatch)
        {
            // Calculate visible grid bounds
            Vector2 topLeft = ScreenToWorld(Vector2.Zero);
            Vector2 bottomRight = ScreenToWorld(new Vector2(screenBounds.Width, screenBounds.Height));

            int startX = (int)(topLeft.X / gridSize) * gridSize;
            int endX = (int)(bottomRight.X / gridSize) * gridSize + gridSize;
            int startY = (int)(topLeft.Y / gridSize) * gridSize;
            int endY = (int)(bottomRight.Y / gridSize) * gridSize + gridSize;

            Color gridColor = new Color(80, 80, 100, 128);

            // Draw vertical lines
            for (int x = startX; x <= endX; x += gridSize)
            {
                DrawLine(spriteBatch, new Vector2(x, startY), new Vector2(x, endY), gridColor, 1);
            }

            // Draw horizontal lines
            for (int y = startY; y <= endY; y += gridSize)
            {
                DrawLine(spriteBatch, new Vector2(startX, y), new Vector2(endX, y), gridColor, 1);
            }

            // Draw origin
            DrawLine(spriteBatch, new Vector2(-50, 0), new Vector2(50, 0), RetroUI.Red, 2);
            DrawLine(spriteBatch, new Vector2(0, -50), new Vector2(0, 50), RetroUI.Green, 2);
        }

        private void DrawConnections(SpriteBatch spriteBatch)
        {
            foreach (var node in worldMap.Nodes)
            {
                foreach (var connectedId in node.ConnectedNodeIds)
                {
                    var connectedNode = worldMap.Nodes.FirstOrDefault(n => n.Id == connectedId);
                    if (connectedNode != null)
                    {
                        // Only draw each connection once
                        if (string.Compare(node.Id, connectedId) < 0)
                        {
                            Color pathColor = new Color(255, 255, 255, 128);
                            DrawLine(spriteBatch, node.Position, connectedNode.Position, pathColor, 4);
                        }
                    }
                }
            }
        }

        private void DrawNode(SpriteBatch spriteBatch, LevelNode node, bool isSelected, bool isHovered)
        {
            Color nodeColor = node.IsUnlocked ? RetroUI.Blue : RetroUI.DarkGray;

            // Animation for selected/hovered
            if (isSelected)
            {
                float pulse = (float)Math.Sin(animationTime * 4) * 0.3f + 0.7f;
                nodeColor = Color.Lerp(nodeColor, RetroUI.Yellow, pulse);
            }
            else if (isHovered)
            {
                nodeColor = Color.Lerp(nodeColor, RetroUI.White, 0.3f);
            }

            // Draw node circle
            DrawCircle(spriteBatch, node.Position, NODE_RADIUS, nodeColor, filled: true);
            DrawCircle(spriteBatch, node.Position, NODE_RADIUS, RetroUI.White, filled: false, thickness: 2);

            // Draw stars if node is completed
            if (node.StarsEarned > 0)
            {
                for (int i = 0; i < node.StarsEarned; i++)
                {
                    Vector2 starPos = node.Position + new Vector2(-10 + i * 10, -NODE_RADIUS - 10);
                    DrawCircle(spriteBatch, starPos, 3, RetroUI.Yellow, filled: true);
                }
            }

            // Draw node name
            if (font != null)
            {
                Vector2 textSize = font.MeasureString(node.LevelName);
                Vector2 textPos = node.Position - new Vector2(textSize.X / 2, NODE_RADIUS + textSize.Y + 5);

                // Text background
                Rectangle textBg = new Rectangle(
                    (int)textPos.X - 4,
                    (int)textPos.Y - 2,
                    (int)textSize.X + 8,
                    (int)textSize.Y + 4
                );
                spriteBatch.Draw(pixelTexture, textBg, new Color(0, 0, 0, 192));
                DrawRectangleOutline(spriteBatch, textBg, RetroUI.Cyan, 1);

                spriteBatch.DrawString(font, node.LevelName, textPos, RetroUI.White);
            }
        }

        private Matrix GetCameraMatrix()
        {
            return Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0) *
                   Matrix.CreateScale(cameraZoom);
        }

        private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();

            spriteBatch.Draw(pixelTexture,
                new Rectangle((int)start.X, (int)start.Y, (int)length, thickness),
                null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }

        private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, bool filled = true, int thickness = 1)
        {
            int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)i / segments * MathHelper.TwoPi;
                float angle2 = (float)(i + 1) / segments * MathHelper.TwoPi;

                Vector2 p1 = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * radius;
                Vector2 p2 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * radius;

                if (filled)
                {
                    DrawLine(spriteBatch, center, p1, color, (int)radius);
                }
                else
                {
                    DrawLine(spriteBatch, p1, p2, color, thickness);
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            pixelTexture?.Dispose();
        }

        // Undo/Redo Actions for World Map Editor

        private class AddNodeAction : IEditorAction
        {
            private List<LevelNode> nodes;
            private LevelNode node;

            public string Description => $"Add node {node.LevelName}";

            public AddNodeAction(List<LevelNode> nodes, LevelNode node)
            {
                this.nodes = nodes;
                this.node = node;
            }

            public void Execute()
            {
                if (!nodes.Contains(node))
                {
                    nodes.Add(node);
                }
            }

            public void Undo()
            {
                nodes.Remove(node);
            }
        }

        private class DeleteNodeAction : IEditorAction
        {
            private List<LevelNode> nodes;
            private LevelNode node;
            private int index;
            private List<string> connections;

            public string Description => $"Delete node {node.LevelName}";

            public DeleteNodeAction(List<LevelNode> nodes, LevelNode node)
            {
                this.nodes = nodes;
                this.node = node;
                this.index = nodes.IndexOf(node);
                this.connections = new List<string>(node.ConnectedNodeIds);
            }

            public void Execute()
            {
                // Remove all connections to this node
                foreach (var otherNode in nodes)
                {
                    otherNode.ConnectedNodeIds.Remove(node.Id);
                }
                nodes.Remove(node);
            }

            public void Undo()
            {
                if (index >= 0 && index <= nodes.Count)
                {
                    nodes.Insert(index, node);
                }
                else
                {
                    nodes.Add(node);
                }

                // Restore connections
                node.ConnectedNodeIds.Clear();
                foreach (var connId in connections)
                {
                    node.ConnectedNodeIds.Add(connId);
                }
            }
        }

        private class MoveNodeAction : IEditorAction
        {
            private LevelNode node;
            private Vector2 oldPosition;
            private Vector2 newPosition;

            public string Description => $"Move node {node.LevelName}";

            public MoveNodeAction(LevelNode node, Vector2 oldPosition, Vector2 newPosition)
            {
                this.node = node;
                this.oldPosition = oldPosition;
                this.newPosition = newPosition;
            }

            public void Execute()
            {
                node.Position = newPosition;
            }

            public void Undo()
            {
                node.Position = oldPosition;
            }
        }

        private class ConnectNodesAction : IEditorAction
        {
            private LevelNode nodeA;
            private LevelNode nodeB;

            public string Description => $"Connect {nodeA.LevelName} to {nodeB.LevelName}";

            public ConnectNodesAction(LevelNode nodeA, LevelNode nodeB)
            {
                this.nodeA = nodeA;
                this.nodeB = nodeB;
            }

            public void Execute()
            {
                if (!nodeA.ConnectedNodeIds.Contains(nodeB.Id))
                {
                    nodeA.ConnectedNodeIds.Add(nodeB.Id);
                }
                if (!nodeB.ConnectedNodeIds.Contains(nodeA.Id))
                {
                    nodeB.ConnectedNodeIds.Add(nodeA.Id);
                }
            }

            public void Undo()
            {
                nodeA.ConnectedNodeIds.Remove(nodeB.Id);
                nodeB.ConnectedNodeIds.Remove(nodeA.Id);
            }
        }
    }
}
