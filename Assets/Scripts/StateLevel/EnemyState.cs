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

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			if (Diplo.IsMyAlly(team, i)) continue;

			StateEval eval = new StateEval(team, i);
			//Debug.Log(team + " " + i + " " + eval.pVictory);
			if (ROE.AreWeAtWar(team, i))
			{
				if (Map.ins.state_populations[i] < 1) ROE.MakePeace(team, i);

				War war = War.Peer;
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
					if (Time.timeSinceLevelLoad> 10 && eval.pVictory > 0.7f && !ROE.AreWeAtWar(team))
					{
						Debug.Log(team + " starting a war with " + i);
						ROE.DeclareWar(team, i);
						ConductWar_Update(i, War.Colonial);
						//Vector2 ep = Diplo.states[i].transform.position;
						//List<Unit> troops = GetArmies(team, 30, ep, recentlyOrdered).ToList();
						//SendTroopsToBorder(i, troops);
					}
					else {  
						//TEST hack remove when done plz
						if(!Diplo.HasAllies(team) && !Diplo.HasAllies(i)) {
							Diplo.JoinAlliance(i, team);
						}
					}
				}
			}
		}
	}

	protected override void ConductWar_Update(int enemy, War war)
	{

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
