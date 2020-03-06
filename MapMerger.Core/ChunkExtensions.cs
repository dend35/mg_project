namespace MapMerger.Core
{
    public static class ChunkExtensions
    {
        /// <summary>
        /// Зеркально отражает чанк по оси X
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public static byte[,] ReflectionChunk(this byte[,] chunk)
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

        /// <summary>
        /// Поворачивает чанк по часовой стрелке
        /// </summary>
        /// <param name="oldMatrix"></param>
        /// <returns></returns>
        public static byte[,] RotateMatrixClockwise(this byte[,] oldMatrix)
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

            return newMatrix;
        }
    }
}