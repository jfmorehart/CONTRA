using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
	//Base Selectable class
	// Inherited by anything on the map that takes orders
	[Header("Unit")]

	public int team;
	public int hP;
	protected int maxHP;

	bool selected;

	public Order lastOrder;
	Renderer ren;


	public virtual void Awake() {
		maxHP = hP;
    }

	public virtual void Start()
	{
		ren = GetComponent<Renderer>();
		ren.material = new Material(ren.material);
		ren.material.color = Map.ins.stateColors[team] + Color.white * 0.5f;
	}

	public virtual void Direct(Order order) {

    }

	public virtual void Select() {
		selected = true;
		ren.material.color = Color.white;

	}
	public virtual void Deselect()
	{
		selected = false;
		ren.material.color = Map.ins.stateColors[team]  + Color.white * 0.5f;
	}

	public virtual void Hit()
	{
		hP--;
		if (hP < 1)
		{
			Destroy(gameObject);
		}
	}
	public virtual void Hit(float after) {
		Invoke(nameof(Hit), after);
    }
}
