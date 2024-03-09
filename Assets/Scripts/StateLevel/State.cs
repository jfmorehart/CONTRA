using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArmyUtils;

public class State : MonoBehaviour
{
	// this base class represents a country, and is inherited by State_AI which
	// adds on more advanced functionality. in reality there doesn't need to be 
	// a seperation, but State_AI was initially intended to be purely for NPCs, 
	// and was later made the base class for both the player and the NPCS due to 
	// the redesign to make gameplay less micromanagey.

	public int team;

	public float money; //unused so far

	// the Grid location of the middle of the state
	//really only used in generation but could be nice to have
	public Vector2Int origin;

	protected virtual void Awake()
	{
		LaunchDetection.launchDetectedAction += LaunchDetect;
	}

	public virtual void Setup(int i, Vector2Int pos) {
		//Called a few ms after start
		team = i;
		origin = pos;
		Diplo.RegisterState(this);
		Invoke(nameof(StateUpdate), 0.08f);
		InvokeRepeating(nameof(StateUpdate), i * 0.1f, 5);
    }

	protected virtual void StateUpdate() {
		//called ever 5 seconds
		//this function 
		transform.position = MapUtils.CoordsToPoint(Map.ins.state_centers[team]);
		SpawnTroops();
    }

	protected virtual void SpawnTroops() {

		City[] cities = GetCities(team).ToArray();
		int[] r = new int[cities.Length];
		for (int i = 0; i < r.Length; i++)
		{
			r[i] = Random.Range(0, 100);
		}
		System.Array.Sort(r, cities);

		Unit[] armies = GetArmies(team);
		int pop = (int)Map.ins.state_populations[team];
		if (pop < 1) return;
		int toSpawn = pop - armies.Length;
		if (toSpawn < 1)
		{
			Debug.Log("kill");
			int index = Random.Range(0, armies.Length - 1);
			armies[index].Kill();
			return;
		}
		for (int i = 0; i < cities.Length; i++)
		{
			if (toSpawn == 0) return;
			bool homeTurf = Map.ins.GetOriginalMap(cities[i].mpos) == team;
			if (!homeTurf) Debug.Log(team);
			float amtDown = Mathf.Pow((toSpawn / (float)pop), 2f);
			bool spin = amtDown * (cities[i].truepop / 50f) * (homeTurf ? 10 : 0.5f) >  Random.value;
			if (!spin) continue;
			toSpawn--;
			Vector3 p = Random.insideUnitCircle * Random.Range(10, 50);
			p += cities[i].transform.position;
			if (p.x > Map.ins.transform.localScale.x - 5)
			{
				p.x = Map.ins.transform.localScale.x - 5;
			}
			if (p.y > Map.ins.transform.localScale.y - 5)
			{
				p.y = Map.ins.transform.localScale.y - 5;
			}
			if (p.x < 5)
			{
				p.x = 5;
			}
			if (p.y < 5)
			{
				p.y = 5;
			}

			InfluenceMan.ins.PlaceArmy(p);
			Debug.Log("placed " + team);
		}
	}

	public virtual void WarStarted(int by)
	{

	}

	public virtual void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim) { 
    
    }
		
	public virtual void ReadyForOrders(Unit un) {

    }
}
