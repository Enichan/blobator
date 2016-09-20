using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Blobator.Json {
    [XmlRoot("Tile")]
    public class BlobTile {
        public BlobRect Source { get; set; }
        public int[] Indices { get; set; }
    }
}
