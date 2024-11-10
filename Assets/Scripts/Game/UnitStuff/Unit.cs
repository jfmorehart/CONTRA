using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : NetworkBehaviour
{
	//Base Selectable class
	// Inherited by anything on the map that takes orders
	[Header("Unit")]

	public int team;
	public int actingTeam;
	public int hP;
	public int positionChunk; //used for efficiently knowing where units are

	public int constructionCost;

	public int baseUpkeepCost;

	[HideInInspector]
	public int upkeepCost;

	protected int maxHP;
	protected Renderer ren;

	[HideInInspector]
	public int id;

	public NetworkObject no;

	protected bool useChunkSystem = true;

	public virtual void Awake() {
		maxHP = hP;
		id = Random.Range(0, 10000);
	}
	
	public override void OnNetworkSpawn() {
		base.OnNetworkSpawn();
		no = GetComponent<NetworkObject>();
    }

	public virtual void Start()
	{
		Vector2Int pt = MapUtils.PointToCoords(transform.position);
		team = Map.ins.GetPixTeam(pt);
		gameObject.name = team.ToString() + id.ToString();
		actingTeam = team;
		if (useChunkSystem)
		{
			positionChunk = UnitChunks.ChunkLookup(transform.position);
			UnitChunks.AddToChunk(positionChunk, this);
		}

		ArmyManager.ins.RegisterUnit(this);

		if (Map.multi && Map.host && IsOwner)
		{
			NetworkObject no = GetComponent<NetworkObject>();
			if(no.OwnerClientId != MultiplayerVariables.ins.clientIDs[team]) {
				no.ChangeOwnership(MultiplayerVariables.ins.clientIDs[team]);
			}
		}

		upkeepCost = baseUpkeepCost;
		ren = GetComponent<Renderer>();
		//ren.material = new Material(ren.material);
		Pool.ins.GetRingEffect().Spawn(transform.position);
		Research.ResearchChange[team] += ApplyUpgrades;
		ApplyUpgrades();
		//ren.material.color = Map.ins.state_colors[team] + Color.white * 0.5f;
	}
	public void Check4ChunkUpdate() {
		int tch = UnitChunks.ChunkLookup(transform.position);
		if(positionChunk != tch) {
			UnitChunks.RemoveFromChunk(positionChunk, this);
			positionChunk = tch;
			UnitChunks.AddToChunk(positionChunk, this);
		}
	}

	public virtual void Direct(Order order) {

    }

	public virtual void Select() {
		//ren.material.color = Color.white;

	}
	public virtual void Deselect()
	{
		//ren.material.color = Map.ins.state_colors[team]  + Color.white * 0.5f;
	}

	public virtual void Hit()
	{
		hP--;
		if (hP < 1)
		{
			Kill();
		}
	}
	public virtual void Hit(float after) {
		Invoke(nameof(Hit), after);
    }

	public virtual void Kill(bool multiplayerOverride = false) {
		ArmyManager.ins.DeregisterUnit(this);
		Research.ResearchChange[team] -= ApplyUpgrades;

		if (useChunkSystem) {
			UnitChunks.RemoveFromChunk(positionChunk, this);
		}

		if (Map.multi && !multiplayerOverride) {
			if (Map.host)
			{
				MultiplayerVariables.ins.KillObjClientRPC(NetworkManager.Singleton.LocalClientId, no.NetworkObjectId);
				no.Despawn(true);
			}
			else {
				if (!IsOwner) {
					Debug.Log("killing something that doesn't belong to me");
				}
				MultiplayerVariables.ins.KillObjServerRPC(NetworkManager.Singleton.LocalClientId, no.NetworkObjectId);
			}
		}
		else {
			Destroy(gameObject);
		}
	}

	//only for use in sensitive units like buildings
	protected void GroundCheck()
	{
		if (Map.ins.GetPixTeam(MapUtils.PointToCoords(transform.position)) != team)
		{
			Kill();
		}
	}

	public virtual void ApplyUpgrades() { 
    
    }
}
