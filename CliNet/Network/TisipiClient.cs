using NetJunk;
using NetJunk.Converters;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CliNet
{
    class TisipiClient
    {
        private const string MESSAGE_END = "<END>";

        private string _ipAddress;
        private int _port;

        private static bool registered = false;

        internal TisipiClient(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        internal async Task Forward()
        {
            await StartClient(_ipAddress, _port);
        }

        public async Task StartClient(string ipAddress, int port)
        {
            using (var client = new TcpClient())
            {
                client.ReceiveBufferSize = 8192;
                client.SendBufferSize = 8192;

                while (!client.Connected)
                {
                    try
                    {
                        await client.ConnectAsync(ipAddress, port);
                        Console.WriteLine("Connected successfully!");
                    }
                    catch (SocketException)
                    {
                        await Task.Delay(1000);
                    }
                }

                while (true)
                {
                    var stream = client.GetStream();

                    byte[] lengthBuffer = new byte[4];
                    await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);

                    Console.WriteLine($"Received task -> solving...");

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    byte[] messageBuffer = new byte[messageLength];
                    await stream.ReadAsync(messageBuffer, 0, messageLength);

                    await stream.WriteAsync(new byte[] { 1 }, 0, 1);

                    string jsonData = Encoding.UTF8.GetString(messageBuffer);
                    //Console.WriteLine(jsonData);
                    await SolveTask(jsonData, stream);
                }
            }
        }

        private async Task SolveTask(string task, NetworkStream stream)
        {
            Convolute packet = DeserializeTask(task);
            Console.WriteLine($"Task index : {packet.Index}");
            GradientSolver.Multiply(packet.A, packet.P, packet.Pa);

            string result = SerializeResult(packet);
            byte[] messageBuffer = Encoding.UTF8.GetBytes(result);
            var length = BitConverter.GetBytes(messageBuffer.Length);

            await stream.WriteAsync(length, 0, length.Length);
            await stream.WriteAsync(messageBuffer, 0, messageBuffer.Length);

            Console.WriteLine("Task result sended to Server!");
        }

        public Convolute DeserializeTask(string jsonData)
        {
            try
            {
                jsonData = jsonData.Replace(MESSAGE_END, "");

                var options = new JsonSerializerOptions
                {
                    Converters = { new DoubleArrayConverter() }
                };

                var packet = JsonSerializer.Deserialize<Convolute>(jsonData, options);
                packet.Pa = new double[packet.A.GetLength(0)];

                return packet;
            }
            catch (Exception ex)
            {
                var t = jsonData;
                var e = 937845;
                return null;
            }
        }

        public string SerializeResult(Convolute data)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new DoubleArrayConverter() }
            };

            data.A = null;
            data.P = null;

            return JsonSerializer.Serialize(data, options);
        }
    }
}
