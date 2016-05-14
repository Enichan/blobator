using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blobator.Json {
    public class JsonTileBits {
        public int TopLeft { get; set; }
        public int Top { get; set; }
        public int TopRight { get; set; }
        public int Right { get; set; }
        public int BottomRight { get; set; }
        public int Bottom { get; set; }
        public int BottomLeft { get; set; }
        public int Left { get; set; }

        public JsonTileBits() {
            TopLeft = (int)TileBits.TopLeft;
            Top = (int)TileBits.Top;
            TopRight = (int)TileBits.TopRight;
            Right = (int)TileBits.Right;
            BottomRight = (int)TileBits.BottomRight;
            Bottom = (int)TileBits.Bottom;
            BottomLeft = (int)TileBits.BottomLeft;
            Left = (int)TileBits.Left;
        }
    }
}
