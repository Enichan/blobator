using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Blobator.Tileset {
    // This file contains all code related to reading blobator xml files and nothing else
    // If you don't use xml files (default) then you don't need this file
    partial class BlobTileset {
        public static Dictionary<string, dynamic> FromXmlFile(string path) {
            return FromXmlString(File.ReadAllText(path));
        }

        public static Dictionary<string, dynamic> FromXmlString(string xml) {
            var data = new Dictionary<string, dynamic>();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            var root = xmlDoc.SelectSingleNode("/Tileset");
            if (root == null) {
                throw new TilesetDataException("Could not find Tileset root element in xml");
            }

            var image = root.SelectSingleNode("Image");
            if (image == null) {
                throw new TilesetDataException("Could not find Image element in xml");
            }
            data["image"] = image.InnerText;

            var bits = root.SelectSingleNode("Bits");
            if (bits == null) {
                throw new TilesetDataException("Could not find Bits element in xml");
            }
            
            var bitsData = new Dictionary<string, int>();
            foreach (XmlNode child in bits.ChildNodes) {
                var key = child.LocalName.Substring(0, 1).ToLowerInvariant() + child.LocalName.Substring(1);
                int value;
                if (Int32.TryParse(child.InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) {
                    bitsData[key] = value;
                }
            }
            data["bits"] = bitsData;

            var tiles = root.SelectSingleNode("Tiles");
            if (tiles == null) {
                throw new TilesetDataException("Could not find Tiles element in xml");
            }

            var tilesData = new List<Dictionary<string, dynamic>>();
            foreach (XmlNode child in tiles.ChildNodes) {
                tilesData.Add(DeserializeXmlTile(child));
            }
            data["tiles"] = tilesData;

            return data;
        }

        private static Dictionary<string, dynamic> DeserializeXmlTile(XmlNode child) {
            var data = new Dictionary<string, dynamic>();

            var source = child.SelectSingleNode("Source");
            if (source == null) {
                throw new TilesetDataException("Tile xml element had no Source child");
            }

            var indices = child.SelectSingleNode("Indices");
            if (indices == null) {
                throw new TilesetDataException("Tile xml element had no Indices child");
            }

            var sourceData = new Dictionary<string, int>();
            int x, y, width, height;
            x = DeserializeXmlChildToInt(source, "X");
            y = DeserializeXmlChildToInt(source, "Y");
            width = DeserializeXmlChildToInt(source, "Width");
            height = DeserializeXmlChildToInt(source, "Height");

            sourceData["x"] = x; sourceData["y"] = y;
            sourceData["width"] = width; sourceData["height"] = height;
            data["source"] = sourceData;

            var indexData = new List<int>();
            foreach (XmlNode index in indices.SelectNodes("int")) {
                int value;
                if (Int32.TryParse(index.InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) {
                    indexData.Add(value);
                }
            }
            data["indices"] = indexData;

            return data;
        }

        private static int DeserializeXmlChildToInt(XmlNode parent, string childName) {
            var node = parent.SelectSingleNode(childName);
            if (node != null) {
                int value;
                if (Int32.TryParse(node.InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) {
                    return value;
                }
            }
            return 0;
        }
    }
}
