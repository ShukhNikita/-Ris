using NetJunk;
using NetJunk.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HTCP_Project
{
    internal class TcpServer
    {
        private const string MESSAGE_END = "<END>";

        private string _url;
        private string _textBufferPath;

        internal Stopwatch workTimer = new Stopwatch();

        private List<TcpClient> _webNodes = new List<TcpClient>();
        private List<bool> _availableNodes = new List<bool>();
        private bool _isSolving = false;
        private TcpListener _listener = null;
        private StreamWriter _writer = null;

        private MasterZadanij _master = null;
        private List<Zadanie> _tasksQueue = new List<Zadanie>();

        internal delegate void SlauResult(double[] result, long time);
        internal event SlauResult slauSolved = delegate { };
        internal bool isRunning = false;

        internal TcpServer(string url, string textBufferPath)
        {
            _url = url;
            _textBufferPath = textBufferPath;
        }


        private const int BUFFER_SIZE = 8192;

        internal void StartServer()
        {
            _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 941);
            _listener.Start();
            isRunning = true;

            Debug.WriteLine("Сервер запущен и ожидает подключений...");
            Task.Run(() => Loop());
        }

        private async Task Loop()
        {
            while (true)
            {
                if (_listener.Pending())
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    client.ReceiveBufferSize = BUFFER_SIZE;
                    client.SendBufferSize = BUFFER_SIZE;
                    _webNodes.Add(client);
                    _availableNodes.Add(true);
                }

                if (_master != null && _tasksQueue.Count > 0)
                {
                    var tasks = new List<Task>();

                    for (int i = 0; i < _webNodes.Count; i++)
                    {
                        if (!_availableNodes[i]) continue;

                        var pendingTask = _tasksQueue.FirstOrDefault(t => t.state == Zadanie.TaskState.Waiting);
                        if (pendingTask == null) break;

                        pendingTask.state = Zadanie.TaskState.Solving;

                        _availableNodes[i] = false;
                        var nodeIndex = i;

                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var stream = _webNodes[nodeIndex].GetStream();
                                await SendTask(stream, pendingTask);
                                await GetResult(stream);
                                CheckResult();
                            }
                            finally
                            {
                                _availableNodes[nodeIndex] = true;
                            }
                        }));
                    }

                    if (tasks.Any())
                    {
                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }

        private async Task SendTask(NetworkStream stream, Zadanie task)
        {
            var packet = task.GetPacket();
            var messageLength = BitConverter.GetBytes(packet.Length);

            await stream.WriteAsync(messageLength, 0, messageLength.Length);

            byte[] buffer = Encoding.UTF8.GetBytes(packet);
            await stream.WriteAsync(buffer, 0, buffer.Length);

            var res = new byte[1];
            await stream.ReadAsync(res, 0, 1);
            Console.WriteLine($"Read status: {res[0]}");
        }

        private async Task GetResult(NetworkStream stream)
        {
            byte[] lengthBuffer = new byte[4];
            await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            byte[] messageBuffer = new byte[messageLength];
            await stream.ReadAsync(messageBuffer, 0, messageLength);

            string jsonData = Encoding.UTF8.GetString(messageBuffer);
            Convolute result = DeserializeResult(jsonData);

            var task = _tasksQueue.First(t => t.GetIndex() == result.Index);
            task.SetResult(result.Pa);
        }


        private void CheckResult()
        {
            if (_tasksQueue.Count > 0 && _tasksQueue.All(t => t.state == Zadanie.TaskState.Solved))
            {
                Dictionary<int, double[]> solution = new Dictionary<int, double[]>();
                foreach (var task in _tasksQueue)
                    solution[task.GetIndex()] = task.GetResult();

                _master.EndIteration(solution);

                if (_master.IsSolved())
                    SendResult();
                else
                    _tasksQueue = _master.GetIterationTasks();
            }
        }

        internal void SolveMatrix()
        {
            _master = new MasterZadanij(_textBufferPath);
            _master.BeginSolving(_webNodes.Count);
            _tasksQueue = _master.GetIterationTasks();
            workTimer.Reset();
            workTimer.Start();
            _isSolving = true;
        }

        public Convolute DeserializeResult(string jsonData)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new DoubleArrayConverter() }
            };

            return JsonSerializer.Deserialize<Convolute>(jsonData, options);
        }

        private void SendResult()
        {
            workTimer.Stop();
            var fullResult = _master.GetStringSolution();
            slauSolved(_master.GetSolution(), workTimer.ElapsedMilliseconds);

            Debug.WriteLine("Задача была успешно решена за " + workTimer.ElapsedMilliseconds.ToString() + " мсек.");
            Debug.WriteLine("Полный результат:");
            Debug.WriteLine(fullResult);

            _tasksQueue.Clear();
            _master = null;
            _isSolving = false;
        }
    }
}
