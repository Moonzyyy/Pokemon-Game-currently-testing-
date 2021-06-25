using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Pokemon_Server
{
    //The class for server
    class Server
    {
        //The amount of max players
        public static int MaxPlayers { get; private set; }
        //the port of the server
        public static int Port { get; private set; }
        //Holds the client
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();

        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static  Dictionary<int, PacketHandler> packetHandlers;
        //Will Listen to TCP connections
        private static TcpListener tcpListener;

        private static UdpClient udpListener;

        //Called at first frame
        public static void Start(int _maxPlayers, int _port)
        {
            //The maximum amount of players
            MaxPlayers = _maxPlayers;
            //The port
            Port = _port;

            //Write on console that we are starting server
            ////For some reason does not work on console?
            Console.WriteLine("Starting Server...");
            //Calls this method
            InitializeServerData();

            //1 tcp listener for every client
            tcpListener = new TcpListener(IPAddress.Any, Port);
            //We start the listener
            tcpListener.Start();
            //We begin to try acceptin new clients
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            //we say what port the server is using
            Console.WriteLine($"Server started on {Port}.");
        }
        
        //Waiting for reply
        private static void TCPConnectCallback(IAsyncResult _result)
        {
            //New instance of client
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            //We call the function and see for a result
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            //Incoming connection
            Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");
            //create new players until max players has been reached
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    return;
                }
            }
            //We write the end point if client is full
            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server Full");
        }

        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientID = _packet.ReadInt();

                    if (_clientID == 0)
                    {
                        return;
                    }

                    if (clients[_clientID].udp.endPoint == null)
                    {
                        clients[_clientID].udp.Connect(_clientEndPoint);
                        return;

                    }

                    if(clients[_clientID].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        clients[_clientID].udp.HandleData(_packet);
                    }
                }
            }

            catch (Exception _ex)
            {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
            }
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if(_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }

            catch (Exception _ex)
            {
                Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }
        //Initialise the amount of clients able to connect
        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived},
                {(int)ClientPackets.playermovement, ServerHandle.PlayerMovement},

            };
            Console.WriteLine("Initialized packets.");
        }
    }
}
