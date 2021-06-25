using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Client : MonoBehaviour
{
    //Create a new client object
    public static Client instance;

    //How Big the packets are ... this example is 4kb
    public static int dataBufferSize = 4096;

    //The Server IP
    public string ip = "127.0.0.1";
    //Port number we want to connect
    public int port = 24554;

    
    public int myID = 0;

    //Transmission Control Protocol, where the receiver recieves packets and we tell the program we got
    //UDP, Where U send a packet and we do not need to reply <---- This is mostly used for gaming, however we are using TCP currently
    public TCP tcp;
    public UDP udp;
    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    //Calls Before the game starts
    private void Awake()
    {
        //If the client is not connected
        if (instance == null)
        {
            //we create a new instance of client
            instance = this;
        }

        //however if it is already created
        else  if (instance != this)
        {
            //We destroy the new client
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    //starts calling in the first frame
    private void Start()
    {
        //we create a new instance of TCP
        tcp = new TCP();
        udp = new UDP();

        myID = UnityEngine.Random.Range(0,100000);
    }

    //This is a method to connect to the server
    public void ConnectToServer()
    {
        InitializeClientData();
        tcp.Connect();
    }

    //This is the TCP class
    public class TCP
    {
        //Technoclogy term for creating a connection
        public TcpClient socket;

        //The stream of packets
        private NetworkStream stream;

        private Packet receivedData;

        //Array for byte recieving
        private byte[] receiveBuffer;

        //Method for the connection
        public void Connect()
        {
            //We call the client as a new client
            socket = new TcpClient
            {
                //The buffer sized recieved
                ReceiveBufferSize = dataBufferSize,
                //the buffer size we have to send
                SendBufferSize = dataBufferSize
            };
            
            //Insert new byte and overwrite
            receiveBuffer = new byte[dataBufferSize];
            //Begin connection
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        //waiting for reply
        //IAsyncResult --> Awaits for reply, we wait for result
        private void ConnectCallback(IAsyncResult _result)
        {
            //We end trying to connect to the server
            socket.EndConnect(_result);

            //If socket is not connected
            if (!socket.Connected)
            {
                //we end the whole method
                return;
            }

            //We take in the network stream
            stream = socket.GetStream();

            receivedData = new Packet();

            //we begin the streams 
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        //This is receiving packets from the server
        private void ReceiveCallback(IAsyncResult _result)
        {
            //if something happens
            try
            {
                //We stp reading the stream when receving a result
                int _byteLength = stream.EndRead(_result);
                // if there is no byte
                if (_byteLength <= 0)
                {
                    // TODO: disconnect
                    return;
                }

                //We overwrite new bytes
                byte[] _data = new byte[_byteLength];
                //Copying one array to another, Receiver buffer is copied to data and tells how big it is supposed to be
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));

                //Placing everything into IAsyncResult ;
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            //If something bad happens we disconnect
            catch
            {
                //Todo : Disconnect
            }
        }

        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;

            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                {
                    return true;
                }
            }

            while(_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                     using (Packet _packet = new Packet(_packetBytes))
                     {
                         int _packetID = _packet.ReadInt();
                         packetHandlers[_packetID](_packet);
                     }
                });

                _packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (_packetLength <= 1)
            {
                return true;
            }

            return false;
        }

    }


    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int _localPort)
        {
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        public void SendData(Packet _packet)
        {
            try
            {
                _packet.InsertInt(instance.myID);
                if (socket != null)
                {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }


        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if(_data.Length < 4)
                {
                    // TODO: disconnect
                    return;
                }

                HandleData(_data);
            }

            catch
            {
                // TODO: disconnect
            }
        }


        private void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLength = _packet.ReadInt();
                _data = _packet.ReadBytes(_packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_data))
                {
                    int _packetId = _packet.ReadInt();
                    packetHandlers[_packetId](_packet);
                }
            });
        }
    }

    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer },
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition },
            //{ (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation },
        };
        Debug.Log("Initialize packets.");
    }
}
