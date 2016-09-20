using Blobator.Tileset;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobatorTest {
    class Program {
        private readonly static Dictionary<bool[], Rectangle> testBools = new Dictionary<bool[],Rectangle> {
            { new bool[8] { true, false, true, false, true, false, true, false }, new Rectangle(0, 0, 32, 32) },
            { new bool[8] { false, false, false, true, false, false, false, true }, new Rectangle(64, 64, 32, 32) },
            { new bool[8] { true, false, false, true, true, false, true, true }, new Rectangle(64, 64, 32, 32) },
            { new bool[8] { false, false, false, false, false, true, false, true }, new Rectangle(0, 96, 32, 32) },
            { new bool[8] { true, false, true, false, false, true, false, true }, new Rectangle(0, 96, 32, 32) },
            { new bool[8] { false, false, false, false, true, true, false, true }, new Rectangle(0, 96, 32, 32) },
            { new bool[8] { true, true, true, true, false, false, false, true }, new Rectangle(192, 64, 32, 32) },
            { new bool[8] { true, true, true, true, true, true, true, true }, new Rectangle(128, 192, 32, 32) },
        };

        private readonly static Dictionary<int, Rectangle> testValues = new Dictionary<int, Rectangle>() {
            { 0, new Rectangle(0, 0, 32, 32) },
            { 17, new Rectangle(0, 0, 32, 32) },
            { 19, new Rectangle(32, 0, 32, 32) },
            { 77, new Rectangle(64, 0, 32, 32) },
            { 10, new Rectangle(96, 0, 32, 32) },
            { 30, new Rectangle(128, 0, 32, 32) },
            { 52, new Rectangle(160, 0, 32, 32) },
            { 102, new Rectangle(192, 0, 32, 32) },
            { 108, new Rectangle(0, 32, 32, 32) },
            { 42, new Rectangle(32, 32, 32, 32) },
            { 47, new Rectangle(64, 32, 32, 32) },
            { 120, new Rectangle(96, 32, 32, 32) },
            { 123, new Rectangle(128, 32, 32, 32) },
            { 63, new Rectangle(160, 32, 32, 32) },
            { 129, new Rectangle(192, 32, 32, 32) },
            { 134, new Rectangle(0, 64, 32, 32) },
            { 147, new Rectangle(32, 64, 32, 32) },
            { 157, new Rectangle(64, 64, 32, 32) },
            { 202, new Rectangle(96, 64, 32, 32) },
            { 219, new Rectangle(128, 64, 32, 32) },
            { 142, new Rectangle(160, 64, 32, 32) },
            { 159, new Rectangle(192, 64, 32, 32) },
            { 177, new Rectangle(0, 96, 32, 32) },
            { 182, new Rectangle(32, 96, 32, 32) },
            { 163, new Rectangle(64, 96, 32, 32) },
            { 169, new Rectangle(96, 96, 32, 32) },
            { 170, new Rectangle(128, 96, 32, 32) },
            { 171, new Rectangle(160, 96, 32, 32) },
            { 174, new Rectangle(192, 96, 32, 32) },
            { 175, new Rectangle(0, 128, 32, 32) },
            { 185, new Rectangle(32, 128, 32, 32) },
            { 186, new Rectangle(64, 128, 32, 32) },
            { 187, new Rectangle(96, 128, 32, 32) },
            { 190, new Rectangle(128, 128, 32, 32) },
            { 191, new Rectangle(160, 128, 32, 32) },
            { 244, new Rectangle(192, 128, 32, 32) },
            { 246, new Rectangle(0, 160, 32, 32) },
            { 227, new Rectangle(32, 160, 32, 32) },
            { 233, new Rectangle(64, 160, 32, 32) },
            { 234, new Rectangle(96, 160, 32, 32) },
            { 235, new Rectangle(128, 160, 32, 32) },
            { 238, new Rectangle(160, 160, 32, 32) },
            { 239, new Rectangle(192, 160, 32, 32) },
            { 252, new Rectangle(0, 192, 32, 32) },
            { 250, new Rectangle(32, 192, 32, 32) },
            { 251, new Rectangle(64, 192, 32, 32) },
            { 254, new Rectangle(96, 192, 32, 32) },
            { 255, new Rectangle(128, 192, 32, 32) },
        };

        static void Main(string[] args) {
            bool failed = false;
            Console.WriteLine("Running blobator.exe tests");

            Console.WriteLine("Generating blobtest.png and blobtest.json at size 32");
            Blobator.Generator.Generate(32, "blobtest");

            Console.WriteLine("Loading blobtest.json as TestTileset");
            TestTileset tileset;
            try {
                tileset = new TestTileset("blobtest.json");
            }
            catch (Exception e) {
                Console.WriteLine("Failed: " + e);
                tileset = null;
                failed = true;
            }

            if (tileset != null) {
                TestTileset(tileset, ref failed);
            }

            Console.WriteLine("Generating blobtest.png and blobtest.xml at size 32");
            Blobator.Generator.Generate(32, "blobtest", xml: true);

            Console.WriteLine("Loading blobtest.xml as TestTileset");
            var xmlData = BlobTileset.FromXmlFile("blobtest.xml");
            try {
                tileset = new TestTileset(xmlData);
            }
            catch (Exception e) {
                Console.WriteLine("Failed: " + e);
                tileset = null;
                failed = true;
            }

            if (tileset != null) {
                TestTileset(tileset, ref failed);
            }

            if (failed) {
                Console.WriteLine("!! Some tests failed !!");
            }
            else {
                Console.WriteLine("All test passed");
            }

            Console.WriteLine("Hit any key to continue");
            Console.ReadKey();
        }

        static void TestTileset(TestTileset tileset, ref bool failed) {
            Console.WriteLine("Testing image width/height");
            if (tileset.Image.Width != 224 || tileset.Image.Height != 224) {
                Console.WriteLine("Failed: image size");
                Console.WriteLine("  Returned: " + tileset.Image.Width + "x" + tileset.Image.Height);
                Console.WriteLine("  Expected: 224x224");
                failed = true;
            }

            Console.WriteLine("Testing bit values");
            for (int i = 0; i < 8; i++) {
                var expected = 1 << i;
                var value = tileset.Bits[i];

                if (value != expected) {
                    Console.WriteLine("Failed: bit at index " + i);
                    Console.WriteLine("  Returned: " + value);
                    Console.WriteLine("  Expected: " + expected);
                    failed = true;
                }
            }

            Console.WriteLine("Testing index values");
            foreach (var kvp in testValues) {
                if (tileset[kvp.Key] != kvp.Value) {
                    Console.WriteLine("Failed: index " + kvp.Key);
                    Console.WriteLine("  Returned: " + tileset[kvp.Key]);
                    Console.WriteLine("  Expected: " + kvp.Value);
                    failed = true;
                }
            }

            Console.WriteLine("Testing GetTile with boolean values");
            foreach (var kvp in testBools) {
                var b = kvp.Key;
                var ret = tileset.GetTile(b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7]);
                if (ret != kvp.Value) {
                    Console.WriteLine("Failed:");
                    Console.WriteLine("  Args: " + string.Join(", ", b));
                    Console.WriteLine("  Returned: " + ret);
                    Console.WriteLine("  Expected: " + kvp.Value);
                    failed = true;
                }
            }
        }
    }
}
