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

	//this value between 0 and 1 represents the likelihood 
    // that we will win the wars we are currently engaged in
	// is equal to 1 when we are at peace;
	float confidence;
	float confidence_ground;

	protected override void Awake()
	{
		base.Awake();
		rivals = new StateDynamic[Map.ins.numStates];
		opinion = new float[Map.ins.numStates];
		for(int i =0; i < Map.ins.numStates; i++) {
			opinion[i] = Random.Range(0.45f, 0.55f);
		}
 	}

	protected override void StateUpdate()
	{
		if (!alive) return;
		base.StateUpdate();

		//loop through the current wars and determine our standing
		confidence = WarsEvaluation();

		//can support foreign adventures
		if(confidence > 0.6 && assesment.percentGrowth > 0.3f) {

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
			total += Mathf.Max(0, troopAllocations[i]);
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] = Mathf.Max(0, troopAllocations[i] / total);
		}
	}
	float WarsEvaluation() {
		float confidence_wholistic = 1;
		confidence_ground = 1;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplomacy.IsMyAlly(team, i)) continue;
			if (!Diplomacy.states[i].alive)
			{
				ROE.MakePeace(team, i);
				continue;
			}

			StateDynamic eval = new StateDynamic(team, i);
			rivals[i] = eval;

			//COMBAT STUFF
			if (ROE.AreWeAtWar(team, i))
			{
				opinion[i] -= 0.02f;
				List<int> enemiesOfEnemy = ROE.GetEnemies(i);
				foreach (int e in enemiesOfEnemy)
				{
					opinion[e] += 0.01f;
				}

				if (sharesBorder[i])
				{
					confidence_wholistic *= eval.pVictory;

					//magic number soup converts enemy/friendly army count into chance of success
					float COVARM = Mathf.Clamp(Mathf.Pow(0.08f, Mathf.Pow(eval.armyRatio * 0.5f, 2)), 0.01f, 0.99f);
					confidence_ground *= COVARM;

				}
				else
				{
					//offer peace if we don't look good for a ranged war
					if((eval.airRatio + eval.nukeRatio) * 0.5f > 1) {
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
		}
		return confidence_wholistic;
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
				if (sharesBorder[i] && confidence * rivals[i].pVictory > opinion[i] * 2f && rivals[i].pVictory > 0.5f)
				{
					confidence *= rivals[i].pVictory;
					ROE.DeclareWar(team, i);
					ScrambleAircraft();
					return;
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
		if (ROE.AreWeAtWar(team))
		{
			//AT WAR
			WartimeEconomics();
		}
		else
		{
			//AT PEACE
			if (GetBuildings(team).Length < assesment.buyingPower / 30 && assesment.percentGrowth > 0.3f) {
				//we can afford more
				ConsiderNewConstruction();
			}

			if (ArmyUtils.conventionalCount[team] < Map.ins.state_populations[team] * 0.1f && assesment.percentGrowth > 0.3f)
			{
				//if our standing army is too small, grow it by a tenth of the surplus cash
				int spawnWave = Mathf.FloorToInt((-assesment.costOverrun * 0.1f) / Economics.cost_armyUpkeep);
				if (spawnWave > 0)
				{
					SpawnTroops(spawnWave);
				}
			}
			if (assesment.percentGrowth < 0.25 && construction_sites.Count < 1)
			{
				BalanceBudget(5);
			}
			else if(assesment.percentGrowth < 0){
				BalanceBudget(assesment.costOverrun + 5);
			}
		}
	}
	void WartimeEconomics() {
		if (confidence_ground < 0.8f || confidence_ground < 0.95 && assesment.costOverrun < 1)
		{
			int spawnWave = Mathf.RoundToInt(10 / (Economics.cost_armySpawn * confidence_ground));
			spawnWave = Mathf.Min(spawnWave, Mathf.RoundToInt(Map.ins.state_populations[team] - ArmyUtils.conventionalCount[team]));
			if (spawnWave > 0)
			{
				SpawnTroops(spawnWave);
			}
		}
		else if (confidence_ground < 0.5)
		{
			//spawn as many as we can
			SpawnTroops(Mathf.Min(10, Mathf.RoundToInt(Map.ins.state_populations[team] - ArmyUtils.conventionalCount[team])));
		}

		if(confidence < 0.8 && confidence_ground > 0.8f && assesment.costOverrun < -5) {
			//building for ranged wars
			ConsiderNewConstruction();
		}

		if (assesment.costOverrun > 1 && confidence > 0.9)
		{
			//economy is bad, and we're winning
			//we should cut back on troops

			//this will shrink spending by disbanding troops and mothballing silos
			BalanceBudget(assesment.costOverrun * confidence);
		}

		if (assesment.percentGrowth > 0 && ArmyUtils.GetAirbases(ROE.GetEnemies(team)[0]).Length > GetAAAs(team).Length + GetAirbases(team).Length)
		{
			//force parity?
			//need to either build AAA or airbases
			if (construction_sites.Count < 1)
			{
				ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.AAA);
			}
		}
	}
	void ConsiderNewConstruction() {

		if (batteries[team].Count + airbases[team].Count + construction_sites.Count < (assesment.buyingPower / 70) - 1)
		{
			//todo make this mean something
			if(Random.value > 0.5f) {
				ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.Airbase);
			}
			else {
				ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.BuildingType.AAA);
			}
			
			return;
		}

		if (silos[team].Count + construction_sites.Count < (assesment.buyingPower / 90) - 1)
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
} 
