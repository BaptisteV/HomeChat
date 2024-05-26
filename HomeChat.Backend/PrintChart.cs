using System.Data.SqlTypes;
using System.Text;

namespace HomeChat.Backend;

public static class PrintChart
{
    private static char[,] RotateArrayClockwise(char[,] src)
    {
        int width;
        int height;
        char[,] dst;

        width = src.GetUpperBound(0) + 1;
        height = src.GetUpperBound(1) + 1;
        dst = new char[height, width];

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int newRow;
                int newCol;

                newRow = col;
                newCol = height - (row + 1);

                dst[newCol, newRow] = src[col, row];
            }
        }

        return dst;
    }
    private static char[,] ConvertToMultiDimensionalArray(char[][] jaggedArray)
    {
        int numRows = jaggedArray.Length;
        int numCols = jaggedArray[0].Length; // Assuming all sub-arrays have the same length

        char[,] multiArray = new char[numRows, numCols];

        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numCols; j++)
            {
                multiArray[i, j] = jaggedArray[i][j];
            }
        }

        return multiArray;
    }
    static char[][] ConvertToJaggedArray(char[,] multiArray)
    {
        int numRows = multiArray.GetLength(0);
        int numCols = multiArray.GetLength(1);

        char[][] jaggedArray = new char[numRows][];

        for (int i = 0; i < numRows; i++)
        {
            jaggedArray[i] = new char[numCols];
            for (int j = 0; j < numCols; j++)
            {
                jaggedArray[i][j] = multiArray[i, j];
            }
        }

        return jaggedArray;
    }
    public static string Generate(List<double> percentages)
    {
        var colTab = new char[percentages.Count][];
        for (var x = 0; x < percentages.Count; x++)
        {
            colTab[x] = new char[10];
            var sample = percentages[x];
            for (var y = 0; y < 10; y++)
            {
                var targetValue = y * 10;
                colTab[x][y] = sample <= targetValue ? 'O' : '■';
            }
        }
        static string CharsToString(char[] chars)
        {
            var result = "";
            foreach (var c in chars)
            {
                if (c != default)
                {
                    result += c;
                }
            }
            return result;
        }

        var rotated = RotateArrayClockwise(ConvertToMultiDimensionalArray(colTab));
        var res = ConvertToJaggedArray(rotated);
        var sb = new StringBuilder();
        foreach (var ll in res.Select(a => CharsToString(a.ToArray())))
        {
            sb.AppendLine(ll);
        }
        return sb.ToString();

    }
}
