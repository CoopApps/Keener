using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Entities;
using PlatformerEngine.Core.Input;
using PlatformerEngine.Core.Scenes;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Screens
{
    /// <summary>
    /// Level node on world map
    /// </summary>
    public class LevelNode
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public Vector2 Position { get; set; }
        public List<string> ConnectedNodeIds { get; set; } = new List<string>();
        public bool IsUnlocked { get; set; }
        public bool IsCompleted { get; set; }
        public int StarsEarned { get; set; }
        public Texture2D Icon { get; set; }
        public Color NodeColor { get; set; } = Color.White;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// World map data
    /// </summary>
    public class WorldMapData
    {
        public string Name { get; set; }
        public List<LevelNode> Nodes { get; set; } = new List<LevelNode>();
        public string CurrentNodeId { get; set; }
        public Texture2D BackgroundTexture { get; set; }

        public LevelNode GetNode(string id)
        {
            return Nodes.Find(n => n.Id == id);
        }

        public List<LevelNode> GetConnectedNodes(string nodeId)
        {
            var node = GetNode(nodeId);
            if (node == null) return new List<LevelNode>();

            var connected = new List<LevelNode>();
            foreach (var connectedId in node.ConnectedNodeIds)
            {
                var connectedNode = GetNode(connectedId);
                if (connectedNode != null)
                    connected.Add(connectedNode);
            }
            return connected;
        }
    }

    /// <summary>
    /// Player cursor on world map
    /// </summary>
    public class MapCursor
    {
        public Vector2 Position { get; set; }
        public Vector2 TargetPosition { get; set; }
        public float MoveSpeed { get; set; } = 200f;
        public bool IsMoving => Vector2.Distance(Position, TargetPosition) > 1f;

        public void Update(float deltaTime)
        {
            if (IsMoving)
            {
                Vector2 direction = TargetPosition - Position;
                float distance = direction.Length();

                if (distance > MoveSpeed * deltaTime)
                {
                    direction.Normalize();
                    Position += direction * MoveSpeed * deltaTime;
                }
                else
                {
                    Position = TargetPosition;
                }
            }
        }

        public void MoveTo(Vector2 target)
        {
            TargetPosition = target;
        }
    }

    /// <summary>
    /// World map / Level select screen
    /// </summary>
    public class WorldMapScreen : Scene
    {
        private WorldMapData worldMap;
        private MapCursor cursor;
        private SpriteFont font;
        private SpriteFont titleFont;
        private Texture2D pixelTexture;
        private Texture2D cursorTexture;
        private InputManager input;

        // Current selection
        private LevelNode currentNode;
        private List<LevelNode> availableNodes = new List<LevelNode>();
        private int selectedNodeIndex = 0;

        // Camera
        private Vector2 cameraPosition;
        private Vector2 targetCameraPosition;
        private float cameraSpeed = 3f;

        // UI
        private bool showLevelInfo = false;
        private float infoFadeAlpha = 0f;

        // Colors
        private Color unlockedColor = Color.White;
        private Color lockedColor = Color.Gray;
        private Color completedColor = Color.Gold;
        private Color pathColor = Color.White;

        // Events
        public Action<string> OnLevelSelected { get; set; }
        public Action OnBack { get; set; }

        public WorldMapScreen(string name = "WorldMap") : base(name)
        {
            cursor = new MapCursor();
        }

        public void Initialize(SpriteFont font, SpriteFont titleFont, WorldMapData mapData)
        {
            this.font = font;
            this.titleFont = titleFont;
            this.worldMap = mapData;
            input = InputManager.Instance;

            // Set initial node
            if (!string.IsNullOrEmpty(worldMap.CurrentNodeId))
            {
                currentNode = worldMap.GetNode(worldMap.CurrentNodeId);
            }
            else
            {
                currentNode = worldMap.Nodes.Count > 0 ? worldMap.Nodes[0] : null;
            }

            if (currentNode != null)
            {
                cursor.Position = currentNode.Position;
                cursor.TargetPosition = currentNode.Position;
                cameraPosition = currentNode.Position;
                targetCameraPosition = currentNode.Position;
            }

            UpdateAvailableNodes();
        }

        public override void LoadContent()
        {
            base.LoadContent();

            if (pixelTexture == null && game != null)
            {
                pixelTexture = new Texture2D(game.GraphicsDevice, 1, 1);
                pixelTexture.SetData(new[] { Color.White });

                // Create cursor texture (simple circle)
                cursorTexture = CreateCursorTexture(game.GraphicsDevice);
            }
        }

        private Texture2D CreateCursorTexture(GraphicsDevice graphicsDevice)
        {
            int size = 32;
            var texture = new Texture2D(graphicsDevice, size, size);
            var data = new Color[size * size];

            int center = size / 2;
            int radius = size / 2 - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x - center;
                    int dy = y - center;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius && distance >= radius - 3)
                    {
                        data[y * size + x] = Color.Yellow;
                    }
                    else
                    {
                        data[y * size + x] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        public override void Update(GameTime gameTime)
        {
            if (input == null || worldMap == null)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update cursor
            cursor.Update(deltaTime);

            // Update camera (follow cursor)
            if (Vector2.Distance(cameraPosition, targetCameraPosition) > 1f)
            {
                cameraPosition = Vector2.Lerp(cameraPosition, targetCameraPosition, cameraSpeed * deltaTime);
            }

            // Update info panel fade
            if (showLevelInfo)
            {
                infoFadeAlpha = Math.Min(infoFadeAlpha + deltaTime * 4f, 1f);
            }
            else
            {
                infoFadeAlpha = Math.Max(infoFadeAlpha - deltaTime * 4f, 0f);
            }

            // Input handling
            if (!cursor.IsMoving)
            {
                HandleInput();
            }
        }

        private void HandleInput()
        {
            // Move between connected nodes
            if (input.IsActionPressed(InputAction.MoveLeft) || input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Left))
            {
                SelectPreviousNode();
            }

            if (input.IsActionPressed(InputAction.MoveRight) || input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Right))
            {
                SelectNextNode();
            }

            if (input.IsActionPressed(InputAction.MoveUp) || input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Up))
            {
                SelectPreviousNode();
            }

            if (input.IsActionPressed(InputAction.MoveDown) || input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Down))
            {
                SelectNextNode();
            }

            // Select level
            if (input.IsActionPressed(InputAction.Confirm) || input.IsActionPressed(InputAction.Jump))
            {
                if (currentNode != null && currentNode.IsUnlocked)
                {
                    OnLevelSelected?.Invoke(currentNode.Id);
                }
            }

            // Toggle info
            if (input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.I))
            {
                showLevelInfo = !showLevelInfo;
            }

            // Back
            if (input.IsActionPressed(InputAction.Cancel) || input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                OnBack?.Invoke();
                sceneManager?.PopScene();
            }
        }

        private void SelectNextNode()
        {
            if (availableNodes.Count == 0) return;

            selectedNodeIndex = (selectedNodeIndex + 1) % availableNodes.Count;
            MoveToCurrent Node(availableNodes[selectedNodeIndex]);
        }

        private void SelectPreviousNode()
        {
            if (availableNodes.Count == 0) return;

            selectedNodeIndex--;
            if (selectedNodeIndex < 0)
                selectedNodeIndex = availableNodes.Count - 1;

            MoveToCurrentNode(availableNodes[selectedNodeIndex]);
        }

        private void MoveToCurrentNode(LevelNode node)
        {
            currentNode = node;
            cursor.MoveTo(node.Position);
            targetCameraPosition = node.Position;
        }

        private void UpdateAvailableNodes()
        {
            if (currentNode == null) return;

            availableNodes.Clear();

            // Add current node
            availableNodes.Add(currentNode);

            // Add connected unlocked nodes
            var connected = worldMap.GetConnectedNodes(currentNode.Id);
            foreach (var node in connected)
            {
                if (node.IsUnlocked)
                {
                    availableNodes.Add(node);
                }
            }

            selectedNodeIndex = 0;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (worldMap == null || font == null)
                return;

            var graphics = game.GraphicsDevice;
            int screenWidth = graphics.Viewport.Width;
            int screenHeight = graphics.Viewport.Height;

            // Calculate camera transform
            Matrix cameraTransform = Matrix.CreateTranslation(
                -cameraPosition.X + screenWidth / 2f,
                -cameraPosition.Y + screenHeight / 2f,
                0f
            );

            // Draw background
            if (worldMap.BackgroundTexture != null)
            {
                spriteBatch.Draw(worldMap.BackgroundTexture,
                    new Rectangle(0, 0, screenWidth, screenHeight),
                    Color.White);
            }

            // Begin world space drawing
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, null, cameraTransform);

            // Draw paths between nodes
            DrawPaths(spriteBatch);

            // Draw nodes
            DrawNodes(spriteBatch);

            // Draw cursor
            if (cursorTexture != null && currentNode != null)
            {
                float pulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 3f) * 0.2f + 0.8f;
                spriteBatch.Draw(cursorTexture,
                    cursor.Position - new Vector2(16, 16),
                    Color.White * pulse);
            }

            spriteBatch.End();

            // Begin screen space UI drawing
            spriteBatch.Begin();

            // Draw level info panel
            if (infoFadeAlpha > 0f && currentNode != null)
            {
                DrawLevelInfo(spriteBatch, screenWidth, screenHeight);
            }

            // Draw instructions
            DrawInstructions(spriteBatch, screenWidth, screenHeight);

            spriteBatch.End();

            // Restart for next frame
            spriteBatch.Begin();
        }

        private void DrawPaths(SpriteBatch spriteBatch)
        {
            foreach (var node in worldMap.Nodes)
            {
                foreach (var connectedId in node.ConnectedNodeIds)
                {
                    var connectedNode = worldMap.GetNode(connectedId);
                    if (connectedNode != null)
                    {
                        DrawPath(spriteBatch, node.Position, connectedNode.Position,
                            node.IsUnlocked && connectedNode.IsUnlocked);
                    }
                }
            }
        }

        private void DrawPath(SpriteBatch spriteBatch, Vector2 start, Vector2 end, bool unlocked)
        {
            Color color = unlocked ? pathColor : lockedColor;
            color *= 0.5f;

            // Simple line (for better visuals, use actual path texture)
            Vector2 direction = end - start;
            float distance = direction.Length();
            float angle = (float)Math.Atan2(direction.Y, direction.X);

            spriteBatch.Draw(pixelTexture,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(distance, 3),
                SpriteEffects.None,
                0f);
        }

        private void DrawNodes(SpriteBatch spriteBatch)
        {
            foreach (var node in worldMap.Nodes)
            {
                Color nodeColor = node.IsCompleted ? completedColor :
                                 node.IsUnlocked ? unlockedColor :
                                 lockedColor;

                // Draw node icon or circle
                if (node.Icon != null)
                {
                    spriteBatch.Draw(node.Icon,
                        node.Position - new Vector2(16, 16),
                        nodeColor);
                }
                else
                {
                    // Draw simple circle
                    Rectangle nodeBounds = new Rectangle(
                        (int)node.Position.X - 12,
                        (int)node.Position.Y - 12,
                        24, 24
                    );
                    spriteBatch.Draw(pixelTexture, nodeBounds, nodeColor);
                }

                // Draw stars if completed
                if (node.IsCompleted && node.StarsEarned > 0)
                {
                    for (int i = 0; i < node.StarsEarned; i++)
                    {
                        Vector2 starPos = node.Position + new Vector2(-10 + i * 10, -25);
                        spriteBatch.DrawString(font, "★", starPos, Color.Gold);
                    }
                }
            }
        }

        private void DrawLevelInfo(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            int panelWidth = 300;
            int panelHeight = 200;
            int panelX = screenWidth - panelWidth - 20;
            int panelY = 20;

            // Draw panel background
            Rectangle panelBounds = new Rectangle(panelX, panelY, panelWidth, panelHeight);
            spriteBatch.Draw(pixelTexture, panelBounds, new Color(0, 0, 0, 180) * infoFadeAlpha);

            // Draw border
            DrawBorder(spriteBatch, panelBounds, Color.White * infoFadeAlpha, 2);

            // Draw level info
            int textX = panelX + 20;
            int textY = panelY + 20;
            int lineHeight = 25;

            spriteBatch.DrawString(font, currentNode.DisplayName, new Vector2(textX, textY), Color.White * infoFadeAlpha);
            textY += lineHeight + 10;

            string status = currentNode.IsCompleted ? "COMPLETED" :
                           currentNode.IsUnlocked ? "UNLOCKED" : "LOCKED";
            Color statusColor = currentNode.IsCompleted ? Color.Gold :
                               currentNode.IsUnlocked ? Color.Green : Color.Red;

            spriteBatch.DrawString(font, $"Status: {status}", new Vector2(textX, textY), statusColor * infoFadeAlpha);
            textY += lineHeight;

            if (currentNode.IsCompleted)
            {
                spriteBatch.DrawString(font, $"Stars: {currentNode.StarsEarned}/3", new Vector2(textX, textY), Color.White * infoFadeAlpha);
            }
        }

        private void DrawInstructions(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            string instructions = "Arrows: Move | SPACE: Select | I: Info | ESC: Back";
            Vector2 size = font.MeasureString(instructions);
            Vector2 pos = new Vector2((screenWidth - size.X) / 2f, screenHeight - 30);
            spriteBatch.DrawString(font, instructions, pos, Color.White * 0.7f);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}
