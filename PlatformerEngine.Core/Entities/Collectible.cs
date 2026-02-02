using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Graphics;
using System;

namespace PlatformerEngine.Core.Entities
{
    /// <summary>
    /// Types of collectible items
    /// </summary>
    public enum CollectibleType
    {
        Coin,
        Gem,
        Health,
        PowerUp,
        Key,
        Ammo,
        Life,
        Custom
    }

    /// <summary>
    /// Base collectible item that can be picked up by the player
    /// </summary>
    public class Collectible : Entity
    {
        // Properties
        public CollectibleType Type { get; set; } = CollectibleType.Coin;
        public int Value { get; set; } = 1;
        public bool IsCollected { get; private set; }
        public float CollectRadius { get; set; } = 16f;

        // Animation
        public bool Animate { get; set; } = true;
        public float BounceHeight { get; set; } = 5f;
        public float BounceSpeed { get; set; } = 3f;
        public float SpinSpeed { get; set; } = 2f;

        private float animationTimer;
        private Vector2 startPosition;
        private float currentRotation;

        // Magnetic attraction
        public bool IsMagnetic { get; set; } = false;
        public float MagnetRadius { get; set; } = 50f;
        public float MagnetSpeed { get; set; } = 150f;

        // Events
        public event Action<Entity> OnCollected;  // collector entity

        public Collectible()
        {
            Width = 16;
            Height = 16;
            BoundsWidth = 12;
            BoundsHeight = 12;
            BoundsOffset = new Vector2(2, 2);

            UseGravity = false;
            IsSolid = false;
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            if (!IsActive || IsCollected)
                return;

            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            animationTimer += deltaTime;

            // Remember start position for bounce animation
            if (startPosition == Vector2.Zero)
                startPosition = Position;

            // Bounce animation
            if (Animate)
            {
                float bounce = (float)Math.Sin(animationTimer * BounceSpeed) * BounceHeight;
                Position.Y = startPosition.Y + bounce;

                // Spin
                currentRotation += SpinSpeed * deltaTime;
                Rotation = currentRotation;
            }
        }

        /// <summary>
        /// Try to collect this item
        /// </summary>
        public virtual bool TryCollect(Entity collector)
        {
            if (IsCollected || !IsActive)
                return false;

            // Check if collector is close enough
            float distance = Vector2.Distance(Center, collector.Center);
            if (distance > CollectRadius)
                return false;

            Collect(collector);
            return true;
        }

        /// <summary>
        /// Collect this item
        /// </summary>
        protected virtual void Collect(Entity collector)
        {
            IsCollected = true;
            IsActive = false;
            IsVisible = false;

            OnCollected?.Invoke(collector);
        }

        /// <summary>
        /// Update magnetic attraction to target
        /// </summary>
        public void UpdateMagneticAttraction(Entity target, float deltaTime)
        {
            if (!IsMagnetic || IsCollected || target == null)
                return;

            float distance = Vector2.Distance(Center, target.Center);

            if (distance <= MagnetRadius)
            {
                // Move towards target
                Vector2 direction = target.Center - Center;
                if (direction != Vector2.Zero)
                {
                    direction.Normalize();
                    Position += direction * MagnetSpeed * deltaTime;
                }
            }
        }
    }

    /// <summary>
    /// Coin collectible
    /// </summary>
    public class Coin : Collectible
    {
        public Coin(int value = 1)
        {
            Type = CollectibleType.Coin;
            Value = value;
        }
    }

    /// <summary>
    /// Health pickup
    /// </summary>
    public class HealthPickup : Collectible
    {
        public int HealAmount { get; set; } = 20;

        public HealthPickup(int healAmount = 20)
        {
            Type = CollectibleType.Health;
            HealAmount = healAmount;
            Value = healAmount;
        }

        protected override void Collect(Entity collector)
        {
            base.Collect(collector);

            // Heal the collector if they have health
            if (collector is Combat.IHasHealth hasHealth)
            {
                hasHealth.Health.Heal(HealAmount);
            }
        }
    }

    /// <summary>
    /// Power-up item that grants temporary effects
    /// </summary>
    public class PowerUp : Collectible
    {
        public string PowerUpId { get; set; }
        public float Duration { get; set; } = 10f;

        public PowerUp(string powerUpId, float duration = 10f)
        {
            Type = CollectibleType.PowerUp;
            PowerUpId = powerUpId;
            Duration = duration;
        }
    }

    /// <summary>
    /// Key for unlocking doors/gates
    /// </summary>
    public class Key : Collectible
    {
        public string KeyId { get; set; }
        public Color KeyColor { get; set; } = Color.Gold;

