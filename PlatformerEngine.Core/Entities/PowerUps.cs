using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Combat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatformerEngine.Core.Entities
{
    /// <summary>
    /// Power-up effect types
    /// </summary>
    public enum PowerUpType
    {
        JumpBoost,          // Increases jump height (pogo stick)
        SpeedBoost,         // Increases movement speed
        Invincibility,      // Temporary invincibility
        Shield,             // Absorbs damage
        DoubleDamage,       // Increases attack damage
        Magnet,             // Attracts coins/collectibles
        HealthRegen,        // Regenerates health over time
        Ammo,               // Gives ammo for shooting
        DoubleJump,         // Grants double jump ability
        WallJump,           // Grants wall jump ability
        Glide,              // Allows gliding/slow fall
        Dash,               // Grants dash ability
        SlowMotion,         // Slows down time
        GravityFlip,        // Reverses gravity
        Ghost,              // Pass through walls
        LightSource         // Illuminates dark areas
    }

    /// <summary>
    /// Base class for power-up effects
    /// </summary>
    public abstract class PowerUpEffect
    {
        public PowerUpType Type { get; set; }
        public float Duration { get; set; }
        public float RemainingTime { get; protected set; }
        public bool IsActive { get; protected set; }
        public bool IsPermanent { get; set; }

        public event Action<PowerUpEffect> OnEffectStart;
        public event Action<PowerUpEffect> OnEffectEnd;

        protected PowerUpEffect(PowerUpType type, float duration)
        {
            Type = type;
            Duration = duration;
            RemainingTime = duration;
            IsPermanent = duration <= 0;
        }

        public virtual void Start(Player player)
        {
            IsActive = true;
            RemainingTime = Duration;
            OnEffectStart?.Invoke(this);
        }

        public virtual void Update(float deltaTime, Player player)
        {
            if (!IsActive || IsPermanent)
                return;

            RemainingTime -= deltaTime;

            if (RemainingTime <= 0)
            {
                End(player);
            }
        }

        public virtual void End(Player player)
        {
            IsActive = false;
            RemainingTime = 0;
            OnEffectEnd?.Invoke(this);
        }

        public void Refresh()
        {
            RemainingTime = Duration;
        }
    }

    /// <summary>
    /// Jump boost power-up (pogo stick equivalent)
    /// </summary>
    public class JumpBoostEffect : PowerUpEffect
    {
        public float JumpMultiplier { get; set; } = 1.5f;
        private float originalJumpForce;

        public JumpBoostEffect(float duration = 10f) : base(PowerUpType.JumpBoost, duration)
        {
        }

        public override void Start(Player player)
        {
            base.Start(player);
            originalJumpForce = player.JumpForce;
            player.JumpForce *= JumpMultiplier;
        }

        public override void End(Player player)
        {
            player.JumpForce = originalJumpForce;
            base.End(player);
        }
    }

    /// <summary>
    /// Speed boost power-up
    /// </summary>
    public class SpeedBoostEffect : PowerUpEffect
    {
        public float SpeedMultiplier { get; set; } = 1.5f;
        private float originalMoveSpeed;

        public SpeedBoostEffect(float duration = 10f) : base(PowerUpType.SpeedBoost, duration)
        {
        }

        public override void Start(Player player)
        {
            base.Start(player);
            originalMoveSpeed = player.MoveSpeed;
            player.MoveSpeed *= SpeedMultiplier;
        }

        public override void End(Player player)
        {
            player.MoveSpeed = originalMoveSpeed;
            base.End(player);
        }
    }

    /// <summary>
    /// Invincibility power-up
    /// </summary>
    public class InvincibilityEffect : PowerUpEffect
    {
        public InvincibilityEffect(float duration = 8f) : base(PowerUpType.Invincibility, duration)
        {
        }

        public override void Start(Player player)
        {
            base.Start(player);
            var health = player as IHasHealth;
            if (health?.Health != null)
            {
                health.Health.IsInvulnerable = true;
            }
        }

        public override void End(Player player)
        {
            var health = player as IHasHealth;
            if (health?.Health != null)
            {
                health.Health.IsInvulnerable = false;
            }
            base.End(player);
        }
    }

    /// <summary>
    /// Shield power-up (absorbs damage)
    /// </summary>
    public class ShieldEffect : PowerUpEffect
    {
        public int ShieldHealth { get; set; }
        public int MaxShieldHealth { get; private set; }

        public ShieldEffect(int shieldHealth = 3) : base(PowerUpType.Shield, 0)
        {
            ShieldHealth = shieldHealth;
            MaxShieldHealth = shieldHealth;
            IsPermanent = true; // Lasts until shield breaks
        }

        public bool AbsorbDamage(int damage)
        {
            ShieldHealth -= damage;
            return ShieldHealth > 0;
        }

        public override void Update(float deltaTime, Player player)
        {
            if (ShieldHealth <= 0)
            {
                End(player);
            }
        }
    }

    /// <summary>
    /// Double damage power-up
    /// </summary>
    public class DoubleDamageEffect : PowerUpEffect
    {
        public float DamageMultiplier { get; set; } = 2.0f;
        private float originalDamageMultiplier;

        public DoubleDamageEffect(float duration = 10f) : base(PowerUpType.DoubleDamage, duration)
        {
        }

        public override void Start(Player player)
        {
            base.Start(player);
            var combat = player as IHasAttack;
            if (combat?.Attack != null)
            {
                originalDamageMultiplier = combat.Attack.DamageMultiplier;
                combat.Attack.DamageMultiplier *= DamageMultiplier;
            }
        }

        public override void End(Player player)
        {
            var combat = player as IHasAttack;
            if (combat?.Attack != null)
            {
                combat.Attack.DamageMultiplier = originalDamageMultiplier;
            }
            base.End(player);
        }
    }

    /// <summary>
    /// Magnet power-up (attracts collectibles)
    /// </summary>
    public class MagnetEffect : PowerUpEffect
    {
        public float AttractionRadius { get; set; } = 200f;
        public float AttractionForce { get; set; } = 300f;

        public MagnetEffect(float duration = 10f) : base(PowerUpType.Magnet, duration)
        {
        }

        public void AttractCollectibles(Player player, List<Collectible> collectibles, float deltaTime)
        {
            if (!IsActive)
                return;

            foreach (var collectible in collectibles.Where(c => c.IsActive))
            {
                float distance = Vector2.Distance(player.Position, collectible.Position);

                if (distance < AttractionRadius && distance > 10f)
                {
                    Vector2 direction = player.Position - collectible.Position;
                    direction.Normalize();
                    collectible.Position += direction * AttractionForce * deltaTime;
                }
            }
        }
    }

    /// <summary>
    /// Health regeneration power-up
    /// </summary>
    public class HealthRegenEffect : PowerUpEffect
    {
        public int HealPerSecond { get; set; } = 5;
        private float healTimer;

        public HealthRegenEffect(float duration = 10f) : base(PowerUpType.HealthRegen, duration)
        {
        }

        public override void Update(float deltaTime, Player player)
        {
            base.Update(deltaTime, player);

            if (!IsActive)
                return;

            healTimer += deltaTime;

            if (healTimer >= 1f)
            {
                healTimer = 0;
                var health = player as IHasHealth;
                health?.Health?.Heal(HealPerSecond);
            }
        }
    }

    /// <summary>
    /// Double jump power-up
    /// </summary>
    public class DoubleJumpEffect : PowerUpEffect
    {
        private int originalMaxAirJumps;

        public DoubleJumpEffect(float duration = 0f) : base(PowerUpType.DoubleJump, duration)
        {
            IsPermanent = true;
        }

        public override void Start(Player player)
        {
            base.Start(player);
            originalMaxAirJumps = player.MaxAirJumps;
            player.MaxAirJumps = Math.Max(player.MaxAirJumps, 1);
        }

        public override void End(Player player)
        {
            player.MaxAirJumps = originalMaxAirJumps;
            base.End(player);
        }
    }

    /// <summary>
    /// Glide/slow fall power-up
    /// </summary>
    public class GlideEffect : PowerUpEffect
    {
        public float GlideGravityScale { get; set; } = 0.3f;
        private float originalGravityScale;
        private bool isGliding;

        public GlideEffect(float duration = 10f) : base(PowerUpType.Glide, duration)
        {
        }

        public override void Start(Player player)
        {
            base.Start(player);
            originalGravityScale = player.GravityScale;
        }

        public void StartGliding(Player player)
        {
            if (!IsActive || isGliding)
                return;

            isGliding = true;
            player.GravityScale = GlideGravityScale;
        }

        public void StopGliding(Player player)
        {
            if (!isGliding)
                return;

            isGliding = false;
            player.GravityScale = originalGravityScale;
        }

        public override void End(Player player)
        {
            player.GravityScale = originalGravityScale;
            base.End(player);
        }
    }

    /// <summary>
    /// Dash ability power-up
    /// </summary>
    public class DashEffect : PowerUpEffect
    {
        public float DashSpeed { get; set; } = 500f;
        public float DashDuration { get; set; } = 0.2f;
        public float DashCooldown { get; set; } = 1.0f;

        private float dashTimer;
        private float cooldownTimer;
        private bool isDashing;
        private Vector2 dashDirection;

        public DashEffect(float duration = 0f) : base(PowerUpType.Dash, duration)
        {
            IsPermanent = true;
        }

        public bool CanDash => !isDashing && cooldownTimer <= 0;

        public void StartDash(Player player, Vector2 direction)
        {
            if (!CanDash || !IsActive)
                return;

            isDashing = true;
            dashTimer = DashDuration;
            cooldownTimer = DashCooldown;
            dashDirection = direction;
            dashDirection.Normalize();
        }

        public override void Update(float deltaTime, Player player)
        {
            base.Update(deltaTime, player);

            if (cooldownTimer > 0)
                cooldownTimer -= deltaTime;

            if (isDashing)
            {
                dashTimer -= deltaTime;
                player.Velocity = dashDirection * DashSpeed;

                if (dashTimer <= 0)
                {
                    isDashing = false;
                }
            }
        }
    }

    /// <summary>
    /// Power-up collectible
    /// </summary>
    public class PowerUp : Collectible
    {
        public PowerUpType PowerUpType { get; set; }
        public float EffectDuration { get; set; }
        public Func<PowerUpEffect> CreateEffect { get; set; }

        public PowerUp(PowerUpType type, float duration = 10f)
        {
            PowerUpType = type;
            EffectDuration = duration;
            Type = CollectibleType.PowerUp;

            // Set default effect creator
            CreateEffect = () => CreateDefaultEffect(type, duration);
        }

        protected override void OnCollected(Player player)
        {
            base.OnCollected(player);

            // Apply power-up effect through PowerUpManager
            var effect = CreateEffect?.Invoke();
            if (effect != null)
            {
                PowerUpManager.Instance.AddEffect(player, effect);
            }
        }

        private static PowerUpEffect CreateDefaultEffect(PowerUpType type, float duration)
        {
            switch (type)
            {
                case PowerUpType.JumpBoost:
                    return new JumpBoostEffect(duration);
                case PowerUpType.SpeedBoost:
                    return new SpeedBoostEffect(duration);
                case PowerUpType.Invincibility:
                    return new InvincibilityEffect(duration);
                case PowerUpType.Shield:
                    return new ShieldEffect(3);
                case PowerUpType.DoubleDamage:
                    return new DoubleDamageEffect(duration);
                case PowerUpType.Magnet:
                    return new MagnetEffect(duration);
                case PowerUpType.HealthRegen:
                    return new HealthRegenEffect(duration);
                case PowerUpType.DoubleJump:
                    return new DoubleJumpEffect(0);
                case PowerUpType.Glide:
                    return new GlideEffect(duration);
                case PowerUpType.Dash:
                    return new DashEffect(0);
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Manages active power-up effects
    /// </summary>
    public class PowerUpManager
    {
        private static PowerUpManager instance;
        public static PowerUpManager Instance => instance ??= new PowerUpManager();

        private Dictionary<Player, List<PowerUpEffect>> activeEffects = new Dictionary<Player, List<PowerUpEffect>>();

        public event Action<Player, PowerUpEffect> OnEffectAdded;
        public event Action<Player, PowerUpEffect> OnEffectRemoved;

        private PowerUpManager()
        {
        }

        /// <summary>
        /// Add a power-up effect to a player
        /// </summary>
        public void AddEffect(Player player, PowerUpEffect effect)
        {
            if (!activeEffects.ContainsKey(player))
                activeEffects[player] = new List<PowerUpEffect>();

            // Check if player already has this effect type
            var existingEffect = activeEffects[player].FirstOrDefault(e => e.Type == effect.Type);

            if (existingEffect != null)
            {
                // Refresh duration if stackable, otherwise replace
                if (!existingEffect.IsPermanent)
                {
                    existingEffect.Refresh();
                    return;
                }
            }

            activeEffects[player].Add(effect);
            effect.Start(player);
            OnEffectAdded?.Invoke(player, effect);
        }

        /// <summary>
        /// Remove a specific effect from a player
        /// </summary>
        public void RemoveEffect(Player player, PowerUpEffect effect)
        {
            if (!activeEffects.ContainsKey(player))
                return;

            effect.End(player);
            activeEffects[player].Remove(effect);
            OnEffectRemoved?.Invoke(player, effect);
        }

        /// <summary>
        /// Check if player has an active effect
        /// </summary>
        public bool HasEffect(Player player, PowerUpType type)
        {
            if (!activeEffects.ContainsKey(player))
                return false;

            return activeEffects[player].Any(e => e.Type == type && e.IsActive);
        }

        /// <summary>
        /// Get an active effect
        /// </summary>
        public PowerUpEffect GetEffect(Player player, PowerUpType type)
        {
            if (!activeEffects.ContainsKey(player))
                return null;

            return activeEffects[player].FirstOrDefault(e => e.Type == type && e.IsActive);
        }

        /// <summary>
        /// Get all active effects for a player
        /// </summary>
        public List<PowerUpEffect> GetActiveEffects(Player player)
        {
            if (!activeEffects.ContainsKey(player))
                return new List<PowerUpEffect>();

            return new List<PowerUpEffect>(activeEffects[player].Where(e => e.IsActive));
        }

        /// <summary>
        /// Update all active effects
        /// </summary>
        public void Update(float deltaTime, Player player)
        {
            if (!activeEffects.ContainsKey(player))
                return;

            var effects = activeEffects[player].ToList();

            foreach (var effect in effects)
            {
                effect.Update(deltaTime, player);

                if (!effect.IsActive)
                {
                    activeEffects[player].Remove(effect);
                    OnEffectRemoved?.Invoke(player, effect);
                }
            }
        }

        /// <summary>
        /// Update magnet effects (needs access to collectibles list)
        /// </summary>
        public void UpdateMagnetEffects(Player player, List<Collectible> collectibles, float deltaTime)
        {
            if (!activeEffects.ContainsKey(player))
                return;

            var magnetEffects = activeEffects[player].OfType<MagnetEffect>();
            foreach (var magnet in magnetEffects)
            {
                magnet.AttractCollectibles(player, collectibles, deltaTime);
            }
        }

        /// <summary>
        /// Clear all effects from a player
        /// </summary>
        public void ClearEffects(Player player)
        {
            if (!activeEffects.ContainsKey(player))
                return;

            var effects = activeEffects[player].ToList();
            foreach (var effect in effects)
            {
                effect.End(player);
            }

            activeEffects[player].Clear();
        }

        /// <summary>
        /// Handle shield damage absorption
        /// </summary>
        public bool TryAbsorbDamage(Player player, int damage)
        {
            var shield = GetEffect(player, PowerUpType.Shield) as ShieldEffect;
            return shield?.AbsorbDamage(damage) ?? false;
        }
    }

    /// <summary>
    /// Helper factory for creating power-ups
    /// </summary>
    public static class PowerUpFactory
    {
        /// <summary>
        /// Create a jump boost power-up (pogo stick equivalent)
        /// </summary>
        public static PowerUp CreateJumpBoost(float duration = 10f, float multiplier = 1.5f)
        {
            return new PowerUp(PowerUpType.JumpBoost, duration)
            {
                CreateEffect = () => new JumpBoostEffect(duration) { JumpMultiplier = multiplier }
            };
        }

        /// <summary>
        /// Create a speed boost power-up
        /// </summary>
        public static PowerUp CreateSpeedBoost(float duration = 10f, float multiplier = 1.5f)
        {
            return new PowerUp(PowerUpType.SpeedBoost, duration)
            {
                CreateEffect = () => new SpeedBoostEffect(duration) { SpeedMultiplier = multiplier }
            };
        }

        /// <summary>
        /// Create an invincibility power-up
        /// </summary>
        public static PowerUp CreateInvincibility(float duration = 8f)
        {
            return new PowerUp(PowerUpType.Invincibility, duration)
            {
                CreateEffect = () => new InvincibilityEffect(duration)
            };
        }

        /// <summary>
        /// Create a shield power-up
        /// </summary>
        public static PowerUp CreateShield(int shieldHealth = 3)
        {
            return new PowerUp(PowerUpType.Shield, 0)
            {
                CreateEffect = () => new ShieldEffect(shieldHealth)
            };
        }

        /// <summary>
        /// Create a double damage power-up
        /// </summary>
        public static PowerUp CreateDoubleDamage(float duration = 10f)
        {
            return new PowerUp(PowerUpType.DoubleDamage, duration);
        }

        /// <summary>
        /// Create a magnet power-up
        /// </summary>
        public static PowerUp CreateMagnet(float duration = 10f, float radius = 200f)
        {
            return new PowerUp(PowerUpType.Magnet, duration)
            {
                CreateEffect = () => new MagnetEffect(duration) { AttractionRadius = radius }
            };
        }

        /// <summary>
        /// Create a health regen power-up
        /// </summary>
        public static PowerUp CreateHealthRegen(float duration = 10f, int healPerSecond = 5)
        {
            return new PowerUp(PowerUpType.HealthRegen, duration)
            {
                CreateEffect = () => new HealthRegenEffect(duration) { HealPerSecond = healPerSecond }
            };
        }

        /// <summary>
        /// Create a double jump power-up
        /// </summary>
        public static PowerUp CreateDoubleJump()
        {
            return new PowerUp(PowerUpType.DoubleJump, 0);
        }

        /// <summary>
        /// Create a glide power-up
        /// </summary>
        public static PowerUp CreateGlide(float duration = 10f)
        {
            return new PowerUp(PowerUpType.Glide, duration);
        }

        /// <summary>
        /// Create a dash power-up
        /// </summary>
        public static PowerUp CreateDash()
        {
            return new PowerUp(PowerUpType.Dash, 0);
        }
    }
}
