using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static ArmyUtils;
using Unity.VisualScripting;

public class State_Enemy : State_AI
{
	//this class is super fucked
	//its trying to mimic a nation state in the cold war so its a little spaghetti


	StateDynamic[] rivals; //most importantly stores pVictory
	//the likelihood we'd win in a one-versus-one

	public float[] opinion; // between 0 and 1, 0 is bad, 1 is good

	public int invasionTarget; //for preparing invasions

	//this value between 0 and 1 represents the likelihood 
	// that we will win the wars we are currently engaged in
	// in peacetime, should represent fear of attack
	float confidence;
	float confidence_ground;
	float confidence_air;

	float airThreat; //defensive airforce strength we should have
	float groundThreat; //army strength we should have

	protected override void Awake()
	{
		base.Awake();
		rivals = new StateDynamic[Map.ins.numStates];
		opinion = new float[Map.ins.numStates];
 	}

	public override void Setup(int i, Vector2Int pos)
	{
		base.Setup(i, pos);

		//Default to random, relevant for unafilliated states
		for (int x = 0; x < Map.ins.numStates; x++)
		{
			opinion[x] = Random.Range(0.45f, 0.5f);
		}

		//Update for scenario
		int affiliation = Simulator.AffiliatedCheck(team);
		if (affiliation != -1)
		{
			for (int y = 0; y < Simulator.activeScenario.affiliations.Length; y++)
			{
				if (y == affiliation)
				{
					//allies
					opinion[y] = 0.95f;
				}
				else
				{
					//enemies
					opinion[y] = 0.05f;
				}
			}
		}
	}

