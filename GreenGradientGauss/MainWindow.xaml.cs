using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HTCP_Project
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _matrixFilePath = "D:\\4 курс\\GreenGradientGauss\\GreenGradientGauss\\Files\\numbers.txt";
        TcpServer _tcpServer = null;
        double[] solution = null;

        public MainWindow()
        {
            InitializeComponent();
            RunServer();
        }

        private void RunServer()
        {
            _tcpServer = new TcpServer("http://localhost:8081/", _matrixFilePath);
            _tcpServer.slauSolved += DisplayResultsVector;
            Task.Run(() => _tcpServer.StartServer());
        }

        private void DisplayResultsVector(double[] vector, long time)
        {
            Dispatcher.Invoke(() =>
            {
                solution = vector;
                ResultsListBox.Items.Clear();

                for (int i = 0; i < vector.Length; i++)
                {
                    if (i >= 50) break;

                    ResultsListBox.Items.Add(vector[i].ToString());
                }

                DistributedTimeText.Text = time.ToString() + " ms";
            });
        }

        private void GenerateMatrixButton_Click(object sender, RoutedEventArgs e)
        {
            int size = int.Parse(slauSizeTextBox.Text);
            var (matrix, vector) = GenerateSymmetricPositiveDefiniteMatrixAndVector(size);
            SaveMatrixAndVectorToFile(matrix, vector);
            DisplayMatrixInGrid(matrix, vector, 50);
        }

        private (double[,], double[]) GenerateSymmetricPositiveDefiniteMatrixAndVector(int size)
        {
            Random rand = new Random();
            double[,] matrix = new double[size, size];
            double[] vector = new double[size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    matrix[i, j] = rand.NextDouble() * 10;
                    matrix[j, i] = matrix[i, j];
                }
                matrix[i, i] += size;
            }

            for (int i = 0; i < size; i++)
            {
                vector[i] = rand.NextDouble() * 10;
            }

            return (matrix, vector);
        }

        private void SaveMatrixAndVectorToFile(double[,] matrix, double[] vector)
        {
            using (StreamWriter writer = new StreamWriter(_matrixFilePath))
            {
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        writer.Write(matrix[i, j] + " ");
                    }

                    writer.Write(vector[i]);
                    writer.WriteLine();
                }
            }
        }

        private void DisplayMatrixInGrid(double[,] matrix, double[] vector, int maxDisplay)
        {
            MatrixGrid.Columns.Clear();
            MatrixGrid.Items.Clear();

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int displayRows = Math.Min(maxDisplay, rows);
            int displayCols = Math.Min(maxDisplay, cols);

            for (int j = 0; j < displayCols + 1; j++)
            {
                DataGridTextColumn column = new DataGridTextColumn
                {
                    Header = $"A{j + 1}",
                    Binding = new Binding($"[{j}]")
                };
                MatrixGrid.Columns.Add(column);
            }

            for (int i = 0; i < displayRows; i++)
            {
                var row = new double[displayCols + 1];
                for (int j = 0; j < displayCols; j++)
                {
                    row[j] = Math.Round(matrix[i, j], 2);
                }

                row[displayCols] = Math.Round(vector[i], 2);
                MatrixGrid.Items.Add(row);
            }
        }

        private void SolveDistributedButton_Click(object sender, RoutedEventArgs e)
        {
            _tcpServer.SolveMatrix();
        }

        private void SolveClassicalButton_Click(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = Stopwatch.StartNew();
            MasterZadanij wisard = new MasterZadanij(_matrixFilePath);

            var sol = wisard.SolveClassicGauss();
            sw.Stop();

            ResultsListBox.Items.Clear();
            solution = sol;

            for (int i = 0; i < sol.Length; i++)
            {
                if (i >= 50) break;

                ResultsListBox.Items.Add(sol[i].ToString());
            }

            ClassicalTimeText.Text = sw.ElapsedMilliseconds.ToString() + " ms";
        }

        private void OnLoadMatrixButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog(); if (openFileDialog.ShowDialog() == true)
            {
                string sourceFilePath = openFileDialog.FileName;
                
                File.Copy(sourceFilePath, _matrixFilePath, true);
                var result = ReadMatrixAndVectorFromFile(_matrixFilePath);
                DisplayMatrixInGrid(result.Item1, result.Item2, 50);
            }
        }

        private Tuple<double[,], double[]> ReadMatrixAndVectorFromFile(string filePath)
        {
            double[,] matrix = null;
            double[] vector = null;
            int row = 0;

            using (var reader = new StreamReader(filePath))
            {
                string line;

                while (row < 50 && (line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(new char[] { ' ', '\t' });
                    if (matrix == null)
                    {
                        int size = Math.Min(50, parts.Length - 1);
                        matrix = new double[size, size];
                        vector = new double[size];
                    }

                    int cols = Math.Min(parts.Length - 1, 50);

                    for (int col = 0; col < cols; col++)
                        matrix[row, col] = double.Parse(parts[col]);
                    if (parts.Length > 0)
                        vector[row] = double.Parse(parts[parts.Length - 1]);

                    row++;
                }
            }

            return new Tuple<double[,], double[]>(matrix, vector);
        }

        private void OnSaveResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (solution == null || solution.Length == 0)
            {
                MessageBox.Show("Вектор пуст! Невозможно сохранить пустой вектор.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*", Title = "Сохранить вектор" };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                
                try
                {
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        foreach (double value in solution)
                        {
                            writer.WriteLine(value.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении вектора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
