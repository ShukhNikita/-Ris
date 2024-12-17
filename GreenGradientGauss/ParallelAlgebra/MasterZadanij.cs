using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HTCP_Project
{
    public class MasterZadanij
    {
        private string _matrixPath;
        private double[,] A;
        private double[] b;
        private int _iteration;
        private int _maxIterations = 100;
        private double _eps = 1e-5;
        private bool _solved = true;
        private int _tasksCount;
        private int _maxTaskSize;
        private int _taskSize;
        private int _taskOverflow;
        private bool _tooLarge = false;

        private int n;
        private double[] x;
        private double[] r;
        private double[] p;
        private double[] Ap;
        private double rsold;

        private List<Zadanie> _tasksQueue;


        public MasterZadanij(string matrixPath)
        {
            _matrixPath = matrixPath;
        }

        public bool IsSolved() => _solved;

        public double[] GetSolution() => x;

        public string GetStringSolution() => VectorToString(x);

        public void BeginSolving(int tasksCount, int maxTaskSize = 500)
        {
            _tasksCount = tasksCount;
            _maxTaskSize = maxTaskSize;
            _solved = false;
            _iteration = 0;
            ReadVector();

            _taskSize = b.Length / _tasksCount;
            
            if (_taskSize > maxTaskSize)
            {
                _taskSize = maxTaskSize;
                _tasksCount = b.Length / _taskSize;
            }

            _taskOverflow = b.Length - (_tasksCount * _taskSize);
            if (_tasksCount * _taskSize >= 5000) _tooLarge = true;

            n = b.Length;
            x = new double[n];
            r = new double[n];
            p = new double[n];
            Ap = new double[n];

            Array.Copy(b, r, n);
            Array.Copy(r, p, n);

            rsold = DotProduct(r, r);
        }

        public List<Zadanie> GetIterationTasks()
        {
            if (_tasksQueue == null)
            {
                _tasksQueue = new List<Zadanie>();
                int taskIndex = 0;

                for (int i = _taskSize; i <= n; i += _taskSize)
                {
                    _tasksQueue.Add(new Zadanie(_matrixPath, p, taskIndex, i - _taskSize, _taskSize, _tooLarge));
                    taskIndex++;
                }
                if (_taskOverflow > 0)
                    _tasksQueue.Add(new Zadanie(_matrixPath, p, taskIndex, _tasksCount * _taskSize, _taskOverflow, _tooLarge));
            }
            else
            {
                foreach (var t in _tasksQueue)
                    t.UpdateP(p);
            }

            return _tasksQueue;
        }

        public void EndIteration(Dictionary<int, double[]> results)
        {
            GlueTasksResult(results);

            double alpha = rsold / DotProduct(p, Ap);

            for (int i = 0; i < n; i++)
            {
                x[i] += alpha * p[i];
                r[i] -= alpha * Ap[i];
            }

            double rsnew = DotProduct(r, r);

            if (Math.Sqrt(rsnew) < _eps || _iteration >= _maxIterations)
                _solved = true;

            for (int i = 0; i < n; i++)
            {
                p[i] = r[i] + (rsnew / rsold) * p[i];
            }

            rsold = rsnew;
            _iteration++;
        }

        private void GlueTasksResult(Dictionary<int, double[]> results)
        {
            foreach (var part in results)
            {
                int partIndex = part.Key;
                double[] partVector = part.Value;
                int startIndex = partIndex * _taskSize;

                Array.Copy(partVector, 0, Ap, startIndex, partVector.Length);
            }
        }

        private double DotProduct(double[] a, double[] b)
        {
            double result = 0;

            for (int i = 0; i < a.Length; i++)
                result += a[i] * b[i];

            return result;
        }

        internal static void Multiply(double[,] A, double[] x, double[] result)
        {
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = 0;

                for (int j = 0; j < x.Length; j++)
                {
                    result[i] += A[i, j] * x[j];
                }
            }
        }

        private void ReadMatrix()
        {
            string strMatrix = File.ReadAllText(_matrixPath);
            double[,] matrix = StringToMatrix(strMatrix);

            A = new double[matrix.GetLength(0), matrix.GetLength(1) - 1];
            b = new double[matrix.GetLength(0)];

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                b[i] = matrix[i, matrix.GetLength(1) - 1];

                for (int j = 0; j < matrix.GetLength(1) - 1; j++)
                    A[i, j] = matrix[i, j];
            }
        }

        public void ReadVector()
        {
            List<double> vector = new List<double>();
            
            using (var reader = new StreamReader(_matrixPath))
            {
                string line;
                
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(new char[] { ' ', '\t' });

                    if (parts.Length > 0 && double.TryParse(parts[parts.Length - 1], out double vectorValue))
                    {
                        vector.Add(vectorValue);
                    }
                }
            }

            b = vector.ToArray();
        }

        private double[,] StringToMatrix(string strMatrix)
        {
            string[] strLines = strMatrix.Split('\n');
            string[] firstLine = strLines[0].Split(' ');
            double[,] matrix = new double[strLines.Length - 1, firstLine.Length];

            string[] lineData;

            for (int i = 0; i < strLines.Length - 1; i++)
            {
                lineData = strLines[i].Split(' ');

                for (int j = 0; j < lineData.Length; j++)
                {
                    matrix[i, j] = double.Parse(lineData[j]);
                }
            }

            return matrix;
        }

        private static string VectorToString(double[] x)
        {
            StringBuilder sb = new StringBuilder();

            foreach (double d in x)
                sb.Append(d.ToString() + " ");

            return sb.ToString();
        }


        public double[] SolveClassicGauss()
        {
            ReadMatrix();

            int n = b.Length;
            double[,] augmentedMatrix = new double[n, n + 1];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmentedMatrix[i, j] = A[i, j];
                }
                augmentedMatrix[i, n] = b[i];
            }

            for (int i = 0; i < n; i++)
            {
                double maxElement = Math.Abs(augmentedMatrix[i, i]);
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(augmentedMatrix[k, i]) > maxElement)
                    {
                        maxElement = Math.Abs(augmentedMatrix[k, i]);
                        maxRow = k;
                    }
                }

                for (int k = i; k < n + 1; k++)
                {
                    double temp = augmentedMatrix[maxRow, k];
                    augmentedMatrix[maxRow, k] = augmentedMatrix[i, k];
                    augmentedMatrix[i, k] = temp;
                }

                for (int k = i + 1; k < n; k++)
                {
                    double coef = augmentedMatrix[k, i] / augmentedMatrix[i, i];
                    for (int j = i; j < n + 1; j++)
                    {
                        if (i == j)
                        {
                            augmentedMatrix[k, j] = 0;
                        }
                        else
                        {
                            augmentedMatrix[k, j] -= coef * augmentedMatrix[i, j];
                        }
                    }
                }
            }

            x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = augmentedMatrix[i, n] / augmentedMatrix[i, i];
                for (int k = i - 1; k >= 0; k--)
                {
                    augmentedMatrix[k, n] -= augmentedMatrix[k, i] * x[i];
                }
            }

            return x;
        }
    }
}
