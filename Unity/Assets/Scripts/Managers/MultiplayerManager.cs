﻿using UnityEngine;
using System.Collections;
using PlayerIOClient;
using System.Collections.Generic;

public class MultiplayerManager : MonoBehaviour {
    #region Singleton
	static MultiplayerManager mInst;
	static public MultiplayerManager instance { get { return mInst; } }		
	void Awake () {
        if (null == mInst) {
            mInst = this;
        }
		DontDestroyOnLoad(this); 		
	}
    #endregion

    #region Attributs publics
    public string userId = "";
    public bool localhost;
    public bool developmentServer;
    public string ipDevServ = "192.168.1.3";
    public float serverRate = 0.1f;
    #endregion

    #region Attributs privés
    private Connection pioConnection;
	private List<PlayerIOClient.Message> messages = new List<PlayerIOClient.Message> ();
	private bool joinedRoom = false;
	private PlayerIOClient.Client pioClient;
    private Player player;
    #endregion

    #region Accesseurs
    public bool IsConnected {
        get { return pioConnection != null ? pioConnection.Connected : false; }
    }
    #endregion

    #region Méthodes publiques
    public void Disconnect () {
        if (!pioConnection.Connected) return;
        pioConnection.Disconnect ();
    }

    public void Disconnected (object sender, string error) {
        Debug.LogWarning ("Disconnected !");
    }

    public void SendChat (string text) {
        pioConnection.Send ("Chat", text);
    }

    public void SendStart () {
        Debug.Log ("Sending Start to Server");
        pioConnection.Send ("Start");
    }
	
    public void StartConnection () {
		string playerId = SystemInfo.deviceUniqueIdentifier;
		
		//user is just using this device with no account
		Debug.Log ("Annonymous connect : " + playerId);	
		userId = playerId;
		PlayerIOClient.PlayerIO.Connect (
            "nemesis-fortress-d6ukh1q69kgyacgrmn52pw",	// Game id 
			"public",							// The id of the connection, as given in the settings section of the admin panel. By default, a connection with id='public' is created on all games.
			playerId,							// The id of the user connecting. 
			null,								// If the connection identified by the connection id only accepts authenticated requests, the auth value generated based on UserId is added here
			null,
			null,				
			delegate(Client client) { 
			    SuccessfullConnect (client);
		    },
		    delegate(PlayerIOError error) {
                Debug.Log ("Error connecting: " + error.ToString ());
		    }
		);
	}
    #endregion

    #region Méthode privées
    void FixedUpdate () {
        // Process message queue
        foreach (PlayerIOClient.Message message in messages) {
            //Debug.Log (Time.time + " - Message received from server " + message.ToString ());
            switch (message.Type) {
                // Basic connection / deconnection

                // Lobby Messages
                case "PlayerJoined":
                    Debug.Log ("PlayerJoined : " + message.GetString (0));
                    break;
                case "PlayerLeft":
                    Debug.Log ("PlayerLeft : " + message.GetString (0));
                    break;
                case "Chat":
                    Debug.Log (message.GetString (0) + ":" + message.GetString (1));
                    break;
                case "Position":
                    break;
                case "Enemy Spawn":
                    Debug.Log ("Spawn " + message.GetString (0) + " enemy (" + message.GetString (1) + ", y, " + " enemy (" + message.GetString (2) + ")");
                    break;
            }
        }

        // clear message queue after it's been processed
        messages.Clear ();
    }

    void HandleMessage (object sender, PlayerIOClient.Message message) {
        messages.Add (message);
    }

    void JoinGameRoom (string roomId) {
        pioClient.Multiplayer.CreateJoinRoom (
            roomId,				//Room is the Alliance of the player 
            "RoomType",							//The room type started on the server
            false,									//Should the room be visible in the lobby?
            null,
            null,
            delegate (Connection connection) {
                Debug.Log ("Joined Room : " + roomId);
                // We successfully joined a room so set up the message handler
                pioConnection = connection;
                pioConnection.OnMessage += HandleMessage;
                pioConnection.OnDisconnect += Disconnected;
                joinedRoom = true;
            },
            delegate (PlayerIOError error) {
                Debug.LogError ("Error Joining Room: " + error.ToString ());
            }
        );
    }

    void OnLevelWasLoaded (int level) {
        if (Application.loadedLevelName.Equals ("Level")) {

        }
    }

    void Start () {
        player = GameObject.Find ("Player").GetComponent<Player> ();
        StartConnection ();
        StartCoroutine ("UpdateServer");
    }
	
    void SuccessfullConnect (Client client) {
        Debug.Log ("Successfully connected to Player.IO");

        if (developmentServer) {
            client.Multiplayer.DevelopmentServer = new ServerEndpoint (System.String.IsNullOrEmpty (ipDevServ) ? "192.168.1.96" : ipDevServ, 8184);
        }

        if (localhost) {
            client.Multiplayer.DevelopmentServer = new ServerEndpoint ("127.0.0.1", 8184);
        }

        // Create or join the room	
        string roomId = "Fortress";
        if (string.IsNullOrEmpty (roomId)) {
            roomId = userId;
        }

        client.Multiplayer.CreateJoinRoom (
            roomId,	//Room is the Alliance of the player 
            "NemesisFortress",					//The room type started on the server
            false,									//Should the room be visible in the lobby?
            null,
            null,
            delegate (Connection connection) {
                Debug.Log ("Joined Room : " + roomId);
                // We successfully joined a room so set up the message handler
                pioConnection = connection;
                pioConnection.OnMessage += HandleMessage;
                pioConnection.OnDisconnect += Disconnected;
                joinedRoom = true;
            },
            delegate (PlayerIOError error) {
                Debug.LogError ("Error Joining Room: " + error.ToString ());
            }
        );

        pioClient = client;
    }
    
    IEnumerator UpdateServer () {
        do {
            // Envoi des coordonnées du joueur
            //pioConnection.Send ("Position", player.transform.position.x, player.transform.position.y, player.transform.position.z, player.transform.rotation.x, player.transform.rotation.y, player.transform.rotation.z);
            yield return new WaitForSeconds (serverRate);
        } while (true);
    }
    #endregion
}
