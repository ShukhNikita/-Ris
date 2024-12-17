using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetJunk.Converters
{
    public class DoubleArrayConverter : JsonConverter<double[,]>
    {
        public override double[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = JsonSerializer.Deserialize<List<List<double>>>(ref reader, options);
            var rows = list.Count;
            var cols = list[0].Count;
            var array = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    array[i, j] = list[i][j];
                }
            }
            return array;
        }

        public override void Write(Utf8JsonWriter writer, double[,] value, JsonSerializerOptions options)
        {
            var rows = value.GetLength(0);
            var cols = value.GetLength(1);
            var list = new List<List<double>>(rows);
            for (int i = 0; i < rows; i++)
            {
                var row = new List<double>(cols);
                for (int j = 0; j < cols; j++)
                {
                    row.Add(value[i, j]);
                }
                list.Add(row);
            }
            JsonSerializer.Serialize(writer, list, options);
        }
    }
}
