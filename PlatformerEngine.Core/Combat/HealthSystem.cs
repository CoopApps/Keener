using Microsoft.Xna.Framework;
using PlatformerEngine.Core.Entities;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Combat
{
    /// <summary>
    /// Damage types for different attack sources
    /// </summary>
    public enum DamageType
    {
        Physical,
        Fire,
        Ice,
        Electric,
        Poison,
        Fall,
        Crush,
        Projectile
    }

    /// <summary>
    /// Component that gives an entity health and damage handling
    /// </summary>
    public class HealthComponent
    {
        // Health values
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; private set; }

        // State
        public bool IsAlive => CurrentHealth > 0;
        public bool IsDead => CurrentHealth <= 0;
        public bool IsInvulnerable { get; set; }
        public float InvulnerabilityDuration { get; set; } = 1.0f;
        private float invulnerabilityTimer;

        // Regeneration
        public bool CanRegenerate { get; set; }
        public int RegenerateAmount { get; set; } = 1;
        public float RegenerateInterval { get; set; } = 1.0f;
        private float regenerateTimer;

        // Events
        public event Action<int, int, DamageType, Entity> OnDamaged;  // damage, remaining health, type, source
        public event Action<int, int> OnHealed;  // amount, new health
        public event Action<Entity> OnDeath;  // source that caused death
        public event Action OnRespawn;

        // Damage resistance (0 = no resistance, 1 = immune)
        private Dictionary<DamageType, float> resistances = new Dictionary<DamageType, float>();

        public HealthComponent(int maxHealth = 100, int? startingHealth = null)
        {
            MaxHealth = maxHealth;
            CurrentHealth = startingHealth ?? maxHealth;

            // Initialize resistances to 0
            foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
            {
                resistances[type] = 0f;
            }
        }

        /// <summary>
        /// Update health component (handles invulnerability timer, regeneration)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update invulnerability timer
            if (invulnerabilityTimer > 0)
            {
                invulnerabilityTimer -= deltaTime;
                if (invulnerabilityTimer <= 0)
                {
                    IsInvulnerable = false;
                }
            }

            // Handle regeneration
            if (CanRegenerate && IsAlive && CurrentHealth < MaxHealth)
            {
                regenerateTimer += deltaTime;
                if (regenerateTimer >= RegenerateInterval)
                {
                    Heal(RegenerateAmount);
                    regenerateTimer = 0;
                }
            }
        }

        /// <summary>
        /// Apply damage to this entity
        /// </summary>
        public bool TakeDamage(int amount, DamageType type = DamageType.Physical, Entity source = null)
        {
            if (IsDead || IsInvulnerable || amount <= 0)
                return false;

            // Apply resistance
            float resistance = GetResistance(type);
            int finalDamage = (int)(amount * (1f - resistance));

            if (finalDamage <= 0)
                return false;

            // Apply damage
            CurrentHealth -= finalDamage;
            if (CurrentHealth < 0)
                CurrentHealth = 0;

            // Trigger invulnerability
            if (InvulnerabilityDuration > 0)
            {
                IsInvulnerable = true;
                invulnerabilityTimer = InvulnerabilityDuration;
            }

            // Fire event
            OnDamaged?.Invoke(finalDamage, CurrentHealth, type, source);

            // Check for death
            if (IsDead)
            {
                OnDeath?.Invoke(source);
            }

            return true;
        }

        /// <summary>
        /// Heal this entity
        /// </summary>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
                return;

            int oldHealth = CurrentHealth;
            CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
            int actualHealed = CurrentHealth - oldHealth;

            if (actualHealed > 0)
            {
                OnHealed?.Invoke(actualHealed, CurrentHealth);
            }
        }

        /// <summary>
        /// Fully restore health
        /// </summary>
        public void FullHeal()
        {
            Heal(MaxHealth);
        }

        /// <summary>
        /// Instantly kill this entity
        /// </summary>
        public void Kill(Entity source = null)
        {
            if (IsDead)
                return;

            CurrentHealth = 0;
            OnDeath?.Invoke(source);
        }

        /// <summary>
        /// Respawn with full health
        /// </summary>
        public void Respawn()
        {
            CurrentHealth = MaxHealth;
            IsInvulnerable = false;
            invulnerabilityTimer = 0;
            regenerateTimer = 0;
            OnRespawn?.Invoke();
        }

        /// <summary>
        /// Set resistance for a damage type (0 = no resistance, 1 = immune)
        /// </summary>
        public void SetResistance(DamageType type, float resistance)
        {
            resistances[type] = MathHelper.Clamp(resistance, 0f, 1f);
        }

        /// <summary>
        /// Get resistance for a damage type
        /// </summary>
        public float GetResistance(DamageType type)
        {
            return resistances.TryGetValue(type, out float resistance) ? resistance : 0f;
        }

        /// <summary>
        /// Get health as a percentage (0.0 to 1.0)
        /// </summary>
        public float GetHealthPercentage()
        {
            return MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
        }

        /// <summary>
        /// Check if health is below a certain percentage
        /// </summary>
        public bool IsHealthBelow(float percentage)
        {
            return GetHealthPercentage() < percentage;
        }
    }

    /// <summary>
    /// Component for entities that can attack
    /// </summary>
    public class AttackComponent
    {
        // Attack properties
        public int Damage { get; set; } = 10;
        public DamageType DamageType { get; set; } = DamageType.Physical;
        public float AttackCooldown { get; set; } = 0.5f;
        public float AttackRange { get; set; } = 20f;
        public float KnockbackForce { get; set; } = 100f;

        // State
        public bool CanAttack => attackCooldownTimer <= 0;
        private float attackCooldownTimer;

        // Events
        public event Action<Entity> OnAttack;  // target
        public event Action<Entity, int> OnHit;  // target, damage dealt

        public void Update(GameTime gameTime)
        {
            if (attackCooldownTimer > 0)
            {
                attackCooldownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        /// <summary>
        /// Attempt to attack a target
        /// </summary>
        public bool Attack(Entity attacker, Entity target)
        {
            if (!CanAttack || target == null)
                return false;

            // Check if target is in range
            float distance = Vector2.Distance(attacker.Center, target.Center);
            if (distance > AttackRange)
                return false;

            // Check if target has health component
            var targetHealth = GetHealthComponent(target);
            if (targetHealth == null || targetHealth.IsDead)
                return false;

            // Perform attack
            OnAttack?.Invoke(target);

            // Apply damage
            bool damaged = targetHealth.TakeDamage(Damage, DamageType, attacker);

            if (damaged)
            {
                OnHit?.Invoke(target, Damage);

                // Apply knockback
                if (KnockbackForce > 0)
                {
                    Vector2 knockbackDirection = target.Center - attacker.Center;
                    if (knockbackDirection != Vector2.Zero)
                    {
                        knockbackDirection.Normalize();
                        target.Velocity += knockbackDirection * KnockbackForce;
                    }
                }
            }

            // Start cooldown
            attackCooldownTimer = AttackCooldown;

            return damaged;
        }

        /// <summary>
        /// Helper to get health component from entity (would normally use component system)
        /// </summary>
        private HealthComponent GetHealthComponent(Entity entity)
        {
            // This is a simplified approach - in a full ECS you'd look up components
            // For now, we'll add health components to entities via extension/derived classes
            if (entity is IHasHealth hasHealth)
                return hasHealth.Health;

            return null;
        }

        /// <summary>
        /// Reset attack cooldown (for combos, etc.)
        /// </summary>
        public void ResetCooldown()
        {
            attackCooldownTimer = 0;
        }
    }

    /// <summary>
    /// Interface for entities that have health
    /// </summary>
    public interface IHasHealth
    {
        HealthComponent Health { get; }
    }

    /// <summary>
    /// Projectile entity for ranged combat
    /// </summary>
    public class Projectile : Entity
    {
        public int Damage { get; set; } = 10;
        public DamageType DamageType { get; set; } = DamageType.Projectile;
        public float Speed { get; set; } = 200f;
        public float Lifetime { get; set; } = 5f;
        public bool PierceTargets { get; set; } = false;
        public Entity Owner { get; set; }

        private float lifetimeTimer;
        private HashSet<Entity> hitEntities = new HashSet<Entity>();

        public event Action<Entity> OnHitTarget;

        public Projectile(Entity owner, Vector2 direction, float speed = 200f)
        {
            Owner = owner;
            Speed = speed;

            if (direction != Vector2.Zero)
                direction.Normalize();

            Velocity = direction * Speed;
            UseGravity = false;
            Width = 8;
            Height = 8;
            BoundsWidth = 8;
            BoundsHeight = 8;
        }

        public override void Update(GameTime gameTime, Graphics.TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update lifetime
            lifetimeTimer += deltaTime;
            if (lifetimeTimer >= Lifetime)
            {
                OnDestroy();
                return;
            }

            // Move
            Position += Velocity * deltaTime;

            // Check tile collision
            if (tileMap.CheckCollision(Bounds, Graphics.TileCollision.Solid))
            {
                OnDestroy();
            }
        }

        /// <summary>
        /// Check collision with an entity and apply damage
        /// </summary>
        public bool CheckHit(Entity target)
        {
            if (target == Owner || !IsActive || !target.IsActive)
                return false;

            if (!PierceTargets && hitEntities.Contains(target))
                return false;

            if (!Overlaps(target))
                return false;

            // Try to damage target
            if (target is IHasHealth hasHealth)
            {
                bool damaged = hasHealth.Health.TakeDamage(Damage, DamageType, Owner);
                if (damaged)
                {
                    hitEntities.Add(target);
                    OnHitTarget?.Invoke(target);

                    if (!PierceTargets)
                    {
                        OnDestroy();
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
