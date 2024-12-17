using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Text.Json;
using System.Text.Json.Serialization;
using NetJunk.Converters;
using NetJunk;
using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;

namespace HTCP_Project
{
    public class Zadanie
    {
        private double[,] _Apart;
        private double[] _p;
        private double[] _Ap;
        private int _index;

        private string _matrixPath;
        private int _from;
        private int _length;
        private bool _tooLarge;

        internal TaskState state = TaskState.Waiting;
        internal enum TaskState
        {
            Waiting,
            Solving,
            Solved
        }

        public Zadanie(string matrixPath, double[] p, int index, int from, int lenght, bool tooLarge = false)
        {
            _p = p;
            _Ap = new double[lenght];
            _index = index;

            _matrixPath = matrixPath;
            _from = from;
            _length = lenght;
            _tooLarge = tooLarge;
        }

        public void ReadAPart(string filePath, int from, int length)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                for (int i = 0; i < from; i++)
                    reader.ReadLine();

                string line;

                for (int i = 0; i < length; i++)
                {
                    line = reader.ReadLine();
                    if (line == null)
                        throw new InvalidOperationException("Reached end of file before reading all required lines");

                    var values = line.Split(' ');

                    if (_Apart == null)
                    {
                        _Apart = new double[length, values.Length];
                    }

                    for (int j = 0; j < values.Length; j++)
                    {
                        _Apart[i, j] = double.Parse(values[j]);
                    }
                }
            }
        }

        internal void UpdateP(double[] newP)
        {
            _p = newP;
            state = TaskState.Waiting;
        }

        internal int GetIndex() => _index;

        internal double[] GetResult() => _Ap;

        internal void SetResult(double[] result)
        {
            state = TaskState.Solved;
            _Ap = result;
        }

        internal string GetPacket()
        {
            state = TaskState.Solving;

            if (_Apart == null)
                ReadAPart(_matrixPath, _from, _length);

            var data = new Convolute
            {
                A = _Apart,
                P = _p,
                Pa = null,
                Index = _index
            };

            if (_tooLarge)
                _Apart = null;

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new DoubleArrayConverter() }
            };

            var json = JsonSerializer.Serialize(data, jsonOptions);

            return json;
        }
    }
}
