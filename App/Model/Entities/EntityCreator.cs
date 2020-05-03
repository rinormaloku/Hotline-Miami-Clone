using System.Collections.Generic;
using System.Drawing;
using App.Engine;
using App.Engine.Physics;
using App.Engine.Sprites;
using App.Model.LevelData;

namespace App.Model.Entities
{
    public static class EntityCreator
    {
        private static Dictionary<string, int> weaponFramesID = new Dictionary<string, int>
        {
            {"AK-303", 0},
            {"Shotgun", 1},
            {"MP6", 2},
            {"Saiga Full-Auto", 3},
        };
        
        public static Player CreatePlayer(Vector startPosition, float angle, List<Weapon> startWeapons, Level currentLevel)
        {
            var weaponSprites = new Dictionary<string, Sprite>();
            foreach (var weaponName in weaponFramesID.Keys)
            {
                weaponSprites.Add(weaponName, 
                    new StaticSprite(
                        currentLevel.PlayerWeaponsTileMap,
                        weaponFramesID[weaponName], 
                        new Size(85, 64))); 
            }

            return new Player(
                startPosition,
                angle,
                new PlayerBodySprite(
                    startPosition, currentLevel.PlayerClothesTileMap,1, 14, 27,
                    new Size(64, 64)),
                startWeapons,
                weaponSprites);
        }
    }
}