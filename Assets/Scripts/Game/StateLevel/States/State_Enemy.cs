using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static ArmyUtils;

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
	[SerializeField] float confidence_warTotal;
	[SerializeField] float confidence_ground;
	[SerializeField] float confidence_air;

	float airThreat; //defensive airforce strength we should have
	public float groundThreat; //army strength we should have

	//this list is of people we're worried will invade us
	public List<int> threats = new List<int>();
	//people who we want to protect
	public List<int> friends = new List<int>();

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
			opinion[x] = 0.5f;
		}
		AlignOpinionsToTeams();

	}
	void AlignOpinionsToTeams() {
		//Update for scenario
		int affiliation = Simulator.AffiliatedCheck(team);
		if (affiliation == -1) return; //not on a team

		for (int bloc = 0; bloc < Simulator.activeScenario.affiliations.Length; bloc++)
		{
			for (int index = 0; index < Simulator.activeScenario.affiliations[bloc].Length; index++)
			{
				if (affiliation == bloc)
				{
					//this nation is on our team!
					opinion[Simulator.activeScenario.affiliations[bloc][index]] = 0.7f;
				}
				else
				{
					//this nation is on an enemy team!
					opinion[Simulator.activeScenario.affiliations[bloc][index]] = 0.3f;
				}
			}
		}
	}

	protected override void StateUpdate()
	{
		if (!alive) return;
		base.StateUpdate();

		//loop through the current wars and determine our standing
		SituationEvaluation();
		StateOpinions();

		if (Research.currentlyResearching[team] == -Vector2Int.one) {
			NewResearch();
			Debug.Log("team " + team + " is now researching " + Research.currentlyResearching[team]);
		}
		Research.budget[team] = Mathf.Clamp(assesment.percentGrowth + 0.5f, 0.1f, 1);

		//are we preparing an invasion?
		if (invasionTarget != -1) {
			opinion[invasionTarget] -= 0.005f;
			//consider calling off the invasion
			StateDynamic stdy = new StateDynamic(team, invasionTarget);
			if (stdy.armyRatio > 0.7f || stdy.pVictory < 0.55 || assesment.percentGrowth < -0.9f) //unlikely to crush them
			{
				//call it off
				invasionTarget = -1;
			}
		}

		//keep army counts reasonable
		ConstructionAndBudgeting();

		if (Simulator.tutorialOverride) return;//dont allow them to do smart things

		//can support foreign adventures
		if ((confidence_warTotal > 0.6 && assesment.percentGrowth > 0f) || invasionTarget != -1)
		{
			ForeignAdventures();
		}

		if (confidence_warTotal < 0.6f) {
			AttemptDeescalation();
		}
	}
	#region sitrep
	void SituationEvaluation() {
		//this function runs at the beginning of each state update
		//it sets values for the confidence and threat data

		confidence_ground = 1;
		confidence_air = 1;
		confidence_warTotal = 1;

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
					WarsEvaluation(i, st_dynamic, st_eval);
				}
			}
			else {
				//peacetime threat calc
				//threat strength * probability of attack

				if (sharesBorder[i]) {
					groundThreat += st_eval.str_army * ProbabilityOfWar(st_dynamic) * 0.7f;
				}
				airThreat += AirAttackStrength(i) * ProbabilityOfWar(st_dynamic);
			}
		}
	}
	float ProbabilityOfWar(StateDynamic dynamic) {
		//the likelihood of war is their ability to invade us, 
		//mitigated by our opinion of them

		float pLoss = 1 - dynamic.pVictory;
		float invOpinion = 1 - opinion[dynamic.team2];
		return pLoss * invOpinion;
    }
	float ProbabilityOfWar(int enemy)
	{
		StateDynamic dynamic = new StateDynamic(team, enemy);
		return ProbabilityOfWar(dynamic);
	}
	#endregion sitrep
	#region wars 
	void ForeignAdventures()
	{
		//lets think about declaring war
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplomacy.IsMyAlly(team, i)) continue;
			if (!Diplomacy.states[i].alive) continue;

			if (opinion[i] < 0.1f && rivals[i].nukeRatio < 1 && rivals[i].pVictory > 0.6f && rivals[i].armyRatio > 1) {
				//consider pre-emptive strike

				int numMissiles = 0;
				for(int s = 0; s < silos[team].Count; s++) {
					numMissiles += silos[team][s].numMissiles;
				}

				if(numMissiles > silos[i].Count + airbases[i].Count + 5) { //5 is wiggle room to nuke cities
					//first strike is feasible
					ROE.DeclareWar(team, i);
					SpawnTroops(50);
					ICBMStrike(100, GetTargets(i).ToList(), i);
					ScrambleAircraft();
					return;
				}
			}
			if (sharesBorder[i] || opinion[i] < 0.2f)
			{
				//todo replace with more sophisticated method for mimicking anger
				//Debug.Log(team + " " + i + "  " + rivals[i].pVictory);
				bool tryGroundInvasion = sharesBorder[i] && rivals[i].pVictory > opinion[i] * 2f && rivals[i].pVictory > 0.55f;
				tryGroundInvasion |= (invasionTarget == i);

				if (tryGroundInvasion)
				{
					//float armyThreshold = Mathf.Min(armies[team].Count * 0.6f, Map.ins.state_populations[i] * 0.6f);//Mathf.Max(Map.ins.state_populations[i] * 0.4f, armies[i].Count * 0.5f);
					float armyThreshold = Mathf.Min(EnemiesReadyOnFront(i) + Map.ins.state_populations[i] * 0.3f, Map.ins.state_populations[i]);
					armyThreshold = Mathf.Max(armyThreshold, 0.3f * ArmyUtils.armies[team].Count);
					//are we ready to attack?
					//Debug.Log(team + " vs " + i + " with= " + ArmiesReadyOnFront(i) + " requires " + armyThreshold);
					if (ArmiesReadyOnFront(i) > armyThreshold)
					{
						Debug.Log(ArmiesReadyOnFront(i) + " was over " + armyThreshold);
						//invade!
						invasionTarget = -1;
						confidence_warTotal *= rivals[i].pVictory;
						ROE.DeclareWar(team, i);
						ScrambleAircraft();
						return;
					}
					else
					{
						//make preparations to invade
						if (armies[team].Count < armyThreshold)
						{
							SpawnTroops(15);
						}
						invasionTarget = i;
						//ReAssignGarrisons(true);
					}

				}
				else if (opinion[i] < 0.49 || (confidence_warTotal > 0.95 && opinion[i] < 0.6))
				{
					if (opinion[i] > 0.2)
					{
						opinion[i] -= 0.005f;
					}
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
					if (RatioToCOV(rivals[i].nukeRatio) > 0.5 && RatioToCOV(rivals[i].airRatio) > 0.6f) { 
						//we can attack them i guess
					}
				}
			}
		}
	}
	void WarsEvaluation(int i, StateDynamic st_dynamic, StateEval st_eval) {
		//COMBAT STUFF

		opinion[i] -= 0.02f;
		List<int> enemiesOfEnemy = ROE.GetEnemies(i);
		foreach (int e in enemiesOfEnemy)
		{
			opinion[e] += 0.01f;
		}

		if (sharesBorder[i])
		{
			groundThreat += st_eval.str_army;
			confidence_ground *= RatioToCOV(st_dynamic.armyRatio);
		}
		else {
			//offer peace if we don't look good for a ranged war
			if (AirAttackStrength(team) < AirDefenseStrength(i) + 1)
			{
				Diplomacy.OfferPeace(team, i);
			}
		}

		confidence_warTotal *= st_dynamic.pVictory;
		airThreat += AirAttackStrength(i);
		confidence_air *= RatioToCOV(AirAttackStrength(i) / AirDefenseStrength(team));


		War war = War.Peer;
		//the war type determines nuclear targets
		if (st_dynamic.popRatio < 0.5f)
		{
			war = War.Colonial;
		}
		if (st_dynamic.armyRatio > 1.5)
		{
			war = War.Defensive;
		}
		if (st_dynamic.pVictory < 0.1 || st_dynamic.isHotWar)
		{
			war = War.Total;
		}
		if (!sharesBorder[i])
		{
			war = War.Ranged;
		}
		ConductWar_Update(i, war);
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
				airdoctrine[enemy] = new bool[] { true, true, true, true };
				// Limited countervalue attack if no counterforce threat
				if (rivals[enemy].nukeRatio < 0.1)
				{
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
				if (rivals[enemy].isHotWar)
				{
					targets.AddRange(StrategicTargets(enemy));
				}
				if (confidence_warTotal < 0.5)
				{
					targets.AddRange(CivilianTargets(enemy));
				}
				targets = TargetSort(targets.ToArray()).ToList();
				ICBMStrike(30, targets, enemy);
				break;
		}
	}
	#endregion wars
	#region money
	void ConstructionAndBudgeting() {
		//THINKING STUFF
		if (ROE.AreWeAtWar(team))
		{
			//AT WAR
			WartimeEconomics();
		}
		else if(invasionTarget != -1)
		{
			SpawnTroops(20);
		}
		else 
		{
			//AT PEACE

			//build if we want
			if (GetBuildings(team).Length < assesment.buyingPower / 40 && assesment.percentGrowth > 0.1f) {
				//we can afford more
				if (Simulator.tutorialOverride) return;
				ConsiderNewConstruction();
			}

			//too few armies?
			if (armies[team].Count + 5 < groundThreat)
			{
				//we can afford more
				if (assesment.percentGrowth > 0.1f) {
					//if our standing army is too small, grow it by a tenth of the surplus cash
					int spawnWave = Mathf.FloorToInt((-assesment.costOverrun * 0.1f) / Economics.cost_armyUpkeep);
					if (spawnWave > 0)
					{
						SpawnTroops(spawnWave);
					}
				}else if(assesment.percentGrowth < 0) {
					//who cares if we need armies, our economy is screwed
					BalanceBudget(assesment.buyingPower * 0.1f);
				}
			}
			else {
				//too many armies
				if(construction_sites.Count < 1) {
					BalanceBudget(Mathf.Max(assesment.costOverrun, assesment.buyingPower * 0.1f));
				}
				
				////if we're low growth and not building, its overkill
				//if (assesment.percentGrowth < 0.2 && construction_sites.Count < 1)
				//{
					
				//}
				////probably shouldn't be negative in peacetime
				//else if (assesment.percentGrowth < 0)
				//{
				//	BalanceBudget(assesment.costOverrun + 5);
				//}
			}

		}
	}
	void WartimeEconomics() {
		if (armies[team].Count < groundThreat * 1.5f || armies[team].Count < Map.ins.state_populations[team] * 0.15f) {
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

		if(confidence_warTotal < 0.8 && confidence_ground > 0.8f && assesment.costOverrun < -5) {
			//building for ranged wars
			ConsiderNewConstruction();
		}

		if (assesment.costOverrun > 1 && confidence_warTotal > 0.7 && invasionTarget == -1)
		{
			//economy is bad, and we're winning
			//we should cut back on troops

			//this will shrink spending by disbanding troops and mothballing silos
			BalanceBudget(assesment.costOverrun * confidence_warTotal);
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
	#endregion money
	#region politics
	void StateOpinions(){
		//todo manage opinions;

		friends.Clear();
		threats.Clear();
		//things shouldn't really stabilize
		//but ideally reactions will be mostly understandable

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (i == team) continue;

			float o = opinion[i];
			o = Mathf.Max(o, 0);
			o = Mathf.Min(o, 1);
			if (ROE.AreWeAtWar(team, i))
			{
				o -= 0.003f;
				threats.Add(i);
			}
			else if(o < 0.4 && ProbabilityOfWar(i) > 0.6) {
				threats.Add(i);
			}
			opinion[i] = o;
		}

		//threats are generated, determine friends in response to threats
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (i == team) continue;
			if (opinion[i] < 0.45) continue;
			if (ProbabilityOfWar(i) > 0.6) continue;
			for (int j = 0; j < threats.Count; j++)
			{
				if (j == team || j == i) continue;
				if (!ROE.AreWeAtWar(i, j)) continue;
				//this person is at war with someone whos a threat
				//we should like them
				if (opinion[i] < 0.8) opinion[i] += 0.003f;
				friends.Add(i);
				continue;
			}
			if (opinion[i] > 0.6) friends.Add(i);
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

	public override void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim)
	{
		base.LaunchDetect(launcher, target, perp, victim);
	}

	public override void StrikeDetect(int perp, int victim, bool provoked)
	{
		if (victim == -1 || perp == -1) return;
		Debug.Log("strdtc " + perp + " " + victim);
		Debug.Log("oplen =" + opinion.Length + " vs vic: " + victim);
		if (opinion.Length <= victim) return;
		Debug.Log("oplen =" + opinion.Length + " vs perp: " + perp);
		if (opinion.Length <= perp) return;
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
		opinion[from] += 0.1f;
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
		if (t1 < 0 || t2 < 0) return;
		base.ReceiveNews(news, t1, t2);
		if (team == t1 || team == t2) return;//i should already know

		switch (news) {
			case Diplomacy.NewsItem.War:
				opinion[t1] += HarmOpinionMultiplier(t2, 0.5f) - 1;
				break;
			case Diplomacy.NewsItem.Aid:
				opinion[t1] += HelpOpinionMultiplier(t2, 0.1f) - 1;
				break;
			case Diplomacy.NewsItem.Nuke:
				opinion[t1] += HarmOpinionMultiplier(t2, 0.3f) - 1;
				break;
		}
	}
	#endregion politics
	#region research
	public void NewResearch()
	{
		//begin new research and decide budget based on gamestate
		if (groundThreat > ArmyUtils.armies[team].Count * 1.2f)
		{
			if (CanResearchBranch(Research.Branch.ground))
			{
				Research.DeclareResearchTopic(team, Research.Branch.ground);
				return;
			}
		}
		if (airThreat > AirDefenseStrength(team))
		{
			if (airbases[team].Count < batteries[team].Count) {
				if (CanResearchBranch(Research.Branch.air))
				{
					Research.DeclareResearchTopic(team, Research.Branch.aaa);
					return;
				}
			}
			else {
				if (CanResearchBranch(Research.Branch.aaa))
				{
					Research.DeclareResearchTopic(team, Research.Branch.aaa);
					return;
				}
			}
		}
		Research.Branch branch = (Research.Branch)(Random.Range(0, 4));
		if (CanResearchBranch(branch))
		{
			Research.DeclareResearchTopic(team, branch);
		}
	}
	bool CanResearchBranch(Research.Branch branch)
	{
		if (Research.unlockedUpgrades[team][(int)branch] > 3)
		{
			return false;
		}
		return true;
	}
	#endregion research
	#region troops
	public override void GenerateTroopAllocations()
	{
		base.GenerateTroopAllocations();
		float total = 0;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (!Diplomacy.states[i].alive) continue;
			troopAllocations[i] *= (1 - opinion[i]) + 0.001f;
			if (!ROE.AreWeAtWar(team) && invasionTarget == i)
			{
				troopAllocations[i] *= 10f;
			}
			total += Mathf.Max(0, troopAllocations[i]);
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] = Mathf.Max(0, troopAllocations[i] / total);
		}
	}
	public int ArmiesReadyOnFront(int front)
	{
		int armiesReady = 0;
		for (int i = 0; i < garrisons[front].Count; i++)
		{
			if (garrisons[front][i] is Army)
			{
				Army am = garrisons[front][i] as Army;
				if (am.path != null)
				{
					if (am.path.Length < Map.ins.armyReadyDistance)
					{
						armiesReady++;
					}
				}
				else
				{
					//count stopped armies, assuming they're nearby
					armiesReady++;
				}
			}
		}
		return armiesReady;
	}
	public float EnemiesReadyOnFront(int enemy) {
		if (Map.multi) {
			//cant be sure that their pathing info will be accurate
			//todo replace with distance check mode
			return Mathf.Min(armies[team].Count * 0.6f, Map.ins.state_populations[enemy] * 0.6f);
		}

		int armiesReady = 0;
		List<Unit>[] enemyGarrisons = (Diplomacy.states[enemy] as State_AI).garrisons;
		for (int i = 0; i < enemyGarrisons[team].Count; i++)
		{
			if (enemyGarrisons[team][i] is Army)
			{
				Army am = enemyGarrisons[team][i] as Army;
				if (am.path != null)
				{
					if (am.path.Length < Map.ins.armyReadyDistance)
					{
						armiesReady++;
					}
				}
				else
				{
					//count stopped armies, assuming they're nearby
					armiesReady++;
				}
			}
		}
		return armiesReady;
	}
	#endregion troops
}
