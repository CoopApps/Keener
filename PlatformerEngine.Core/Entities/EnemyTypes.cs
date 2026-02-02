using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Combat;
using PlatformerEngine.Core.Graphics;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Entities
{
    /// <summary>
    /// Walking enemy that patrols back and forth
    /// </summary>
    public class WalkerEnemy : Enemy
    {
        public float PatrolDistance { get; set; } = 100f;
        public bool TurnAtEdges { get; set; } = true;
        public bool TurnAtWalls { get; set; } = true;

        private Vector2 patrolStartPosition;
        private int patrolDirection = 1; // 1 = right, -1 = left

        public WalkerEnemy()
        {
            MoveSpeed = 40f;
            PatrolSpeed = 40f;
            ChaseSpeed = 60f;
            Width = 32;
            Height = 32;
            BoundsWidth = 28;
            BoundsHeight = 32;
            UseGravity = true;
            IsSolid = true;

            // Setup health and attack
            Health = new HealthComponent { MaxHealth = 30, CurrentHealth = 30 };
            Attack = new AttackComponent { Damage = 10, AttackRange = 32f, AttackCooldown = 1.0f };
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            patrolStartPosition = Position;
        }

        protected override void UpdatePatrol(float deltaTime, TileMap tileMap)
        {
            // Move in patrol direction
            Velocity.X = patrolDirection * PatrolSpeed;

            // Check if reached patrol distance limit
            float distanceFromStart = Position.X - patrolStartPosition.X;
            if (Math.Abs(distanceFromStart) >= PatrolDistance)
            {
                patrolDirection *= -1;
                IsFacingRight = patrolDirection > 0;
            }

            // Turn at edges if enabled
            if (TurnAtEdges && IsGrounded)
            {
                // Check if there's ground ahead
                Rectangle aheadBounds = new Rectangle(
                    (int)(Position.X + (patrolDirection > 0 ? BoundsWidth : -8)),
                    (int)(Position.Y + BoundsHeight + 1),
                    8, 8
                );

                if (!tileMap.CheckCollision(aheadBounds, TileCollision.Solid))
                {
                    // No ground ahead - turn around
                    patrolDirection *= -1;
                    IsFacingRight = patrolDirection > 0;
                }
            }

            // Turn at walls if enabled
            if (TurnAtWalls)
            {
                Rectangle aheadBounds = new Rectangle(
                    (int)(Position.X + (patrolDirection > 0 ? BoundsWidth : -8)),
                    (int)(Position.Y),
                    8, (int)BoundsHeight
                );

                if (tileMap.CheckCollision(aheadBounds, TileCollision.Solid))
                {
                    // Hit wall - turn around
                    patrolDirection *= -1;
                    IsFacingRight = patrolDirection > 0;
                }
            }
        }

        protected override void UpdateChase(float deltaTime, TileMap tileMap)
        {
            if (Target == null)
            {
                State = EnemyState.Patrol;
                return;
            }

            // Move towards target
            float direction = Math.Sign(Target.Position.X - Position.X);
            Velocity.X = direction * ChaseSpeed;
            IsFacingRight = direction > 0;

            // Attack if in range
            if (CanAttack() && Vector2.Distance(Position, Target.Position) <= Attack.AttackRange)
            {
                State = EnemyState.Attack;
            }
        }

        protected override void UpdateAttack(float deltaTime, TileMap tileMap)
        {
            Velocity.X = 0; // Stop moving when attacking

            if (Target != null && CanAttack())
            {
                PerformAttack(Target);
                attackCooldownTimer = Attack.AttackCooldown;
            }

            // Return to chase after attack
            attackTimer += deltaTime;
            if (attackTimer >= 0.5f) // Attack animation duration
            {
                attackTimer = 0;
                State = EnemyState.Chase;
            }
        }
    }

    /// <summary>
    /// Flying enemy that hovers and swoops at player
    /// </summary>
    public class FlyerEnemy : Enemy
    {
        public float HoverHeight { get; set; } = 100f;
        public float HoverSpeed { get; set; } = 50f;
        public float SwoopSpeed { get; set; } = 200f;
        public float SwoopCooldown { get; set; } = 3.0f;

        private Vector2 hoverPosition;
        private float swoopTimer;
        private bool isSwooping;
        private Vector2 swoopTarget;

        public FlyerEnemy()
        {
            MoveSpeed = 50f;
            PatrolSpeed = 50f;
            ChaseSpeed = 80f;
            Width = 32;
            Height = 32;
            BoundsWidth = 28;
            BoundsHeight = 28;
            UseGravity = false; // Flying enemies ignore gravity
            IsSolid = false;

            Health = new HealthComponent { MaxHealth = 20, CurrentHealth = 20 };
            Attack = new AttackComponent { Damage = 15, AttackRange = 40f, AttackCooldown = 2.0f };
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            hoverPosition = Position;
        }

        protected override void UpdatePatrol(float deltaTime, TileMap tileMap)
        {
            // Hover in circular pattern
            float time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
            Vector2 targetPos = hoverPosition + new Vector2(
                (float)Math.Cos(time) * 50f,
                (float)Math.Sin(time * 2) * 20f
            );

            Vector2 direction = targetPos - Position;
            if (direction.Length() > 5f)
            {
                direction.Normalize();
                Velocity = direction * HoverSpeed;
            }
            else
            {
                Velocity = Vector2.Zero;
            }
        }

        protected override void UpdateChase(float deltaTime, TileMap tileMap)
        {
            if (Target == null)
            {
                State = EnemyState.Patrol;
                return;
            }

            swoopTimer -= deltaTime;

            if (!isSwooping)
            {
                // Hover above player
                Vector2 targetPos = Target.Position + new Vector2(0, -HoverHeight);
                Vector2 direction = targetPos - Position;

                if (direction.Length() > 10f)
                {
                    direction.Normalize();
                    Velocity = direction * ChaseSpeed;
                    IsFacingRight = direction.X > 0;
                }

                // Swoop attack
                if (swoopTimer <= 0 && Vector2.Distance(Position, Target.Position) < DetectionRange)
                {
                    isSwooping = true;
                    swoopTarget = Target.Position;
                    swoopTimer = SwoopCooldown;
                }
            }
            else
            {
                // Swooping down
                Vector2 direction = swoopTarget - Position;
                float distance = direction.Length();

                if (distance > 10f)
                {
                    direction.Normalize();
                    Velocity = direction * SwoopSpeed;
                }
                else
                {
                    // Swoop complete - attack and return to hover
                    if (Target != null && CanAttack())
                    {
                        PerformAttack(Target);
                    }
                    isSwooping = false;
                }
            }
        }
    }

    /// <summary>
    /// Shooter enemy that fires projectiles
    /// </summary>
    public class ShooterEnemy : Enemy
    {
        public float ShootRange { get; set; } = 200f;
        public float ShootCooldown { get; set; } = 2.0f;
        public int ProjectileDamage { get; set; } = 10;
        public float ProjectileSpeed { get; set; } = 150f;
        public Texture2D ProjectileTexture { get; set; }

        private float shootTimer;
        public List<Projectile> ActiveProjectiles { get; private set; } = new List<Projectile>();

        public ShooterEnemy()
        {
            MoveSpeed = 30f;
            PatrolSpeed = 30f;
            ChaseSpeed = 40f;
            Width = 32;
            Height = 32;
            BoundsWidth = 28;
            BoundsHeight = 32;
            UseGravity = true;
            IsSolid = true;

            Health = new HealthComponent { MaxHealth = 25, CurrentHealth = 25 };
            Attack = new AttackComponent { Damage = 0, AttackRange = 0f }; // Uses projectiles instead
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            shootTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update projectiles
            for (int i = ActiveProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = ActiveProjectiles[i];
                projectile.Update(gameTime, tileMap);

                if (!projectile.IsActive)
                {
                    ActiveProjectiles.RemoveAt(i);
                }
            }
        }

        protected override void UpdatePatrol(float deltaTime, TileMap tileMap)
        {
            // Stand still and look for targets
            Velocity.X = 0;
        }

        protected override void UpdateChase(float deltaTime, TileMap tileMap)
        {
            if (Target == null)
            {
                State = EnemyState.Patrol;
                return;
            }

            float distanceToTarget = Vector2.Distance(Position, Target.Position);

            // Keep distance and shoot
            if (distanceToTarget > ShootRange * 0.7f)
            {
                // Move closer
                float direction = Math.Sign(Target.Position.X - Position.X);
                Velocity.X = direction * ChaseSpeed;
                IsFacingRight = direction > 0;
            }
            else if (distanceToTarget < ShootRange * 0.5f)
            {
                // Back away
                float direction = -Math.Sign(Target.Position.X - Position.X);
                Velocity.X = direction * ChaseSpeed;
                IsFacingRight = -direction > 0;
            }
            else
            {
                // In ideal range - stop and shoot
                Velocity.X = 0;
                IsFacingRight = Target.Position.X > Position.X;

                if (shootTimer <= 0)
                {
                    ShootAtTarget();
                    shootTimer = ShootCooldown;
                }
            }
        }

        private void ShootAtTarget()
        {
            if (Target == null)
                return;

            Vector2 direction = Target.Position - Position;
            direction.Normalize();

            var projectile = new Projectile
            {
                Position = Position + new Vector2(BoundsWidth / 2, BoundsHeight / 2),
                Velocity = direction * ProjectileSpeed,
                Damage = ProjectileDamage,
                DamageType = DamageType.Projectile,
                Owner = this,
                UseGravity = false,
                Width = 8,
                Height = 8,
                BoundsWidth = 8,
                BoundsHeight = 8,
                Texture = ProjectileTexture,
                MaxLifetime = 5f
            };

            ActiveProjectiles.Add(projectile);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            // Draw projectiles
            foreach (var projectile in ActiveProjectiles)
            {
                projectile.Draw(spriteBatch);
            }
        }
    }

    /// <summary>
    /// Charger enemy that charges at player when in range
    /// </summary>
    public class ChargerEnemy : Enemy
    {
        public float ChargeRange { get; set; } = 150f;
        public float ChargeSpeed { get; set; } = 200f;
        public float ChargeWindupTime { get; set; } = 0.5f;
        public float ChargeCooldown { get; set; } = 2.0f;
        public float ChargeMaxDistance { get; set; } = 300f;

        private bool isCharging;
        private bool isWindingUp;
        private float windupTimer;
        private float chargeTimer;
        private Vector2 chargeStartPosition;
        private float chargeDirection;

        public ChargerEnemy()
        {
            MoveSpeed = 30f;
            PatrolSpeed = 30f;
            ChaseSpeed = 50f;
            Width = 40;
            Height = 32;
            BoundsWidth = 36;
            BoundsHeight = 32;
            UseGravity = true;
            IsSolid = true;

            Health = new HealthComponent { MaxHealth = 40, CurrentHealth = 40 };
            Attack = new AttackComponent { Damage = 20, AttackRange = 50f, AttackCooldown = 1.0f };
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (isWindingUp)
            {
                windupTimer -= deltaTime;
                Velocity.X = 0;

                if (windupTimer <= 0)
                {
                    isWindingUp = false;
                    isCharging = true;
                    chargeStartPosition = Position;
                }
            }
            else if (isCharging)
            {
                // Charging forward
                Velocity.X = chargeDirection * ChargeSpeed;

                // Stop charging if hit wall or traveled max distance
                float chargeDistance = Math.Abs(Position.X - chargeStartPosition.X);
                if (chargeDistance >= ChargeMaxDistance || WasBlockedX)
                {
                    isCharging = false;
                    chargeTimer = ChargeCooldown;
                    State = EnemyState.Stunned;
                    stunTimer = 1.0f; // Stunned briefly after charge
                }
            }
        }

        protected override void UpdateChase(float deltaTime, TileMap tileMap)
        {
            if (Target == null)
            {
                State = EnemyState.Patrol;
                return;
            }

            if (isCharging || isWindingUp)
                return;

            float distanceToTarget = Vector2.Distance(Position, Target.Position);
            float horizontalDistance = Math.Abs(Target.Position.X - Position.X);

            // Check if should charge
            if (chargeTimer <= 0 && horizontalDistance <= ChargeRange && Math.Abs(Target.Position.Y - Position.Y) < 50f)
            {
                // Start charging
                isWindingUp = true;
                windupTimer = ChargeWindupTime;
                chargeDirection = Math.Sign(Target.Position.X - Position.X);
                IsFacingRight = chargeDirection > 0;
            }
            else
            {
                // Normal chase
                float direction = Math.Sign(Target.Position.X - Position.X);
                Velocity.X = direction * ChaseSpeed;
                IsFacingRight = direction > 0;
                chargeTimer -= deltaTime;
            }
        }

        protected override void UpdateStunned(float deltaTime, TileMap tileMap)
        {
            base.UpdateStunned(deltaTime, tileMap);
            Velocity.X *= 0.9f; // Slow down while stunned
        }
    }

    /// <summary>
    /// Stationary turret enemy that rotates and shoots
    /// </summary>
    public class TurretEnemy : Enemy
    {
        public float RotationSpeed { get; set; } = 2f;
        public float ShootCooldown { get; set; } = 1.5f;
        public int ProjectileDamage { get; set; } = 15;
        public float ProjectileSpeed { get; set; } = 200f;
        public float FireRange { get; set; } = 250f;
        public Texture2D ProjectileTexture { get; set; }

        private float currentAngle;
        private float targetAngle;
        private float shootTimer;
        public List<Projectile> ActiveProjectiles { get; private set; } = new List<Projectile>();

        public TurretEnemy()
        {
            MoveSpeed = 0f; // Stationary
            Width = 32;
            Height = 32;
            BoundsWidth = 32;
            BoundsHeight = 32;
            UseGravity = false;
            IsSolid = true;

            Health = new HealthComponent { MaxHealth = 50, CurrentHealth = 50 };
            Attack = new AttackComponent { Damage = 0, AttackRange = 0f }; // Uses projectiles
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            shootTimer -= deltaTime;

            // Rotate towards target
            if (currentAngle != targetAngle)
            {
                float angleDiff = targetAngle - currentAngle;

                // Normalize angle difference
                while (angleDiff > MathHelper.Pi)
                    angleDiff -= MathHelper.TwoPi;
                while (angleDiff < -MathHelper.Pi)
                    angleDiff += MathHelper.TwoPi;

                float rotationStep = RotationSpeed * deltaTime;
                if (Math.Abs(angleDiff) < rotationStep)
                {
                    currentAngle = targetAngle;
                }
                else
                {
                    currentAngle += Math.Sign(angleDiff) * rotationStep;
                }
            }

            // Update projectiles
            for (int i = ActiveProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = ActiveProjectiles[i];
                projectile.Update(gameTime, tileMap);

                if (!projectile.IsActive)
                {
                    ActiveProjectiles.RemoveAt(i);
                }
            }
        }

        protected override void UpdatePatrol(float deltaTime, TileMap tileMap)
        {
            // Slowly rotate back and forth
            float time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
            targetAngle = (float)Math.Sin(time) * MathHelper.PiOver2;
        }

        protected override void UpdateChase(float deltaTime, TileMap tileMap)
        {
            if (Target == null)
            {
                State = EnemyState.Patrol;
                return;
            }

            // Aim at target
            Vector2 toTarget = Target.Position - Position;
            targetAngle = (float)Math.Atan2(toTarget.Y, toTarget.X);

            // Shoot if aimed and in range
            if (Math.Abs(currentAngle - targetAngle) < 0.1f &&
                toTarget.Length() <= FireRange &&
                shootTimer <= 0)
            {
                ShootProjectile();
                shootTimer = ShootCooldown;
            }
        }

        private void ShootProjectile()
        {
            Vector2 direction = new Vector2(
                (float)Math.Cos(currentAngle),
                (float)Math.Sin(currentAngle)
            );

            var projectile = new Projectile
            {
                Position = Position + new Vector2(BoundsWidth / 2, BoundsHeight / 2),
                Velocity = direction * ProjectileSpeed,
                Damage = ProjectileDamage,
                DamageType = DamageType.Projectile,
                Owner = this,
                UseGravity = false,
                Width = 8,
                Height = 8,
                BoundsWidth = 8,
                BoundsHeight = 8,
                Texture = ProjectileTexture,
                MaxLifetime = 3f
            };

            ActiveProjectiles.Add(projectile);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw turret rotated
            if (Texture != null && spriteBatch != null)
            {
                Rectangle sourceRect = CurrentFrame ?? new Rectangle(0, 0, (int)Width, (int)Height);
                Vector2 origin = new Vector2(Width / 2, Height / 2);
                Vector2 drawPosition = Position + origin;

                spriteBatch.Draw(
                    Texture,
                    drawPosition,
                    sourceRect,
                    Color.White,
                    currentAngle,
                    origin,
                    1.0f,
                    SpriteEffects.None,
                    0f
                );
            }

            // Draw projectiles
            foreach (var projectile in ActiveProjectiles)
            {
                projectile.Draw(spriteBatch);
            }
        }
    }

    /// <summary>
    /// Jumping enemy that hops around
    /// </summary>
    public class JumperEnemy : Enemy
    {
        public float JumpForce { get; set; } = 300f;
        public float JumpCooldown { get; set; } = 2.0f;
        public float JumpChance { get; set; } = 0.7f;

        private float jumpTimer;
        private Random random = new Random();

        public JumperEnemy()
        {
            MoveSpeed = 20f;
            PatrolSpeed = 20f;
            ChaseSpeed = 40f;
            Width = 24;
            Height = 24;
            BoundsWidth = 22;
            BoundsHeight = 24;
            UseGravity = true;
            IsSolid = true;

            Health = new HealthComponent { MaxHealth = 15, CurrentHealth = 15 };
            Attack = new AttackComponent { Damage = 5, AttackRange = 30f, AttackCooldown = 1.5f };
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            jumpTimer -= deltaTime;

            // Jump periodically when grounded
            if (IsGrounded && jumpTimer <= 0 && random.NextDouble() < JumpChance)
            {
                Jump();
                jumpTimer = JumpCooldown;
            }
        }

        protected override void UpdatePatrol(float deltaTime, TileMap tileMap)
        {
            // Random horizontal movement
            if (IsGrounded)
            {
                Velocity.X = (float)(random.NextDouble() - 0.5) * PatrolSpeed * 2;
            }
        }

        protected override void UpdateChase(float deltaTime, TileMap tileMap)
        {
            if (Target == null)
            {
                State = EnemyState.Patrol;
                return;
            }

            // Jump towards player
            float direction = Math.Sign(Target.Position.X - Position.X);
            Velocity.X = direction * ChaseSpeed;
            IsFacingRight = direction > 0;
        }

        private void Jump()
        {
            Velocity.Y = -JumpForce;
        }
    }
}
