using Microsoft.Xna.Framework;
using PlatformerEngine.Core.Data;
using PlatformerEngine.Core.Graphics;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Editor
{
    /// <summary>
    /// Paint tile action for undo/redo
    /// </summary>
    public class PaintTileAction : IEditorAction
    {
        private TileLayer layer;
        private int x, y;
        private int oldTileId, newTileId;

        public string Description => $"Paint tile at ({x},{y})";

        public PaintTileAction(TileLayer layer, int x, int y, int newTileId)
        {
            this.layer = layer;
            this.x = x;
            this.y = y;
            this.oldTileId = layer.GetTile(x, y);
            this.newTileId = newTileId;
        }

        public void Execute()
        {
            layer.SetTile(x, y, newTileId);
        }

        public void Undo()
        {
            layer.SetTile(x, y, oldTileId);
        }
    }

    /// <summary>
    /// Paint collision action
    /// </summary>
    public class PaintCollisionAction : IEditorAction
    {
        private TileMap tileMap;
        private int x, y;
        private TileCollision oldCollision, newCollision;

        public string Description => $"Paint collision at ({x},{y})";

        public PaintCollisionAction(TileMap tileMap, int x, int y, TileCollision newCollision)
        {
            this.tileMap = tileMap;
            this.x = x;
            this.y = y;
            this.oldCollision = tileMap.GetTileCollision(x, y);
            this.newCollision = newCollision;
        }

        public void Execute()
        {
            tileMap.SetTileCollision(x, y, newCollision);
        }

        public void Undo()
        {
            tileMap.SetTileCollision(x, y, oldCollision);
        }
    }

    /// <summary>
    /// Place entity action
    /// </summary>
    public class PlaceEntityAction : IEditorAction
    {
        private List<EntityData> entities;
        private EntityData entity;

        public string Description => $"Place {entity.Type}";

        public PlaceEntityAction(List<EntityData> entities, EntityData entity)
        {
            this.entities = entities;
            this.entity = entity;
        }

        public void Execute()
        {
            if (!entities.Contains(entity))
            {
                entities.Add(entity);
            }
        }

        public void Undo()
        {
            entities.Remove(entity);
        }
    }

    /// <summary>
    /// Delete entity action
    /// </summary>
    public class DeleteEntityAction : IEditorAction
    {
        private List<EntityData> entities;
        private EntityData entity;
        private int index;

        public string Description => $"Delete {entity.Type}";

        public DeleteEntityAction(List<EntityData> entities, EntityData entity)
        {
            this.entities = entities;
            this.entity = entity;
            this.index = entities.IndexOf(entity);
        }

        public void Execute()
        {
            entities.Remove(entity);
        }

        public void Undo()
        {
            if (index >= 0 && index <= entities.Count)
            {
                entities.Insert(index, entity);
            }
            else
            {
                entities.Add(entity);
            }
        }
    }

    /// <summary>
    /// Compound action (for multiple actions as one)
    /// </summary>
    public class CompoundAction : IEditorAction
    {
        private List<IEditorAction> actions = new List<IEditorAction>();
        private string description;

        public string Description => description;

        public CompoundAction(string description)
        {
            this.description = description;
        }

        public void AddAction(IEditorAction action)
        {
            actions.Add(action);
        }

        public void Execute()
        {
            foreach (var action in actions)
            {
                action.Execute();
            }
        }

        public void Undo()
        {
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                actions[i].Undo();
            }
        }
    }

    /// <summary>
    /// Prefab data - reusable entity groups
    /// </summary>
    public class Prefab
    {
        public string Name { get; set; }
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        public Vector2 PivotPoint { get; set; }

        public Prefab Clone()
        {
            return new Prefab
            {
                Name = Name,
                Entities = new List<EntityData>(Entities),
                PivotPoint = PivotPoint
            };
        }
    }

    /// <summary>
    /// Clipboard for copy/paste
    /// </summary>
    public class EditorClipboard
    {
        public int[,] Tiles { get; set; }
        public TileCollision[,] Collisions { get; set; }
        public List<EntityData> Entities { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool IsEmpty => Tiles == null && Collisions == null &&
                              (Entities == null || Entities.Count == 0);
    }
}
