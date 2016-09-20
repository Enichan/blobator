using Blobator.Tileset;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BlobatorTest {
    public class TestTileset : BlobTileset<Bitmap, Rectangle> {
        protected override Bitmap LoadImage(string path) {
            var bmp = Bitmap.FromFile(path) as Bitmap;
            if (bmp != null) {
                // copy bitmap then dispose original to unlock the source file
                // see https://support.microsoft.com/en-us/kb/814675 for details
                var dest = new Bitmap(bmp.Width, bmp.Height);
                using (var graphics = Graphics.FromImage(dest)) {
                    graphics.DrawImage(bmp,
                        new Rectangle(0, 0, dest.Width, dest.Height),
                        new Rectangle(0, 0, bmp.Width, bmp.Height),
                        GraphicsUnit.Pixel
                    );
                }
                bmp.Dispose();
                return dest;
            }
            return null;
        }

        protected override Rectangle LoadRegion(Bitmap image, int x, int y, int width, int height) {
            return new Rectangle(x, y, width, height);
        }

        public TestTileset(string path)
            : base(path) {
        }

        public TestTileset(Dictionary<string, dynamic> json)
            : base(json) {
        }
    }
}
