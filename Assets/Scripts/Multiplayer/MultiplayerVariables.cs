using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerVariables : NetworkBehaviour
{
	public static MultiplayerVariables ins;
    private NetworkVariable<int> _mapSeed = new NetworkVariable<int>();
	public int MapSeed { get => _mapSeed.Value; set => _mapSeed.Value = value; }

	private NetworkVariable<int> _numPlayers = new NetworkVariable<int>();
	public int NumPlayers { get => _numPlayers.Value; set => _numPlayers.Value = value; }
	public ulong[] clientIDs;

	public override void OnNetworkSpawn()
	{
		ins = this;
		DontDestroyOnLoad(gameObject);

		if (!NetworkManager.IsHost) return;
		_mapSeed.Value = UnityEngine.Random.Range(-5000, 5000);
	}


	public static float Ran(Vector2 uv) {
		float f = (Mathf.Sin(Vector2.Dot(uv, new Vector2(12.9898f, 78.233f))) * 43758.5453123f);
		return f - Mathf.FloorToInt(f);
	}

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
	[ServerRpc(RequireOwnership = false)]
	public void SpawnArmyServerRPC(Vector2 position) {
		ArmyManager.ins.PlaceArmy(position);
	}
	[ServerRpc(RequireOwnership = false)]
	public void KillObjServerRPC(ulong owner, ulong obj_id)
	{
		NetworkObject kill = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj_id];
		if(kill.TryGetComponent(out Unit un)) {
			un.Kill();
		}
		Debug.Log("calling client rpc killing " + kill.gameObject.name);
		KillObjClientRPC(owner, obj_id);
	}
	[ClientRpc]
	public void KillObjClientRPC(ulong owner, ulong obj_id)
	{
		if (NetworkManager.Singleton.IsServer) return;
		if (NetworkManager.LocalClientId == owner) return; //already killed it locally

		NetworkObject kill = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj_id];
		if (kill.TryGetComponent(out Unit un))
		{
			un.Kill();
			Debug.Log("client rpc killing " + un.name);
		}
	}
}
