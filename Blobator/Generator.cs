using Blobator.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;
using Newtonsoft.Json.Serialization;
using System.Xml.Serialization;

namespace Blobator {
    enum Edge {
        Top, Left, Right, Bottom
    }

    enum PartMicro {
        BR_CornerInner = 0,
        BL_CornerInner = 1,
        TR_CornerInner = 2,
        TL_CornerInner = 3,
        TL_CornerOuter = 4,
        TL_TR_EdgeHorizontal = 5,
        TR_CornerOuter = 6,
        TL_BL_EdgeVertical = 7,
        Full = 8,
        TR_BR_EdgeVertical = 9,
        BL_CornerOuter = 10,
        BL_BR_EdgeHorizontal = 11,
        BR_CornerOuter = 12
    }

    enum Part {
        BR_CornerInner = 0,
        BL_CornerInner = 1,
        TR_CornerInner = 2,
        TL_CornerInner = 3,

        TL_CornerOuter = 4,
        TL_EdgeHorizontal = 6,
        TL_EdgeVertical = 12,
        TL_Full = 14,

        BL_CornerOuter = 16,
        BL_EdgeHorizontal = 18,
        BL_EdgeVertical = 8,
        BL_Full = 10,

        TR_CornerOuter = 7,
        TR_EdgeHorizontal = 5,
        TR_EdgeVertical = 15,
        TR_Full = 13,

        BR_CornerOuter = 19,
        BR_EdgeHorizontal = 17,
        BR_EdgeVertical = 11,
        BR_Full = 9,
    }

    [Flags]
    enum TileBits {
        TopLeft = 1,
        Top = 1 << 1,
        TopRight = 1 << 2,
        Right = 1 << 3,
        BottomRight = 1 << 4,
        Bottom = 1 << 5,
        BottomLeft = 1 << 6,
        Left = 1 << 7
    }

    // Binary assignments:
    // |  1  |  2  |  4  |
    // | 128 |     |  8  |
    // | 64  | 32  | 16  |
    //
    // Parts in order from left-right top-bottom:
    // XX  XX
    // X-  -X
    //
    // X-  -X
    // XX  XX
    //
    // --  --  --
    // -X  XX  X-
    //
    // -X  XX  X-
    // -X  XX  X-
    //
    // -X  XX  X-
    // --  --  --

    public class Generator {
        const int TopLeft = (int)TileBits.TopLeft;
        const int Top = (int)TileBits.Top;
        const int TopRight = (int)TileBits.TopRight;
        const int Right = (int)TileBits.Right;
        const int BottomRight = (int)TileBits.BottomRight;
        const int Bottom = (int)TileBits.Bottom;
        const int BottomLeft = (int)TileBits.BottomLeft;
        const int Left = (int)TileBits.Left;

        static void Main() {
            var args = CommandLineArguments.Parse();

            if (args.Count == 0) {
                Console.WriteLine("Usage: blobator.exe <size> <filename without extension>");
                Console.WriteLine("  If filename is unspecified, 'blob' is used.");
                Console.WriteLine("Options:");
                Console.WriteLine("  -pow2, -poweroftwo: fit output image to power of 2 size");
                Console.WriteLine("  -indent, -indented: indent json output");
                Console.WriteLine("  -source <filename>: assemble tiles from subtiles");
                Console.WriteLine("  -index: draw lowest tile index on template");
                Console.WriteLine("  -font <family> <size>: specify font to use for text rendering");
                Console.WriteLine("  -xml: output xml instead of json");
                Console.WriteLine("  -iso: isometric mode");
                return;
            }

            // default arguments
            var size = 32;
            var powerOfTwo = false;
            var filename = "blob";
            var indented = false;
            string source = null;
            var indexed = false;
            var fontFamily = "Times New Roman";
            var xml = false;
            var isometric = false;

            if (args.Count > 0) {
                size = Int32.Parse(args[0]);
            }

            int optStart = 2;
            if (args.Count > 1) {
                if (args[1].StartsWith("-")) {
                    optStart = 1;
                }
                else {
                    filename = args[1];
                }
            }

            for (int i = optStart; i < args.Count; i++) {
                var arg = args[i].ToLowerInvariant();
                if (arg == "-pow2" || arg == "-poweroftwo") {
                    powerOfTwo = true;
                }
                if (arg == "-indent" || arg == "-indented") {
                    indented = true;
                }
                if (arg == "-index") {
                    indexed = true;
                }
                if (arg == "-font") {
                    i++;
                    if (i < args.Count) {
                        fontFamily = args[i];
                        continue;
                    }
                }
                if (arg == "-source") {
                    i++;
                    if (i < args.Count) {
                        source = args[i];
                        continue;
                    }
                }
                if (arg == "-xml") {
                    xml = true;
                }
                if (arg == "-iso") {
                    isometric = true;
                }
            }

            try {
                Generate(size, filename, powerOfTwo, indented, source, indexed, fontFamily, xml, isometric);
            }
            catch (Exception e) {
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine("Aborting");
            }
        }

