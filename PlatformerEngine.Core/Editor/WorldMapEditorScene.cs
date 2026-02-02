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
    /// World Map Editor - Create and edit world maps with level nodes
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

        // Selection and interaction
        private LevelNode selectedNode;
        private LevelNode connectStartNode;
        private LevelNode hoverNode;
        private Vector2 dragOffset;

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

        // Property editing
        private bool isEditingNodeName;
        private string editingNodeName = "";
        private int editingCursorPos;

        // Node appearance
        private const int NODE_RADIUS = 20;
        private const int NODE_SELECT_RADIUS = 25;
        private readonly Color NODE_COLOR = new Color(100, 150, 255);
        private readonly Color NODE_LOCKED_COLOR = new Color(128, 128, 128);
        private readonly Color NODE_SELECTED_COLOR = Color.Yellow;
        private readonly Color PATH_COLOR = new Color(255, 255, 255, 128);

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

            // Initialize empty world map
            if (worldMap == null)
            {
                worldMap = new WorldMapData
                {
                    MapName = "New World Map",
                    BackgroundTexture = "world_map_bg"
                };
            }
        }

        public override void Update(GameTime gameTime)
        {
            var input = InputManager.Instance;
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();
            Vector2 mouseWorldPos = ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));

            // ESC to exit
            if (input.IsActionPressed(InputAction.Pause))
            {
                Scenes.SceneManager.Instance.ChangeScene("MainMenu");
                return;
            }

            // Tool selection (1-4 keys)
            if (keyboardState.IsKeyDown(Keys.D1)) currentTool = EditorTool.Select;
            if (keyboardState.IsKeyDown(Keys.D2)) currentTool = EditorTool.AddNode;
            if (keyboardState.IsKeyDown(Keys.D3)) currentTool = EditorTool.Connect;
            if (keyboardState.IsKeyDown(Keys.D4)) currentTool = EditorTool.Delete;

            // Camera controls (middle mouse or space + drag)
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
            int scrollDelta = mouseState.ScrollWheelValue;
            static int previousScroll = 0;
            if (scrollDelta != previousScroll)
            {
                float zoomDelta = (scrollDelta - previousScroll) * 0.001f;
                cameraZoom = MathHelper.Clamp(cameraZoom + zoomDelta, 0.25f, 4.0f);
                previousScroll = scrollDelta;
            }

            // Toggle grid (G key)
            if (keyboardState.IsKeyDown(Keys.G))
            {
                showGrid = !showGrid;
            }

            // Save/Load (Ctrl+S, Ctrl+O)
            if (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl))
            {
                if (keyboardState.IsKeyDown(Keys.S))
                {
                    SaveWorldMap();
                }
                if (keyboardState.IsKeyDown(Keys.O))
                {
                    LoadWorldMap();
                }
            }

            // Find node under mouse
            hoverNode = FindNodeAtPosition(mouseWorldPos);

            // Handle property editing mode
            if (isEditingNodeName)
            {
                HandleNodeNameEditing(keyboardState);
                return; // Don't process other input while editing
            }

            // Handle different editor modes
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

        private void HandleNormalMode(MouseState mouseState, Vector2 mouseWorldPos)
        {
            // Left click actions
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                static bool wasPressed = false;
                if (!wasPressed)
                {
                    wasPressed = true;

                    switch (currentTool)
                    {
                        case EditorTool.Select:
                            if (hoverNode != null)
                            {
                                selectedNode = hoverNode;
                                dragOffset = selectedNode.Position - mouseWorldPos;
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
            }
            else
            {
                wasPressed = false;
            }

            // Right click to edit properties
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                static bool wasRightPressed = false;
                if (!wasRightPressed && hoverNode != null)
                {
                    wasRightPressed = true;
                    selectedNode = hoverNode;
                    StartEditingNodeName();
                }
            }
            else
            {
                wasRightPressed = false;
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
                currentMode = EditorMode.Normal;
            }
        }

        private void HandleConnectingNodes(MouseState mouseState, Vector2 mouseWorldPos)
        {
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                static bool wasPressed = false;
                if (!wasPressed && hoverNode != null && hoverNode != connectStartNode)
                {
                    wasPressed = true;
                    ConnectNodes(connectStartNode, hoverNode);
                    currentMode = EditorMode.Normal;
                    connectStartNode = null;
                }
            }
            else
            {
                wasPressed = false;
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
            // Get pressed keys
            var pressedKeys = keyboardState.GetPressedKeys();
            static Keys[] previousKeys = new Keys[0];

            var newKeys = pressedKeys.Except(previousKeys).ToArray();

            foreach (var key in newKeys)
            {
                if (key == Keys.Enter)
                {
                    // Finish editing
                    selectedNode.LevelName = editingNodeName;
                    selectedNode.Id = editingNodeName.ToLower().Replace(" ", "_");
                    isEditingNodeName = false;
                }
                else if (key == Keys.Escape)
                {
                    // Cancel editing
                    isEditingNodeName = false;
                }
                else if (key == Keys.Back && editingNodeName.Length > 0)
                {
                    // Backspace
                    editingNodeName = editingNodeName.Substring(0, editingNodeName.Length - 1);
                }
                else if (key == Keys.Space)
                {
                    editingNodeName += " ";
                }
                else
                {
                    // Try to convert key to character
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

            previousKeys = pressedKeys;
        }

        private void AddNode(Vector2 position)
        {
            int nodeCount = worldMap.Nodes.Count;
            var newNode = new LevelNode
            {
                Id = $"level{nodeCount + 1}",
                LevelName = $"Level {nodeCount + 1}",
                Position = position,
                IsUnlocked = nodeCount == 0 // First node is unlocked
            };

            worldMap.Nodes.Add(newNode);
            selectedNode = newNode;
        }

        private void DeleteNode(LevelNode node)
        {
            // Remove all connections to this node
            foreach (var otherNode in worldMap.Nodes)
            {
                otherNode.ConnectedNodeIds.Remove(node.Id);
            }

            worldMap.Nodes.Remove(node);
            if (selectedNode == node)
            {
                selectedNode = null;
            }
        }

        private void ConnectNodes(LevelNode nodeA, LevelNode nodeB)
        {
            // Add bidirectional connection
            if (!nodeA.ConnectedNodeIds.Contains(nodeB.Id))
            {
                nodeA.ConnectedNodeIds.Add(nodeB.Id);
            }
            if (!nodeB.ConnectedNodeIds.Contains(nodeA.Id))
            {
                nodeB.ConnectedNodeIds.Add(nodeA.Id);
            }
        }

        private void StartEditingNodeName()
        {
            isEditingNodeName = true;
            editingNodeName = selectedNode.LevelName;
            editingCursorPos = editingNodeName.Length;
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
            spriteBatch.GraphicsDevice.Clear(new Color(40, 40, 60));

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
                DrawLine(spriteBatch, connectStartNode.Position, mouseWorldPos, Color.Yellow, 3);
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
            DrawUI(spriteBatch);
            spriteBatch.End();
        }

        private Matrix GetCameraMatrix()
        {
            return Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0) *
                   Matrix.CreateScale(cameraZoom);
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
            DrawLine(spriteBatch, new Vector2(-50, 0), new Vector2(50, 0), Color.Red, 2);
            DrawLine(spriteBatch, new Vector2(0, -50), new Vector2(0, 50), Color.Green, 2);
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
                        // Only draw each connection once (from lower ID to higher ID)
                        if (string.Compare(node.Id, connectedId) < 0)
                        {
                            DrawLine(spriteBatch, node.Position, connectedNode.Position, PATH_COLOR, 4);
                        }
                    }
                }
            }
        }

        private void DrawNode(SpriteBatch spriteBatch, LevelNode node, bool isSelected, bool isHovered)
        {
            Color nodeColor = node.IsUnlocked ? NODE_COLOR : NODE_LOCKED_COLOR;
            if (isSelected) nodeColor = NODE_SELECTED_COLOR;
            else if (isHovered) nodeColor = Color.Lerp(nodeColor, Color.White, 0.3f);

            // Draw node circle
            DrawCircle(spriteBatch, node.Position, NODE_RADIUS, nodeColor, filled: true);
            DrawCircle(spriteBatch, node.Position, NODE_RADIUS, Color.White, filled: false, thickness: 2);

            // Draw stars if node is completed
            if (node.StarsEarned > 0)
            {
                for (int i = 0; i < node.StarsEarned; i++)
                {
                    Vector2 starPos = node.Position + new Vector2(-10 + i * 10, -NODE_RADIUS - 10);
                    DrawCircle(spriteBatch, starPos, 3, Color.Yellow, filled: true);
                }
            }

            // Draw node name
            if (font != null)
            {
                Vector2 textSize = font.MeasureString(node.LevelName);
                Vector2 textPos = node.Position - new Vector2(textSize.X / 2, NODE_RADIUS + textSize.Y + 5);

                // Text background
                DrawRectangle(spriteBatch, new Rectangle(
                    (int)textPos.X - 2,
                    (int)textPos.Y - 2,
                    (int)textSize.X + 4,
                    (int)textSize.Y + 4
                ), new Color(0, 0, 0, 192));

                spriteBatch.DrawString(font, node.LevelName, textPos, Color.White);
            }
        }

        private void DrawUI(SpriteBatch spriteBatch)
        {
            if (font == null) return;

            int y = 10;
            int lineHeight = 20;

            // Tool bar
            DrawRectangle(spriteBatch, new Rectangle(0, 0, screenBounds.Width, 150), new Color(0, 0, 0, 200));

            DrawText(spriteBatch, "WORLD MAP EDITOR", new Vector2(10, y), Color.Yellow);
            y += lineHeight * 2;

            DrawText(spriteBatch, $"Tool: {currentTool} (1-4 to switch)", new Vector2(10, y), Color.White);
            y += lineHeight;
            DrawText(spriteBatch, $"Nodes: {worldMap.Nodes.Count}", new Vector2(10, y), Color.White);
            y += lineHeight;
            DrawText(spriteBatch, $"Zoom: {cameraZoom:F2}x (Mouse Wheel)", new Vector2(10, y), Color.White);
            y += lineHeight;

            // Controls
            y += lineHeight;
            DrawText(spriteBatch, "Controls:", new Vector2(10, y), Color.Cyan);
            y += lineHeight;
            DrawText(spriteBatch, "  1=Select  2=Add  3=Connect  4=Delete", new Vector2(10, y), Color.Gray);
            y += lineHeight;
            DrawText(spriteBatch, "  Right Click=Edit Properties  Middle Mouse=Pan", new Vector2(10, y), Color.Gray);
            y += lineHeight;
            DrawText(spriteBatch, "  Ctrl+S=Save  Ctrl+O=Load  ESC=Exit", new Vector2(10, y), Color.Gray);

            // Selected node properties
            if (selectedNode != null)
            {
                int propX = screenBounds.Width - 300;
                int propY = 10;
                DrawRectangle(spriteBatch, new Rectangle(propX - 10, propY - 10, 290, 200), new Color(0, 0, 0, 200));

                DrawText(spriteBatch, "Selected Node:", new Vector2(propX, propY), Color.Yellow);
                propY += lineHeight;
                DrawText(spriteBatch, $"ID: {selectedNode.Id}", new Vector2(propX, propY), Color.White);
                propY += lineHeight;
                DrawText(spriteBatch, $"Name: {selectedNode.LevelName}", new Vector2(propX, propY), Color.White);
                propY += lineHeight;
                DrawText(spriteBatch, $"Unlocked: {selectedNode.IsUnlocked}", new Vector2(propX, propY), Color.White);
                propY += lineHeight;
                DrawText(spriteBatch, $"Completed: {selectedNode.IsCompleted}", new Vector2(propX, propY), Color.White);
                propY += lineHeight;
                DrawText(spriteBatch, $"Stars: {selectedNode.StarsEarned}/3", new Vector2(propX, propY), Color.White);
                propY += lineHeight;
                DrawText(spriteBatch, $"Connections: {selectedNode.ConnectedNodeIds.Count}", new Vector2(propX, propY), Color.White);
            }

            // Name editing overlay
            if (isEditingNodeName)
            {
                int boxWidth = 400;
                int boxHeight = 100;
                int boxX = (screenBounds.Width - boxWidth) / 2;
                int boxY = (screenBounds.Height - boxHeight) / 2;

                DrawRectangle(spriteBatch, new Rectangle(boxX, boxY, boxWidth, boxHeight), new Color(0, 0, 0, 240));
                DrawRectangle(spriteBatch, new Rectangle(boxX, boxY, boxWidth, boxHeight), Color.White, filled: false, thickness: 2);

                DrawText(spriteBatch, "Edit Level Name:", new Vector2(boxX + 10, boxY + 10), Color.Yellow);
                DrawText(spriteBatch, editingNodeName + "_", new Vector2(boxX + 10, boxY + 40), Color.White);
                DrawText(spriteBatch, "Press ENTER to confirm, ESC to cancel", new Vector2(boxX + 10, boxY + 70), Color.Gray);
            }
        }

        private void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
        {
            if (font != null)
            {
                spriteBatch.DrawString(font, text, position, color);
            }
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, bool filled = true, int thickness = 1)
        {
            if (filled)
            {
                spriteBatch.Draw(pixelTexture, rect, color);
            }
            else
            {
                // Top
                spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
                // Bottom
                spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
                // Left
                spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
                // Right
                spriteBatch.Draw(pixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
            }
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
                    // Draw triangle from center to edge
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
    }
}
