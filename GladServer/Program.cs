using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GladServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerHeart server = new ServerHeart();
            server.RunServer();
        }
    }
}
