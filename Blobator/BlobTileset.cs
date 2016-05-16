using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blobator.Tileset {
    public class TilesetJsonException : Exception {
        public TilesetJsonException()
            : base() {
        }
        public TilesetJsonException(string message)
            : base(message) {
        }
        public TilesetJsonException(string message, Exception innerException)
            : base(message, innerException) {
        }
    }

    public abstract class BlobTileset<TImage, TRegion> {
        #region Static
        /// <summary>
        /// Default json deserializer. Requires a reference to System.Web.Extensions.
        /// </summary>
        public static Dictionary<string, dynamic> FromString(string json) {
            var deserializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            return deserializer.Deserialize<Dictionary<string, dynamic>>(json);
        }

        /// <summary>
        /// Default json deserializer. Requires a reference to System.Web.Extensions.
        /// </summary>
        public static Dictionary<string, dynamic> FromFile(string path) {
            return FromString(File.ReadAllText(path));
        }

        /// <summary>
        /// In order of top left, top, top right, right, bottom right, bottom, bottom left, left
        /// </summary>
        public static List<int> DefaultBits = new List<int>() {
            1, 2, 4, 8, 16, 32, 64, 128
        };
        #endregion

        private List<int> bits;
        private Dictionary<int, TRegion> tiles;
        private TImage image;

        public BlobTileset() {
            bits = new List<int>(DefaultBits);
            tiles = new Dictionary<int, TRegion>();
        }

        public BlobTileset(string path)
            : this(FromFile(path)) {
        }

        public BlobTileset(Dictionary<string, dynamic> json) 
            : this() {
            ImageFromJson(json);
            BitsFromJson(json);
            TilesFromJson(json);
        }

        #region Json
        protected virtual void ImageFromJson(Dictionary<string, dynamic> json) {
            dynamic imageJson;
            if (!json.TryGetValue("image", out imageJson)) {
                throw new TilesetJsonException(
                    string.Format("Error deserializing {0} tiles: {1}",
                        GetType().Name, "image path not found.")
                );
            }

            var imagePath = imageJson as string;
            if (imagePath == null) {
                throw new TilesetJsonException(
                    string.Format("Error deserializing {0} tiles: {1}",
                        GetType().Name, "image path was not a string value.")
                );
            }

            image = LoadImage(imagePath);
        }

        protected virtual void BitsFromJson(Dictionary<string, dynamic> json) {
            dynamic bitsJson;
            if (json.TryGetValue("bits", out bitsJson)) {
                try {
                    bits[0] = bitsJson["topLeft"];
                    bits[1] = bitsJson["top"];
                    bits[2] = bitsJson["topRight"];
                    bits[3] = bitsJson["right"];
                    bits[4] = bitsJson["bottomRight"];
                    bits[5] = bitsJson["bottom"];
                    bits[6] = bitsJson["bottomLeft"];
                    bits[7] = bitsJson["left"];
                }
                catch (Exception e) {
                    throw new TilesetJsonException(
                        string.Format("Error deserializing {0} bits: {1}",
                            GetType().Name, e.Message),
                        e
                    );
                }
            }
        }

        protected virtual void TilesFromJson(Dictionary<string, dynamic> json) {
            dynamic tilesJson;
            if (!json.TryGetValue("tiles", out tilesJson)) {
                throw new TilesetJsonException(
                    string.Format("Error deserializing {0} tiles: {1}",
                        GetType().Name, "tiles section not found.")
                );
            }

            try {
                foreach (var tileJson in tilesJson) {
                    var source = tileJson["source"];
                    var region = (TRegion)LoadRegion(image, source["x"], source["y"], source["width"], source["height"]);

                    foreach (var index in tileJson["indices"]) {
                        tiles[(int)index] = region;
                    }
                }
            }
            catch (Exception e) {
                throw new TilesetJsonException(
                    string.Format("Error deserializing {0} bits: {1}",
                        GetType().Name, e.Message),
                    e
                );
            }
        }
        #endregion

        #region Public Methods
        public int IndexFromTiles(bool topLeft, bool top, bool topRight, bool right, bool bottomRight, bool bottom, bool bottomLeft, bool left) {
            var index = 0;
            if (topLeft) {
                index |= bits[0];
            }
            if (top) {
                index |= bits[1];
            }
            if (topRight) {
                index |= bits[2];
            }
            if (right) {
                index |= bits[3];
            }
            if (bottomRight) {
                index |= bits[4];
            }
            if (bottom) {
                index |= bits[5];
            }
            if (bottomLeft) {
                index |= bits[6];
            }
            if (left) {
                index |= bits[7];
            }
            return index;
        }

        public TRegion GetTile(bool topLeft, bool top, bool topRight, bool right, bool bottomRight, bool bottom, bool bottomLeft, bool left) {
            TRegion region;
            GetTile(IndexFromTiles(topLeft, top, topRight, right, bottomRight, bottom, bottomLeft, left), out region);
            return region;
        }

        public bool GetTile(bool topLeft, bool top, bool topRight, bool right, bool bottomRight, bool bottom, bool bottomLeft, bool left, out TRegion region) {
            return GetTile(IndexFromTiles(topLeft, top, topRight, right, bottomRight, bottom, bottomLeft, left), out region);
        }

        public TRegion GetTile(int index) {
            TRegion region;
            GetTile(index, out region);
            return region;
        }

        public virtual bool GetTile(int index, out TRegion region) {
            if (index < 0 || index > 255) {
                region = default(TRegion);
                return false;
            }

            if (tiles.TryGetValue(index, out region)) {
                return true;
            }

            region = default(TRegion);
            return false;
        }
        #endregion

        #region Abstract Methods
        protected abstract TImage LoadImage(string path);
        protected abstract TRegion LoadRegion(TImage image, int x, int y, int width, int height);
        #endregion

        #region Properties
        public TImage Image { get { return image; } set { image = value; } }
        public TRegion this[int index] {
            get {
                return GetTile(index);
            }
            set {
                tiles[index] = value;
            }
        }
        public List<int> Bits { get { return bits; } set { bits = value; } }
        #endregion
    }
}
