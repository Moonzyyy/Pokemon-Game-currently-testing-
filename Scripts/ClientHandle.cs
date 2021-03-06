using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    public static void Welcome(Packet _packet)
    {
        //needs to be in order with serversend welcome method, in it strings first then int
        string _msg = _packet.ReadString();
        int _myID = _packet.ReadInt();

        Debug.Log($"/Message from server: {_msg}");
        Client.instance.myID = _myID;
        ClientSend.WelcomeReceived();

        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    public static void SpawnPlayer(Packet _packet)
    {
        int _id = _packet.ReadInt();
        string _username = _packet.ReadString();
        Vector2 _position = _packet.ReadVector2();
        GameManager.instance.SpawnPlayer(_id, _username, _position);
    }

    public static void PlayerPosition(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Vector2 _position = _packet.ReadVector2();
        //player 2 is also 1 for some reason
        //GameManager.players[_id].transform.position = _position;


    }

    public static void PlayerRotation(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Quaternion _rotation = Quaternion.identity;

        GameManager.players[_id].transform.rotation = _rotation;
    }
}
