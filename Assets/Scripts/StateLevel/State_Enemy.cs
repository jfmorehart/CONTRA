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


	StateEval[] rivals; //most importantly stores pVictory
	//the likelihood we'd win in a one-versus-one

	public float[] opinion; // between 0 and 1, 0 is bad, 1 is good

	//this value between 0 and 1 represents the likelihood 
    // that we will win the wars we are currently engaged in
	// is equal to 1 when we are at peace;
	float confidence;

	protected override void Awake()
	{
		base.Awake();
		rivals = new StateEval[Map.ins.numStates];
		opinion = new float[Map.ins.numStates];
		for(int i =0; i < Map.ins.numStates; i++) {
			opinion[i] = Random.Range(0.45f, 0.55f);
		}
 	}

	protected override void StateUpdate()
	{
		base.StateUpdate();

		//loop through the current wars and determine our standing
		confidence = WarsEvaluation();

		//can support foreign adventures
		if(confidence > 0.6 && assesment.percentGrowth > 0.3f) {

			ForeignAdventures(confidence);
		}

		//keep army counts reasonable
		ConstructionAndBudgeting();

		if(confidence < 0.5f) {
			AttemptDeescalation();
		}

		StateOpinions();
	}

	float WarsEvaluation() {
		float confidence = 1;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplomacy.IsMyAlly(team, i)) continue;
			if (!Diplomacy.states[i].alive)
			{
				ROE.MakePeace(team, i);
				continue;
			}

			StateEval eval = new StateEval(team, i);
			rivals[i] = eval;

			//COMBAT STUFF
			if (ROE.AreWeAtWar(team, i))
			{
				if (sharesBorder[i])
				{
					confidence *= eval.pVictory;
				}
				else
				{
					//todo make more sophisticated
					Diplomacy.OfferPeace(team, i);
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
				ConductWar_Update(i, war);
			}
		}
		return confidence;
	}
	void ForeignAdventures(float confidence) {
		//lets think about declaring war
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplomacy.IsMyAlly(team, i)) continue;
			if (!Diplomacy.states[i].alive) continue;

			if (sharesBorder[i])
			{
				//todo replace with more sophisticated method for mimicking anger
				//Debug.Log(team + " " + i + "  " + rivals[i].pVictory);
				if (confidence * rivals[i].pVictory > opinion[i] * 1.5f && rivals[i].pVictory > 0.5f)
				{
					confidence *= rivals[i].pVictory;
					ROE.DeclareWar(team, i);
					ScrambleAircraft();
				}
				else if (opinion[i] < 0.49)
				{
					List<int> enemiesOfEnemy = ROE.GetEnemies(i);
					foreach (int e in enemiesOfEnemy)
					{
						//Debug.Log(team + " says " + e + " is an enemy of " + i);
						if (opinion[e] + 0.1 > opinion[i] && rivals[e].pVictory > rivals[i].pVictory) //e weaker than i
						{
							SendAid(e);
							opinion[e] += 0.05f;
							Debug.Log(team + "donating to " + e);
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
			if (confidence < 0.8f || confidence < 0.95 && assesment.costOverrun < 1)
			{
				int spawnWave = Mathf.FloorToInt((-assesment.costOverrun / (Economics.cost_armyUpkeep * confidence)));
				spawnWave = Mathf.Min(spawnWave, Mathf.RoundToInt(Map.ins.state_populations[team] - ArmyUtils.conventionalCount[team]));
				if (spawnWave > 0)
				{
					SpawnTroops(spawnWave);
				}
			}
			else if (assesment.costOverrun > 1 && confidence > 0.9)
			{
				//economy is bad, and we're winning
				//we should cut back on troops

				//this will shrink spending by disbanding troops and mothballing silos
				BalanceBudget(assesment.costOverrun * confidence);
			}
		}
		else
		{
			//AT PEACE
			if (ArmyUtils.airbases[team].Count + (2 * construction_sites.Count) < (assesment.buyingPower / 60) - 1 && assesment.percentGrowth > 0.3f)
			{
				ArmyManager.ins.NewConstruction(team, MapUtils.RandomPointInState(team), ArmyManager.ins.airbasePrefab.GetComponent<Unit>());
				return;
			}

			if (ArmyUtils.conventionalCount[team] < Map.ins.state_populations[team] * 0.1f && assesment.percentGrowth > 0.2f)
			{
				//if our standing army is too small, grow it by a tenth of the surplus cash
				int spawnWave = Mathf.FloorToInt((-assesment.costOverrun * 0.1f) / Economics.cost_armyUpkeep);
				if (spawnWave > 0)
				{
					SpawnTroops(spawnWave);
				}
			}
			if (assesment.percentGrowth < 0.125)
			{
				BalanceBudget(assesment.costOverrun + 5);
			}
		}
	}
    void StateOpinions(){
		//todo manage opinions;

		//scrapped; things shouldn't stabilize

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
			if (rivals[i].pVictory < opinion[i] && !Diplomacy.peaceOffers[team, i])
			{
				Diplomacy.OfferPeace(team, i);
				return;
			}
		}
	}

	protected override void ConductWar_Update(int enemy, War war)
	{
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
				//targets.AddRange(NuclearTargets(enemy));
				//ICBMStrike(20, targets);
				break;
			case War.Defensive:
				// Repel invasion 
				// Diplomatic Pressure from allies
				// Maintain limited countervalue threat
				targets.AddRange(ConventionalTargets(enemy));
				targets = TargetSort(targets.ToArray()).ToList();
				ICBMStrike(6, targets, enemy);
				break;
			case War.Total:

				// Short Term: Eliminate Nuclear Assets
				// Long Term: Eliminate Cities
				targets.AddRange(NuclearTargets(enemy));
				targets.AddRange(CivilianTargets(enemy));
				targets.AddRange(ConventionalTargets(enemy));
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
		Debug.Log("recieved aid from " + from);
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
