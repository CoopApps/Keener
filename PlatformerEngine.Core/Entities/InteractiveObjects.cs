using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlatformerEngine.Core.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatformerEngine.Core.Entities
{
    /// <summary>
    /// Switch that can be activated by player
    /// </summary>
    public class Switch : Entity
    {
        public bool IsActivated { get; private set; }
        public bool IsToggle { get; set; } = false; // If false, needs to be held
        public bool RequiresWeight { get; set; } = false; // Activated by standing on it
        public string SwitchId { get; set; }

        public event Action<Switch> OnActivated;
        public event Action<Switch> OnDeactivated;

        private HashSet<Entity> entitiesOnSwitch = new HashSet<Entity>();

        public Switch(string id)
        {
            SwitchId = id;
            UseGravity = false;
            IsSolid = true;
            Width = 32;
            Height = 16;
            BoundsWidth = 32;
            BoundsHeight = 16;
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            if (RequiresWeight)
            {
                // Check if anything is on the switch (handled externally)
                bool shouldBeActive = entitiesOnSwitch.Count > 0;

                if (shouldBeActive && !IsActivated)
                {
                    Activate();
                }
                else if (!shouldBeActive && IsActivated && !IsToggle)
                {
                    Deactivate();
                }
            }
        }

        public void CheckEntity(Entity entity)
        {
            if (!RequiresWeight)
                return;

            // Check if entity is standing on switch
            Rectangle entityBounds = entity.Bounds;
            Rectangle switchTop = new Rectangle(
                (int)Position.X,
                (int)Position.Y - 5,
                (int)BoundsWidth,
                5
            );

            if (entityBounds.Intersects(switchTop) && entity.Velocity.Y >= 0)
            {
                if (!entitiesOnSwitch.Contains(entity))
                {
                    entitiesOnSwitch.Add(entity);
                }
            }
            else
            {
                entitiesOnSwitch.Remove(entity);
            }
        }

        public void Activate()
        {
            if (!IsActivated || IsToggle)
            {
                IsActivated = true;
                OnActivated?.Invoke(this);
            }
        }

        public void Deactivate()
        {
            if (IsActivated)
            {
                IsActivated = false;
                OnDeactivated?.Invoke(this);
            }
        }

        public void Toggle()
        {
            if (IsActivated)
                Deactivate();
            else
                Activate();
        }

        public void Reset()
        {
            IsActivated = false;
            entitiesOnSwitch.Clear();
        }
    }

    /// <summary>
    /// Bridge or platform that can be toggled on/off
    /// </summary>
    public class Bridge : Entity
    {
        public bool IsExtended { get; private set; }
        public float ExtendSpeed { get; set; } = 100f;
        public Vector2 RetractedPosition { get; set; }
        public Vector2 ExtendedPosition { get; set; }
        public string BridgeId { get; set; }

        private float transitionProgress;
        private bool isTransitioning;

        public Bridge(string id)
        {
            BridgeId = id;
            UseGravity = false;
            IsSolid = true;
            Width = 96;
            Height = 16;
            BoundsWidth = 96;
            BoundsHeight = 16;
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            if (isTransitioning)
            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float progressDelta = deltaTime * ExtendSpeed / Vector2.Distance(RetractedPosition, ExtendedPosition);

                if (IsExtended)
                {
                    transitionProgress += progressDelta;
                    if (transitionProgress >= 1.0f)
                    {
                        transitionProgress = 1.0f;
                        isTransitioning = false;
                    }
                }
                else
                {
                    transitionProgress -= progressDelta;
                    if (transitionProgress <= 0.0f)
                    {
                        transitionProgress = 0.0f;
                        isTransitioning = false;
                        IsSolid = false; // Can't collide when retracted
                    }
                }

                Position = Vector2.Lerp(RetractedPosition, ExtendedPosition, transitionProgress);
            }
        }

        public void Extend()
        {
            if (!IsExtended)
            {
                IsExtended = true;
                isTransitioning = true;
                IsSolid = true;
            }
        }

        public void Retract()
        {
            if (IsExtended)
            {
                IsExtended = false;
                isTransitioning = true;
            }
        }

        public void Toggle()
        {
            if (IsExtended)
                Retract();
            else
                Extend();
        }

        public void SetupPositions(Vector2 retractedPos, Vector2 extendedPos)
        {
            RetractedPosition = retractedPos;
            ExtendedPosition = extendedPos;
            Position = retractedPos;
            transitionProgress = 0;
        }
    }

    /// <summary>
    /// Teleporter that transports entities to another location
    /// </summary>
    public class Teleporter : Entity
    {
        public string TeleporterId { get; set; }
        public string TargetTeleporterId { get; set; }
        public Teleporter TargetTeleporter { get; set; }
        public bool RequiresActivation { get; set; } = true;
        public bool IsTwoWay { get; set; } = true;
        public float TeleportCooldown { get; set; } = 0.5f;

        private float cooldownTimer;
        private HashSet<Entity> entitiesInside = new HashSet<Entity>();
        private HashSet<Entity> recentlyTeleported = new HashSet<Entity>();

        public event Action<Entity, Teleporter> OnEntityTeleported;

        public Teleporter(string id)
        {
            TeleporterId = id;
            UseGravity = false;
            IsSolid = false;
            Width = 32;
            Height = 48;
            BoundsWidth = 32;
            BoundsHeight = 48;
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            cooldownTimer -= deltaTime;

            // Clear recently teleported list after cooldown
            if (cooldownTimer <= 0)
            {
                recentlyTeleported.Clear();
            }
        }

        public void CheckEntity(Entity entity)
        {
            bool isInside = Overlaps(entity);

            if (isInside && !entitiesInside.Contains(entity))
            {
                entitiesInside.Add(entity);

                // Auto-teleport if doesn't require activation
                if (!RequiresActivation && !recentlyTeleported.Contains(entity))
                {
                    TeleportEntity(entity);
                }
            }
            else if (!isInside)
            {
                entitiesInside.Remove(entity);
            }
        }

        public bool CanTeleport(Entity entity)
        {
            return TargetTeleporter != null &&
                   entitiesInside.Contains(entity) &&
                   !recentlyTeleported.Contains(entity);
        }

        public void TeleportEntity(Entity entity)
        {
            if (!CanTeleport(entity))
                return;

            // Teleport to target
            entity.Position = TargetTeleporter.Position;

            // Mark as recently teleported to prevent instant return
            recentlyTeleported.Add(entity);
            if (TargetTeleporter != null)
            {
                TargetTeleporter.recentlyTeleported.Add(entity);
            }

            cooldownTimer = TeleportCooldown;
            if (TargetTeleporter != null)
            {
                TargetTeleporter.cooldownTimer = TeleportCooldown;
            }

            OnEntityTeleported?.Invoke(entity, TargetTeleporter);
        }

        public bool IsEntityInside(Entity entity)
        {
            return entitiesInside.Contains(entity);
        }
    }

    /// <summary>
    /// Platform that disappears and reappears on a timer
    /// </summary>
    public class DisappearingPlatform : Entity
    {
        public float VisibleDuration { get; set; } = 2.0f;
        public float InvisibleDuration { get; set; } = 2.0f;
        public bool StartsVisible { get; set; } = true;
        public float FadeTime { get; set; } = 0.3f;

        private float stateTimer;
        private float fadeAlpha = 1.0f;
        private bool isFading;

        public DisappearingPlatform()
        {
            UseGravity = false;
            IsSolid = true;
            Width = 64;
            Height = 16;
            BoundsWidth = 64;
            BoundsHeight = 16;
            IsVisible = StartsVisible;
            stateTimer = VisibleDuration;
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            stateTimer -= deltaTime;

            // Handle fading
            if (isFading)
            {
                if (IsVisible)
                {
                    // Fading out
                    fadeAlpha -= deltaTime / FadeTime;
                    if (fadeAlpha <= 0)
                    {
                        fadeAlpha = 0;
                        IsVisible = false;
                        IsSolid = false;
                        isFading = false;
                        stateTimer = InvisibleDuration;
                    }
                }
                else
                {
                    // Fading in
                    fadeAlpha += deltaTime / FadeTime;
                    if (fadeAlpha >= 1.0f)
                    {
                        fadeAlpha = 1.0f;
                        IsVisible = true;
                        IsSolid = true;
                        isFading = false;
                        stateTimer = VisibleDuration;
                    }
                }
            }
            else if (stateTimer <= 0)
            {
                // Start fading
                isFading = true;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture != null && spriteBatch != null && fadeAlpha > 0)
            {
                Rectangle sourceRect = CurrentFrame ?? new Rectangle(0, 0, (int)Width, (int)Height);
                spriteBatch.Draw(Texture, Position, sourceRect, Color.White * fadeAlpha);
            }
        }
    }

    /// <summary>
    /// Platform that crumbles when stepped on
    /// </summary>
    public class CrumblingPlatform : Entity
    {
        public float CrumbleDelay { get; set; } = 0.5f;
        public float RespawnTime { get; set; } = 3.0f;
        public bool AutoRespawn { get; set; } = true;

        private bool isCrumbling;
        private bool hasCrumbled;
        private float crumbleTimer;
        private float respawnTimer;
        private HashSet<Entity> entitiesOnPlatform = new HashSet<Entity>();

        public event Action<CrumblingPlatform> OnCrumble;
        public event Action<CrumblingPlatform> OnRespawn;

        public CrumblingPlatform()
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
            base.Update(gameTime, tileMap);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (hasCrumbled)
            {
                if (AutoRespawn)
                {
                    respawnTimer -= deltaTime;
                    if (respawnTimer <= 0)
                    {
                        Respawn();
                    }
                }
            }
            else if (isCrumbling)
            {
                crumbleTimer -= deltaTime;
                if (crumbleTimer <= 0)
                {
                    Crumble();
                }
            }
            else if (entitiesOnPlatform.Count > 0)
            {
                // Start crumbling
                isCrumbling = true;
                crumbleTimer = CrumbleDelay;
            }
        }

        public void CheckEntity(Entity entity)
        {
            if (hasCrumbled)
                return;

            // Check if entity is standing on platform
            Rectangle entityBounds = entity.Bounds;
            Rectangle platformTop = new Rectangle(
                (int)Position.X,
                (int)Position.Y - 5,
                (int)BoundsWidth,
                5
            );

            if (entityBounds.Intersects(platformTop) && entity.Velocity.Y >= 0)
            {
                if (!entitiesOnPlatform.Contains(entity))
                {
                    entitiesOnPlatform.Add(entity);
                }
            }
            else
            {
                entitiesOnPlatform.Remove(entity);
            }
        }

        private void Crumble()
        {
            hasCrumbled = true;
            isCrumbling = false;
            IsSolid = false;
            IsVisible = false;
            respawnTimer = RespawnTime;
            entitiesOnPlatform.Clear();
            OnCrumble?.Invoke(this);
        }

        public void Respawn()
        {
            hasCrumbled = false;
            isCrumbling = false;
            IsSolid = true;
            IsVisible = true;
            OnRespawn?.Invoke(this);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (isCrumbling && !hasCrumbled)
            {
                // Shake effect while crumbling
                float shake = (float)Math.Sin(crumbleTimer * 20) * 2;
                Vector2 shakeOffset = new Vector2(shake, 0);

                if (Texture != null && spriteBatch != null)
                {
                    Rectangle sourceRect = CurrentFrame ?? new Rectangle(0, 0, (int)Width, (int)Height);
                    spriteBatch.Draw(Texture, Position + shakeOffset, sourceRect, Color.White);
                }
            }
            else
            {
                base.Draw(spriteBatch);
            }
        }
    }

    /// <summary>
    /// Platform that moves when pushed
    /// </summary>
    public class MovingBlock : Entity
    {
        public float PushForce { get; set; } = 100f;
        public float Friction { get; set; } = 0.9f;
        public bool CanBePushed { get; set; } = true;

        public MovingBlock()
        {
            UseGravity = true;
            IsSolid = true;
            Width = 48;
            Height = 48;
            BoundsWidth = 48;
            BoundsHeight = 48;
            Mass = 2.0f; // Heavier than player
        }

        public override void Update(GameTime gameTime, TileMap tileMap)
        {
            base.Update(gameTime, tileMap);

            // Apply friction
            Velocity.X *= Friction;
        }

        public void Push(float direction)
        {
            if (CanBePushed)
            {
                Velocity.X += direction * PushForce;
            }
        }
    }

    /// <summary>
    /// Breakable wall that can be destroyed
    /// </summary>
    public class BreakableWall : Entity
    {
        public int Health { get; set; } = 3;
        public bool RequiresSpecialAbility { get; set; } = false;
        public string RequiredAbility { get; set; }

        private int currentHealth;
        public bool IsBroken { get; private set; }

        public event Action<BreakableWall> OnBreak;

        public BreakableWall()
        {
            UseGravity = false;
            IsSolid = true;
            Width = 32;
            Height = 32;
            BoundsWidth = 32;
            BoundsHeight = 32;
            currentHealth = Health;
        }

        public bool TryBreak(Entity attacker, string abilityUsed = null)
        {
            if (IsBroken)
                return false;

            if (RequiresSpecialAbility && abilityUsed != RequiredAbility)
                return false;

            currentHealth--;

            if (currentHealth <= 0)
            {
                Break();
                return true;
            }

            return false;
        }

        private void Break()
        {
            IsBroken = true;
            IsSolid = false;
            IsVisible = false;
            OnBreak?.Invoke(this);
        }

        public void Repair()
        {
            IsBroken = false;
            IsSolid = true;
            IsVisible = true;
            currentHealth = Health;
        }
    }

    /// <summary>
    /// Checkpoint that saves player progress
    /// </summary>
    public class Checkpoint : Entity
    {
        public string CheckpointId { get; set; }
        public bool IsActivated { get; private set; }
        public Vector2 RespawnPosition { get; set; }
        public Vector2 RespawnOffset { get; set; } = new Vector2(0, -32);

        public event Action<Checkpoint, Player> OnCheckpointActivated;

        public Checkpoint(string id)
        {
            CheckpointId = id;
            UseGravity = false;
            IsSolid = false;
            Width = 32;
            Height = 48;
            BoundsWidth = 32;
            BoundsHeight = 48;
        }

        public void CheckActivation(Player player)
        {
            if (!IsActivated && Overlaps(player))
            {
                Activate(player);
            }
        }

        public void Activate(Player player)
        {
            if (!IsActivated)
            {
                IsActivated = true;
                RespawnPosition = Position + RespawnOffset;
                OnCheckpointActivated?.Invoke(this, player);
            }
        }

        public void Reset()
        {
            IsActivated = false;
        }
    }

    /// <summary>
    /// Manager for switch/bridge connections
    /// </summary>
    public class InteractiveObjectManager
    {
        private List<Switch> switches = new List<Switch>();
        private List<Bridge> bridges = new List<Bridge>();
        private List<Teleporter> teleporters = new List<Teleporter>();
        private List<CrumblingPlatform> crumblingPlatforms = new List<CrumblingPlatform>();
        private List<Checkpoint> checkpoints = new List<Checkpoint>();

        private Dictionary<string, List<Bridge>> switchBridgeConnections = new Dictionary<string, List<Bridge>>();

        /// <summary>
        /// Add a switch
        /// </summary>
        public void AddSwitch(Switch switchObj)
        {
            switches.Add(switchObj);
        }

        /// <summary>
        /// Add a bridge
        /// </summary>
        public void AddBridge(Bridge bridge)
        {
            bridges.Add(bridge);
        }

        /// <summary>
        /// Add a teleporter
        /// </summary>
        public void AddTeleporter(Teleporter teleporter)
        {
            teleporters.Add(teleporter);
        }

        /// <summary>
        /// Add a crumbling platform
        /// </summary>
        public void AddCrumblingPlatform(CrumblingPlatform platform)
        {
            crumblingPlatforms.Add(platform);
        }

        /// <summary>
        /// Add a checkpoint
        /// </summary>
        public void AddCheckpoint(Checkpoint checkpoint)
        {
            checkpoints.Add(checkpoint);
        }

        /// <summary>
        /// Connect a switch to a bridge
        /// </summary>
        public void ConnectSwitchToBridge(string switchId, string bridgeId)
        {
            if (!switchBridgeConnections.ContainsKey(switchId))
                switchBridgeConnections[switchId] = new List<Bridge>();

            var bridge = bridges.FirstOrDefault(b => b.BridgeId == bridgeId);
            if (bridge != null)
            {
                switchBridgeConnections[switchId].Add(bridge);

                var switchObj = switches.FirstOrDefault(s => s.SwitchId == switchId);
                if (switchObj != null)
                {
                    switchObj.OnActivated += (s) => bridge.Extend();
                    switchObj.OnDeactivated += (s) => bridge.Retract();
                }
            }
        }

        /// <summary>
        /// Link two teleporters together
        /// </summary>
        public void LinkTeleporters(string teleporter1Id, string teleporter2Id)
        {
            var tele1 = teleporters.FirstOrDefault(t => t.TeleporterId == teleporter1Id);
            var tele2 = teleporters.FirstOrDefault(t => t.TeleporterId == teleporter2Id);

            if (tele1 != null && tele2 != null)
            {
                tele1.TargetTeleporter = tele2;
                tele2.TargetTeleporter = tele1;
                tele1.TargetTeleporterId = teleporter2Id;
                tele2.TargetTeleporterId = teleporter1Id;
            }
        }

        /// <summary>
        /// Update all interactive objects
        /// </summary>
        public void Update(GameTime gameTime, List<Entity> entities)
        {
            // Update switches
            foreach (var switchObj in switches)
            {
                foreach (var entity in entities)
                {
                    switchObj.CheckEntity(entity);
                }
            }

            // Update teleporters
            foreach (var teleporter in teleporters)
            {
                foreach (var entity in entities)
                {
                    teleporter.CheckEntity(entity);
                }
            }

            // Update crumbling platforms
            foreach (var platform in crumblingPlatforms)
            {
                foreach (var entity in entities)
                {
                    platform.CheckEntity(entity);
                }
            }

            // Update checkpoints
            foreach (var checkpoint in checkpoints)
            {
                var player = entities.OfType<Player>().FirstOrDefault();
                if (player != null)
                {
                    checkpoint.CheckActivation(player);
                }
            }
        }

        /// <summary>
        /// Get activated checkpoint for respawn
        /// </summary>
        public Checkpoint GetLastActivatedCheckpoint()
        {
            return checkpoints.LastOrDefault(c => c.IsActivated);
        }

        /// <summary>
        /// Get all entities for rendering/physics
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            var entities = new List<Entity>();
            entities.AddRange(switches);
            entities.AddRange(bridges);
            entities.AddRange(teleporters);
            entities.AddRange(crumblingPlatforms);
            entities.AddRange(checkpoints);
            return entities;
        }
    }
}
