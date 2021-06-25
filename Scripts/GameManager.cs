using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;

    public static Dictionary<int, PlayerManager> players = new Dictionary<int, PlayerManager>();

    public GameObject localPlayerPrefab;
    public GameObject playerPrefab;
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
        else if (instance != this)
        {
            //We destroy the new client
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    public void SpawnPlayer(int _id, string _username, Vector2 _position)
    {

        Debug.Log(_id + "-" + _username);
        GameObject _player;
        if (_id == Client.instance.myID)
        {
            _player = Instantiate(localPlayerPrefab, _position, Quaternion.identity);
            Debug.Log("Initiating LocalPlayer");
        }

        else
        {
            _player = Instantiate(playerPrefab, _position, Quaternion.identity);
            Debug.Log("Initiating Player");
        }

        _player.GetComponent<PlayerManager>().id = _id;
        _player.GetComponent<PlayerManager>().username = _username;
        players.Add(_id, _player.GetComponent<PlayerManager>());
    }
}
