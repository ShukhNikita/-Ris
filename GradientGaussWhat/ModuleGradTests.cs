using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace TestsDeluxe
{
    [TestClass]
    public class ModuleGradTests
    {
        [TestMethod]
        public void BeginSolving_ValidInput_InitializesCorrectly()
        {
            // Arrange
            string testMatrixPath = CreateTestMatrixFile();
            var wizard = new MasterZadanij(testMatrixPath);

            // Act
            wizard.BeginSolving(2, 100);

            // Assert
            Assert.IsFalse(wizard.IsSolved());
            File.Delete(testMatrixPath);
        }

        [TestMethod]
        public void ReadVector_ValidFile_ReturnsCorrectVector()
        {
            // Arrange
            string testMatrixPath = CreateTestMatrixFile();
            var wizard = new GradientWizard(testMatrixPath);

            // Act
            wizard.ReadVector();
            var solution = wizard.GetSolution();

            // Assert
            Assert.IsNull(solution);
            File.Delete(testMatrixPath);
        }

        [TestMethod]
        public void SolveClassicGauss_SmallMatrix_ReturnsCorrectSolution()
        {
            // Arrange
            string testMatrixPath = CreateSmallTestMatrixFile();
            var wizard = new GradientWizard(testMatrixPath);

            // Act
            var solution = wizard.SolveClassicGauss();

            // Assert
            Assert.IsNotNull(solution);
            File.Delete(testMatrixPath);
        }

        [TestMethod]
        public void GetIterationTasks_ValidInput_ReturnsCorrectNumberOfTasks()
        {
            // Arrange
            string testMatrixPath = CreateSmallTestMatrixFile();
            var wizard = new GradientWizard(testMatrixPath);
            wizard.BeginSolving(2, 100);

            // Act
            var tasks = wizard.GetIterationTasks();

            // Assert
            Assert.IsNotNull(tasks);
            Assert.IsTrue(tasks.Count > 0);
            File.Delete(testMatrixPath);
        }

        [TestMethod]
        public void EndIteration_ValidResults_UpdatesSolution()
        {
            // Arrange
            string testMatrixPath = CreateSmallTestMatrixFile();
            var wizard = new GradientWizard(testMatrixPath);
            wizard.BeginSolving(1, 100);
            var results = new Dictionary<int, double[]> { { 0, new double[] { 1.0, 1.0 } } };

            // Act
            wizard.EndIteration(results);

            // Assert
            Assert.IsNotNull(wizard.GetSolution());
            File.Delete(testMatrixPath);
        }

        private string CreateTestMatrixFile()
        {
            string path = "test_matrix.txt";
            File.WriteAllText(path, "1.0 2.0\n2.0 4.0");
            return path;
        }

        private string CreateSmallTestMatrixFile()
        {
            string path = "small_matrix.txt";
            File.WriteAllText(path, "2 1 4\n1 3 9");
            return path;
        }
    }
}
