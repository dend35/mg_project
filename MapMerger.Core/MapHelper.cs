using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MapMerger.Core
{
    public class MapHelper
    {
        public static int Width = 29952;
        public static int Height = 26624;
        static readonly int BlocksX = Width / 32;
        static readonly int BlocksY = Height / 32;
        private static readonly int TotalBlocks = BlocksX * BlocksY;

        public static byte[][] ReadMap(string path)
        {
            var fs = File.OpenRead(path);
            fs.Seek(0L, SeekOrigin.Begin);
            BinaryReader binaryReader = new BinaryReader(fs);
            byte[] buffer1 = new byte[8];
            int[] numArray = new int[2];
            fs.Read(buffer1, 0, buffer1.Length);
            Buffer.BlockCopy(buffer1, 0, numArray, 0, 8);

            Width = numArray[0];
            Height = numArray[1];

            int[] indexes = new int[TotalBlocks];
            int[] backIndex = new int[TotalBlocks];
            int indexByteSize = TotalBlocks * 4;

            fs.Seek(8L, SeekOrigin.Begin);
            byte[] buffer2 = new byte[indexByteSize];
            fs.Read(buffer2, 0, indexByteSize);
            Buffer.BlockCopy(buffer2, 0, indexes, 0, indexByteSize);

            int indexSize = 0;

            for (int index = 0; index < TotalBlocks; ++index)
            {
                if (indexes[index] >= 0)
                    backIndex[indexes[index]] = index;
                if (indexes[index] + 1 > indexSize)
                    indexSize = indexes[index] + 1;
            }

            var map = new byte[TotalBlocks][];

            for (var i = 0; i < map.Length; i++)
            {
                fs.Seek(8 + indexByteSize + Constants.ChunkSize * indexes[i], SeekOrigin.Begin);
                var chunkBufer = new byte[Constants.ChunkSize];
                fs.Read(chunkBufer, 0, Constants.ChunkSize);
                if (chunkBufer.All(x => x == 255 || x == 0))
                    continue;
                map[i] = chunkBufer;
            }

            binaryReader.Close();
            return map;
        }
        public static byte[,][,] ConvertMap(string path)
        {
            var map = ReadMap(path);

            byte[,][,] mapConverted = new byte[BlocksX, BlocksY][,];
            for (var index0 = 0; index0 < mapConverted.GetLength(0); index0++)
            for (var index1 = 0; index1 < mapConverted.GetLength(1); index1++)
            {
                mapConverted[index0, index1] = new byte[32, 32];
            }

            for (int j = 0; j < BlocksY; j++)
            {
                for (var i = 0; i < BlocksX; i++)
                {
                    for (int index1 = 0; index1 < 32; ++index1)
                    {
                        var x = index1;
                        for (int index2 = 0; index2 < 32; ++index2)
                        {
                            var y = index2;
                            mapConverted[i, j][x, y] = (byte)(map[i + (j * BlocksX)] != null
                                ? map[i + (j * BlocksX)][index2 + 32 * index1]
                                : 0);
                        }
                    }
                }
            }
            return RotateChunks(mapConverted);
        }

        #region RotateChunks
        /// <summary>
        /// Поворачивает чанки, я так и не разобрался как в клиенте все работает TODO: Костыль
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        private static byte[,][,] RotateChunks(byte[,][,] map)
        {
            byte[,][,] mapRotated = new byte[BlocksX, BlocksY][,];
            for (var index0 = 0; index0 < mapRotated.GetLength(0); index0++)
                for (var index1 = 0; index1 < mapRotated.GetLength(1); index1++) 
                    mapRotated[index0, index1] = map[index0, index1].RotateMatrixClockwise().ReflectionChunk();

            return mapRotated;
        }


        #endregion

        #region Объединение

        public static void Merge()
        {
            byte[,][,] map;
            if (Directory.Exists("map.json"))
            {
                map = Load();
            }
            else
            {
                map = ConvertMap("kamiriona_v2.map");
            }

            var newMap = ConvertMap("kamiriona_v2.map");
            for (int y = 0; y < BlocksY; y++)
            {
                for (int x = 0; x < BlocksX; x++)
                {
                    if (newMap[x, y][0, 0] != 0)
                    {
                        map[x, y] = newMap[x, y];
                    }
                }
            }
            Save(map);
        }

        private static void Save(byte[,][,] map)
        {
            for (int y = 0; y < BlocksY; y++)
            {
                for (int x = 0; x < BlocksX; x++)
                {
                    if (map[x, y] == null)//TODO: тут сделать проверку что весь чанк пустой
                    {
                        map[x, y] = null;
                    }
                }
            }
            var json = JsonConvert.SerializeObject(map);
            var stream = new StreamWriter("map.json", false);
            stream.Write(json);
            stream.Close();


        }

        private static byte[,][,] Load()
        {
            var stream = new StreamReader("map.json");
            var json = stream.ReadToEnd();
            stream.Close();
            var map = JsonConvert.DeserializeObject<byte[,][,]>(json);
            for (int y = 0; y < BlocksY; y++)
            {
                for (int x = 0; x < BlocksX; x++)
                {
                    if (map[x, y] == null) map[x, y] = new byte[32, 32];//TODO: костыль
                }
            }

            return map;
        }
        #endregion

        #region Render
        public static void RenderMap(string path = "kamiriona_v2.map", bool isNew = true, string fileName = "map")
        {
            byte[,][,] map;
            map = isNew ? ConvertMap(path) : Load();
            InitColors();
            //var img = new Image<Rgba32>[2](Configuration.Default, Width, Height,Color.Black);
            Image<Rgba32>[] imgArr =
            {
                new Image<Rgba32>(Configuration.Default, Width, Height/2,Color.Black),
                new Image<Rgba32>(Configuration.Default, Width, Height/2, Color.Black)
            };
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    bool drawLine = x % 1000 == 0 || y % 1000 == 0;
                   
                    if (map[x / 32, y / 32][x % 32, y % 32] == 0 && !drawLine)
                    {
                        continue;
                    }
                    var drawY = y;
                    var i = 0;
                    if (y >= Height / 2)
                    {
                        i = 1;
                        drawY -= Height / 2;
                    }

                    var pixel = drawLine ? Color.OrangeRed : Colors[map[x / 32, y / 32][x % 32, y % 32]];
                    imgArr[i][x, drawY] = pixel;
                }
            }

            for (var i = 0; i < imgArr.Length; i++)
            {
                var img = imgArr[i];
                img.Save(fileName + i + ".png");
                img.Dispose();
            }
        }

        #region Colors

        private static readonly Color[] Colors = new Color[255];

        private static void InitColors()
        {
            Colors[0] = Color.FromRgb(0, 0, 0);
            Colors[1] = Color.FromRgb(0, 0, 0);
            Colors[32] = Color.FromRgb(0, 0, 0);
            Colors[33] = Color.FromRgb(15, 11, 3);
            Colors[34] = Color.FromRgb(29, 25, 18);
            Colors[35] = Color.FromRgb(68, 68, 68);
            Colors[36] = Color.FromRgb(85, 68, 34);
            Colors[37] = Color.FromRgb(68, 0, 0);
            Colors[38] = Color.FromRgb(51, 68, 0);
            Colors[40] = Color.FromRgb(byte.MaxValue, 97, 107);
            Colors[41] = Color.FromRgb(byte.MaxValue, 107, 97);
            Colors[42] = Color.FromRgb(byte.MaxValue, 107, 107);
            Colors[43] = Color.FromRgb(byte.MaxValue, 187, 251);
            Colors[44] = Color.FromRgb(191, 241, 251);
            Colors[45] = Color.FromRgb(207, 203, 241);
            Colors[48] = Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            Colors[49] = Color.FromRgb(101, 150, 126);
            Colors[50] = Color.FromRgb(101, byte.MaxValue, byte.MaxValue);
            Colors[51] = Color.FromRgb(byte.MaxValue, 51, 51);
            Colors[52] = Color.FromRgb(byte.MaxValue, 101, byte.MaxValue);
            Colors[53] = Color.FromRgb(34, 101, byte.MaxValue);
            Colors[54] = Color.FromRgb(238, 254, byte.MaxValue);
            Colors[55] = Color.FromRgb(238, 254, byte.MaxValue);
            Colors[56] = Color.FromRgb(225, 254, byte.MaxValue);
            Colors[57] = Color.FromRgb(226, 254, byte.MaxValue);
            Colors[58] = Color.FromRgb(227, 254, byte.MaxValue);
            Colors[59] = Color.FromRgb(228, 254, byte.MaxValue);
            Colors[60] = Color.FromRgb(204, 204, 204);
            Colors[61] = Color.FromRgb(221, 221, 221);
            Colors[62] = Color.FromRgb(byte.MaxValue, 204, 204);
            Colors[63] = Color.FromRgb(byte.MaxValue, 221, 221);
            Colors[64] = Color.FromRgb(170, 170, 170);
            Colors[65] = Color.FromRgb(187, 187, 187);
            Colors[66] = Color.FromRgb(184, 153, 51);
            Colors[67] = Color.FromRgb(184, 136, 187);
            Colors[68] = Color.FromRgb(119, 68, 68);
            Colors[69] = Color.FromRgb(136, 68, 153);
            Colors[70] = Color.FromRgb(221, byte.MaxValue, 221);
            Colors[71] = Color.FromRgb(71, 215, 100);
            Colors[72] = Color.FromRgb(101, 134, 247);
            Colors[73] = Color.FromRgb(247, 82, 67);
            Colors[74] = Color.FromRgb(132, 238, 247);
            Colors[75] = Color.FromRgb(byte.MaxValue, 135, 231);
            Colors[82] = Color.FromRgb(17, 102, 102);
            Colors[86] = Color.FromRgb(184, byte.MaxValue, 17);
            Colors[90] = Color.FromRgb(238, 238, 238);
            Colors[91] = Color.FromRgb(byte.MaxValue, 90, 0);
            Colors[92] = Color.FromRgb(193, 187, 187);
            Colors[93] = Color.FromRgb(187, 193, 187);
            Colors[94] = Color.FromRgb(187, 187, 193);
            Colors[95] = Color.FromRgb(184, byte.MaxValue, 34);
            Colors[96] = Color.FromRgb(184, byte.MaxValue, 68);
            Colors[97] = Color.FromRgb(112, 160, 183);
            Colors[98] = Color.FromRgb(112, 187, 207);
            Colors[99] = Color.FromRgb(219, 209, 125);
            Colors[100] = Color.FromRgb(181, 168, 57);
            Colors[101] = Color.FromRgb(76, 191, 0);
            Colors[102] = Color.FromRgb(208, 206, 0);
            Colors[103] = Color.FromRgb(133, 81, 166);
            Colors[104] = Color.FromRgb(153, 153, 136);
            Colors[105] = Color.FromRgb(198, 0, 0);
            Colors[106] = Color.FromRgb(136, 136, 136);
            Colors[107] = Color.FromRgb(8, 215, 100);
            Colors[108] = Color.FromRgb(byte.MaxValue, 0, 0);
            Colors[109] = Color.FromRgb(0, 0, byte.MaxValue);
            Colors[110] = Color.FromRgb(byte.MaxValue, 0, byte.MaxValue);
            Colors[111] = Color.FromRgb(238, 238, byte.MaxValue);
            Colors[112] = Color.FromRgb(0, byte.MaxValue, byte.MaxValue);
            Colors[113] = Color.FromRgb(211, 159, 166);
            Colors[114] = Color.FromRgb(119, 119, 119);
            Colors[115] = Color.FromRgb(56, 118, 65);
            Colors[116] = Color.FromRgb(17, 17, byte.MaxValue);
            Colors[117] = Color.FromRgb(170, 119, 119);
            Colors[118] = Color.FromRgb(100, 98, 21);
            Colors[119] = Color.FromRgb(170, byte.MaxValue, byte.MaxValue);
            Colors[120] = Color.FromRgb(227, 191, 120);
            Colors[121] = Color.FromRgb(163, 136, 72);
            Colors[122] = Color.FromRgb(51, 153, 120);
        }

        #endregion
        #endregion
    }
}
