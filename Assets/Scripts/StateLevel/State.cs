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
	public bool alive = true;

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
		LaunchDetection.strikeDetectedAction += StrikeDetect;
		Diplomacy.News += ReceiveNews;
		DisplayHandler.resetGame += Reset;
	}

	protected virtual void Reset() {
		DisplayHandler.resetGame -= Reset;
		LaunchDetection.launchDetectedAction -= LaunchDetect;
		Diplomacy.News -= ReceiveNews;
	}

	public virtual void Setup(int i, Vector2Int pos) {
		//Called a few ms after start
		team = i;
		origin = pos;
		Diplomacy.RegisterState(this);
		Invoke(nameof(StateUpdate), 0.08f);
		InvokeRepeating(nameof(StateUpdate), i * 0.1f, 1f);
    }

	protected virtual void StateUpdate() {
		if (!alive) return;

		//called ever 5 seconds
		assesment = Economics.RunAssesment(team);
		Economics.state_assesments[team] = assesment;

		//if(team == 0) {
		//	Debug.Log(team + "  pg " + assesment.percentGrowth + "  net " + assesment.net + " uk " + assesment.upkeepCosts + " cc " + assesment.constructionCosts +  " bp " + assesment.buyingPower);
		//}
		transform.position = MapUtils.CoordsToPoint(Map.ins.state_centers[team]);

		ConstructionWork();

		//delete all troops if you have no cities left
		if (Map.ins.state_populations[team] < 5 || (GetCities(team).Count < 1)) {
			KillState();
		}
		if (ArmyUtils.conventionalCount[team] > Map.ins.state_populations[team] + 3) {
			int diff = ArmyUtils.conventionalCount[team] - (int)Map.ins.state_populations[team];
			BalanceBudget(Economics.cost_armyUpkeep * diff);
		}
    }

	protected virtual void BalanceBudget (float budgetCut) {

		//Army disbanding
		Unit[] armies = ArmyUtils.GetArmies(team);
		for (int i = 0; i < armies.Length; i++)
		{
			if(budgetCut > 0) {
				armies[i].Kill();
				budgetCut -= armies[i].upkeepCost;
			}
			else {
				//Debug.Log(team + " disbanded " + i + " units");
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
				GameObject go = Instantiate(ArmyManager.ins.constructionPrefab,
					silos[i].transform.position, silos[i].transform.rotation,
					ArmyManager.ins.transform) ;
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
			manHourDebt -= Economics.cost_armySpawn * 0.5f;
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
				if(team == 0)ConsolePanel.Log("conscripting additional men", 5);
				return;
			}

			toSpawn--;
			if(manHourDebt > assesment.buyingPower * 2) {
				//Debug.Log("we dont have the funds to train more troops right now");
				if(team == 0) {
					ConsolePanel.Log("insufficient funding to train new troops", 5);
				}
				return; //do not let us get too far into debt
			}
			if (conventionalCount[team] > Map.ins.state_populations[team])
			{
				if (team == 0)
				{
					ConsolePanel.Log("insufficient population to conscript new troops");
				}
				return; //do not let us recruit more than entire population
			}

			//occupied cities are much more expensive to recruit new troops in
			int originalTeam = Map.ins.GetOriginalMap(cities[i].mpos);
			bool hometurf = originalTeam == team;
			hometurf = hometurf && (Map.ins.state_populations[originalTeam] > 1);
			manHourDebt += hometurf ? Economics.cost_armySpawn : Economics.cost_armySpawn * 5;

			//ensure that we dont spawn the troops in the wrong country;
			Vector3 spawnPos = Random.insideUnitCircle * Random.Range(10, 50);
			if(Map.ins.GetPixTeam(MapUtils.PointToCoords(spawnPos)) != team) {
				//remove offset
				spawnPos = Vector3.zero;
			}
			spawnPos += cities[i].transform.position;

			if (spawnPos.x > Map.ins.transform.localScale.x - 5)
			{
				spawnPos.x = Map.ins.transform.localScale.x - 5;
			}
			if (spawnPos.y > Map.ins.transform.localScale.y - 5)
			{
				spawnPos.y = Map.ins.transform.localScale.y - 5;
			}
			if (spawnPos.x < 5)
			{
				spawnPos.x = 5;
			}
			if (spawnPos.y < 5)
			{
				spawnPos.y = 5;
			}

			ArmyManager.ins.PlaceArmy(spawnPos);
		}
	}

	protected virtual void ConstructionWork() {
		//todo calc work per site
		float workAmt = assesment.manHoursPerSite;
		for(int i = 0; i < construction_sites.Count; i++) {
			construction_sites[i].Work(workAmt);
		}
    }

	protected void KillState() {
		Unit[] silos = ArmyUtils.GetSilos(team);
		for (int i = 0; i < silos.Length; i++)
		{
			silos[i].Kill();
		}
		for(int i = 0; i < construction_sites.Count; i++) {
			construction_sites[i].Kill();
		}
		DisbandTroops(300);
		if(team == 0) {
			ConsolePanel.Log(ConsolePanel.ColoredName(team) + " have collapsed");
		}
		else {
			ConsolePanel.Log(ConsolePanel.ColoredName(team) + " has collapsed");
		}

		alive = false;
	}


	public virtual void WarStarted(int by)
	{

	}

	public virtual void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim) {
		//this action is per-missile, so its used for tactical stuff
    }

	public virtual void StrikeDetect(int perp, int victim, bool provoked) { 
		//this action is per-strike, so its more about strategy
    }
		
	public virtual void ReadyForOrders(Unit un) {

    }

	public virtual void SendAid(int to)
	{
		ConsolePanel.Log(ConsolePanel.ColoredName(team) + " sent aid to " + ConsolePanel.ColoredName(to)); ;
		Diplomacy.states[team].manHourDebt += Economics.cost_armySpawn * 3;
		Diplomacy.states[to].manHourDebt -= Economics.cost_armySpawn * 5;
		Diplomacy.states[to].RecieveAid(team);

		Diplomacy.AnnounceNews(Diplomacy.NewsItem.Aid, team, to);
	}

	public virtual void RecieveAid(int from) { 
		
    }

	public virtual void ReceiveNews(Diplomacy.NewsItem news, int t1, int t2) { 
		//react to international news
    }
}
