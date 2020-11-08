using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UDPMessager
{
    class Program
    {
        public static int Pocet_ludi = 9;
        static void Main(string[] args)
        {
            UDPer client = new UDPer();
            Console.WriteLine($"UDP Communicator on port {UDPer.PORT_NUMBER}");
            client.Start();
            client.Send("" + Pocet_ludi);
            var x = Console.ReadKey();
            while(x.Key != ConsoleKey.Escape)
            {
                client.Send(x.KeyChar.ToString());
                x = Console.ReadKey();
            }

        }
        class UDPer
        {
            public static readonly int PORT_NUMBER = 6969;
            Thread t = null;
            public void Start()
            {
                if (t != null)
                {
                    throw new Exception("Already started, stop first");
                }
                Console.WriteLine("Started listening");
                StartListening();
            }
            public void Stop()
            {
                try
                {
                    udp.Close();
                    Console.WriteLine("Stopped listening");
                }
                catch { /* don't care */ }
            }

            private readonly UdpClient udp = new UdpClient(PORT_NUMBER);
            IAsyncResult ar_ = null;

            private void StartListening()
            {
                ar_ = udp.BeginReceive(Receive, new object());
            }
            private void Receive(IAsyncResult ar)
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, PORT_NUMBER);
                byte[] bytes = udp.EndReceive(ar, ref ip);
                string message = Encoding.ASCII.GetString(bytes);
                Console.WriteLine("From {0} received: {1} ", ip.Address.ToString(), message);
                StartListening();
            }
            public void Send(string message)
            {
                UdpClient client = new UdpClient();
                IPEndPoint ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), PORT_NUMBER);
                byte[] bytes = Encoding.ASCII.GetBytes(message);
                client.Send(bytes, bytes.Length, ip);
                client.Close();
                Console.WriteLine("Sent: {0} ", message);
            }
        }

    }
}
