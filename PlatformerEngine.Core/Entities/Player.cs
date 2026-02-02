using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Graphics;
using PlatformerEngine.Core.Input;
using System;

namespace PlatformerEngine.Core.Entities
{
    /// <summary>
    /// Player character with platformer physics
    /// </summary>
    public class Player : Entity
    {
        // Movement constants
        public float WalkSpeed { get; set; } = 120f;
        public float RunSpeed { get; set; } = 200f;
        public float Acceleration { get; set; } = 800f;
        public float AirAcceleration { get; set; } = 400f;
        public float Friction { get; set; } = 600f;
        public float AirFriction { get; set; } = 50f;

        // Jump constants
        public float JumpForce { get; set; } = 350f;
        public float JumpHoldGravity { get; set; } = 600f;  // Gravity when holding jump
        public float FallGravity { get; set; } = 900f;      // Gravity when falling
        public float MaxFallSpeed { get; set; } = 400f;
        public float JumpBufferTime { get; set; } = 0.1f;   // Can press jump slightly before landing
        public float CoyoteTime { get; set; } = 0.15f;      // Can jump slightly after leaving ground
        public int MaxAirJumps { get; set; } = 0;           // Double jump count (0 = no double jump)

        // State
        public bool IsRunning { get; private set; }
        public bool IsJumping { get; private set; }
        public bool IsDucking { get; private set; }
        public int AirJumpsRemaining { get; private set; }

        // Timers
        private float jumpBufferTimer;
        private float coyoteTimer;
        private bool jumpHeld;

        // Input reference
        private InputManager input;

        public Player()
        {
            Width = 16;
            Height = 24;
            BoundsWidth = 12;
            BoundsHeight = 24;
            BoundsOffset = new Vector2(2, 0); // Center the smaller collision box

            UseGravity = true;
            IsSolid = true;

            input = InputManager.Instance;
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            if (!IsActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update timers
            if (jumpBufferTimer > 0) jumpBufferTimer -= deltaTime;
            if (coyoteTimer > 0) coyoteTimer -= deltaTime;

            // Handle input
            HandleInput(deltaTime);

            // Apply gravity
            ApplyGravity(deltaTime);

            // Apply movement
            ApplyMovement(deltaTime);

            // Clamp velocity
            Velocity.Y = MathHelper.Clamp(Velocity.Y, -JumpForce, MaxFallSpeed);

            // Move and handle collisions
            MoveAndCollide(deltaTime, tileMap);

            // Update sprite facing direction
            if (Velocity.X > 0)
            {
                FacingDirection = 1;
                SpriteEffect = SpriteEffects.None;
            }
            else if (Velocity.X < 0)
            {
                FacingDirection = -1;
                SpriteEffect = SpriteEffects.FlipHorizontally;
            }

            base.Update(gameTime, tileMap);
        }

        /// <summary>
        /// Handle player input
        /// </summary>
        private void HandleInput(float deltaTime)
        {
            // Horizontal movement
            float horizontalInput = input.GetHorizontalAxis();

            // Running
            IsRunning = input.IsActionDown(InputAction.Run);

            // Ducking (can only duck on ground)
            IsDucking = IsOnGround && input.IsActionDown(InputAction.Duck);

            // Jumping
            if (input.IsActionPressed(InputAction.Jump))
            {
                jumpBufferTimer = JumpBufferTime;
            }

            jumpHeld = input.IsActionDown(InputAction.Jump);

            // Try to jump
            if (jumpBufferTimer > 0)
            {
                // Ground jump
                if (IsOnGround || coyoteTimer > 0)
                {
                    Jump();
                    jumpBufferTimer = 0;
                    coyoteTimer = 0;
                }
                // Air jump (double jump)
                else if (AirJumpsRemaining > 0)
                {
                    Jump();
                    AirJumpsRemaining--;
                    jumpBufferTimer = 0;
                }
            }

            // Calculate target speed
            float targetSpeed = 0f;
            if (!IsDucking && Math.Abs(horizontalInput) > 0.1f)
            {
                targetSpeed = horizontalInput * (IsRunning ? RunSpeed : WalkSpeed);
            }

            // Calculate acceleration
            float accel = IsOnGround ? Acceleration : AirAcceleration;
            float friction = IsOnGround ? Friction : AirFriction;

            // Apply acceleration
            if (Math.Abs(targetSpeed) > 0.1f)
            {
                // Accelerate towards target speed
                float speedDiff = targetSpeed - Velocity.X;
                Velocity.X += Math.Sign(speedDiff) * Math.Min(Math.Abs(speedDiff), accel * deltaTime);
            }
            else
            {
                // Apply friction
                if (Math.Abs(Velocity.X) > friction * deltaTime)
                {
                    Velocity.X -= Math.Sign(Velocity.X) * friction * deltaTime;
                }
                else
                {
                    Velocity.X = 0;
                }
            }
        }

        /// <summary>
        /// Apply gravity to the player
        /// </summary>
        private void ApplyGravity(float deltaTime)
        {
            if (!UseGravity) return;

            float gravity;

            if (Velocity.Y < 0 && jumpHeld)
            {
                // Rising and holding jump = lower gravity for variable jump height
                gravity = JumpHoldGravity;
            }
            else
            {
                // Falling or not holding jump = full gravity
                gravity = FallGravity;
            }

            Velocity.Y += gravity * GravityScale * deltaTime;
        }

        /// <summary>
        /// Apply any additional movement forces
        /// </summary>
        private void ApplyMovement(float deltaTime)
        {
            // Can add wind, conveyor belts, etc. here
        }

        /// <summary>
        /// Perform a jump
        /// </summary>
        private void Jump()
        {
            Velocity.Y = -JumpForce;
            IsJumping = true;
            IsOnGround = false;
            // Could trigger jump sound/animation here
        }

        /// <summary>
        /// Move the player and handle tile collisions
        /// </summary>
        private void MoveAndCollide(float deltaTime, TileMap tileMap)
        {
            // Store previous ground state
            bool wasOnGround = IsOnGround;

            // Reset collision states
            IsOnGround = false;
            IsAgainstWall = false;
            IsAgainstCeiling = false;

            // Move horizontally and check collisions
            Position.X += Velocity.X * deltaTime;
            HandleHorizontalCollisions(tileMap);

            // Move vertically and check collisions
            Position.Y += Velocity.Y * deltaTime;
            HandleVerticalCollisions(tileMap);

            // Handle ground state changes
            if (IsOnGround)
            {
                IsJumping = false;
                AirJumpsRemaining = MaxAirJumps;

                // Just landed
                if (!wasOnGround)
                {
                    OnLand();
                }
            }
            else
            {
                // Just left ground
                if (wasOnGround)
                {
                    coyoteTimer = CoyoteTime;
                }
            }
        }

        /// <summary>
        /// Handle horizontal collision with tiles
        /// </summary>
        private void HandleHorizontalCollisions(TileMap tileMap)
        {
            Rectangle bounds = Bounds;

            // Check collision with solid tiles
            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Solid))
            {
                Rectangle tileBounds = new Rectangle(tileX * tileMap.TileSize, tileY * tileMap.TileSize,
                                                     tileMap.TileSize, tileMap.TileSize);

                // Resolve collision
                if (Velocity.X > 0) // Moving right
                {
                    Position.X = tileBounds.Left - BoundsOffset.X - BoundsWidth;
                    Velocity.X = 0;
                    IsAgainstWall = true;
                }
                else if (Velocity.X < 0) // Moving left
                {
                    Position.X = tileBounds.Right - BoundsOffset.X;
                    Velocity.X = 0;
                    IsAgainstWall = true;
                }

                OnTileCollision(tileX, tileY, collision);
            }

