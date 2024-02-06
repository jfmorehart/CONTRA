using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyState : State_AI
{
	float[] tas;
	float tasum;


	public override void Start()
	{
		base.Start();
		tas = new float[Map.ins.numStates];
	}
	protected override void StateUpdate()
	{
		base.StateUpdate();

		tasum = 0;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			tasum += tas[i];
		}

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			StateEval eval = new StateEval(team, i);
			Debug.Log(team + " " + i + " " + eval.pVictory);

			troopAllocations[i] = tas[i] / (tasum + 0.001f);
			tas[i] = eval.armyRatio * (AsyncPath.ins.SharesBorder(team, i) ? 1 : 0);
			if (ROE.AreWeAtWar(team, i))
			{
				// weight troop allocation by necessity
				tas[i] *= 4;

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
					if (Time.time > 10 && eval.pVictory > 0.7f && !ROE.AreWeAtWar(team))
					{
						Debug.Log(team + " starting a war with " + i);
						ROE.DeclareWar(team, i);
						ConductWar_Update(i, War.Colonial);
						//Vector2 ep = Diplo.states[i].transform.position;
						//List<Unit> troops = GetArmies(team, 30, ep, recentlyOrdered).ToList();
						//SendTroopsToBorder(i, troops);
					}
				}
			}

			ReAssignGarrisons(true);
			for (int r = 0; r < Map.ins.numStates; r++)
			{
				//CityGarrisons(r, garrisons[r]);
				DistributedPositions(r, garrisons[r]);
			}
		}
	}
}
