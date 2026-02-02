using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Graphics;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Entities
{
    /// <summary>
    /// Moving platform that carries entities
    /// </summary>
    public class MovingPlatform : Entity
    {
        public List<Vector2> Waypoints { get; set; } = new List<Vector2>();
        public float Speed { get; set; } = 50f;
        public bool Loop { get; set; } = true;
        public float WaitTime { get; set; } = 1.0f;

        private int currentWaypoint = 0;
        private float waitTimer;
        private List<Entity> passengers = new List<Entity>();

        public MovingPlatform()
        {
            UseGravity = false;
            IsSolid = true;
            Width = 64;
            Height = 16;
            BoundsWidth = 64;
            BoundsHeight = 16;
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            if (!IsActive || Waypoints.Count < 2)
                return;

            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Wait at waypoint
            if (waitTimer > 0)
            {
                waitTimer -= deltaTime;
                Velocity = Vector2.Zero;
                return;
            }

            // Move towards current waypoint
            Vector2 target = Waypoints[currentWaypoint];
            Vector2 direction = target - Position;
            float distance = direction.Length();

            if (distance < Speed * deltaTime)
            {
                // Reached waypoint
                Position = target;
                Velocity = Vector2.Zero;
                waitTimer = WaitTime;

                // Move to next waypoint
                currentWaypoint++;
                if (currentWaypoint >= Waypoints.Count)
                {
                    if (Loop)
                        currentWaypoint = 0;
                    else
                        currentWaypoint = Waypoints.Count - 1;
                }
            }
            else
            {
                // Move towards waypoint
                direction.Normalize();
                Velocity = direction * Speed;
                Position += Velocity * deltaTime;
            }

            // Move passengers
            foreach (var passenger in passengers)
            {
                passenger.Position += Velocity * deltaTime;
            }
        }

        public void AddPassenger(Entity entity)
        {
            if (!passengers.Contains(entity))
                passengers.Add(entity);
        }

        public void RemovePassenger(Entity entity)
        {
            passengers.Remove(entity);
        }
    }

    /// <summary>
    /// Player extension for ladder climbing
    /// </summary>
    public static class LadderPhysics
    {
        public static bool IsOnLadder { get; set; }
        public static float ClimbSpeed { get; set; } = 80f;

        public static void UpdateLadderPhysics(Player player, TileMap tileMap, float verticalInput)
        {
            // Check if player is overlapping a ladder
            var bounds = player.Bounds;
            bool onLadder = false;

            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Ladder))
            {
                onLadder = true;
                break;
            }

            IsOnLadder = onLadder;

            if (IsOnLadder && Math.Abs(verticalInput) > 0.1f)
            {
                // Climbing ladder
                player.UseGravity = false;
                player.Velocity.Y = -verticalInput * ClimbSpeed;

                // Can still move horizontally
                // Velocity.X is handled by normal movement
            }
            else if (IsOnLadder)
            {
                // On ladder but not climbing - stop vertical movement
                player.UseGravity = false;
                player.Velocity.Y = 0;
            }
            else
            {
                // Not on ladder - use normal gravity
                player.UseGravity = true;
            }
        }
    }

    /// <summary>
    /// Player extension for water physics
    /// </summary>
    public static class WaterPhysics
    {
        public static bool IsInWater { get; set; }
        public static float WaterDrag { get; set; } = 0.95f;
        public static float WaterGravity { get; set; } = 0.3f;
        public static float SwimSpeed { get; set; } = 60f;

        public static void UpdateWaterPhysics(Player player, TileMap tileMap, float verticalInput)
        {
            // Check if player is in water
            var bounds = player.Bounds;
            bool inWater = false;

            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Water))
            {
                inWater = true;
                break;
            }

            IsInWater = inWater;

            if (IsInWater)
            {
                // Apply water drag
                player.Velocity *= WaterDrag;

                // Reduced gravity in water
                player.GravityScale = WaterGravity;

                // Swimming (vertical movement)
                if (Math.Abs(verticalInput) > 0.1f)
                {
                    player.Velocity.Y = -verticalInput * SwimSpeed;
                }
            }
            else
            {
                // Normal gravity
                player.GravityScale = 1.0f;
            }
        }
    }

    /// <summary>
    /// Slope collision handling
    /// </summary>
    public static class SlopePhysics
    {
        public static bool IsOnSlope { get; set; }
        public static float SlopeAngle { get; set; }

        public static void UpdateSlopePhysics(Player player, TileMap tileMap)
        {
            // Simplified slope detection - check tiles below player
            var bounds = player.Bounds;
            IsOnSlope = false;

            // This is a basic implementation - full slope physics requires
            // slope angle data stored per tile
            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Slope))
            {
                IsOnSlope = true;

                // Calculate slope angle (simplified - would need tile-specific data)
                // For now, assume 45-degree slopes
                SlopeAngle = MathHelper.PiOver4;

                // Adjust player position to stay on slope
                // This is very simplified - proper implementation needs per-tile slope data
                break;
            }
        }

        /// <summary>
        /// Helper to get slope height at X position
        /// </summary>
        public static float GetSlopeHeight(int tileX, int tileY, float xInTile, bool ascending)
        {
            // For ascending slope (going up left-to-right)
            if (ascending)
                return xInTile;  // 0 at left, 1 at right
            else
                return 1.0f - xInTile;  // 1 at left, 0 at right
        }
    }

    /// <summary>
    /// Extension methods for Player to support these physics features
    /// </summary>
    public static class PlayerPhysicsExtensions
    {
        public static void UpdateAdvancedPhysics(this Player player, TileMap tileMap, float horizontalInput, float verticalInput)
        {
            // Update ladder physics
            LadderPhysics.UpdateLadderPhysics(player, tileMap, verticalInput);

            // Update water physics
            WaterPhysics.UpdateWaterPhysics(player, tileMap, verticalInput);

            // Update slope physics
            SlopePhysics.UpdateSlopePhysics(player, tileMap);
        }

        public static bool IsOnLadder(this Player player) => LadderPhysics.IsOnLadder;
        public static bool IsInWater(this Player player) => WaterPhysics.IsInWater;
        public static bool IsOnSlope(this Player player) => SlopePhysics.IsOnSlope;
    }
}
