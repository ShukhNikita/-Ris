using HTCP_Project;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GradientGaussWhat
{
    [TestClass]
    public class LoadGradTests
    {
        private const int MaxMatrixSize = 20;
        private const int SafeMemoryLimit = 50 * 1024 * 1024;

        [TestMethod]
        public void ParallelProcessing_Performance_Test()
        {
            // Arrange
            string testMatrixPath = CreateLargeTestMatrixFile(5);
            var wizard = new GradientWizard(testMatrixPath);
            var stopwatch = new Stopwatch();

            try
            {
                // Act
                stopwatch.Start();
                wizard.BeginSolving(1, 50);
                //var tasks = wizard.GetIterationTasks();
                stopwatch.Stop();

                // Assert
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000);
            }
            finally
            {
                File.Delete(testMatrixPath);
            }
        }

        [TestMethod]
        public void MemoryEfficiency_Test()
        {
            // Arrange
            string testMatrixPath = CreateLargeTestMatrixFile(2);
            var initialMemory = GC.GetTotalMemory(true);

            try
            {
                // Act
                var wizard = new GradientWizard(testMatrixPath);
                wizard.BeginSolving(1, 50);
                //var tasks = wizard.GetIterationTasks();
                var finalMemory = GC.GetTotalMemory(true);

                // Assert
                var memoryUsed = finalMemory - initialMemory;
                Assert.IsTrue(memoryUsed < 50 * 1024 * 1024);
            }
            finally
            {
                File.Delete(testMatrixPath);
            }
        }

        [TestMethod]
        public void TaskCreation_Performance_Test()
        {
            // Arrange
            string testMatrixPath = CreateLargeTestMatrixFile(5);
            var wizard = new GradientWizard(testMatrixPath);
            var stopwatch = new Stopwatch();

            try
            {
                // Act
                wizard.BeginSolving(1, 50);
                stopwatch.Start();
                //var tasks = wizard.GetIterationTasks();
                stopwatch.Stop();

                // Assert
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000);
            }
            finally
            {
                File.Delete(testMatrixPath);
            }
        }

        [TestMethod]
        public void Sequential_Operations_Test()
        {
            // Arrange
            string testMatrixPath = CreateLargeTestMatrixFile(8);
            var wizard = new GradientWizard(testMatrixPath);
            var stopwatch = new Stopwatch();

            try
            {
                // Act
                stopwatch.Start();
                wizard.BeginSolving(2, 40);
                var results = new Dictionary<int, double[]>();
                stopwatch.Stop();

                // Assert
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000);
            }
            finally
            {
                File.Delete(testMatrixPath);
            }
        }

        [TestMethod]
        public void File_IO_Performance_Test()
        {
            // Arrange
            string testMatrixPath = CreateLargeTestMatrixFile(2);
            var stopwatch = new Stopwatch();

            try
            {
                // Act
                stopwatch.Start();
                var wizard = new GradientWizard(testMatrixPath);
                wizard.ReadVector();
                stopwatch.Stop();

                // Assert
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000);
            }
            finally
            {
                File.Delete(testMatrixPath);
            }
        }

        private string CreateLargeTestMatrixFile(int size)
        {
            if (size > MaxMatrixSize)
            {
                throw new ArgumentException($"Размер матрицы не может превышать {MaxMatrixSize}");
            }

            string path = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(path))
            {
                Random rand = new Random();
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        writer.Write($"{rand.NextDouble():F2} ");
                    }
                    writer.WriteLine();
                }
            }
            return path;
        }

        private long GetAvailableMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return GC.GetTotalMemory(true);
        }
    }
}
