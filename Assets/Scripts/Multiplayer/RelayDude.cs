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
    public string joinCode;

    NetworkManager nm;

    List<ulong> clients;

    public GameObject mv;

	private void Awake()
	{
        ins = this;
        clients = new();
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
        if (localTest)
        {
			NetworkManager.Singleton.StartClient();
			return;
		}
        JoinRelay(code);
    }
    public void StartGame()
    {
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
    }
    private async void CreateRelay()
    {
        try
        {
			Allocation alloc = await RelayService.Instance.CreateAllocationAsync(1);

            joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log("joingcode = " + joinCode);
            TypingInterface.ins.WriteOut("joincode= " + joinCode.ToLower());

            RelayServerData relayServerData = new RelayServerData(alloc, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

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

    
    private async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation jalloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

			RelayServerData relayServerData = new RelayServerData(jalloc, "dtls");
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
			TypingInterface.ins.WriteOut("connected to server");
			TypingInterface.ins.WriteOut("waiting for host...");
		}
		catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public void ClientConnected(ulong id) {
        if (clients.Contains(id)) return;
        if (id == NetworkManager.ServerClientId) return;
        clients.Add(id);
        Debug.Log("client connected!");
        TypingInterface.ins.WriteOut("new client connected.");
		TypingInterface.ins.WriteOut("players = " + nm.ConnectedClientsList.Count);
		TypingInterface.ins.WriteOut("type 'start' when ready");
	}

    //public void
}

