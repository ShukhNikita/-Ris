using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CliNet
{
    internal class GradientSolver
    {
        internal static void Multiply(double[,] A, double[] x, double[] result)
        {
            Parallel.For(0, result.Length, i =>
            {
                result[i] = 0;

                for (int j = 0; j < x.Length; j++)
                {
                    result[i] += A[i, j] * x[j];
                }
            });
        }
    }
}