            // Check for deadly tiles
            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Deadly))
            {
                OnDamage(1, null); // Take damage from deadly tiles
            }
        }

        /// <summary>
        /// Handle vertical collision with tiles
        /// </summary>
        private void HandleVerticalCollisions(TileMap tileMap)
        {
            Rectangle bounds = Bounds;

            // Check collision with solid tiles
            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Solid))
            {
                Rectangle tileBounds = new Rectangle(tileX * tileMap.TileSize, tileY * tileMap.TileSize,
                                                     tileMap.TileSize, tileMap.TileSize);

                // Resolve collision
                if (Velocity.Y > 0) // Moving down (falling)
                {
                    Position.Y = tileBounds.Top - BoundsOffset.Y - BoundsHeight;
                    Velocity.Y = 0;
                    IsOnGround = true;
                }
                else if (Velocity.Y < 0) // Moving up (jumping)
                {
                    Position.Y = tileBounds.Bottom - BoundsOffset.Y;
                    Velocity.Y = 0;
                    IsAgainstCeiling = true;
                }

                OnTileCollision(tileX, tileY, collision);
            }

            // Check platforms (one-way collision from above)
            if (Velocity.Y >= 0) // Only collide when falling
            {
                foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Platform))
                {
                    Rectangle tileBounds = new Rectangle(tileX * tileMap.TileSize, tileY * tileMap.TileSize,
                                                         tileMap.TileSize, tileMap.TileSize);

                    // Only collide if player's feet are above or at the platform top
                    int playerBottom = bounds.Bottom;
                    int prevPlayerBottom = (int)(Position.Y + BoundsOffset.Y + BoundsHeight - Velocity.Y * 0.016f); // Approximate previous position

                    if (prevPlayerBottom <= tileBounds.Top && playerBottom >= tileBounds.Top)
                    {
                        Position.Y = tileBounds.Top - BoundsOffset.Y - BoundsHeight;
                        Velocity.Y = 0;
                        IsOnGround = true;
                        IsOnPlatform = true;
                    }
                }
            }

            // Check for deadly tiles
            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Deadly))
            {
                OnDamage(1, null);
            }
        }

        /// <summary>
        /// Called when player lands on ground
        /// </summary>
        private void OnLand()
        {
            // Could trigger landing sound/animation here
            // Could add landing particle effect
        }

        public override void OnTileCollision(int tileX, int tileY, TileCollision collision)
        {
            // Handle special tile types
            // Override for custom behavior
        }

        public override void OnDamage(int damage, Entity source)
        {
            // Handle player taking damage
            // Could trigger hurt animation, invincibility frames, etc.
            // Override for custom behavior
        }

        /// <summary>
        /// Reset player to spawn position
        /// </summary>
        public void Respawn(Vector2 spawnPosition)
        {
            Position = spawnPosition;
            Velocity = Vector2.Zero;
            IsActive = true;
            IsVisible = true;
            IsOnGround = false;
            IsJumping = false;
            IsDucking = false;
            AirJumpsRemaining = MaxAirJumps;
        }
    }
}
