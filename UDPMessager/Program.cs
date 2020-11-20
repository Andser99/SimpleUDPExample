using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            var x = Console.ReadLine();
            while(x != "q")
            {
                if (x.Contains("TCP"))
                {
                    client.SendTCP(x.Split(">")[1], IPAddress.Parse(x.Split('<')[1].Split('>')[0]));
                }
                else
                {
                    client.Send(x);
                }
                x = Console.ReadLine();
            }

        }
        class UDPer
        {
            public static readonly int PORT_NUMBER = 5490;
            Thread t = null;
            public void Start()
            {
                if (t != null)
                {
                    throw new Exception("Already started, stop first");
                }
                Console.WriteLine("Started listening");
                StartListening();
                StartTCPListener();
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
                Console.WriteLine("From {0} at {2} received: {1} ", ip.Address.ToString(), message, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
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

            public void SendTCP(string message, IPAddress ip)
            {
                TcpClient client = new TcpClient();
                while (!client.Connected)
                {
                    try
                    {
                        client.Connect(ip, 5939);
                    }
                    catch (Exception e) { }
                }
                Byte[] data = Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);
            }
            private async void StartTCPListener()
            {
                IPAddress localAdd = IPAddress.Parse("127.0.0.1");
                //Port 5939 for TCP connections                IPAddress localIP;
                try
                {
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                    {
                        socket.Connect("8.8.8.8", 65530);
                        IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                        localAdd = endPoint.Address;
                    }
                } catch (Exception e)
                {
                    Console.WriteLine("Couldn't get local network address, defaulting to 127.0.0.1");
                }
                TcpListener listener = new TcpListener(localAdd, 5939);
                await Task.Delay(500);
                listener.Start();

                while (true)
                {
                    //---incoming client connected---
                    TcpClient client = listener.AcceptTcpClient();

                    //---get the incoming data through a network stream---
                    NetworkStream nwStream = client.GetStream();
                    byte[] buffer = new byte[client.ReceiveBufferSize];

                    //---read incoming stream---
                    int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                    //---convert the data received into a string---
                    string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("TCP PACKET;");
                    Console.WriteLine(dataReceived);
                    ParseReceivedSettings(dataReceived);
                    client.Close();
                    await Task.Delay(500);
                }
            }
            private void ParseReceivedSettings(string settings)
            {
                var settingsList = settings.Split(':');
                //Split into pairs of <type>:<data>, if one isn't specified, the whole rest of the message is invalid
                for (var i = 2; i < settingsList.Length; i += 2)
                {
                    string settingType = settingsList[i];
                    if (i + 1 == settingsList.Length) break;
                    string settingData = settingsList[i + 1];
                    switch (settingType)
                    {
                        case "MaxPersons":
                            int maxPersons = -1;
                            int.TryParse(settingData, out maxPersons);
                            Console.WriteLine(maxPersons == -1 ? "Default MaxPersons" : maxPersons.ToString());
                            break;
                        case "Offset":
                            int offset = -1;
                            int.TryParse(settingData, out offset);
                            Console.WriteLine(offset == -1 ? "Default Offset" : offset.ToString());
                            break;
                        case "Reset":
                            int reset = -1;
                            int.TryParse(settingData, out reset);
                            Console.WriteLine(reset == -1 ? "Default Reset" : reset.ToString());
                            break;
                    }

                }
            }
        }

    }
}
