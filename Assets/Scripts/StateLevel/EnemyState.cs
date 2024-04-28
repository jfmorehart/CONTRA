using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static ArmyUtils;

public class EnemyState : State_AI
{

	protected override void StateUpdate()
	{
		base.StateUpdate();

		float combinedConfidenceOfVictory = 1;

		//COMBAT STUFF
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplo.IsMyAlly(team, i)) continue;

			StateEval eval = new StateEval(team, i);
			//Debug.Log(team + " " + i + " " + eval.pVictory);
			if (ROE.AreWeAtWar(team, i))
			{
				combinedConfidenceOfVictory *= eval.pVictory;
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
			else
			{
				if (AsyncPath.ins == null) return;
				if (AsyncPath.ins.SharesBorder(team, i))
				{
					//todo replace with more sophisticated method for mimicking anger
					if (Time.timeSinceLevelLoad> 10 && eval.pVictory > 0.65f && !ROE.AreWeAtWar(team))
					{
						ROE.DeclareWar(team, i);
						ConductWar_Update(i, War.Colonial);
					}
				}
			}
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
			else if (combinedConfidenceOfVictory < 1 && assesment.costOverrun > 0)
			{
				//this will shrink spending by disbanding troops and mothballing silos
				BalanceBudget(assesment.costOverrun * combinedConfidenceOfVictory);
			}
		}
		else {
			//AT PEACE
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
				// Maintain countervalue threat
				targets.AddRange(NuclearTargets(enemy));
				ICBMStrike(20, targets);
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
}
