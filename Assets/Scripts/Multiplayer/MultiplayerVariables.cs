using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerVariables : NetworkBehaviour
{
	public static MultiplayerVariables ins;
    private NetworkVariable<int> _mapSeed = new NetworkVariable<int>();
	public int MapSeed { get => _mapSeed.Value; set => _mapSeed.Value = value; }

	private NetworkVariable<int> _numPlayers = new NetworkVariable<int>();
	public int NumPlayers { get => _numPlayers.Value; set => _numPlayers.Value = value; }
	public ulong[] clientIDs;
	public Dictionary<ulong, string> playerNames;
	public string localName;

	public override void OnNetworkSpawn()
	{
		ins = this;
		DontDestroyOnLoad(gameObject);

		string name = PlayerPrefs.GetString("defaultName");
		if (name == "")
		{
			TypingInterface.interfaceInstance.WriteOut("name = ");
			TypingInterface.interfaceInstance.WriteOut("for a custom name, write 'name = [yourname]'");
		}
		else
		{
			TypingInterface.interfaceInstance.WriteOut("name = " + name);
		}
		if (!NetworkManager.IsHost) return;
		_mapSeed.Value = UnityEngine.Random.Range(1, 5000);
	}


	public static float Ran(Vector2 uv) {
		float f = (Mathf.Sin(Vector2.Dot(uv, new Vector2(12.9898f, 78.233f))) * 43758.5453123f);
		return f - Mathf.FloorToInt(f);
	}

	public int TeamFromUlong(ulong networkteam) {
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (clientIDs[i] == networkteam)
			{
				return i;
			}
		}
		Debug.LogError("invalid networkteam " + networkteam);
		return -1;
	}

	//Housekeeping

	[ClientRpc]
	public void ShareClientIDsClientRPC(ulong[] cl_ids) {
		Debug.Log("syncronized client ids");
		clientIDs = cl_ids;
		for(int i = 0; i < cl_ids.Length; i++) {
			if (cl_ids[i] == NetworkManager.Singleton.LocalClientId) {
				Map.localTeam = i;
				Debug.Log("local team is now " + i);
			}
		}
    }
	public void UpdatePlayerNames() {
		playerNames = new Dictionary<ulong, string>();
		RetrieveNamesClientRPC();
    }
	[ClientRpc]
    public void RetrieveNamesClientRPC() {
		if(localName == "") {
			localName = PlayerPrefs.GetString("defaultName");
		}
		if (localName == "") return;
		UpdateStateNameServerRPC(NetworkManager.Singleton.LocalClientId, localName);
    }
	[ServerRpc(RequireOwnership = false)]
	public void UpdateStateNameServerRPC(ulong clientID, string name) {
		if(playerNames == null) playerNames = new Dictionary<ulong, string>();
		if (playerNames.ContainsKey(clientID)) {
			playerNames.Remove(clientID);
		}
		playerNames.Add(clientID, name);
		ShareNameClientRPC(clientID, name);
    }
	[ClientRpc]
	public void ShareNameClientRPC(ulong clientID, string name) {
		if (clientID == NetworkManager.LocalClientId) return;
		if (playerNames == null) playerNames = new Dictionary<ulong, string>();
		if (playerNames.ContainsKey(clientID))
		{
			playerNames.Remove(clientID);
		}
		playerNames.Add(clientID, name);
	}

	// Spawning and Destroying
	#region SpawningRPCS
	[ServerRpc(RequireOwnership = false)]
	public void SpawnArmyServerRPC(Vector2 position) {
		ArmyManager.ins.PlaceArmy(position);
	}
	[ServerRpc(RequireOwnership = false)]
	public void PlaceBuildingServerRPC(Vector2 position, int btype)
	{
		GameObject go = Instantiate(ArmyManager.ins.buildPrefabs[btype], position, Quaternion.identity);
		go.GetComponent<NetworkObject>().Spawn();
	}
	[ServerRpc(RequireOwnership = false)]
	public void NewConstructionServerRPC(Vector2Int mapPos, int btype) {
		//this spawns a construction icon

		int team = Map.ins.GetPixTeam(mapPos);
		Unit t = ArmyManager.ins.NewConstruction(team, mapPos, (ArmyManager.BuildingType)btype);
		ulong id = t.GetComponent<NetworkObject>().NetworkObjectId;

		NewConstructionClientRPC(id, btype); //tells clients to set it up
	}
	[ClientRpc]
	public void NewConstructionClientRPC(ulong obj_id, int btype)
	{
		NetworkObject init = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj_id];
		Construction co = init.GetComponent<Construction>();
		co.PrepareBuild((ArmyManager.BuildingType)btype);
	}

	//I now realize that the design pattern I used for these RPCs is largely terrible
	//They interrupt regular functions to move the call to the server, who turns around and calls the 
	// function again.... this would lead to an infinite recursion of RPCs...
	//remote procedure resonance cascade
	[ServerRpc(RequireOwnership = false)]
	public void KillObjServerRPC(ulong owner, ulong obj_id)
	{
		Debug.Log(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count);
		if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(obj_id, out NetworkObject kill))
		{
			if (kill.TryGetComponent(out Unit un))
			{
				un.Kill(true);
			}
			KillObjClientRPC(owner, obj_id);
			kill.Despawn();
		}

	}
	[ClientRpc]
	public void KillObjClientRPC(ulong owner, ulong obj_id)
	{
		if (NetworkManager.Singleton.IsServer) return;
		if (NetworkManager.LocalClientId == owner) return; //already killed it locally

		NetworkObject kill = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj_id];
		if (kill == null) return;
		if (kill.TryGetComponent(out Unit un))
		{
			un.Kill(true);
		}
	}
	#endregion

	//Diplomacy
	#region Diplomacy
	[ServerRpc(RequireOwnership = false)]
	public void DeclareWarServerRPC(ulong team1, ulong team2, ulong sender) {
		DeclareWarClientRPC(team1, team2, sender);
	}
	[ClientRpc] 
	public void DeclareWarClientRPC(ulong team1, ulong team2, ulong sender) {
		if (sender == NetworkManager.Singleton.LocalClientId) return;// we sent this, dont duplicate
		int t1 = -1;
		int t2 = -1;
		for (int i = 0; i < Map.ins.numStates; i++) {
			if (clientIDs[i] == team1) {
				t1 = i;
			}
			if (clientIDs[i] == team2)
			{
				t2 = i;
			}
		}
		ROE.DeclareWar(t1, t2);
    }
	[ServerRpc(RequireOwnership = false)]
	public void OfferPeaceServerRPC(ulong team1, ulong team2, ulong sender)
	{
		OfferPeaceClientRPC(team1, team2, sender);
	}
	[ClientRpc]
	public void OfferPeaceClientRPC(ulong team1, ulong team2, ulong sender)
	{
		if (sender == NetworkManager.Singleton.LocalClientId) return;// we sent this, dont duplicate
		int t1 = -1;
		int t2 = -1;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (clientIDs[i] == team1)
			{
				t1 = i;
			}
			if (clientIDs[i] == team2)
			{
				t2 = i;
			}
		}
		Diplomacy.OfferPeace(t1, t2);
	}
	[ServerRpc(RequireOwnership = false)]
	public void SendAidServerRPC(ulong team1, ulong team2, ulong sender)
	{
		SendAidClientRPC(team1, team2, sender);
	}
	[ClientRpc]
	public void SendAidClientRPC(ulong team1, ulong team2, ulong sender)
	{
		if (sender == NetworkManager.Singleton.LocalClientId) return;// we sent this, dont duplicate
		int t1 = -1;
		int t2 = -1;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (clientIDs[i] == team1)
			{
				t1 = i;
			}
			if (clientIDs[i] == team2)
			{
				t2 = i;
			}
		}
		Diplomacy.states[t1].SendAid(t2);
	}
	[ServerRpc(RequireOwnership = false)]
	public void StrikeDetectServerRPC(ulong team1, ulong team2, ulong sender)
	{
		StrikeDetectClientRPC(team1, team2,sender);
	}
	[ClientRpc]
	public void StrikeDetectClientRPC(ulong team1, ulong team2, ulong sender)
	{
		if (sender == NetworkManager.Singleton.LocalClientId) return;// we sent this, dont duplicate
		int t1 = -1;
		int t2 = -1;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (clientIDs[i] == team1)
			{
				t1 = i;
			}
			if (clientIDs[i] == team2)
			{
				t2 = i;
			}
		}
		LaunchDetection.StrikeDetected(t1, t2);
	}
	[ServerRpc(RequireOwnership = false)]
	public void SalvoServerRPC(ulong obj_id, Vector2 target, int warheads, ulong sender)
	{
		SalvoClientRPC(obj_id, target, warheads, sender);
	}
	[ClientRpc]
	public void SalvoClientRPC(ulong obj_id, Vector2 target, int warheads, ulong sender)
	{
		if (sender == NetworkManager.Singleton.LocalClientId) return;// we sent this, dont duplicate
		NetworkObject no = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj_id];
		Silo si = no.GetComponent<Silo>();
		ArmyUtils.Salvo(si, new Order(Order.Type.Attack, target), warheads);
	}
	#endregion

	#region events
	[ServerRpc(RequireOwnership = false)]
	public void DropBombServerRPC(int team, Vector2 pos, float radius, ServerRpcParams pars = default)
	{
		Debug.Log("recieved bomb server rpc");
		ulong id = pars.Receive.SenderClientId;
		StartCoroutine(nameof(DropWithDelay), (pos, radius, team));
		DropBombClientRPC(id, team, pos, radius);
	}
	[ClientRpc]
	public void DropBombClientRPC(ulong original, int team, Vector2 pos, float radius)
	{
		Debug.Log("recieved bomb client rpc");
		if (original == NetworkManager.Singleton.LocalClientId) return;
		StartCoroutine(nameof(DropWithDelay), (pos, radius, team));
	}
	IEnumerator DropWithDelay((Vector2, float, int) t1) {
		Debug.Log("drop delay called");
		yield return new WaitForSeconds(1);
		Map.ins.Detonate(t1.Item1, t1.Item2, t1.Item3);
	}
	[ClientRpc]
	public void ATAMDetonateClientRPC(Vector2 pos, int team)
	{
		Debug.Log("got detonate call");
		if (NetworkManager.Singleton.LocalClientId == 0) return;//dont double nuke
		Map.ins.Detonate(pos, 0.5f, team);
	}
	[ClientRpc] 
	public void FireballDownClientRPC(ulong victimTeam, Vector2 dest) {
		int team = TeamFromUlong(victimTeam);

		if(team == -1) {
			Debug.LogError("invalid fireballdown team");
			return;
		}
		Missile fireball = null;
		foreach(Missile m in TerminalMissileRegistry.registry[team]) { 
			if(Vector2.Distance(m.en, dest) < 5) {
				fireball = m;
				break;
			}
		}
		if (fireball == null)
		{
			Debug.LogError("invalid fireball");
			return;
		}
		fireball.Toggle(false);
	}
	[ServerRpc(RequireOwnership = false)]
	public void ResearchUpdateServerRPC(ulong networkteam, int[] research)
	{
		Debug.Log("server research updated");
		int team = TeamFromUlong(networkteam);
		Research.unlockedUpgrades[team] = research;
		Research.ResearchChange[team]?.Invoke();
		ResearchUpdateClientRPC(networkteam, research);
	}
	[ClientRpc]
	public void ResearchUpdateClientRPC(ulong networkteam, int[] research) {
		if (networkteam == NetworkManager.LocalClientId) return;
		Debug.Log("client research updated");
		int team = TeamFromUlong(networkteam);
		Research.unlockedUpgrades[team] = research;
		Research.ResearchChange[team]?.Invoke();
	}
	[ClientRpc]
	public void EndGameClientRPC()
	{
		DisplayHandler.resetGame?.Invoke();
		NetworkManager.Singleton.Shutdown();
		SceneManager.LoadScene("Menu");
	}
	[ServerRpc(RequireOwnership = false)]
	public void EndGameServerRPC()
	{
		EndGameClientRPC();
		NetworkManager.Singleton.Shutdown();
		SceneManager.LoadScene("Menu");
	}
	#endregion
}
