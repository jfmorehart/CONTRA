using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State : MonoBehaviour
{
	public int team;
	public float money;
	public Vector2Int origin;

	int warScope = 6;
	List<City> attacked;

	int armySize; 
    List<Unit> recentlyOrdered;


	protected virtual void Awake()
	{

		recentlyOrdered = new List<Unit>();
		attacked = new List<City>();
	}

	public void Setup(int i, Vector2Int pos) {
		//Called a few ms after start
		team = i;
		origin = pos;
		Diplo.RegisterState(this);
		InvokeRepeating(nameof(StateUpdate), i * 0.1f, 1);
    }

	protected virtual void StateUpdate() {
		if (!ROE.AreWeAtWar(team)) return;
		armySize = ArmyUtils.GetUnits(team).Length;
		for(int i =0; i < Map.ins.numStates; i++) {
			if (ROE.AreWeAtWar(team, i)) {
				if (team == i) continue;
				CaptureACity(i);
			}
		}
    }

	//Test
	void CaptureACity(int ofteam) {
		City toAttack = ArmyUtils.NearestCity(transform.position, ofteam, attacked);
		if (toAttack == null) return; // war over lmao
		foreach (City ci in attacked)
		{
			attacked.Add(ci);
			if (attacked.Count >= warScope)
			{
				attacked.RemoveAt(0);
			}
		}
		Unit[] units = ArmyUtils.GetUnits(team, 5, toAttack.transform.position, recentlyOrdered);
		foreach(Unit un in units) {
			recentlyOrdered.Add(un);
		}
		Vector2[] pos = ArmyUtils.Encircle(toAttack.transform.position, 20, units.Length);
		for(int i =0; i< pos.Length; i++) {
			units[i].Direct(new Order(Order.Type.MoveTo, pos[i]));
		}
    }

	public void ReadyForOrders(Unit un) {
		recentlyOrdered.Remove(un);
    }
}