        public static void Generate(int size, string path, bool powerOfTwo = false, bool indented = false, string source = null, bool indexed = false, string fontFamily = "Times New Roman", bool xml = false, bool isometric = false) {
            var images = GetParts(size);
            var uniques = new HashSet<int>();
            var duplicates = new Dictionary<int, int>();

            GetSets(uniques, duplicates);

            List<Bitmap> parts;
            if (source == null) {
                if (isometric) {
                    throw new NotImplementedException("Please use -source when using -iso");
                }
                parts = GetParts(size / 2);
            }
            else {
                if (!File.Exists(source)) {
                    throw new Exception("Could not find source file '" + source + "'");
                }
                parts = GetParts(source, size / 2, isometric);
            }
            var micro = parts.Count < 20;

            var sources = new Dictionary<int, Rectangle>();

            // 7x7 resize to power of 2 if desired
            Bitmap blob;
            if (powerOfTwo) {
                int power = 0;
                while (Math.Pow(2, power) < 7 * size) {
                    power++;
                }

                var width = (int)Math.Pow(2, power);
                var imagesPerRow = width / size;
                var rows = 47 / imagesPerRow;
                var minimumHeight = rows * size;

                if (isometric) {
                    minimumHeight /= 2;
                }

                power = 0;
                while (Math.Pow(2, power) < minimumHeight) {
                    power++;
                }
                var height = (int)Math.Pow(2, power);

                blob = new Bitmap(width, height);
            }
            else {
                blob = new Bitmap(7 * size, (7 * size) / (isometric ? 2 : 1));
            }

            int x = 0, y = 0;
            int tileIndex = 0;

            var json = new Blob();
            json.Tiles = new BlobTile[47];
            json.Bits = new BlobTileBits();

            using (var blobGfx = Graphics.FromImage(blob)) {
                blobGfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                var font = new Font(fontFamily, (size / 32f) * 10, FontStyle.Bold);

                foreach (var index in uniques) {
                    var top = (index & Top) == Top;
                    var right = (index & Right) == Right;
                    var bottom = (index & Bottom) == Bottom;
                    var left = (index & Left) == Left;

                    using (var img = new Bitmap(size, isometric ? size / 2 : size)) {
                        using (var gfx = Graphics.FromImage(img)) {
                            Bitmap part;

                            if (isometric) {
                                var xSize = size;
                                var ySize = size / 2;

                                // topleft part
                                if (top && left) {
                                    if ((index & TopLeft) == TopLeft) {
                                        part = parts[micro ? (int)PartMicro.Full : (int)Part.TL_Full];
                                    }
                                    else {
                                        part = parts[micro ? (int)PartMicro.TL_CornerInner : (int)Part.TL_CornerInner];
                                    }
                                }
                                else if (top) {
                                    part = parts[micro ? (int)PartMicro.TL_BL_EdgeVertical : (int)Part.TL_EdgeVertical];
                                }
                                else if (left) {
                                    part = parts[micro ? (int)PartMicro.TL_TR_EdgeHorizontal : (int)Part.TL_EdgeHorizontal];
                                }
                                else {
                                    part = parts[micro ? (int)PartMicro.TL_CornerOuter : (int)Part.TL_CornerOuter];
                                }
                                gfx.DrawImage(part, xSize / 4, 0);

                                // topright part
                                if (top && right) {
                                    if ((index & TopRight) == TopRight) {
                                        part = parts[micro ? (int)PartMicro.Full : (int)Part.TR_Full];
                                    }
                                    else {
                                        part = parts[micro ? (int)PartMicro.TR_CornerInner : (int)Part.TR_CornerInner];
                                    }
                                }
                                else if (top) {
                                    part = parts[micro ? (int)PartMicro.TR_BR_EdgeVertical : (int)Part.TR_EdgeVertical];
                                }
                                else if (right) {
                                    part = parts[micro ? (int)PartMicro.TL_TR_EdgeHorizontal : (int)Part.TR_EdgeHorizontal];
                                }
                                else {
                                    part = parts[micro ? (int)PartMicro.TR_CornerOuter : (int)Part.TR_CornerOuter];
                                }
                                gfx.DrawImage(part, xSize / 2, ySize / 4);

                                // bottomleft part
                                if (bottom && left) {
                                    if ((index & BottomLeft) == BottomLeft) {
                                        part = parts[micro ? (int)PartMicro.Full : (int)Part.BL_Full];
                                    }
                                    else {
                                        part = parts[micro ? (int)PartMicro.BL_CornerInner : (int)Part.BL_CornerInner];
                                    }
                                }
                                else if (bottom) {
                                    part = parts[micro ? (int)PartMicro.TL_BL_EdgeVertical : (int)Part.BL_EdgeVertical];
                                }
                                else if (left) {
                                    part = parts[micro ? (int)PartMicro.BL_BR_EdgeHorizontal : (int)Part.BL_EdgeHorizontal];
                                }
                                else {
                                    part = parts[micro ? (int)PartMicro.BL_CornerOuter : (int)Part.BL_CornerOuter];
                                }
                                gfx.DrawImage(part, 0, ySize / 4);

                                // bottomright part
                                if (bottom && right) {
                                    if ((index & BottomRight) == BottomRight) {
                                        part = parts[micro ? (int)PartMicro.Full : (int)Part.BR_Full];
                                    }
                                    else {
                                        part = parts[micro ? (int)PartMicro.BR_CornerInner : (int)Part.BR_CornerInner];
                                    }
                                }
                                else if (bottom) {
                                    part = parts[micro ? (int)PartMicro.TR_BR_EdgeVertical : (int)Part.BR_EdgeVertical];
                                }
                                else if (right) {
                                    part = parts[micro ? (int)PartMicro.BL_BR_EdgeHorizontal : (int)Part.BR_EdgeHorizontal];
                                }
                                else {
                                    part = parts[micro ? (int)PartMicro.BR_CornerOuter : (int)Part.BR_CornerOuter];
                                }
                                gfx.DrawImage(part, xSize / 4, ySize / 2);
                            }
                            else {
                                // topleft part
                                if (top && left) {
                                    if ((index & TopLeft) == TopLeft) {
                                        part = parts[micro ? (int)PartMicro.Full : (int)Part.TL_Full];
                                    }
                                    else {
                                        part = parts[micro ? (int)PartMicro.TL_CornerInner : (int)Part.TL_CornerInner];
                                    }
                                }
                                else if (top) {
                                    part = parts[micro ? (int)PartMicro.TL_BL_EdgeVertical : (int)Part.TL_EdgeVertical];
                                }
                                else if (left) {
                                    part = parts[micro ? (int)PartMicro.TL_TR_EdgeHorizontal : (int)Part.TL_EdgeHorizontal];
                                }
                                else {
                                    part = parts[micro ? (int)PartMicro.TL_CornerOuter : (int)Part.TL_CornerOuter];
                                }
                                gfx.DrawImage(part, 0, 0);

                                // topright part
                                if (top && right) {
                                    if ((index & TopRight) == TopRight) {
                                        part = parts[micro ? (int)PartMicro.Full : (int)Part.TR_Full];
                                    }
                                    else {
                                        part = parts[micro ? (int)PartMicro.TR_CornerInner : (int)Part.TR_CornerInner];
                                    }
                                }
                                else if (top) {
                                    part = parts[micro ? (int)PartMicro.TR_BR_EdgeVertical : (int)Part.TR_EdgeVertical];
                                }
                                else if (right) {
                                    part = parts[micro ? (int)PartMicro.TL_TR_EdgeHorizontal : (int)Part.TR_EdgeHorizontal];
                                }
                                else {
                                    part = parts[micro ? (int)PartMicro.TR_CornerOuter : (int)Part.TR_CornerOuter];
                                }
                                gfx.DrawImage(part, size / 2, 0);

                                // bottomleft part
                                if (bottom && left) {
                                    if ((index & BottomLeft) == BottomLeft) {
                                        part = parts[micro ? (int)PartMicro.Full : (int)Part.BL_Full];
                                    }
                                    else {
                                        part = parts[micro ? (int)PartMicro.BL_CornerInner : (int)Part.BL_CornerInner];
                                    }
                                }
                                else if (bottom) {
                                    part = parts[micro ? (int)PartMicro.TL_BL_EdgeVertical : (int)Part.BL_EdgeVertical];
                                }
                                else if (left) {
                                    part = parts[micro ? (int)PartMicro.BL_BR_EdgeHorizontal : (int)Part.BL_EdgeHorizontal];
                                }
                                else {
                                    part = parts[micro ? (int)PartMicro.BL_CornerOuter : (int)Part.BL_CornerOuter];
                                }
                                gfx.DrawImage(part, 0, size / 2);

                                // bottomright part
                                if (bottom && right) {
                                    if ((index & BottomRight) == BottomRight) {
                                        part = parts[micro ? (int)PartMicro.Full : (int)Part.BR_Full];
                                    }
                                    else {
                                        part = parts[micro ? (int)PartMicro.BR_CornerInner : (int)Part.BR_CornerInner];
                                    }
                                }
                                else if (bottom) {
                                    part = parts[micro ? (int)PartMicro.TR_BR_EdgeVertical : (int)Part.BR_EdgeVertical];
                                }
                                else if (right) {
                                    part = parts[micro ? (int)PartMicro.BL_BR_EdgeHorizontal : (int)Part.BR_EdgeHorizontal];
                                }
                                else {
                                    part = parts[micro ? (int)PartMicro.BR_CornerOuter : (int)Part.BR_CornerOuter];
                                }
                                gfx.DrawImage(part, size / 2, size / 2);
                            }
                        }

                        blobGfx.DrawImage(img, new Point(x, y));

                        if (indexed) {
                            var text = index.ToString(CultureInfo.InvariantCulture);
                            var textSize = blobGfx.MeasureString(text, font);
                            var width = size;
                            var height = isometric ? size / 2 : size;

                            blobGfx.DrawString(text, font, Brushes.Black,
                                new PointF(x + (float)width / 2 - textSize.Width / 2, y + (float)height / 2 - textSize.Height / 2));
                        }
                   }

                    json.Tiles[tileIndex] = new BlobTile() {
                        Source = new BlobRect(x, y, size, isometric ? size / 2 : size),
                        Indices = new List<int>() { index }.Concat(from dup in duplicates where dup.Value == index select dup.Key).ToArray()
                    };
                    tileIndex++;

                    x += size;
                    if (x >= blob.Width) {
                        x = 0;
                        y += isometric ? size / 2 : size;
                    }
                }
            }

            blob.Save(path + ".png", ImageFormat.Png);
            json.Image = path + ".png";

            if (!xml) {
                File.WriteAllText(
                    path + ".json",
                    JsonConvert.SerializeObject(
                        json,
                        indented ? Formatting.Indented : Formatting.None,
                        new JsonSerializerSettings() {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    )
                );
            }
            else {
                using (var stream = File.Open(path + ".xml", FileMode.Create)) {
                    var serializer = new XmlSerializer(json.GetType());
                    if (!indented) {
                        using (var xmlWriter = System.Xml.XmlWriter.Create(stream, 
                            new System.Xml.XmlWriterSettings() { 
                                Indent = false, 
                                NewLineHandling = System.Xml.NewLineHandling.None 
                            })) {
                            serializer.Serialize(xmlWriter, json);
                        }
                    }
                    else {
                        serializer.Serialize(stream, json);
                    }
                }
            }

            blob.Dispose();
            foreach (var part in parts) {
                part.Dispose();
            }
        }

        static void GetSets(HashSet<int> uniques, Dictionary<int, int> duplicates) {
            for (int i = 0; i < 256; i++) {
                var top = (i & Top) == Top;
                var right = (i & Right) == Right;
                var bottom = (i & Bottom) == Bottom;
                var left = (i & Left) == Left;

                var index = i;

                if (!(top && left)) {
                    index &= ~TopLeft;
                }
                if (!(top && right)) {
                    index &= ~TopRight;
                }
                if (!(bottom && left)) {
                    index &= ~BottomLeft;
                }
                if (!(bottom && right)) {
                    index &= ~BottomRight;
                }

                if (!uniques.Add(index)) {
                    duplicates[i] = index;
                }
            }
        }

        static List<Bitmap> GetParts(string source, int size, bool isometric) {
            var baseImg = (Bitmap)Image.FromFile(source);
            var imageWidth = 3 * size;
            var imageHeight = isometric ? (5 * size) / 2 : 5 * size;

            if (baseImg.Width < imageWidth || baseImg.Height < imageHeight) {
                throw new Exception("Insufficient source file size, must be at least " + imageWidth + "x" + imageHeight + ", was " + baseImg.Width + "x" + baseImg.Height);
            }

            var macroWidth = 4 * size;
            var macroHeight = isometric ? (6 * size) / 2 : 6 * size;
            bool micro = baseImg.Width < macroWidth || baseImg.Height < macroHeight;
            List<Bitmap> ret;

            var xSize = size;
            var ySize = isometric ? size / 2 : size;

            if (micro) {
                ret = new List<Bitmap>() {
                    CreateImage(xSize, ySize, baseImg, xSize, 0),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, 0),
                    CreateImage(xSize, ySize, baseImg, xSize, ySize),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize),

                    CreateImage(xSize, ySize, baseImg, 0, ySize * 2),
                    CreateImage(xSize, ySize, baseImg, xSize, ySize * 2),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize * 2),

                    CreateImage(xSize, ySize, baseImg, 0, ySize * 3),
                    CreateImage(xSize, ySize, baseImg, xSize, ySize * 3),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize * 3),