        public Key(string keyId)
        {
            Type = CollectibleType.Key;
            KeyId = keyId;
        }
    }

    /// <summary>
    /// Extra life
    /// </summary>
    public class ExtraLife : Collectible
    {
        public ExtraLife()
        {
            Type = CollectibleType.Life;
            Value = 1;
            CollectRadius = 20f;
        }
    }

    /// <summary>
    /// Manages player inventory and collection tracking
    /// </summary>
    public class Inventory
    {
        // Counters
        public int Coins { get; private set; }
        public int Lives { get; private set; } = 3;
        public int Score { get; private set; }

        // Collections
        private System.Collections.Generic.HashSet<string> collectedKeys = new System.Collections.Generic.HashSet<string>();
        private System.Collections.Generic.Dictionary<string, PowerUpInstance> activePowerUps = new System.Collections.Generic.Dictionary<string, PowerUpInstance>();

        // Events
        public event Action<int, int> OnCoinsChanged;  // new amount, change
        public event Action<int, int> OnLivesChanged;  // new amount, change
        public event Action<int, int> OnScoreChanged;  // new amount, change
        public event Action<string> OnKeyCollected;
        public event Action<string, float> OnPowerUpActivated;  // id, duration
        public event Action<string> OnPowerUpExpired;

        private class PowerUpInstance
        {
            public string Id;
            public float TimeRemaining;
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update active power-ups
            var expiredPowerUps = new System.Collections.Generic.List<string>();

            foreach (var powerUp in activePowerUps.Values)
            {
                powerUp.TimeRemaining -= deltaTime;
                if (powerUp.TimeRemaining <= 0)
                {
                    expiredPowerUps.Add(powerUp.Id);
                }
            }

            // Remove expired power-ups
            foreach (var id in expiredPowerUps)
            {
                activePowerUps.Remove(id);
                OnPowerUpExpired?.Invoke(id);
            }
        }

        /// <summary>
        /// Add coins
        /// </summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            Coins += amount;
            OnCoinsChanged?.Invoke(Coins, amount);
        }

        /// <summary>
        /// Remove coins
        /// </summary>
        public bool RemoveCoins(int amount)
        {
            if (amount <= 0 || Coins < amount)
                return false;

            Coins -= amount;
            OnCoinsChanged?.Invoke(Coins, -amount);
            return true;
        }

        /// <summary>
        /// Add score
        /// </summary>
        public void AddScore(int points)
        {
            if (points <= 0) return;

            Score += points;
            OnScoreChanged?.Invoke(Score, points);
        }

        /// <summary>
        /// Add lives
        /// </summary>
        public void AddLives(int amount)
        {
            if (amount <= 0) return;

            Lives += amount;
            OnLivesChanged?.Invoke(Lives, amount);
        }

        /// <summary>
        /// Remove a life
        /// </summary>
        public bool RemoveLife()
        {
            if (Lives <= 0)
                return false;

            Lives--;
            OnLivesChanged?.Invoke(Lives, -1);
            return true;
        }

        /// <summary>
        /// Collect a key
        /// </summary>
        public void CollectKey(string keyId)
        {
            if (collectedKeys.Add(keyId))
            {
                OnKeyCollected?.Invoke(keyId);
            }
        }

        /// <summary>
        /// Check if player has a key
        /// </summary>
        public bool HasKey(string keyId)
        {
            return collectedKeys.Contains(keyId);
        }

        /// <summary>
        /// Use a key (removes it from inventory)
        /// </summary>
        public bool UseKey(string keyId)
        {
            return collectedKeys.Remove(keyId);
        }

        /// <summary>
        /// Activate a power-up
        /// </summary>
        public void ActivatePowerUp(string powerUpId, float duration)
        {
            // Replace existing power-up or add new
            activePowerUps[powerUpId] = new PowerUpInstance
            {
                Id = powerUpId,
                TimeRemaining = duration
            };

            OnPowerUpActivated?.Invoke(powerUpId, duration);
        }

        /// <summary>
        /// Check if a power-up is active
        /// </summary>
        public bool HasPowerUp(string powerUpId)
        {
            return activePowerUps.ContainsKey(powerUpId);
        }

        /// <summary>
        /// Get remaining time for a power-up
        /// </summary>
        public float GetPowerUpTimeRemaining(string powerUpId)
        {
            return activePowerUps.TryGetValue(powerUpId, out var powerUp) ? powerUp.TimeRemaining : 0f;
        }

        /// <summary>
        /// Reset inventory
        /// </summary>
        public void Reset()
        {
            Coins = 0;
            Score = 0;
            Lives = 3;
            collectedKeys.Clear();
            activePowerUps.Clear();
        }
    }
}
