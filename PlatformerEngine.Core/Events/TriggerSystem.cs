using Microsoft.Xna.Framework;
using PlatformerEngine.Core.Entities;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Events
{
    /// <summary>
    /// Trigger types
    /// </summary>
    public enum TriggerType
    {
        OnEnter,        // Activates when entity enters
        OnExit,         // Activates when entity leaves
        OnStay,         // Continuously active while entity is inside
        OnInteract,     // Requires input to activate
        OnCollect       // For collectibles
    }

    /// <summary>
    /// Trigger zone that executes events
    /// </summary>
    public class Trigger : Entity
    {
        public TriggerType Type { get; set; } = TriggerType.OnEnter;
        public string TriggerId { get; set; }
        public bool OnceOnly { get; set; } = false;
        public bool IsTriggered { get; private set; }
        public List<string> TargetTags { get; set; } = new List<string>();  // Only trigger for these tags

        public event Action<Entity> OnTriggered;

        private HashSet<Entity> entitiesInside = new HashSet<Entity>();

        public Trigger(string id, Rectangle bounds)
        {
            TriggerId = id;
            Position = new Vector2(bounds.X, bounds.Y);
            Width = bounds.Width;
            Height = bounds.Height;
            BoundsWidth = bounds.Width;
            BoundsHeight = bounds.Height;
            UseGravity = false;
            IsSolid = false;
            IsVisible = false;  // Triggers are invisible by default
        }

        public void CheckTrigger(Entity entity)
        {
            if (!IsActive || (OnceOnly && IsTriggered))
                return;

            // Check if entity has required tag
            if (TargetTags.Count > 0)
            {
                // Would need to add tag system to entities
                // For now, skip tag check
            }

            bool isInside = Overlaps(entity);

            switch (Type)
            {
                case TriggerType.OnEnter:
                    if (isInside && !entitiesInside.Contains(entity))
                    {
                        entitiesInside.Add(entity);
                        ActivateTrigger(entity);
                    }
                    else if (!isInside && entitiesInside.Contains(entity))
                    {
                        entitiesInside.Remove(entity);
                    }
                    break;

                case TriggerType.OnExit:
                    if (!isInside && entitiesInside.Contains(entity))
                    {
                        entitiesInside.Remove(entity);
                        ActivateTrigger(entity);
                    }
                    else if (isInside && !entitiesInside.Contains(entity))
                    {
                        entitiesInside.Add(entity);
                    }
                    break;

                case TriggerType.OnStay:
                    if (isInside)
                    {
                        entitiesInside.Add(entity);
                        ActivateTrigger(entity);
                    }
                    else
                    {
                        entitiesInside.Remove(entity);
                    }
                    break;

                case TriggerType.OnInteract:
                    // Would need input check here
                    if (isInside)
                    {
                        entitiesInside.Add(entity);
                    }
                    else
                    {
                        entitiesInside.Remove(entity);
                    }
                    break;
            }
        }

        public void InteractTrigger(Entity entity)
        {
            if (Type == TriggerType.OnInteract && entitiesInside.Contains(entity))
            {
                ActivateTrigger(entity);
            }
        }

        private void ActivateTrigger(Entity entity)
        {
            if (OnceOnly && IsTriggered)
                return;

            IsTriggered = true;
            OnTriggered?.Invoke(entity);
        }

        public void Reset()
        {
            IsTriggered = false;
            entitiesInside.Clear();
        }
    }

    /// <summary>
    /// Event action that can be triggered
    /// </summary>
    public abstract class GameEvent
    {
        public string EventId { get; set; }
        public abstract void Execute(Entity instigator = null);
    }

    /// <summary>
    /// Event manager
    /// </summary>
    public class EventManager
    {
        private Dictionary<string, List<GameEvent>> events = new Dictionary<string, List<GameEvent>>();
        private List<Trigger> triggers = new List<Trigger>();

        public void RegisterEvent(string triggerId, GameEvent gameEvent)
        {
            if (!events.ContainsKey(triggerId))
                events[triggerId] = new List<GameEvent>();

            events[triggerId].Add(gameEvent);
        }

        public void AddTrigger(Trigger trigger)
        {
            triggers.Add(trigger);
            trigger.OnTriggered += (entity) => ExecuteEvents(trigger.TriggerId, entity);
        }

        public void ExecuteEvents(string triggerId, Entity instigator = null)
        {
            if (events.TryGetValue(triggerId, out var eventList))
            {
                foreach (var gameEvent in eventList)
                {
                    gameEvent.Execute(instigator);
                }
            }
        }

        public void Update(List<Entity> entities)
        {
            foreach (var trigger in triggers)
            {
                foreach (var entity in entities)
                {
                    trigger.CheckTrigger(entity);
                }
            }
        }
    }

    /// <summary>
    /// Example event implementations
    /// </summary>
    public class MessageEvent : GameEvent
    {
        public string Message { get; set; }
        public Action<string> OnShowMessage { get; set; }

        public override void Execute(Entity instigator = null)
        {
            OnShowMessage?.Invoke(Message);
        }
    }

    public class SpawnEntityEvent : GameEvent
    {
        public Func<Entity> CreateEntity { get; set; }
        public Vector2 SpawnPosition { get; set; }

        public override void Execute(Entity instigator = null)
        {
            var entity = CreateEntity?.Invoke();
            if (entity != null)
            {
                entity.Position = SpawnPosition;
            }
        }
    }

    public class ChangeSceneEvent : GameEvent
    {
        public string SceneName { get; set; }
        public Action<string> OnChangeScene { get; set; }

        public override void Execute(Entity instigator = null)
        {
            OnChangeScene?.Invoke(SceneName);
        }
    }
}