                    CreateImage(xSize, ySize, baseImg, 0, ySize * 4),
                    CreateImage(xSize, ySize, baseImg, xSize, ySize * 4),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize * 4),
                };
            }
            else {
                ret = new List<Bitmap>() {
                    CreateImage(xSize, ySize, baseImg, xSize * 2, 0),
                    CreateImage(xSize, ySize, baseImg, xSize * 3, 0),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize),
                    CreateImage(xSize, ySize, baseImg, xSize * 3, ySize),

                    CreateImage(xSize, ySize, baseImg, 0, ySize * 2),
                    CreateImage(xSize, ySize, baseImg, xSize, ySize * 2),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize * 2),
                    CreateImage(xSize, ySize, baseImg, xSize * 3, ySize * 2),

                    CreateImage(xSize, ySize, baseImg, 0, ySize * 3),
                    CreateImage(xSize, ySize, baseImg, xSize, ySize * 3),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize * 3),
                    CreateImage(xSize, ySize, baseImg, xSize * 3, ySize * 3),

                    CreateImage(xSize, ySize, baseImg, 0, ySize * 4),
                    CreateImage(xSize, ySize, baseImg, xSize, ySize * 4),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize * 4),
                    CreateImage(xSize, ySize, baseImg, xSize * 3, ySize * 4),

                    CreateImage(xSize, ySize, baseImg, 0, ySize * 5),
                    CreateImage(xSize, ySize, baseImg, xSize, ySize * 5),
                    CreateImage(xSize, ySize, baseImg, xSize * 2, ySize * 5),
                    CreateImage(xSize, ySize, baseImg, xSize * 3, ySize * 5),
                };
            }

            baseImg.Dispose();
            return ret;
        }

        static List<Bitmap> GetParts(int size) {
            var images = new List<Bitmap>() {
                CreateImage(size, Brushes.Yellow, (gfx) => { DrawInnerCorner(gfx, 1.0f, 1.0f, size, Brushes.White); }),
                CreateImage(size, Brushes.Yellow, (gfx) => { DrawInnerCorner(gfx, 0.0f, 1.0f, size, Brushes.White); }),
                CreateImage(size, Brushes.Yellow, (gfx) => { DrawInnerCorner(gfx, 1.0f, 0.0f, size, Brushes.White); }),
                CreateImage(size, Brushes.Yellow, (gfx) => { DrawInnerCorner(gfx, 0.0f, 0.0f, size, Brushes.White); }),

                CreateImage(size, Brushes.White, (gfx) => { DrawInnerCorner(gfx, 1.0f, 1.0f, size, Brushes.Yellow); }),
                CreateImage(size, Brushes.White, (gfx) => { DrawEdge(gfx, Edge.Bottom, size, Brushes.Yellow); }),
                CreateImage(size, Brushes.White, (gfx) => { DrawInnerCorner(gfx, 0.0f, 1.0f, size, Brushes.Yellow); }),

                CreateImage(size, Brushes.White, (gfx) => { DrawEdge(gfx, Edge.Right, size, Brushes.Yellow); }),
                CreateImage(size, Brushes.Yellow, (gfx) => { }),
                CreateImage(size, Brushes.White, (gfx) => { DrawEdge(gfx, Edge.Left, size, Brushes.Yellow); }),

                CreateImage(size, Brushes.White, (gfx) => { DrawInnerCorner(gfx, 1.0f, 0.0f, size, Brushes.Yellow); }),
                CreateImage(size, Brushes.White, (gfx) => { DrawEdge(gfx, Edge.Top, size, Brushes.Yellow); }),
                CreateImage(size, Brushes.White, (gfx) => { DrawInnerCorner(gfx, 0.0f, 0.0f, size, Brushes.Yellow); }),
            };
            return images;
        }

        static Bitmap CreateImage(int size, Bitmap source, int x, int y) {
            return CreateImage(size, size, source, x, y);
        }

        static Bitmap CreateImage(int width, int height, Bitmap source, int x, int y) {
            Bitmap img;
            Graphics gfx;

            img = new Bitmap(width, height);
            using (gfx = Graphics.FromImage(img)) {
                gfx.DrawImage(source,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(x, y, width, height),
                    GraphicsUnit.Pixel
                );
            }

            return img;
        }

        static Bitmap CreateImage(int size, Brush fillColor, Action<Graphics> f) {
            Bitmap img;
            Graphics gfx;

            img = new Bitmap(size, size);
            using (gfx = Graphics.FromImage(img)) {
                Fill(gfx, size, fillColor);
                f(gfx);
            }

            return img;
        }

        static void Fill(Graphics gfx, int size, Brush brush) {
            gfx.FillRectangle(brush, new Rectangle(0, 0, size, size));
        }

        static void DrawEdge(Graphics gfx, Edge edge, int size, Brush brush) {
            RectangleF fillRect;
            RectangleF lineRect;

            switch (edge) {
                default:
                case Edge.Left:
                    fillRect = new RectangleF(0.0f, 0.0f, 0.5f, 1.0f);
                    lineRect = new RectangleF(0.5f, 0.0f, 0.0f, 1.0f);
                    break;
                case Edge.Top:
                    fillRect = new RectangleF(0.0f, 0.0f, 1.0f, 0.5f);
                    lineRect = new RectangleF(0.0f, 0.5f, 1.0f, 0.0f);
                    break;
                case Edge.Right:
                    fillRect = new RectangleF(1.0f - 0.5f, 0.0f, 0.5f, 1.0f);
                    lineRect = new RectangleF(0.5f, 0.0f, 0.0f, 1.0f);
                    break;
                case Edge.Bottom:
                    fillRect = new RectangleF(0.0f, 1.0f - 0.5f, 1.0f, 0.5f);
                    lineRect = new RectangleF(0.0f, 0.5f, 1.0f, 0.0f);
                    break;
            }

            gfx.FillRectangle(brush, fillRect.Left * size, fillRect.Top * size, fillRect.Width * size, fillRect.Height * size);
            gfx.DrawLine(Pens.Gray, lineRect.Left * size, lineRect.Top * size, lineRect.Right * size, lineRect.Bottom * size);
        }

        static void DrawInnerCorner(Graphics gfx, float u, float v, int size, Brush brush) {
            gfx.FillEllipse(brush, u * size - (float)size / 2, v * size - (float)size / 2, (float)size, (float)size);
            gfx.DrawEllipse(Pens.Gray, u * size - (float)size / 2, v * size - (float)size / 2, (float)size, (float)size);
        }
    }
}
