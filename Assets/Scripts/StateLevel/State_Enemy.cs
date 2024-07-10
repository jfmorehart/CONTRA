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

	StateEval[] rivals;
	public float[] opinion;

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

		float combinedConfidenceOfVictory = 1;

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplomacy.IsMyAlly(team, i)) continue;
			if (!Diplomacy.states[i].alive) {
				ROE.MakePeace(team, i);
				continue;
			}

			StateEval eval = new StateEval(team, i);
			rivals[i] = eval;

			//COMBAT STUFF
			if (ROE.AreWeAtWar(team, i))
			{
				if (sharesBorder[i]) {
					combinedConfidenceOfVictory *= eval.pVictory;
				}
				else {
					//todo make more sophisticated
					Diplomacy.OfferPeace(team, i);
				}

				if (Map.ins.state_populations[i] < 1) ROE.MakePeace(team, i);

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

		//can support foreign adventures
		if(combinedConfidenceOfVictory > 0.6f && assesment.percentGrowth > 0.3f) {

			ForeignAdventures(combinedConfidenceOfVictory);
		}

		//THINKING STUFF
		if (ROE.AreWeAtWar(team)) {

			//AT WAR
			if (combinedConfidenceOfVictory < 0.8f)
			{
				int spawnWave = Mathf.FloorToInt((-assesment.costOverrun / (Economics.cost_armyUpkeep * combinedConfidenceOfVictory)));
				if (spawnWave > 0)
				{
					SpawnTroops(spawnWave);
				}
			}
			else if (assesment.costOverrun > 1 && combinedConfidenceOfVictory > 0.9)
			{
				//economy is bad, and we're winning
				//we should cut back on troops

				//this will shrink spending by disbanding troops and mothballing silos
				BalanceBudget(assesment.costOverrun * combinedConfidenceOfVictory);
			}

		}
		else {
			//AT PEACE
			if (nuclearCount[team] + (2 * construction_sites.Count) < (assesment.buyingPower / 60) - 1)
			{
				InfluenceMan.ins.NewConstruction(team, MapUtils.RandomPointInState(team));
			}

			if (ArmyUtils.conventionalCount[team] < Map.ins.state_populations[team] * 0.1f)
			{
				//if our standing army is too small, grow it by a tenth of the surplus cash
				int spawnWave = Mathf.FloorToInt((-assesment.costOverrun * 0.1f) / Economics.cost_armyUpkeep);
				if (spawnWave > 0)
				{
					SpawnTroops(spawnWave);
				}
			}
			else if(ArmyUtils.conventionalCount[team] > Map.ins.state_populations[team] * 0.25f)
			{
				BalanceBudget(Map.ins.state_populations[team] * 0.05f * Economics.cost_armyUpkeep);
			}
		}

		if(combinedConfidenceOfVictory < 0.5f) {
			AttemptDeescalation();
		}
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
				}
				else if (rivals[i].pVictory < 0.7)
				{
					List<int> enemiesOfEnemy = ROE.GetEnemies(i);
					foreach (int e in enemiesOfEnemy)
					{
						Debug.Log(team + " says " + e + " is an enemy of " + i);
						if (opinion[e] > 0.5 && rivals[e].pVictory > rivals[i].pVictory)
						{
							SendAid(e);
							opinion[e] *= 1.05f;
							Debug.Log(team + "donating to " + e);
						}
					}
				}
			}
		}
	}
	void AttemptDeescalation()
	{
		//offer peace to the smaller one
		foreach (int i in ROE.GetEnemies(team))
		{
			if (rivals[i].pVictory < opinion[i] * 1.5)
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
					ICBMStrike(5, targets);
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
				ICBMStrike(6, targets);
				break;
			case War.Total:

				// Short Term: Eliminate Nuclear Assets
				// Long Term: Eliminate Cities
				targets.AddRange(NuclearTargets(enemy));
				targets.AddRange(CivilianTargets(enemy));
				targets.AddRange(ConventionalTargets(enemy));
				targets = TargetSort(targets.ToArray()).ToList();
				ICBMStrike(30, targets);
				break;
		}
	}

	public override void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim, bool provoked)
	{
		base.LaunchDetect(launcher, target, perp, victim, provoked);
		if(victim == team) {
			opinion[perp] *= 0.5f;
			Debug.Log(team + " bad");
		}
		else if(ROE.AreWeAtWar(team, victim)) {
			opinion[perp] *= 1.1f;
			Debug.Log(team + " good");
		}
		else {
			//mm yummy magic number soup
			float opmult = OpinionMultiplier(victim, 0.1f) * 0.95f;
			Debug.Log(team + " " + perp + " *= " + opmult);
			opinion[perp] *= opmult;
		}
		

	}

	public override void RecieveAid(int from)
	{
		base.RecieveAid(from);
		Debug.Log("recieved aid from " + from);
		opinion[from] *= 1.05f;
	}

	public override void WarStarted(int by)
	{
		base.WarStarted(by);
		if (by == team) return;
		opinion[by] *= 0.25f;
	}

	public float OpinionMultiplier(int victim, float scale) {
		return (1 - ((opinion[victim] - 0.5f) * scale));
	}

}
