using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArmyUtils;

public class State : MonoBehaviour
{
	// this base class represents a country, and contains a lot of the basic 
	// levers of power that a state has: spending, troop recruitment, etc.

	// it is inherited by State_AI, which expands on the framework to do 
	// more advanced controls that in a normal RTS would be handled by the
	// player— but in this more indirect RTS, are abstracted back to the machine

	//0 through numStates
	public int team;

	//We tell the sites how quickly to build themselves
	public List<Construction> construction_sites;

	// caps military spending. guarantees some city growth if less than one
	public float econ_military_max;

	//used for hamstringing the economy
	public float manHourDebt;

	//general economic information from the last StateUpdate tick
	public Economics.Assesment assesment;

	// the Grid location of the middle of the state
	// NOTE this is really only for generation:
	// it is not guaranteed to even lie within the State!
	public Vector2Int origin;

	protected virtual void Awake()
	{
		LaunchDetection.launchDetectedAction += LaunchDetect;
		DisplayHandler.resetGame += Reset;
	}

	protected virtual void Reset() {
		DisplayHandler.resetGame -= Reset;
		LaunchDetection.launchDetectedAction -= LaunchDetect;
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
		assesment = Economics.RunAssesment(team);
		Economics.state_assesments[team] = assesment;

		if(team == 0) {
			Debug.Log(team + "  pg " + assesment.percentGrowth + "  net " + assesment.net + " uk " + assesment.upkeepCosts + " cc " + assesment.constructionCosts +  " bp " + assesment.buyingPower);
		}
		transform.position = MapUtils.CoordsToPoint(Map.ins.state_centers[team]);

		ConstructionWork();

		//delete all troops if you have no cities left
		if (Map.ins.state_populations[team] < 2) {
			DisbandTroops(100);
		}
		
    }

	protected virtual void BalanceBudget (float budgetCut) {

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
	public void DisbandTroops(int toDisband) {
		Unit[] armies = ArmyUtils.GetArmies(team);
		int disbanded = 0;
		for (int i = 0; i < armies.Length; i++)
		{
			armies[i].Kill();
			disbanded++;
			if (disbanded > toDisband) return;
		}
	}

	public void SpawnTroops(int toSpawn) {

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
			if (toSpawn == 0) {
				if(team == 0)ConsolePanel.Log("conscripting additional men");
				return;
			}

			toSpawn--;
			if(manHourDebt > assesment.buyingPower * 2) {
				//Debug.Log("we dont have the funds to train more troops right now");
				if(team == 0) {
					ConsolePanel.Log("insufficient funding to train new troops");
				}
				return; //do not let us get too far into debt
			}
			manHourDebt += Economics.cost_armySpawn;
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
