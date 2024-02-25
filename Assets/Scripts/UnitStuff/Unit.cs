using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour
{
	//Base Selectable class
	// Inherited by anything on the map that takes orders
	[Header("Unit")]

	public int team;
	public int hP;
	public int positionChunk; //used for efficiently knowing where units are

	protected int maxHP;

	protected Renderer ren;


	public virtual void Awake() {
		maxHP = hP;
		Vector2Int pt = MapUtils.PointToCoords(transform.position);
		team = Map.ins.GetPixTeam(pt);
		gameObject.name = team.ToString() + Random.Range(0, 10000).ToString();
		positionChunk = UnitChunks.ChunkLookup(transform.position);
		UnitChunks.AddToChunk(positionChunk, this);
	}

	public virtual void Start()
	{
		InfluenceMan.ins.RegisterUnit(this);
		ren = GetComponent<Renderer>();
		ren.material = new Material(ren.material);
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

	public virtual void Kill() {
		InfluenceMan.ins.DeregisterUnit(this);
		UnitChunks.RemoveFromChunk(positionChunk, this);
		Destroy(gameObject);
	}
}
