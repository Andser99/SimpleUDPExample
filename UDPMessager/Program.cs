using System;
using System.Linq;
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
        static void PrintInfoMessage()
        {
            Console.WriteLine("Any messages which don't start with PORT/TCP/BYTES:");
            Console.WriteLine("are broadcast to 255.255.255.255");
            Console.WriteLine("PORT <port_number>  - sets the UDP port number to the specified port");
            Console.WriteLine("TCP<address><message> - sends a TCP message to the specified address");
            Console.WriteLine("BYTES:<message> - Sends a base 64 encoded message");
            Console.WriteLine("--------------------------------------------------------------------");
        }
        static void Main(string[] args)
        {
            PrintInfoMessage();
            UDPer client = new UDPer();
            Console.WriteLine($"UDP Communicator on port {client.Port}");
            var x = Console.ReadLine();
            while(x != "q")
            {
                if (x.StartsWith("PORT"))
                {
                    int.TryParse(x.Split("PORT")[0], out int port);
                    Console.WriteLine($"Port set to {port}");
                    client.Port = port;
                }
                else if (x.StartsWith("TCP"))
                {
                    client.SendTCP(x.Split(">")[1], IPAddress.Parse(x.Split('<')[1].Split('>')[0]));
                }
                else
                {
                    if (x.StartsWith("BYTES:"))
                    {
                        client.Send(x.Split("BYTES:")[1], true);
                    }
                    else
                    {
                        client.Send(x);
                    }
                }
                x = Console.ReadLine();
            }

        }
        class UDPer
        {
            //Default port number for Elisan networking
            public static readonly int PORT_NUMBER = 5490;
            private int _port;
            Task tcpListenerTask = null;
            public int Port
            {
                get
                {
                    return _port;
                }
                set
                {
                    if (value != _port)
                    {
                        if (udp != null && udp2 != null)
                        {
                            Stop();
                        }
                        udp = new UdpClient("localhost", value);
                        udp2 = new UdpClient("localhost", value + 1);
                        Start();
                    }
                    _port = value;
                }
            }

            public UDPer(int port = 8888)
            {
                Port = port;
                udp = new UdpClient("localhost", Port);
                udp2 = new UdpClient("localhost", Port + 1);
            }
            public void Start()
            {
                Console.WriteLine("UDP Started listening on {0}", Port);
                StartListening();
                if (tcpListenerTask == null)
                {
                    tcpListenerTask = StartTCPListener();
                }
            }
            public void Stop()
            {
                try
                {
                    udp.Close();
                    udp2.Close();
                    Console.WriteLine("UDP Stopped listening");
                }
                catch { /* don't care */ }
            }

            private UdpClient udp;
            private UdpClient udp2;


            private void StartListening()
            {
                udp.BeginReceive(Receive, new object());
                udp2.BeginReceive(Receive2, new object());
            }
            private void Receive(IAsyncResult ar)
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, Port);
                byte[] bytes;
                try
                {
                    bytes = udp.EndReceive(ar, ref ip);

                }
                catch(Exception e)
                {
                    Console.WriteLine($"{udp.Client} was closed");
                    System.Diagnostics.Debug.WriteLine(e);
                    return;
                }
                string message = Encoding.ASCII.GetString(bytes);
                Console.WriteLine("From {0} at {2} received: {1} ", ip.Address.ToString(), message, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
                StartListening();
            }
            private void Receive2(IAsyncResult ar)
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, Port + 1);
                byte[] bytes;
                try
                {
                    bytes = udp2.EndReceive(ar, ref ip);

                }
                catch (Exception e)
                {
                    Console.WriteLine($"{udp.Client} was closed");
                    System.Diagnostics.Debug.WriteLine(e);
                    return;
                }
                string message = Encoding.ASCII.GetString(bytes);
                Console.WriteLine("From {0} at {2} received: {1} ", ip.Address.ToString(), message, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
                StartListening();
            }

            public void Send(string message, bool asBytes = false)
            {
                UdpClient client = new UdpClient();
                IPEndPoint ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), Port);
                //IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), PORT_NUMBER);

                byte[] bytes;
                if (asBytes)
                {
                    if (message.Length % 2 != 0)
                    {
                        Console.WriteLine("Invalid message length (not a multiple of 2)");
                        return;
                    }
                    else
                    {
                        bytes = Enumerable.Range(0, message.Length)
                            .Where(x => x % 2 == 0)
                            .Select(x => Convert.ToByte(message.Substring(x, 2), 16))
                            .ToArray();
                    }
                }
                else
                {
                    bytes = Encoding.ASCII.GetBytes(message);
                } 


                client.Send(bytes, bytes.Length, ip);
                client.Close();
                Console.WriteLine("Sent ({1}): {0}", message, asBytes ? "as bytes" : "as text");
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
                    catch (Exception e) 
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                    }
                }
                Byte[] data = Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);
            }
            private async Task StartTCPListener()
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
                    System.Diagnostics.Debug.WriteLine(e);
                }
                TcpListener listener = new TcpListener(localAdd, 5939);
                Console.WriteLine("TCP listening on {0}:{1}", localAdd, 5939);
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