	protected override void StateUpdate()
	{
		if (!alive) return;
		base.StateUpdate();

		//loop through the current wars and determine our standing
		confidence = SituationEvaluation();

		//if (team == 1)
		//{
		//	Debug.Log(ArmiesReadyOnFront(0));
		//}

		if (Research.currentlyResearching[team] == -Vector2Int.one) {
			NewResearch();
			Debug.Log("team " + team + " is now researching " + Research.currentlyResearching[team]);
		}
		Research.budget[team] = Mathf.Clamp(assesment.percentGrowth + 0.5f, 0.1f, 1);
		//Debug.Log(team + " progress = " + Research.unlockProgress[team]);

		//are we preparing an invasion?
		if (invasionTarget != -1) {
			opinion[invasionTarget] -= 0.005f;
			//consider calling off the invasion
			StateDynamic stdy = new StateDynamic(team, invasionTarget);
			if (stdy.pVictory < opinion[invasionTarget] * 1.8f || stdy.pVictory < 0.55) //unlikely to crush them
			{
				//call it off
				invasionTarget = -1;
			}
		}

		//can support foreign adventures
		if ((confidence > 0.6 && assesment.percentGrowth > 0f) ||invasionTarget != -1) {

			ForeignAdventures();
		}

		//keep army counts reasonable
		ConstructionAndBudgeting();

		if(confidence < 0.6f) {
			AttemptDeescalation();
		}

		StateOpinions();
	}
	public override void GenerateTroopAllocations()
	{
		base.GenerateTroopAllocations();
		float total = 0;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (!Diplomacy.states[i].alive) continue;
			troopAllocations[i] *= 1 - opinion[i];
			if (!ROE.AreWeAtWar(team) && invasionTarget == i) {
				troopAllocations[i] *= 10f;
			}
			total += Mathf.Max(0, troopAllocations[i]);
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] = Mathf.Max(0, troopAllocations[i] / total);
		}
	}
	float SituationEvaluation() {
		float confidence_wholistic = 1;
		confidence_ground = 1;
		confidence_air = 1;
		groundThreat = Map.ins.state_populations[team] * 0.05f; //you always need at least a couple
		airThreat = 0;

		bool atWar = ROE.AreWeAtWar(team);

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplomacy.IsMyAlly(team, i)) continue;
			if (!Diplomacy.states[i].alive)
			{
				ROE.MakePeace(team, i);
				continue;
			}

			StateDynamic st_dynamic = new StateDynamic(team, i);
			StateEval st_eval = new StateEval(i);
			rivals[i] = st_dynamic;
			if (atWar) {
				if (ROE.AreWeAtWar(team, i))
				{
					if (sharesBorder[i]) {
						groundThreat += st_eval.str_army;
					}
					airThreat += AirAttackStrength(i);
					WarsEvaluation(i, st_dynamic);
					confidence_wholistic *= st_dynamic.pVictory;
				}
			}
			else {
				//peacetime threat calc
				//threat strength * probability of attack

				if (sharesBorder[i]) {
					groundThreat += st_eval.str_army * ProbabilityOfWar(i);
				}
				airThreat += AirAttackStrength(i) * ProbabilityOfWar(i);
			}
		}
		return confidence_wholistic;
	}
	float ProbabilityOfWar(int with) {
		return 1 - (Mathf.Pow(opinion[with], 0.2f));
    }
	void WarsEvaluation(int i, StateDynamic eval) {
		//COMBAT STUFF

		opinion[i] -= 0.02f;
		List<int> enemiesOfEnemy = ROE.GetEnemies(i);
		foreach (int e in enemiesOfEnemy)
		{
			opinion[e] += 0.01f;
		}

		if (sharesBorder[i])
		{
			confidence *= eval.pVictory;

			//magic number soup converts enemy/friendly army count into chance of success
			float COVARM = RatioToCOV(eval.armyRatio);
			confidence_ground *= COVARM;
		}
		else
		{
			confidence_air *= RatioToCOV(AirAttackStrength(i) / AirDefenseStrength(team));
			//offer peace if we don't look good for a ranged war
			if (AirAttackStrength(team) < AirDefenseStrength(i) + 2)
			{
				Diplomacy.OfferPeace(team, i);
			}
		}

		War war = War.Peer;
		//the war type determines nuclear targets
		if (eval.popRatio < 0.5f)
		{
			war = War.Colonial;
		}
		if (eval.armyRatio > 1.5)
		{
			war = War.Defensive;
		}
		if (eval.pVictory < 0.1 || eval.isHotWar)
		{
			war = War.Total;
		}
		if (!sharesBorder[i])
		{
			war = War.Ranged;
		}
		ConductWar_Update(i, war);
	}
	void ForeignAdventures() {
		//lets think about declaring war
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplomacy.IsMyAlly(team, i)) continue;
			if (!Diplomacy.states[i].alive) continue;

			if (sharesBorder[i] || opinion[i] < 0.2f)
			{
				//todo replace with more sophisticated method for mimicking anger
				//Debug.Log(team + " " + i + "  " + rivals[i].pVictory);
				bool prepareInvasion = sharesBorder[i] && rivals[i].pVictory > opinion[i] * 2f && rivals[i].pVictory > 0.55f;
				prepareInvasion |= (invasionTarget == i);

				if(prepareInvasion)
				{
					float armyThreshold = Mathf.Max(Map.ins.state_populations[i] * 0.4f, armies[i].Count * 0.6f);

					//are we ready to attack?
					//Debug.Log(team + " vs " + i + " with= " + ArmiesReadyOnFront(i) + " requires " + armyThreshold);
					if (ArmiesReadyOnFront(i) > armyThreshold)
					{
						//invade!
						invasionTarget = -1;
						confidence *= rivals[i].pVictory;
						ROE.DeclareWar(team, i);
						ScrambleAircraft();
						return;
					}
					else {
						//make preparations to invade
						if(armies[team].Count < armyThreshold) {
							SpawnTroops(15);
						}
						invasionTarget = i;
						ReAssignGarrisons(true);
					}
	
				}
				else if (opinion[i] < 0.49 || (confidence > 0.95 && opinion[i] < 0.6))
				{
					opinion[i] -= 0.005f;
					List<int> enemiesOfEnemy = ROE.GetEnemies(i);
					foreach (int e in enemiesOfEnemy)
					{
						//Debug.Log(team + " says " + e + " is an enemy of " + i);
						if (opinion[e] + 0.1 > opinion[i] && rivals[e].pVictory > rivals[i].pVictory) //e weaker than i
						{
							SendAid(e);
							opinion[e] += 0.025f;
						}
					}
				}
			}
		}
	}
	void ConstructionAndBudgeting() {
		//THINKING STUFF
		if (ROE.AreWeAtWar(team) || invasionTarget != -1)
		{
			//AT WAR
			WartimeEconomics();
		}
		else
		{
			//AT PEACE
			if (GetBuildings(team).Length < assesment.buyingPower / 30 && assesment.percentGrowth > 0.1f) {
				//we can afford more
				ConsiderNewConstruction();
			}

			if (armies[team].Count < groundThreat && assesment.percentGrowth > 0.1f)
			{
				//if our standing army is too small, grow it by a tenth of the surplus cash
				int spawnWave = Mathf.FloorToInt((-assesment.costOverrun * 0.1f) / Economics.cost_armyUpkeep);
				if (spawnWave > 0)
				{
					SpawnTroops(spawnWave);
				}
			}
			if (assesment.percentGrowth < 0.1 && construction_sites.Count < 1)
			{
				BalanceBudget(5);
			}
			else if(assesment.percentGrowth < 0){
				BalanceBudget(assesment.costOverrun + 5);
			}
		}
	}
	void WartimeEconomics() {
		if (armies[team].Count < groundThreat * 2f) {
			int spawnWave = Mathf.RoundToInt(10 / (Economics.cost_armySpawn * confidence_ground));
			spawnWave = Mathf.Min(spawnWave, Mathf.RoundToInt(Map.ins.state_populations[team] - armies[team].Count) - 1);
			if (spawnWave > 0)
			{
				SpawnTroops(spawnWave);
			}
		}
		//if (confidence_ground < 0.8f || (confidence_ground < 0.95 && assesment.costOverrun < 1))
		//{
		//	int spawnWave = Mathf.RoundToInt(10 / (Economics.cost_armySpawn * confidence_ground));
		//	spawnWave = Mathf.Min(spawnWave, Mathf.RoundToInt(Map.ins.state_populations[team] - ArmyUtils.armies[team].Count));
		//	if (spawnWave > 0)
		//	{
		//		SpawnTroops(spawnWave);
		//	}
		//}
		else if (confidence_ground < 0.5)
		{
			//spawn as many as we can
			SpawnTroops(Mathf.Min(10, Mathf.RoundToInt(Map.ins.state_populations[team] - ArmyUtils.armies[team].Count)));
		}

		if(confidence < 0.8 && confidence_ground > 0.8f && assesment.costOverrun < -5) {
			//building for ranged wars
			ConsiderNewConstruction();
		}

		if (assesment.costOverrun > 1 && confidence > 0.9 && invasionTarget == -1)
		{
			//economy is bad, and we're winning
			//we should cut back on troops

			//this will shrink spending by disbanding troops and mothballing silos
			BalanceBudget(assesment.costOverrun * confidence);
		}

		if (assesment.percentGrowth > 0 && (Map.ins.state_populations[team] - armies[team].Count) < 5)
		{
			//we're fighting as hard as we can and we're getting lots of funding

			//force parity?
			//need to either build AAA or airbases
			if (construction_sites.Count < 1)
			{
				if(airThreat > AirDefenseStrength(team)) {
					ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.AAA);
				}
				else { 
					if(Random.value > 0.5) {
						ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.Airbase);
					}
					else {
						ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.Silo);
					}
				}
			}
		}
	}
	void ConsiderNewConstruction() {

		//Debug.Log(team + " " + AirDefenseStrength(team) + " " + batteries[team].Count);
		//Debug.Log(team + "  aa " + AirAttackStrength(team) + "  - - ad " + AirDefenseStrength(team));
		bool canAAA = Research.unlockedUpgrades[team][(int)Research.Branch.aaa] > 0;
		bool canAirbase = Research.unlockedUpgrades[team][(int)Research.Branch.air] > 0;
		bool canSilo = Research.unlockedUpgrades[team][(int)Research.Branch.silo] > 0;

		if (airbases[team].Count + batteries[team].Count + construction_sites.Count < (assesment.buyingPower / 70) - 1)
		{
			//todo make this mean something
			
			if (AirDefenseStrength(team) + 1 > airThreat && canAirbase) {
				ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.Airbase);
			}
			else if(canAAA) {
				ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.AAA);
			}
			
			return;
		}

		if (canSilo && silos[team].Count + construction_sites.Count < (assesment.buyingPower / 90) - 1)
		{
			ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.Silo);
			return;
		}

	}
    void StateOpinions(){
		//todo manage opinions;

		//things shouldn't really stabilize
		//but ideally reactions will be mostly understandable

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			float o = opinion[i];
			o = Mathf.Max(o, 0);
			o = Mathf.Min(o, 1);
			if (ROE.AreWeAtWar(team, i))
			{
				o -= 0.003f;
			}
			opinion[i] = o;
		}
	}
	void AttemptDeescalation()
	{
		foreach (int i in ROE.GetEnemies(team))
		{
			if (Diplomacy.peaceOffers[team, i]) continue;

			if (rivals[i].pVictory < opinion[i])
			{
				//unmotivated
				Diplomacy.OfferPeace(team, i);
				return;
			}
			if(rivals[i].pVictory < 0.5f && ROE.GetEnemies(i).Count < 2) {
				//edge of defeat
				Diplomacy.OfferPeace(team, i);
				return;
			}
			if (rivals[i].armyRatio > 1 && assesment.percentGrowth < -0.5) {
				//face a bloody battle
				Diplomacy.OfferPeace(team, i);
				return;
			}
		}
	}

	protected override void ConductWar_Update(int enemy, War war)
	{
		if (!alive) return;

		// this function is called every StateUpdate tick, once for every war
		// that the base state is invoved in. 

		// In this higher level inherited class it just handles Nuclear Strike policy
		// todo overhaul strike policies to better understand limited nuclear warfare

		base.ConductWar_Update(enemy, war);
		List<Target> targets = new List<Target>();
		switch (war)
		{
			case War.Peer:
				// Conventional Invasion

				//air civ ground strat
				airdoctrine[enemy] = new bool[]{true, true, true, true};
				// Limited countervalue attack if no counterforce threat
				if (rivals[enemy].nukeRatio < 0.1) {
					targets.AddRange(CivilianTargets(enemy));
					targets.AddRange(ConventionalTargets(enemy));
					ICBMStrike(5, targets, enemy);
				}

				break;
			case War.Colonial:
				// Conventional Invasion
				// Prevent escalation with countervalue deterrence (offer way out)
				// Counterforce to preserve capturable civilian centers


				//air civ ground strat
				airdoctrine[enemy] = new bool[] { true, false, true, true };


				//targets.AddRange(NuclearTargets(enemy));
				//ICBMStrike(20, targets);


				break;
			case War.Defensive:
				// Repel invasion 
				// Diplomatic Pressure from allies
				// Maintain limited countervalue threat

				//air civ ground strat
				airdoctrine[enemy] = new bool[] { true, false, true, true };


				targets.AddRange(ConventionalTargets(enemy));
				targets = TargetSort(targets.ToArray()).ToList();
				ICBMStrike(6, targets, enemy);
				break;
			case War.Total:

				//air civ ground strat
				airdoctrine[enemy] = new bool[] { true, true, true, true };

				// Short Term: Eliminate Nuclear Assets
				// Long Term: Eliminate Cities
				targets.AddRange(StrategicTargets(enemy));
				targets.AddRange(CivilianTargets(enemy));
				targets.AddRange(ConventionalTargets(enemy));
				targets = TargetSort(targets.ToArray()).ToList();
				ICBMStrike(30, targets, enemy);
				break;
			case War.Ranged:
				airdoctrine[enemy] = new bool[] { true, true, true, true };
				if (rivals[enemy].isHotWar) {
					targets.AddRange(StrategicTargets(enemy));
				}
				if(confidence < 0.5) {
					targets.AddRange(CivilianTargets(enemy));
				}
				targets = TargetSort(targets.ToArray()).ToList();
				ICBMStrike(30, targets, enemy);
				break;
		}
	}

	public void NewResearch() { 
		//begin new research and decide budget based on gamestate
		if(groundThreat > assesment.buyingPower * 0.3f) {
			if (CanResearchBranch(Research.Branch.ground)) {
				Research.DeclareResearchTopic(team, Research.Branch.ground);
				return;
			}
		}
		if(airThreat > AirDefenseStrength(team)){
			if (CanResearchBranch(Research.Branch.aaa))
			{
				Research.DeclareResearchTopic(team, Research.Branch.aaa);
				return;
			}
		}
		Research.Branch branch = (Research.Branch)(Random.Range(0, 4));
		if (CanResearchBranch(branch)) {
			Research.DeclareResearchTopic(team, branch);
		}
	}
	bool CanResearchBranch(Research.Branch branch) {
		if (Research.unlockedUpgrades[team][(int)branch] > 4) {
			return false;
		}
	return true;
    }

	public override void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim)
	{
		base.LaunchDetect(launcher, target, perp, victim);
	}

	public override void StrikeDetect(int perp, int victim, bool provoked)
	{
		base.StrikeDetect(perp, victim, provoked);
		if (victim == team)
		{
			opinion[perp] -= 0.5f;
		}
		else if (ROE.AreWeAtWar(team, victim))
		{
			opinion[perp] += 0.2f;
		}
		else
		{
			//mm yummy magic number soup
			float opmult = HarmOpinionMultiplier(victim, 0.5f) * 0.95f;
			opinion[perp] += opmult - 1;
		}
	}

	public override void RecieveAid(int from)
	{
		base.RecieveAid(from);
		opinion[from] += 0.05f;
	}

	public override void WarStarted(int by)
	{
		base.WarStarted(by);
		if (by == team) return;
		opinion[by] -= 0.5f;
		opinion[by] = Mathf.Min(opinion[by], 0.2f);
		ReAssignGarrisons(true);
		ScrambleAircraft();
	}


	//these two functions model empathy for the victim of a certain action
	//the Harm function is for negative actions, like declaring war or bombing
	//the Help function is for aid. 
	//the math is unreadable, but it just will output values under 1 for
	//negative opinions about the action, and values over one for positive opinions
	//depending upon how strongly we feel about the whole thing
	public float HarmOpinionMultiplier(int victim, float scale) {
		return (1 - ((opinion[victim] - 0.5f) * scale));
	}
	public float HelpOpinionMultiplier(int victim, float scale)
	{
		return 1 - ((0.5f - opinion[victim]) * scale);
	}

	public override void ReceiveNews(Diplomacy.NewsItem news, int t1, int t2)
	{
		base.ReceiveNews(news, t1, t2);
		if (team == t1 || team == t2) return;//i should already know

		switch (news) {
			case Diplomacy.NewsItem.War:
				opinion[t1] += HarmOpinionMultiplier(t2, 0.3f) - 1;
				break;
			case Diplomacy.NewsItem.Aid:
				opinion[t1] += HelpOpinionMultiplier(t2, 0.05f) - 1;
				break;
			case Diplomacy.NewsItem.Nuke:
				opinion[t1] += HarmOpinionMultiplier(t2, 0.3f) - 1;
				break;
		}
	}

	public int ArmiesReadyOnFront(int front) {

		int armiesReady = 0;
		for(int i = 0; i < garrisons[front].Count; i++) {
			if (garrisons[front][i] is Army) {
				Army am = garrisons[front][i] as Army;
				if (am.path != null) {
					if (am.path.Length < Map.ins.armyReadyDistance) {
						armiesReady++;
					}
				}
				else {
					//count stopped armies, assuming they're nearby
					armiesReady++;
				}
			}
		}
		return armiesReady;
    }

	/*	public int ArmiesReadyOnFront(int front) {

		int armiesReady = 0;
		for(int i = 0; i < garrisons[front].Count; i++) {
			Vector2Int upos = MapUtils.PointToCoords(garrisons[front][i].transform.position);
			for (int x = 0; x < Map.ins.borderPoints[front].Length; x++)
			{
				if (Vector2Int.Distance(Map.ins.borderPoints[team][front][x], upos) < Map.ins.armyReadyDistance) {
					armiesReady++;
				}	
			}
		}
		return armiesReady;
    }*/
} 
