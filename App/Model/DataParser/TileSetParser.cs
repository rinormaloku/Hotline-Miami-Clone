using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;
using App.Model.LevelData;

namespace App.Model.DataParser
{
    public static class TileSetParser
    {
        public static Dictionary<string, TileSet> LoadTileSets(Dictionary<string, Bitmap> tileMaps)
        {
            var tileSetFileNames = Directory.GetFiles("Assets/TileSets");
            var tileSets = new Dictionary<string, TileSet>();
            foreach (var fileName in tileSetFileNames)
            {
                var tileSet = ParseTileSet(fileName, tileMaps);
                tileSets.Add(tileSet.tileSetName, tileSet);
            }

            return tileSets;
        }
        
        private static TileSet ParseTileSet(string tileSetFilename, Dictionary<string, Bitmap> tileMaps)
        {
            var doc = new XmlDocument();
            doc.Load(tileSetFilename);
            var root = doc.DocumentElement;
            
            string source = null;
            foreach (XmlNode node in root)
            {
                if (node.Name == "image") source = node.Attributes.GetNamedItem("source").Value;
            }

            return new TileSet
            {
                tileSetName = Path.GetFileName(tileSetFilename),
                tileSize = int.Parse(root.GetAttribute("tileSize")),
                tileCount = int.Parse(root.GetAttribute("tilecount")),
                columns = int.Parse(root.GetAttribute("columns")),
                image = tileMaps[source],
            };
        }
    }
}