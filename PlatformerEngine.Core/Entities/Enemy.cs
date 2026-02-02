using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Combat;
using PlatformerEngine.Core.Graphics;
using System;

namespace PlatformerEngine.Core.Entities
{
    /// <summary>
    /// Enemy AI behavior states
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Flee,
        Stunned,
        Dead
    }

    /// <summary>
    /// Base enemy class with AI behaviors
    /// </summary>
    public class Enemy : Entity, IHasHealth
    {
        // Health
        public HealthComponent Health { get; private set; }

        // Combat
        public AttackComponent Attack { get; private set; }

        // AI State
        public EnemyState State { get; protected set; } = EnemyState.Patrol;
        protected EnemyState previousState;

        // AI Properties
        public float DetectionRange { get; set; } = 150f;
        public float AttackRange { get; set; } = 20f;
        public float LoseTargetRange { get; set; } = 200f;
        public float PatrolSpeed { get; set; } = 50f;
        public float ChaseSpeed { get; set; } = 100f;

        // Patrol behavior
        public float PatrolDistance { get; set; } = 100f;
        protected Vector2 patrolStartPosition;
        protected int patrolDirection = 1;

        // Target tracking
        public Entity Target { get; protected set; }
        protected float targetDistance;

        // Timers
        protected float stateTimer;
        protected float idleTime = 1.0f;

        // Death
        public float DeathDespawnTime { get; set; } = 2.0f;
        protected float deathTimer;

        public Enemy()
        {
            // Initialize health
            Health = new HealthComponent(maxHealth: 30);
            Health.InvulnerabilityDuration = 0.5f;
            Health.OnDeath += OnDeath;
            Health.OnDamaged += OnDamaged;

            // Initialize attack
            Attack = new AttackComponent
            {
                Damage = 10,
                AttackCooldown = 1.0f,
                AttackRange = 20f,
                KnockbackForce = 50f
            };

            UseGravity = true;
            IsSolid = true;
            Width = 16;
            Height = 16;
            BoundsWidth = 14;
            BoundsHeight = 16;
            BoundsOffset = new Vector2(1, 0);
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            if (!IsActive)
                return;

            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            stateTimer += deltaTime;

            // Update components
            Health.Update(gameTime);
            Attack.Update(gameTime);

            // Don't process AI when dead
            if (State == EnemyState.Dead)
            {
                UpdateDeath(deltaTime);
                return;
            }

            // Update AI based on state
            switch (State)
            {
                case EnemyState.Idle:
                    UpdateIdle(deltaTime, tileMap);
                    break;
                case EnemyState.Patrol:
                    UpdatePatrol(deltaTime, tileMap);
                    break;
                case EnemyState.Chase:
                    UpdateChase(deltaTime, tileMap);
                    break;
                case EnemyState.Attack:
                    UpdateAttack(deltaTime, tileMap);
                    break;
                case EnemyState.Stunned:
                    UpdateStunned(deltaTime, tileMap);
                    break;
            }

            // Apply movement
            ApplyMovement(deltaTime, tileMap);

            // Update facing direction
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
        }

        /// <summary>
        /// Idle state - standing still
        /// </summary>
        protected virtual void UpdateIdle(float deltaTime, TileMap tileMap)
        {
            Velocity.X = 0;

            // Look for targets
            if (ScanForTargets())
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            // Return to patrol after idle time
            if (stateTimer >= idleTime)
            {
                ChangeState(EnemyState.Patrol);
            }
        }

        /// <summary>
        /// Patrol state - walking back and forth
        /// </summary>
        protected virtual void UpdatePatrol(float deltaTime, TileMap tileMap)
        {
            // Set patrol start position on first patrol
            if (previousState != EnemyState.Patrol)
            {
                patrolStartPosition = Position;
            }

            // Move in patrol direction
            Velocity.X = patrolDirection * PatrolSpeed;

            // Check if reached patrol boundary
            float distanceFromStart = Position.X - patrolStartPosition.X;
            if (Math.Abs(distanceFromStart) >= PatrolDistance)
            {
                patrolDirection *= -1;  // Reverse direction
                ChangeState(EnemyState.Idle);
                return;
            }

            // Check for edges/walls
            if (IsAgainstWall || IsNearEdge(tileMap))
            {
                patrolDirection *= -1;  // Reverse direction
                ChangeState(EnemyState.Idle);
                return;
            }

            // Look for targets
            if (ScanForTargets())
            {
                ChangeState(EnemyState.Chase);
            }
        }

        /// <summary>
        /// Chase state - pursuing target
        /// </summary>
        protected virtual void UpdateChase(float deltaTime, TileMap tileMap)
        {
            if (Target == null || !Target.IsActive)
            {
                Target = null;
                ChangeState(EnemyState.Patrol);
                return;
            }

            // Update distance to target
            targetDistance = Vector2.Distance(Center, Target.Center);

            // Lost target if too far
            if (targetDistance > LoseTargetRange)
            {
                Target = null;
                ChangeState(EnemyState.Patrol);
                return;
            }

            // Close enough to attack
            if (targetDistance <= Attack.AttackRange)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            // Move towards target
            if (Target.Center.X > Center.X)
            {
                Velocity.X = ChaseSpeed;
            }
            else
            {
                Velocity.X = -ChaseSpeed;
            }

            // Jump if target is above
            if (IsOnGround && Target.Center.Y < Center.Y - 20)
            {
                Velocity.Y = -250f;  // Jump
            }
        }

        /// <summary>
        /// Attack state - attempting to damage target
        /// </summary>
        protected virtual void UpdateAttack(float deltaTime, TileMap tileMap)
        {
            if (Target == null || !Target.IsActive)
            {
                Target = null;
                ChangeState(EnemyState.Patrol);
                return;
            }

            // Stop moving
            Velocity.X = 0;

            // Update distance
            targetDistance = Vector2.Distance(Center, Target.Center);

            // Too far to attack
            if (targetDistance > Attack.AttackRange * 1.5f)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            // Perform attack
            if (Attack.CanAttack)
            {
                Attack.Attack(this, Target);
            }
        }

        /// <summary>
        /// Stunned state - can't do anything
        /// </summary>
        protected virtual void UpdateStunned(float deltaTime, TileMap tileMap)
        {
            Velocity.X *= 0.9f;  // Slow down

            // Return to patrol after stun duration
            if (stateTimer >= 2.0f)
            {
                ChangeState(EnemyState.Patrol);
            }
        }

        /// <summary>
        /// Death state - dead enemy
        /// </summary>
        protected virtual void UpdateDeath(float deltaTime)
        {
            deathTimer += deltaTime;

            // Fade out
            float alpha = 1f - (deathTimer / DeathDespawnTime);
            TintColor = Color.White * MathHelper.Clamp(alpha, 0f, 1f);

            // Despawn after time
            if (deathTimer >= DeathDespawnTime)
            {
                IsActive = false;
            }
        }

        /// <summary>
        /// Apply movement and handle collisions
        /// </summary>
        protected virtual void ApplyMovement(float deltaTime, TileMap tileMap)
        {
            // Apply gravity if using it
            if (UseGravity)
            {
                Velocity.Y += 900f * deltaTime;
                Velocity.Y = MathHelper.Clamp(Velocity.Y, -400f, 400f);
            }

            // Move and handle collisions (simplified - a full implementation would be more robust)
            Position += Velocity * deltaTime;

            // Simple collision check
            Rectangle bounds = Bounds;
            IsOnGround = false;
            IsAgainstWall = false;

            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Solid))
            {
                Rectangle tileBounds = new Rectangle(tileX * tileMap.TileSize, tileY * tileMap.TileSize,
                                                     tileMap.TileSize, tileMap.TileSize);

                // Vertical collision
                if (Velocity.Y > 0 && bounds.Bottom > tileBounds.Top && bounds.Top < tileBounds.Top)
                {
                    Position.Y = tileBounds.Top - BoundsOffset.Y - BoundsHeight;
                    Velocity.Y = 0;
                    IsOnGround = true;
                }
                // Horizontal collision
                else if (Velocity.X != 0)
                {
                    if (Velocity.X > 0)
                    {
                        Position.X = tileBounds.Left - BoundsOffset.X - BoundsWidth;
                        IsAgainstWall = true;
                    }
                    else
                    {
                        Position.X = tileBounds.Right - BoundsOffset.X;
                        IsAgainstWall = true;
                    }
                    Velocity.X = 0;
                }
            }
        }

        /// <summary>
        /// Scan for potential targets
        /// </summary>
        protected virtual bool ScanForTargets()
        {
            // This would check for player entities within detection range
            // For now, return false - override in game code with actual target detection
            return false;
        }

        /// <summary>
        /// Set the target to chase
        /// </summary>
        public virtual void SetTarget(Entity target)
        {
            Target = target;
            if (target != null && State != EnemyState.Dead)
            {
                ChangeState(EnemyState.Chase);
            }
        }

        /// <summary>
        /// Check if near an edge (prevents falling off platforms)
        /// </summary>
        protected virtual bool IsNearEdge(TileMap tileMap)
        {
            if (!IsOnGround)
                return false;

            // Check one tile ahead in movement direction
            int checkX = (int)((Position.X + BoundsOffset.X + (FacingDirection > 0 ? BoundsWidth : -tileMap.TileSize)) / tileMap.TileSize);
            int checkY = (int)((Position.Y + BoundsOffset.Y + BoundsHeight + 1) / tileMap.TileSize);

            var collision = tileMap.GetCollision(checkX, checkY);
            return collision == TileCollision.None;  // No ground ahead = edge
        }

        /// <summary>
        /// Change AI state
        /// </summary>
        protected virtual void ChangeState(EnemyState newState)
        {
            if (State == newState)
                return;

            previousState = State;
            State = newState;
            stateTimer = 0;

            OnStateChanged(previousState, newState);
        }

        /// <summary>
        /// Called when state changes
        /// </summary>
        protected virtual void OnStateChanged(EnemyState oldState, EnemyState newState)
        {
            // Override for state-specific setup
        }

        /// <summary>
        /// Called when enemy takes damage
        /// </summary>
        protected virtual void OnDamaged(int damage, int remainingHealth, DamageType type, Entity source)
        {
            // Flash effect or knockback could go here
            if (source != null && State != EnemyState.Dead)
            {
                // Set attacker as target
                SetTarget(source);
            }
        }

        /// <summary>
        /// Called when enemy dies
        /// </summary>
        protected virtual void OnDeath(Entity source)
        {
            ChangeState(EnemyState.Dead);
            UseGravity = false;
            IsSolid = false;
            Velocity = Vector2.Zero;
            deathTimer = 0;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Health.OnDeath -= OnDeath;
            Health.OnDamaged -= OnDamaged;
        }
    }

    /// <summary>
    /// Flying enemy that doesn't obey gravity
    /// </summary>
    public class FlyingEnemy : Enemy
    {
        public float HoverHeight { get; set; } = 100f;
        public float FlySpeed { get; set; } = 80f;
        public float HoverAmplitude { get; set; } = 20f;
        public float HoverFrequency { get; set; } = 2f;

        private float hoverTimer;
        private Vector2 hoverOrigin;

        public FlyingEnemy()
        {
            UseGravity = false;
        }

        protected override void ApplyMovement(float deltaTime, TileMap tileMap)
        {
            hoverTimer += deltaTime;

            // Hover up and down
            float hoverOffset = (float)Math.Sin(hoverTimer * HoverFrequency) * HoverAmplitude;

            // Move towards target if chasing
            if (State == EnemyState.Chase && Target != null)
            {
                Vector2 direction = Target.Center - Center;
                if (direction != Vector2.Zero)
                {
                    direction.Normalize();
                    Velocity = direction * FlySpeed;
                }
            }
            else if (State == EnemyState.Patrol)
            {
                Velocity.X = patrolDirection * PatrolSpeed;
                Velocity.Y = (float)Math.Cos(hoverTimer * HoverFrequency) * HoverAmplitude;
            }

            Position += Velocity * deltaTime;

            // Simple wall collision
            Rectangle bounds = Bounds;
            foreach (var (tileX, tileY, collision) in tileMap.GetIntersectingTiles(bounds, TileCollision.Solid))
            {
                Rectangle tileBounds = new Rectangle(tileX * tileMap.TileSize, tileY * tileMap.TileSize,
                                                     tileMap.TileSize, tileMap.TileSize);

                // Reverse direction if hit wall
                if (Velocity.X > 0)
                    patrolDirection = -1;
                else if (Velocity.X < 0)
                    patrolDirection = 1;

                Velocity.X = 0;
            }
        }
    }
}
