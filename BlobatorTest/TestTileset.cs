using Blobator.Tileset;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BlobatorTest {
    public class TestTileset : BlobTileset<Bitmap, Rectangle> {
        protected override Bitmap LoadImage(string path) {
            return Bitmap.FromFile(path) as Bitmap;
        }

        protected override Rectangle LoadRegion(Bitmap image, int x, int y, int width, int height) {
            return new Rectangle(x, y, width, height);
        }

        public TestTileset(string path)
            : base(path) {
        }
    }
}
