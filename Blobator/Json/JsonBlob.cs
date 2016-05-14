using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Blobator.Json {
    public class JsonBlob {
        public string Image { get; set; }
        public JsonTileBits Bits { get; set; }
        public JsonTile[] Tiles { get; set; }
    }
}
