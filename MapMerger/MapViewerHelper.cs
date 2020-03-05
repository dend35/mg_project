using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MapMerger
{

    public class MapBlock
    {
        public byte[] data;
        public bool notSaved;
        public bool isLoaded;
        public bool empty;
        public int bx;
        public int by;

        public MapBlock()
        {
            this.isLoaded = false;
            this.notSaved = false;
        }

        public void Init()
        {
            this.isLoaded = true;
            this.notSaved = true;
            this.data = new byte[1024];
        }
    }
    public class MapViewer
    {
        static int width = 29952;
        static int height = 26624;

        #region Convert
        public static byte[][] ReadMap(string path)
        {
            var fs = File.OpenRead(path);
                fs.Seek(0L, SeekOrigin.Begin);
            BinaryReader binaryReader = new BinaryReader(fs);
            //width = binaryReader.ReadInt32();
            //height = binaryReader.ReadInt32();
            byte[] buffer1 = new byte[8];
            int[] numArray = new int[2];
            fs.Read(buffer1, 0, buffer1.Length);
            Buffer.BlockCopy((Array)buffer1, 0, (Array)numArray, 0, 8);
            width = numArray[0];
            height = numArray[1];
            var blocksX = width / 32;
            var blocksY = height / 32;

            byte[][] blocks = new byte[(width / 32) * (height / 32)][];
            int[] indexes = new int[blocksX* blocksY];
            int[] backIndex = new int[blocksX * blocksY];

            int _indexByteSize = blocksX * blocksY * 4;
            fs.Seek(8L, SeekOrigin.Begin);
            byte[] buffer2 = new byte[_indexByteSize];
            fs.Read(buffer2, 0, _indexByteSize);
            Buffer.BlockCopy((Array)buffer2, 0, (Array)indexes, 0, _indexByteSize);

            int _indexSize = 0;

            for (int index = 0; index < blocksX*blocksY; ++index)
            {
                if (indexes[index] >= 0)
                    backIndex[indexes[index]] = index;
                if (indexes[index] + 1 > _indexSize)
                    _indexSize = indexes[index] + 1;
            }


            MapBlock[] _Blocks = new MapBlock[blocksY * blocksX];


           

            void SetCell(int x, int y, byte cell)
            {
                if (x < 0 || x >= width || (y < 0 || y >= height))
                    return;
                int index = (x >> 5) + (y >> 5) * blocksX;
                if (_Blocks[index] == null)
                {
                    _Blocks[index] = new MapBlock();
                    indexes[index] = _indexSize;
                    backIndex[_indexSize] = index;
                    ++_indexSize;
                    //updateIndex = true;
                }
                if (_Blocks[index].isLoaded)
                   _Blocks[index].Init();
                _Blocks[index].data[x % 32 + 32 * (y % 32)] = cell;
                _Blocks[index].notSaved = true;
            }

            for (int index = 0; index < _indexSize; ++index)
            {
                //this.lastDebugIIndex = index;
                //this.lastDebugIndex = this.backIndex[index];
                _Blocks[backIndex[index]] = new MapBlock();
            }
            HashSet<int> blocksToLoad = new HashSet<int>();
            for (int i = 0; i < _Blocks.Length; i++)
            {
                blocksToLoad.Add(i);
            }
            void LoadBlockFromFile(int block)
            {
                //if (_Blocks[block].isLoaded)
                //    return;
                _Blocks[block] =new MapBlock();
                _Blocks[block].Init();
                fs.Seek((long)(8 + _indexByteSize + 1024 * indexes[block]), SeekOrigin.Begin);
                fs.Read(_Blocks[block].data, 0, 1024);
                _Blocks[block].notSaved = false;
            }
            void LoadBlocks()
            {
                foreach (int block in blocksToLoad)
                    LoadBlockFromFile(block);
                blocksToLoad.Clear();
            }
            int GetCell(int x, int y)
            {
                if (y < 0)
                    return 1;
                if (x < 0 || x >= width || y >= height)
                    return 117;
                int index = (x >> 5) + (y >> 5) * blocksX;
                if (_Blocks[index] == null)
                    return 0;
                if (_Blocks[index].isLoaded)
                    return (int)_Blocks[index].data[x % 32 + 32 * (y % 32)];
                blocksToLoad.Add(index);
                //this.needToLoad = true;
                return 0;
            }
            byte[][] map = new byte[blocksX*blocksY][];
            LoadBlocks();
            for (var i = 0; i < _Blocks.Length; i++)
            {
                var block = _Blocks[i];
                var blockData = block.data;
                if (blockData.All(x=>x == 255))
                {
                    continue;
                }
                map[i] = (byte[]) blockData.Clone();
                _Blocks[i] = null;
            }
            //if (18112 == num1 && 22208 == num2)
            //{
            //    while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
            //    {
            //        uint num3 = binaryReader.ReadUInt32();
            //        blocks[num3] = new byte[1024];
            //        for (int index1 = 0; index1 < 32; index1++)
            //        {
            //            for (int index2 = 0; index2 < 32; index2++)
            //                blocks[num3][index2 + 32 * index1] = (byte)binaryReader.ReadInt32();
            //        }
            //    }
            //}


            binaryReader.Close();
            return map;
        }

        public static byte[,][,] ConvertMap(string path)
        {
            // 566 x
            var map = ReadMap(path);

            byte[,][,] mapConverted = new byte[width / 32, height / 32][,];
            int[,][] mapChunks = new int[width / 32, height / 32][];
            for (var index0 = 0; index0 < mapConverted.GetLength(0); index0++)
                for (var index1 = 0; index1 < mapConverted.GetLength(1); index1++)
                {
                    mapConverted[index0, index1] = new byte[32, 32];
                }

            for (int j = 0; j < height / 32; j++)
            {
                for (var i = 0; i < width/32; i++)
                {
                    for (int index1 = 0; index1 < 32; ++index1)
                    {
                        var x = index1;
                        for (int index2 = 0; index2 < 32; ++index2)
                        {
                            var y = index2;
                            mapConverted[i, j][x, y] = (byte)(map[i + (j * width / 32)] != null
                                ? map[i + (j * width / 32)][index2 + 32 * index1]
                                : 0);
                        }
                    }
                }
            }
            //for (int y = 0; y < height / 32; y++)
            //{
            //    for (int x = 0; x < width / 32; x++)
            //    {
            //        if (mapConverted[x, y][0, 0] == 0)
            //            mapConverted[x, y] = null;
            //    }
            //}
           
            map = null;

            return RotateChunks(mapConverted);
        }
        public static byte[,][,] RotateChunks(byte[,][,] map)
        {
            byte[,][,] mapRotated = new byte[width / 32, height / 32][,];
            for (var index0 = 0; index0 < mapRotated.GetLength(0); index0++)
                for (var index1 = 0; index1 < mapRotated.GetLength(1); index1++)
                {
                    mapRotated[index0, index1] =
                        new MapViewer().ReflectionChunk(new MapViewer().RotateMatrixClockwise(map[index0, index1]));
                }

            return mapRotated;
        }

        public byte[,] RotateMatrixClockwise(byte[,] oldMatrix)
        {
            byte[,] newMatrix = new byte[oldMatrix.GetLength(1), oldMatrix.GetLength(0)];
            int newColumn, newRow = 0;
            for (int oldColumn = oldMatrix.GetLength(1) - 1; oldColumn >= 0; oldColumn--)
            {
                newColumn = 0;
                for (int oldRow = 0; oldRow < oldMatrix.GetLength(0); oldRow++)
                {
                    newMatrix[newRow, newColumn] = oldMatrix[oldRow, oldColumn];
                    newColumn++;
                }

                newRow++;
            }

            oldMatrix = null;
            return newMatrix;
        }

        public int[,] RotateMatrixAntiClockwise(int[,] oldMatrix)
        {
            int[,] newMatrix = new int[oldMatrix.GetLength(1), oldMatrix.GetLength(0)];
            int newColumn, newRow = 0;
            for (int oldColumn = 0; oldColumn < oldMatrix.GetLength(0) - 1; oldColumn++)
            {
                newColumn = 0;
                for (int oldRow = oldMatrix.GetLength(1) - 1; oldRow >= 0; oldRow--)
                {
                    newMatrix[newRow, newColumn] = oldMatrix[oldRow, oldColumn];
                    newColumn++;
                }

                newRow++;
            }

            return newMatrix;
        }

        public byte[,] ReflectionChunk(byte[,] chunk)
        {
            var newChunk = new byte[32, 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    newChunk[x, y] = chunk[31 - x, y];
                }
            }

            return newChunk;
        }


        #endregion


        public void MergeMaps()
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
            for (int y = 0; y < height / 32; y++)
            {
                for (int x = 0; x < width / 32; x++)
                {
                    if (newMap[x, y][0, 0] != 0)
                    {
                        map[x, y] = newMap[x, y];
                    }
                }
            }
            Save(map);
        }

        public void Save(byte[,][,] map)
        {

            //Parallel.For((long) 0, height / 32, y =>
            //{
            //        Parallel.For((long) 0, width / 32, x =>
            //        {
            for (int y = 0; y < height/32; y++)
            {
                for (int x = 0; x < width/32; x++)
                {
                    if (map[x, y][0, 0] == 0)
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

        public byte[,][,] Load()
        {
            var stream = new StreamReader("map.json");
            var json = stream.ReadToEnd();
            stream.Close();
            var map = JsonConvert.DeserializeObject<byte[,][,]>(json);
            for (int y = 0; y < height/32; y++)
            {
                for (int x = 0; x < width/32; x++)
                {
                   if(map[x,y]==null) map[x,y]= new byte[32,32];
                }
            }

            return map;
        }



        #region Render

        public void RenderMap()
        {
            var map = ConvertMap("kamiriona_v2.map");
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
                g.Clear(Color.Black);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x / 32, y / 32][x % 32, y % 32] == 0) continue;
                    var pixel = TakeColor(map[x / 32, y / 32][x % 32, y % 32]);
                    bmp.SetPixel(x, y, pixel);
                }
            }

            bmp.Save("2.png");
            bmp.Dispose();
        }

        public void RenderThisMap()
        {
            var map = Load();
            var bmp1 = new Bitmap(29952, 26624, PixelFormat.Format4bppIndexed);
            var bmp = new Bitmap[4]
            {
                new Bitmap(width / 2+1, height / 2+1),
                new Bitmap(width / 2+1, height / 2+1),
                new Bitmap(width / 2+1, height / 2+1),
                new Bitmap(width / 2+1, height / 2+1)
            };
            foreach (var bmpItem in bmp)
            {
                using (var g = Graphics.FromImage(bmpItem))
                    g.Clear(Color.Black);
            }

            var bmpw = width / 2;
            var bmph = height / 2;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x / 32, y / 32][x % 32, y % 32] == 0) continue;
                    var pixel = TakeColor(map[x / 32, y / 32][x % 32, y % 32]);
                    int drawx = x;
                    int drawy = y;
                    int i = 0;
                    if (x < bmpw && y < bmph)
                        i = 0;
                    else if (x >= bmpw && y <= bmph)
                    {
                        i = 1;
                        drawx = x - bmpw;
                    }
                    else if (x <= bmpw && y >= bmph)
                    {
                        i = 2;
                        drawy = y - bmph;
                    }
                    else if (x >= bmpw && y >= bmph)
                    {
                        i = 3;
                        drawx = x - bmpw;
                        drawy = y - bmph;
                    }
                       
                    bmp[i].SetPixel(drawx, drawy, pixel);
                }
            }

            for (var index = 0; index < bmp.Length; index++)
            {
                var img = bmp[index];
                img.Save(index + ".png");
                img.Dispose();
            }
        }

        public static Color TakeColor(byte colorId)
        {
            Color color = Color.Black;
            var colors = InitColors();
            color = colors[colorId];

            return color;
        }

        public static Color[] InitColors()
        {
            var Colors = new Color[200];
            Colors[0] = Color.FromArgb(0, 0, 0);
            Colors[1] = Color.FromArgb(0, 0, 0);
            Colors[32] = Color.FromArgb(0, 0, 0);
            Colors[33] = Color.FromArgb(15, 11, 3);
            Colors[34] = Color.FromArgb(29, 25, 18);
            Colors[35] = Color.FromArgb(68, 68, 68);
            Colors[36] = Color.FromArgb(85, 68, 34);
            Colors[37] = Color.FromArgb(68, 0, 0);
            Colors[38] = Color.FromArgb(51, 68, 0);
            Colors[40] = Color.FromArgb((int)byte.MaxValue, 97, 107);
            Colors[41] = Color.FromArgb((int)byte.MaxValue, 107, 97);
            Colors[42] = Color.FromArgb((int)byte.MaxValue, 107, 107);
            Colors[43] = Color.FromArgb((int)byte.MaxValue, 187, 251);
            Colors[44] = Color.FromArgb(191, 241, 251);
            Colors[45] = Color.FromArgb(207, 203, 241);
            Colors[48] = Color.FromArgb((int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue);
            Colors[49] = Color.FromArgb(101, 150, 126);
            Colors[50] = Color.FromArgb(101, (int)byte.MaxValue, (int)byte.MaxValue);
            Colors[51] = Color.FromArgb((int)byte.MaxValue, 51, 51);
            Colors[52] = Color.FromArgb((int)byte.MaxValue, 101, (int)byte.MaxValue);
            Colors[53] = Color.FromArgb(34, 101, (int)byte.MaxValue);
            Colors[54] = Color.FromArgb(238, 254, (int)byte.MaxValue);
            Colors[55] = Color.FromArgb(238, 254, (int)byte.MaxValue);
            Colors[56] = Color.FromArgb(225, 254, (int)byte.MaxValue);
            Colors[57] = Color.FromArgb(226, 254, (int)byte.MaxValue);
            Colors[58] = Color.FromArgb(227, 254, (int)byte.MaxValue);
            Colors[59] = Color.FromArgb(228, 254, (int)byte.MaxValue);
            Colors[60] = Color.FromArgb(204, 204, 204);
            Colors[61] = Color.FromArgb(221, 221, 221);
            Colors[62] = Color.FromArgb((int)byte.MaxValue, 204, 204);
            Colors[63] = Color.FromArgb((int)byte.MaxValue, 221, 221);
            Colors[64] = Color.FromArgb(170, 170, 170);
            Colors[65] = Color.FromArgb(187, 187, 187);
            Colors[66] = Color.FromArgb(184, 153, 51);
            Colors[67] = Color.FromArgb(184, 136, 187);
            Colors[68] = Color.FromArgb(119, 68, 68);
            Colors[69] = Color.FromArgb(136, 68, 153);
            Colors[70] = Color.FromArgb(221, (int)byte.MaxValue, 221);
            Colors[71] = Color.FromArgb(71, 215, 100);
            Colors[72] = Color.FromArgb(101, 134, 247);
            Colors[73] = Color.FromArgb(247, 82, 67);
            Colors[74] = Color.FromArgb(132, 238, 247);
            Colors[75] = Color.FromArgb((int)byte.MaxValue, 135, 231);
            Colors[82] = Color.FromArgb(17, 102, 102);
            Colors[86] = Color.FromArgb(184, (int)byte.MaxValue, 17);
            Colors[90] = Color.FromArgb(238, 238, 238);
            Colors[91] = Color.FromArgb((int)byte.MaxValue, 90, 0);
            Colors[92] = Color.FromArgb(193, 187, 187);
            Colors[93] = Color.FromArgb(187, 193, 187);
            Colors[94] = Color.FromArgb(187, 187, 193);
            Colors[95] = Color.FromArgb(184, (int)byte.MaxValue, 34);
            Colors[96] = Color.FromArgb(184, (int)byte.MaxValue, 68);
            Colors[97] = Color.FromArgb(112, 160, 183);
            Colors[98] = Color.FromArgb(112, 187, 207);
            Colors[99] = Color.FromArgb(219, 209, 125);
            Colors[100] = Color.FromArgb(181, 168, 57);
            Colors[101] = Color.FromArgb(76, 191, 0);
            Colors[102] = Color.FromArgb(208, 206, 0);
            Colors[103] = Color.FromArgb(133, 81, 166);
            Colors[104] = Color.FromArgb(153, 153, 136);
            Colors[105] = Color.FromArgb(198, 0, 0);
            Colors[106] = Color.FromArgb(136, 136, 136);
            Colors[107] = Color.FromArgb(8, 215, 100);
            Colors[108] = Color.FromArgb((int)byte.MaxValue, 0, 0);
            Colors[109] = Color.FromArgb(0, 0, (int)byte.MaxValue);
            Colors[110] = Color.FromArgb((int)byte.MaxValue, 0, (int)byte.MaxValue);
            Colors[111] = Color.FromArgb(238, 238, (int)byte.MaxValue);
            Colors[112] = Color.FromArgb(0, (int)byte.MaxValue, (int)byte.MaxValue);
            Colors[113] = Color.FromArgb(211, 159, 166);
            Colors[114] = Color.FromArgb(119, 119, 119);
            Colors[115] = Color.FromArgb(56, 118, 65);
            Colors[116] = Color.FromArgb(17, 17, (int)byte.MaxValue);
            Colors[117] = Color.FromArgb(170, 119, 119);
            Colors[118] = Color.FromArgb(100, 98, 21);
            Colors[119] = Color.FromArgb(170, (int)byte.MaxValue, (int)byte.MaxValue);
            Colors[120] = Color.FromArgb(227, 191, 120);
            Colors[121] = Color.FromArgb(163, 136, 72);
            Colors[122] = Color.FromArgb(51, 153, 120);
            return Colors;
        }

        public static void Test()
        {
            var bmp = new Bitmap(100, 100);
            for (int i = 0; i < 100; i++)
            {
                for (int x = 0; x < 100; x++)
                {
                    bmp.SetPixel(i, x, Color.Aqua);
                }
            }

            bmp.Save("test.png");
        }

        #endregion

    }
}