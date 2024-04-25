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

	public List<Construction> construction_sites;

	public float econ_military_max;
	// caps military spending. guarantees some city growth if less than one

	public float manHourDebt;

	public Economics.Assesment assesment;

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
		assesment = Economics.RunAssesment(team);
		Economics.state_assesments[team] = assesment;

		if(team == 0) {
			Debug.Log(team + "  pg " + assesment.percentGrowth + "  net " + assesment.net + " uk " + assesment.upkeepCosts + " cc " + assesment.constructionCosts +  " bp " + assesment.buyingPower);
		}
		transform.position = MapUtils.CoordsToPoint(Map.ins.state_centers[team]);

		ConstructionWork();

		if (assesment.costOverrun < 0f) {
			//half as many as the budget allows for per tick
			//todo replace with mobilization decision

			//spawning too many! use military budget cap for info
			int spawnWave = Mathf.FloorToInt((-assesment.costOverrun / Economics.cost_armyUpkeep));
			if(spawnWave > 0) {
				SpawnTroops(spawnWave);
			}

		}
		else if(assesment.costOverrun > 0.1f){
			BudgetCuts(assesment.costOverrun);
		}
		

    }

	void BudgetCuts(float budgetCut) {

		//Army disbanding
		Unit[] armies = ArmyUtils.GetArmies(team);
		for (int i = 0; i < armies.Length; i++)
		{
			if(budgetCut > 0) {
				armies[i].Kill();
				budgetCut -= Economics.cost_armyUpkeep;
			}
			else {
				Debug.Log(team + " disbanded " + i + " units");
				return;
			}
		}
		if (budgetCut < 0) return;

		//Silo dereliction
		Unit[] silos = ArmyUtils.GetSilos(team);
		for (int i = 0; i < silos.Length; i++)
		{
			if (budgetCut > 0)
			{
				Debug.Log("derelict");
				GameObject go = Instantiate(InfluenceMan.ins.constructionPrefab,
					silos[i].transform.position, silos[i].transform.rotation,
					InfluenceMan.ins.transform) ;
				Construction co = go.GetComponent<Construction>();
				co.team = team;
				co.toBuild = silos[i];
				co.manHoursRemaining = Mathf.Min(budgetCut, 1);
				silos[i].Kill();
				budgetCut -= Economics.cost_siloUpkeep;
			}
			else
			{
				return;
			}
		}
	}

	protected virtual void SpawnTroops(int toSpawn) {

		City[] cities = GetCities(team).ToArray();
		int[] r = new int[cities.Length];
		for (int i = 0; i < r.Length; i++)
		{
			bool homeTurf = Map.ins.GetOriginalMap(cities[i].mpos) == team;
			r[i] = homeTurf ? Random.Range(0, 100) : Mathf.Max(Random.Range(1, 100), Random.Range(1, 100));
		}
		System.Array.Sort(r, cities);

		for (int i = 0; i < cities.Length; i++)
		{
			if (toSpawn == 0) return;

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
		}
	}

	protected virtual void ConstructionWork() {
		//todo calc work per site
		float workAmt = assesment.manHoursPerSite;
		for(int i = 0; i < construction_sites.Count; i++) {
			construction_sites[i].Work(workAmt);
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
