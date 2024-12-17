using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CliNet
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            TisipiClient stp = new TisipiClient("127.0.0.1", 941);
            await stp.Forward();

            Console.ReadKey();
        }
    }
}
