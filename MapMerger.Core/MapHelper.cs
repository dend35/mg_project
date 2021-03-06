﻿using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
        public static void RenderMap(string path = "kamiriona_v2.map", bool isNew = true, string fileName = "map", MapType type = MapType.Normal)
        {
            byte[,][,] map;
            map = isNew ? ConvertMap(path) : Load();
            InitColors(type);
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

            var date = DateTime.Now;
            var dtStr = $"{date.Day}-{date.Month}-{date.Year}_{date.Hour}-{date.Minute}";
            for (var i = 0; i < imgArr.Length; i++)
            {
                var img = imgArr[i];
                img.Save(fileName + i + dtStr + ".png");
                img.Dispose();
            }
        }

        #region Colors

        private static Color[] Colors = new Color[255];

        private static void InitColors(MapType type)
        {
            for (int i = 0; i < 255; i++)
            {
                Colors[i] = Color.Black;
                
            }
            Colors[0] = Color.FromRgba(0, 0, 0,255);
            Colors[1] = Color.FromRgba(0, 0, 0,255);
            Colors[32] = Color.FromRgba(0, 0, 0,255);
            Colors[33] = Color.FromRgba(15, 11, 3, 255);
            Colors[34] = Color.FromRgba(29, 25, 18, 255);
            Colors[35] = Color.FromRgba(68, 68, 68, 255);
            Colors[36] = Color.FromRgba(85, 68, 34, 255);
            Colors[37] = Color.FromRgba(68, 0, 0, 255);
            Colors[38] = Color.FromRgba(51, 68, 0, 255);
            Colors[40] = Color.FromRgba(byte.MaxValue, 97, 107, 255);
            Colors[41] = Color.FromRgba(byte.MaxValue, 107, 97, 255);
            Colors[42] = Color.FromRgba(byte.MaxValue, 107, 107, 255);
            Colors[43] = Color.FromRgba(byte.MaxValue, 187, 251, 255);
            Colors[44] = Color.FromRgba(191, 241, 251, 255);
            Colors[45] = Color.FromRgba(207, 203, 241, 255);
            Colors[48] = Color.FromRgba(byte.MaxValue, byte.MaxValue, byte.MaxValue, 255);
            Colors[49] = Color.FromRgba(101, 150, 126, 255);
            Colors[50] = Color.FromRgba(101, byte.MaxValue, byte.MaxValue, 255);
            Colors[51] = Color.FromRgba(byte.MaxValue, 51, 51, 255);
            Colors[52] = Color.FromRgba(byte.MaxValue, 101, byte.MaxValue, 255);
            Colors[53] = Color.FromRgba(34, 101, byte.MaxValue, 255);
            Colors[54] = Color.FromRgba(238, 254, byte.MaxValue, 255);
            Colors[55] = Color.FromRgba(238, 254, byte.MaxValue, 255);
            Colors[56] = Color.FromRgba(225, 254, byte.MaxValue, 255);
            Colors[57] = Color.FromRgba(226, 254, byte.MaxValue, 255);
            Colors[58] = Color.FromRgba(227, 254, byte.MaxValue, 255);
            Colors[59] = Color.FromRgba(228, 254, byte.MaxValue, 255);
            Colors[60] = Color.FromRgba(204, 204, 204, 255);
            Colors[61] = Color.FromRgba(221, 221, 221, 255);
            Colors[62] = Color.FromRgba(byte.MaxValue, 204, 204, 255);
            Colors[63] = Color.FromRgba(byte.MaxValue, 221, 221, 255);
            Colors[64] = Color.FromRgba(170, 170, 170, 255);
            Colors[65] = Color.FromRgba(187, 187, 187, 255);
            Colors[66] = Color.FromRgba(184, 153, 51, 255);
            Colors[67] = Color.FromRgba(184, 136, 187, 255);
            Colors[68] = Color.FromRgba(119, 68, 68, 255);
            Colors[69] = Color.FromRgba(136, 68, 153, 255);
            Colors[70] = Color.FromRgba(221, byte.MaxValue, 221, 255);
            Colors[71] = Color.FromRgba(71, 215, 100, 255);
            Colors[72] = Color.FromRgba(101, 134, 247, 255);
            Colors[73] = Color.FromRgba(247, 82, 67, 255);
            Colors[74] = Color.FromRgba(132, 238, 247, 255);
            Colors[75] = Color.FromRgba(byte.MaxValue, 135, 231, 255);
            Colors[82] = Color.FromRgba(17, 102, 102, 255);
            Colors[86] = Color.FromRgba(184, byte.MaxValue, 17, 255);
            Colors[90] = Color.FromRgba(238, 238, 238, 255);
            Colors[91] = Color.FromRgba(byte.MaxValue, 90, 0, 255);
            Colors[92] = Color.FromRgba(193, 187, 187, 255);
            Colors[93] = Color.FromRgba(187, 193, 187, 255);
            Colors[94] = Color.FromRgba(187, 187, 193, 255);
            Colors[95] = Color.FromRgba(184, byte.MaxValue, 34, 255);
            Colors[96] = Color.FromRgba(184, byte.MaxValue, 68, 255);
            Colors[97] = Color.FromRgba(112, 160, 183, 255);
            Colors[98] = Color.FromRgba(112, 187, 207, 255);
            Colors[99] = Color.FromRgba(219, 209, 125, 255);
            Colors[100] = Color.FromRgba(181, 168, 57, 255);
            Colors[101] = Color.FromRgba(76, 191, 0, 255);
            Colors[102] = Color.FromRgba(208, 206, 0, 255);
            Colors[103] = Color.FromRgba(133, 81, 166, 255);
            Colors[104] = Color.FromRgba(153, 153, 136, 255);
            Colors[105] = Color.FromRgba(198, 0, 0, 255);
            Colors[106] = Color.FromRgba(136, 136, 136, 255);
            Colors[107] = Color.FromRgba(8, 215, 100, 255);
            Colors[108] = Color.FromRgba(byte.MaxValue, 0, 0, 255);
            Colors[109] = Color.FromRgba(0, 0, byte.MaxValue, 255);
            Colors[110] = Color.FromRgba(byte.MaxValue, 0, byte.MaxValue, 255);
            Colors[111] = Color.FromRgba(238, 238, byte.MaxValue, 255);
            Colors[112] = Color.FromRgba(0, byte.MaxValue, byte.MaxValue, 255);
            Colors[113] = Color.FromRgba(211, 159, 166, 255);
            Colors[114] = Color.FromRgba(119, 119, 119, 255);
            Colors[115] = Color.FromRgba(56, 118, 65, 255);
            Colors[116] = Color.FromRgba(17, 17, byte.MaxValue,255);
            Colors[117] = Color.FromRgba(170, 119, 119, 255);
            Colors[118] = Color.FromRgba(100, 98, 21, 255);
            Colors[119] = Color.FromRgba(170, byte.MaxValue, byte.MaxValue, 255);
            Colors[120] = Color.FromRgba(227, 191, 120, 255);
            Colors[121] = Color.FromRgba(163, 136, 72, 255);
            Colors[122] = Color.FromRgba(51, 153, 120, 255);
            if (type == MapType.Alive)
            {
                for (var i = 0; i < Colors.Length; i++)
                {
                    Colors[i] = Colors[i].WithAlpha(0.2f);
                }

                Colors[50] = Color.FromRgba(101, (int)byte.MaxValue, (int)byte.MaxValue, 255);
                Colors[51] = Color.FromRgba((int)byte.MaxValue, 51, 51, 255);
                Colors[52] = Color.FromRgba((int)byte.MaxValue, 101, (int)byte.MaxValue, 255);
                Colors[53] = Color.FromRgba((int)byte.MaxValue, 138, (int)byte.MaxValue, 255);
                Colors[54] = Color.FromRgba(238, 254, (int)byte.MaxValue, 255);
                Colors[55] = Color.FromRgba(238, 254, (int)byte.MaxValue, 255);
                Colors[116] = Color.FromRgba(161, 162, (int)byte.MaxValue, 255);
                Colors[119] = Color.FromRgba(170, (int)byte.MaxValue, (int)byte.MaxValue, 255);
            }
                    
                    
        }



        #endregion
        #endregion
    }
}

