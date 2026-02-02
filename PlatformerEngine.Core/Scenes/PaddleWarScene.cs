using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Audio;
using PlatformerEngine.Core.Input;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Scenes
{
    /// <summary>
    /// Paddle War - Breakout/Pong bonus game (like in Commander Keen)
    /// </summary>
    public class PaddleWarScene : Scene
    {
        private SpriteFont font;

        // Game objects
        private Paddle playerPaddle;
        private Ball ball;
        private List<Brick> bricks;

        // Game state
        private int score;
        private int lives = 3;
        private int level = 1;
        private bool isGameOver;
        private float gameOverTimer;

        // Resources
        private Texture2D pixelTexture; // 1x1 white texture for drawing

        // Constants
        private const int SCREEN_WIDTH = 640;
        private const int SCREEN_HEIGHT = 480;
        private const int PADDLE_WIDTH = 80;
        private const int PADDLE_HEIGHT = 16;
        private const int BALL_SIZE = 8;
        private const int BRICK_WIDTH = 50;
        private const int BRICK_HEIGHT = 20;

        public PaddleWarScene(SpriteFont font = null)
        {
            this.font = font;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Create 1x1 pixel texture for drawing shapes
            pixelTexture = new Texture2D(SceneManager.Instance.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Initialize game
            InitializeGame();

            // Play game music
            AudioManager.Instance.PlayMusic("paddle_war_music", loop: true);
        }

        private void InitializeGame()
        {
            score = 0;
            lives = 3;
            level = 1;
            isGameOver = false;

            SetupLevel();
        }

        private void SetupLevel()
        {
            // Create player paddle
            playerPaddle = new Paddle
            {
                Position = new Vector2(SCREEN_WIDTH / 2 - PADDLE_WIDTH / 2, SCREEN_HEIGHT - 50),
                Width = PADDLE_WIDTH,
                Height = PADDLE_HEIGHT,
                Speed = 400f
            };

            // Create ball
            ball = new Ball
            {
                Position = new Vector2(SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2),
                Velocity = new Vector2(200f, -200f),
                Size = BALL_SIZE,
                Speed = 200f + (level - 1) * 20f
            };

            // Create bricks
            CreateBricks();
        }

        private void CreateBricks()
        {
            bricks = new List<Brick>();

            int rows = 5 + level;
            int cols = 12;
            int offsetX = 20;
            int offsetY = 50;
            int spacing = 2;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    // Different brick types based on row
                    BrickType type = BrickType.Normal;
                    int hits = 1;
                    int points = 10;

                    if (row == 0)
                    {
                        type = BrickType.Hard;
                        hits = 3;
                        points = 50;
                    }
                    else if (row % 2 == 0)
                    {
                        type = BrickType.Medium;
                        hits = 2;
                        points = 25;
                    }

                    var brick = new Brick
                    {
                        Position = new Vector2(
                            offsetX + col * (BRICK_WIDTH + spacing),
                            offsetY + row * (BRICK_HEIGHT + spacing)
                        ),
                        Width = BRICK_WIDTH,
                        Height = BRICK_HEIGHT,
                        Type = type,
                        HitsRemaining = hits,
                        Points = points
                    };

                    bricks.Add(brick);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var input = InputManager.Instance;

            // Exit to menu
            if (input.IsActionPressed(InputAction.Pause))
            {
                SceneManager.Instance.ChangeScene("MainMenu", new FadeTransition(0.3f));
                return;
            }

            if (isGameOver)
            {
                gameOverTimer += deltaTime;
                if (gameOverTimer > 3f || input.IsActionPressed(InputAction.Jump))
                {
                    SceneManager.Instance.ChangeScene("MainMenu");
                }
                return;
            }

            // Update paddle
            playerPaddle.Update(deltaTime, input);

            // Update ball
            ball.Update(deltaTime);

            // Ball collision with walls
            if (ball.Position.X <= 0 || ball.Position.X + ball.Size >= SCREEN_WIDTH)
            {
                ball.Velocity.X = -ball.Velocity.X;
                AudioManager.Instance.PlaySound("wall_bounce");
            }

            if (ball.Position.Y <= 0)
            {
                ball.Velocity.Y = -ball.Velocity.Y;
                AudioManager.Instance.PlaySound("wall_bounce");
            }

            // Ball fell off bottom
            if (ball.Position.Y > SCREEN_HEIGHT)
            {
                lives--;
                AudioManager.Instance.PlaySound("life_lost");

                if (lives <= 0)
                {
                    isGameOver = true;
                    AudioManager.Instance.PlaySound("game_over");
                }
                else
                {
                    // Reset ball
                    ball.Position = new Vector2(SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2);
                    ball.Velocity = new Vector2(200f, -200f);
                }
            }

            // Ball collision with paddle
            Rectangle ballRect = new Rectangle(
                (int)ball.Position.X,
                (int)ball.Position.Y,
                ball.Size,
                ball.Size
            );

            Rectangle paddleRect = new Rectangle(
                (int)playerPaddle.Position.X,
                (int)playerPaddle.Position.Y,
                playerPaddle.Width,
                playerPaddle.Height
            );

            if (ballRect.Intersects(paddleRect) && ball.Velocity.Y > 0)
            {
                // Calculate bounce angle based on where ball hit paddle
                float hitPosition = (ball.Position.X - playerPaddle.Position.X) / playerPaddle.Width;
                float angle = MathHelper.Lerp(-0.75f, 0.75f, hitPosition); // -135° to -45°

                float speed = ball.Velocity.Length();
                ball.Velocity.X = (float)Math.Sin(angle * Math.PI) * speed;
                ball.Velocity.Y = -(float)Math.Cos(angle * Math.PI) * speed;

                AudioManager.Instance.PlaySound("paddle_bounce");
            }

            // Ball collision with bricks
            for (int i = bricks.Count - 1; i >= 0; i--)
            {
                var brick = bricks[i];

                Rectangle brickRect = new Rectangle(
                    (int)brick.Position.X,
                    (int)brick.Position.Y,
                    brick.Width,
                    brick.Height
                );

                if (ballRect.Intersects(brickRect))
                {
                    // Determine which side was hit
                    Vector2 ballCenter = ball.Position + new Vector2(ball.Size / 2);
                    Vector2 brickCenter = brick.Position + new Vector2(brick.Width / 2, brick.Height / 2);
                    Vector2 diff = ballCenter - brickCenter;

                    if (Math.Abs(diff.X) > Math.Abs(diff.Y))
                    {
                        // Hit left or right side
                        ball.Velocity.X = -ball.Velocity.X;
                    }
                    else
                    {
                        // Hit top or bottom
                        ball.Velocity.Y = -ball.Velocity.Y;
                    }

                    // Damage brick
                    brick.HitsRemaining--;

                    if (brick.HitsRemaining <= 0)
                    {
                        // Brick destroyed
                        score += brick.Points;
                        bricks.RemoveAt(i);
                        AudioManager.Instance.PlaySound("brick_break");
                    }
                    else
                    {
                        AudioManager.Instance.PlaySound("brick_hit");
                    }

                    break;
                }
            }

            // Check if level complete
            if (bricks.Count == 0)
            {
                level++;
                AudioManager.Instance.PlaySound("level_complete");
                SetupLevel();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin();

            // Clear background
            DrawRectangle(spriteBatch, new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), Color.Black);

            if (!isGameOver)
            {
                // Draw paddle
                DrawRectangle(spriteBatch, new Rectangle(
                    (int)playerPaddle.Position.X,
                    (int)playerPaddle.Position.Y,
                    playerPaddle.Width,
                    playerPaddle.Height
                ), Color.White);

                // Draw ball
                DrawRectangle(spriteBatch, new Rectangle(
                    (int)ball.Position.X,
                    (int)ball.Position.Y,
                    ball.Size,
                    ball.Size
                ), Color.White);

                // Draw bricks
                foreach (var brick in bricks)
                {
                    Color color = brick.Type switch
                    {
                        BrickType.Hard => Color.Red,
                        BrickType.Medium => Color.Yellow,
                        _ => Color.Green
                    };

                    DrawRectangle(spriteBatch, new Rectangle(
                        (int)brick.Position.X,
                        (int)brick.Position.Y,
                        brick.Width,
                        brick.Height
                    ), color);
                }
            }

            // Draw UI
            if (font != null)
            {
                string scoreText = $"Score: {score}";
                string livesText = $"Lives: {lives}";
                string levelText = $"Level: {level}";

                spriteBatch.DrawString(font, scoreText, new Vector2(10, 10), Color.White);
                spriteBatch.DrawString(font, livesText, new Vector2(10, 30), Color.White);
                spriteBatch.DrawString(font, levelText, new Vector2(SCREEN_WIDTH - 100, 10), Color.White);

                if (isGameOver)
                {
                    string gameOverText = "GAME OVER";
                    Vector2 textSize = font.MeasureString(gameOverText);
                    Vector2 position = new Vector2(
                        SCREEN_WIDTH / 2 - textSize.X / 2,
                        SCREEN_HEIGHT / 2 - textSize.Y / 2
                    );
                    spriteBatch.DrawString(font, gameOverText, position, Color.Red);

                    string finalScore = $"Final Score: {score}";
                    Vector2 scoreSize = font.MeasureString(finalScore);
                    spriteBatch.DrawString(font, finalScore,
                        new Vector2(SCREEN_WIDTH / 2 - scoreSize.X / 2, position.Y + 40),
                        Color.White);
                }
            }

            spriteBatch.End();
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            if (pixelTexture != null)
            {
                spriteBatch.Draw(pixelTexture, rect, color);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            AudioManager.Instance.StopMusic();
            pixelTexture?.Dispose();
        }

        #region Game Objects

        private class Paddle
        {
            public Vector2 Position { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public float Speed { get; set; }

            public void Update(float deltaTime, InputManager input)
            {
                float horizontal = input.GetHorizontalAxis();
                Position.X += horizontal * Speed * deltaTime;

                // Clamp to screen
                Position.X = MathHelper.Clamp(Position.X, 0, SCREEN_WIDTH - Width);
            }
        }

        private class Ball
        {
            public Vector2 Position { get; set; }
            public Vector2 Velocity { get; set; }
            public int Size { get; set; }
            public float Speed { get; set; }

            public void Update(float deltaTime)
            {
                Position += Velocity * deltaTime;
            }
        }

        private enum BrickType
        {
            Normal,
            Medium,
            Hard
        }

        private class Brick
        {
            public Vector2 Position { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public BrickType Type { get; set; }
            public int HitsRemaining { get; set; }
            public int Points { get; set; }
        }

        #endregion
    }
}
