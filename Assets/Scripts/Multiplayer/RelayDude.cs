using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using static UnityEngine.UI.GridLayoutGroup;

public class RelayDude : MonoBehaviour
{
    public static RelayDude ins;
    public bool localTest;

    public TMP_InputField input;

    public bool hosting;
    public bool connected;
    public string joinCode;

    NetworkManager nm;

    List<ulong> clients;

    public GameObject mv;

	private void Awake()
	{
        ins = this;
        clients = new();
        if (UnityServices.State != ServicesInitializationState.Uninitialized) return;
        StartMultiplayer();
	}
	// Start is called before the first frame update
	public async void StartMultiplayer()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("starting...");
    }

    public void JoinGame(string code)
    {
		if (UnityServices.State == ServicesInitializationState.Uninitialized)
		{
			StartMultiplayer();
		}

		if (nm != null) {
			if(nm.IsConnectedClient || nm.IsServer) {
				nm.Shutdown();
				StartMultiplayer();
			}
		}

        if (localTest)
        {
			NetworkManager.Singleton.StartClient();
			return;
		}
        JoinRelay(code);
    }
    public void StartGame()
    {
		if (UnityServices.State == ServicesInitializationState.Uninitialized) {
			StartMultiplayer();
		}

		if(nm != null) {
			if (nm.IsConnectedClient || !nm.IsServer){
				nm.Shutdown();
			}else if (nm.IsServer) {
				TypingInterface.interfaceInstance.WriteOut("already hosting");
				TypingInterface.interfaceInstance.WriteOut("joincode= " + joinCode.ToLower());
				TypingInterface.interfaceInstance.WriteOut(" ");
			}
		}


        hosting = true;
		if (localTest)
		{
			NetworkManager.Singleton.StartHost();
			//NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            return;
		}
		CreateRelay();
        nm = GetComponent<NetworkManager>();
        nm.OnClientConnectedCallback += ClientConnected;
        nm.OnClientDisconnectCallback += ClientDisconnected;
    }
    private async void CreateRelay()
    {
        try
        {
			Allocation alloc = await RelayService.Instance.CreateAllocationAsync(8);

            joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log("joingcode = " + joinCode);
            TypingInterface.interfaceInstance.WriteOut("joincode= " + joinCode.ToLower());

            RelayServerData relayServerData = new RelayServerData(alloc, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

			NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
			NetworkManager.Singleton.StartHost();

			//NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
		}   catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

		//As above, but with extra logic to transfer orders to the new ship
		//Only runs on server
		GameObject go = Instantiate(mv);
		go.GetComponent<NetworkObject>().SpawnWithOwnership(0);
	} 
    //Trying out vim!

    
    private async void JoinRelay(string inputJoin)
    {
        if(inputJoin.Length < 6) {
			TypingInterface.interfaceInstance.WriteOut("invalid join code");
            return;
		}
        inputJoin = inputJoin[..6];
        try
        {
            JoinAllocation jalloc = await RelayService.Instance.JoinAllocationAsync(inputJoin);
			joinCode = inputJoin;
			RelayServerData relayServerData = new RelayServerData(jalloc, "dtls");
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
			TypingInterface.interfaceInstance.WriteOut("connected to server");
			TypingInterface.interfaceInstance.WriteOut("waiting for host...");
		}
		catch (RelayServiceException e)
        {
            Debug.Log(e);
            TypingInterface.interfaceInstance.WriteOut("join attempt failed");
        }
    }

    public void ClientConnected(ulong id) {
        Debug.Log("cc " + id);
        if (clients.Contains(id)) return;
        Debug.Log("new id");
        if (id == NetworkManager.ServerClientId) return;
        clients.Add(id);
        Debug.Log("client connected!");
        TypingInterface.interfaceInstance.WriteOut("new client connected.");
		TypingInterface.interfaceInstance.WriteOut("players = " + nm.ConnectedClientsList.Count);
		TypingInterface.interfaceInstance.WriteOut("type 'start' when ready");
        MultiplayerVariables.ins.UpdatePlayerNames();
	}

    public void ClientDisconnected(ulong id) {
        if (clients.Contains(id)) {
            clients.Remove(id);
			TypingInterface.interfaceInstance.WriteOut("client disconnected");
			TypingInterface.interfaceInstance.WriteOut("players = " + (nm.ConnectedClientsList.Count - 1));
			if (nm.ConnectedClientsList.Count < 2)
			{
				TypingInterface.interfaceInstance.WriteOut("waiting for players...");
			}
		}
	}


	private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
	{
        Debug.Log("approval request logged!");
		// The client identifier to be authenticated
		var clientId = request.ClientNetworkId;

		// Additional connection data defined by user code
		var connectionData = request.Payload;

		// Your approval logic determines the following values
		response.Approved = true;
		response.CreatePlayerObject = false;

		// The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
		response.PlayerPrefabHash = null;

		// Position to spawn the player object (if null it uses default of Vector3.zero)
		response.Position = Vector3.zero;

		// Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
		response.Rotation = Quaternion.identity;

		// If response.Approved is false, you can provide a message that explains the reason why via ConnectionApprovalResponse.Reason
		// On the client-side, NetworkManager.DisconnectReason will be populated with this message via DisconnectReasonMessage
		response.Reason = "Some reason for not approving the client";

		// If additional approval steps are needed, set this to true until the additional steps are complete
		// once it transitions from true to false the connection approval response will be processed.
		response.Pending = false;
	}
}

