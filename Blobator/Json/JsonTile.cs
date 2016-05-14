using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Blobator.Json {
    public class JsonTile {
        public JsonRect Source { get; set; }
        public int[] Indices { get; set; }
    }
}
