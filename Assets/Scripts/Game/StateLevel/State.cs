using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
	[HideInInspector] public float[] recentPops;
	[HideInInspector] public float[] recentGrowths;
	[HideInInspector]public int graphCham;
	// the Grid location of the middle of the state
	// NOTE this is really only for generation:
	// it is not guaranteed to even lie within the State!
	public Vector2Int origin;

	public const float stateUpdateDelay = 1;

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
		//Called a few ms after awake
		team = i;
		origin = pos;
		recentPops = new float[RightPanel.ins.arraySizes];
		recentGrowths = new float[RightPanel.ins.arraySizes];
		Diplomacy.RegisterState(this);
		Invoke(nameof(StateUpdate), 0.08f);
		InvokeRepeating(nameof(StateUpdate), i * 0.1f, stateUpdateDelay);
    }

	protected virtual void StateUpdate() {
		if (!alive) return;

		//called ever few seconds
		assesment = Economics.RunAssesment(team);
		Economics.state_assesments[team] = assesment;
		RecordEconomyData();

		if (Simulator.tutorialOverride && team == Map.localTeam) {
			//rig the game, 2x max construction and no budget restrictions
			ConstructionWork();
			return;
		}

		if (assesment.percentGrowth < -1) {
			//lower than this number cheats the game a little
			//-1 is the max shrink rate, so we can't let them sit at -5 with a bunch of silos
			BalanceBudget(assesment.costOverrun * 0.15f);
			if (team == Map.localTeam) {
				ConsolePanel.Log("<color=\"red\"> your economy is very unstable! </color>", 5);
			}
		}
		else {
			Research.ConductResearch(team, assesment.researchBudget);

			if (assesment.percentGrowth > -0.5) {
				ConstructionWork();
			}
		}

		transform.position = MapUtils.CoordsToPoint(Map.ins.state_centers[team]);

		//delete all troops if you have no cities left
		if (Map.ins.state_populations[team] < 5 || (GetCities(team).Count < 1)) {
			KillState();
		}
		if (armies[team].Count > Map.ins.state_populations[team] + 5) {
			int diff = armies[team].Count - (int)Map.ins.state_populations[team];
			BalanceBudget(Economics.cost_armyUpkeep * (diff - 5));
		}
    }

	protected void RecordEconomyData() {
		if (graphCham >= recentPops.Length)
		{
			for (int i = 0; i < recentPops.Length - 1; i++)
			{
				recentPops[i] = recentPops[i + 1];
				recentGrowths[i] = recentGrowths[i + 1];
			}
			recentPops[^1] = Map.ins.state_populations[team];// + assesment.percentGrowth;
			recentGrowths[^1] = assesment.percentGrowth;
		}
		else
		{
			recentPops[graphCham] = Map.ins.state_populations[team];// + assesment.percentGrowth;
			recentGrowths[graphCham] = assesment.percentGrowth;
		}
		graphCham++;
		RightPanel.ins.RecordEconomy();
	}

	protected virtual void BalanceBudget (float budgetCut) {

		//Army disbanding
		Unit[] armies = ArmyUtils.GetArmies(team);
		for (int i = 0; i < armies.Length; i++)
		{
			if(budgetCut > 0) {
				armies[armies.Length - i - 1].Kill();
				budgetCut -= armies[armies.Length - i - 1].upkeepCost;
			}
			else {
				//Debug.Log(team + " disbanded " + i + " units");
				return;
			}
		}

		//construction cancellation
		for (int i = 0; i < construction_sites.Count; i++)
		{
			if (budgetCut > 0)
			{
				Construction co = construction_sites[i];
				co.Kill();
				budgetCut -= 5;
			}
			else
			{
				return;
			}
		}

		if (budgetCut < 0) return;

		Unit[] bases = ArmyUtils.GetAirbases(team);
		for (int i = 0; i < bases.Length; i++)
		{
			if (budgetCut > 0)
			{
				//Vector2Int pos = MapUtils.PointToCoords(bases[i].transform.position);
				//Unit u = ArmyManager.ins.NewConstruction(team, pos, ArmyManager.BuildingType.Airbase);
				//if(u is Construction construction) {
				//	construction.manHoursRemaining = Mathf.Min(budgetCut, construction.manHoursRemaining);
				//}
				bases[i].Kill();
				budgetCut -= Economics.cost_siloUpkeep;
			}
			else
			{
				return;
			}
		}

		//Silo dereliction
		//todo expand to all
		bases = ArmyUtils.GetSilos(team);
		for (int i = 0; i < bases.Length; i++)
		{
			if (budgetCut > 0)
			{
				//Vector2Int pos = MapUtils.PointToCoords(bases[i].transform.position);
				//Unit u = ArmyManager.ins.NewConstruction(team, pos, ArmyManager.BuildingType.Silo, true);
				//if (u is Construction construction)
				//{
				//	construction.manHoursRemaining = Mathf.Min(budgetCut, construction.manHoursRemaining);
				//}
				bases[i].Kill();
				budgetCut -= Economics.cost_siloUpkeep;
			}
			else
			{
				return;
			}
		}

		bases = ArmyUtils.GetAAAs(team);
		for (int i = 0; i < bases.Length; i++)
		{
			if (budgetCut > 0)
			{
				//Vector2Int pos = MapUtils.PointToCoords(bases[i].transform.position);
				//Unit u = ArmyManager.ins.NewConstruction(team, pos, ArmyManager.BuildingType.AAA);
				//if (u is Construction construction)
				//{
				//	construction.manHoursRemaining = Mathf.Min(budgetCut, construction.manHoursRemaining);
				//}
				bases[i].Kill();
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

		List<City> lcities = GetCities(team);
		int[] r = new int[lcities.Count];

		List<int> enemies = ROE.GetEnemies(team);
		bool onlyBorderingCities = enemies.Count > 0;


		for (int i = 0; i < r.Length; i++)
		{
			bool homeTurf = Map.ins.GetOriginalMap(lcities[i].mpos) == team;
			r[i] = homeTurf ? Random.Range(0, 100) : Mathf.Max(Random.Range(1, 100), Random.Range(1, 100));

			if (onlyBorderingCities)
			{
				bool validSpawn = false;
				foreach (int e in enemies)
				{
					//spawn in cities that can reach the enemy
					if (lcities[i].reachableCountries.Contains(e))
					{
						validSpawn = true;
						break;
					}
				}
				if (validSpawn) {
					r[i] = homeTurf ? Random.Range(0, 100) : Mathf.Max(Random.Range(1, 100), Random.Range(1, 100));
				}
				else {
					//this city cannot reach the enemy
					r[i] = 1000;
				}
			}
			else //not at war, spawn if not an island 
			{
				if (lcities[i].reachableCountries.Count > 0)
				{
					//bool homeTurf = Map.ins.GetOriginalMap(cities[i].mpos) == team;
					r[i] = homeTurf ? Random.Range(0, 100) : Mathf.Max(Random.Range(1, 100), Random.Range(1, 100));
				}
				else
				{
					//dont spawn here
					r[i] = 1000;
				}
			}

		}
		City[] cities = lcities.ToArray();
		System.Array.Sort(r, cities);

		for (int i = 0; i < cities.Length; i++)
		{
			if (r[i] > 999) {
				//no cities border the enemy;
				if (team != 0) return; //the AI shouldnt spawn anything

				//let the player do it anyhow
			}

			if (toSpawn == 0) {
				if(team == Map.localTeam)ConsolePanel.Log("conscripting additional men", 5);
				return;
			}

			toSpawn--;
			if(manHourDebt > assesment.buyingPower * 2) {
				//Debug.Log("we dont have the funds to train more troops right now");
				if(team == Map.localTeam) {
					ConsolePanel.Log("insufficient funding to train new troops", 5);
				}
				return; //do not let us get too far into debt
			}
			if (armies[team].Count > Map.ins.state_populations[team])
			{
				if (team == Map.localTeam)
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
		if (Simulator.tutorialOverride && team == Map.localTeam) {
			workAmt = Economics.maxPowerPerSite * 2;
		}

		for (int i = 0; i < construction_sites.Count; i++) {
			construction_sites[i].Work(workAmt);
		}
    }

	protected void KillState() {
		Unit[] silos = ArmyUtils.GetSilos(team);
		for (int i = 0; i < silos.Length; i++)
		{
			silos[i].Kill();
		}
		Unit[] airbases = ArmyUtils.GetAirbases(team);
		for (int i = 0; i < airbases.Length; i++)
		{
			airbases[i].Kill();
		}
		Unit[] bats = ArmyUtils.GetAAAs(team);
		for (int i = 0; i < bats.Length; i++)
		{
			bats[i].Kill();
		}
		for (int i = 0; i < construction_sites.Count; i++) {
			construction_sites[i].Kill();
		}
		DisbandTroops(300);

		alive = false;

		if(team == Map.localTeam) {
			TimePanel.ins.EndGame();
		}
		//wait for buildinfluences
		StartCoroutine(AfterInfluences());

		if (Time.timeSinceLevelLoad < 1f) return;

		if (team == Map.localTeam)
		{
			ConsolePanel.Log(ConsolePanel.ColoredName(team) + " have collapsed");
		}
		else
		{
			ConsolePanel.Log(ConsolePanel.ColoredName(team) + " has collapsed");
		}
	}
	IEnumerator AfterInfluences() {
		yield return new WaitForSeconds(0.2f);
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			ROE.MakePeace(team, i);
		}
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
		Diplomacy.states[team].manHourDebt += Economics.cost_armySpawn * 8;
		Diplomacy.states[to].manHourDebt -= Economics.cost_armySpawn * 10;
		Diplomacy.states[to].RecieveAid(team);

		Diplomacy.AnnounceNews(Diplomacy.NewsItem.Aid, team, to);
	}

	public virtual void RecieveAid(int from) { 
		
    }

	public virtual void ReceiveNews(Diplomacy.NewsItem news, int t1, int t2) { 
		//react to international news
    }
}
