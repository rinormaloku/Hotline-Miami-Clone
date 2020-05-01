using App.Engine;
using App.Engine.Physics.RigidShapes;

namespace App.Model.Entities
{
    public class Collectable
    {
        public readonly RigidShape CollisionShape;
        public readonly Weapon Item;
        public readonly SpriteContainer SpriteContainer;

        public Collectable(Weapon item, RigidShape collisionShape, SpriteContainer icon)
        {
            CollisionShape = collisionShape;
            Item = item;
            SpriteContainer = icon;
        }
    }
}