using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Blobator.Json {
    [XmlRoot("Tileset")]
    public class Blob {
        public string Image { get; set; }
        public BlobTileBits Bits { get; set; }
        [XmlArrayItem("Tile")]
        public BlobTile[] Tiles { get; set; }
    }
}
