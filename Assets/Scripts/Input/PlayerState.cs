using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : State_AI
{

	public float[] troopAllocPlayerInput;

	public override void Start()
	{
		troopAllocPlayerInput = new float[Map.ins.numStates];
		base.Start();
	}
	protected override void StateUpdate()
	{
		base.StateUpdate();
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			StateEval eval = new StateEval(team, i);
			//Debug.Log(team + " " + i + " " + eval.pVictory);
			if (ROE.AreWeAtWar(team, i))
			{
				ConductWar_Update(i, War.Peer);
			}
		}
	}
	public override void GenerateTroopAllocations()
	{
		base.GenerateTroopAllocations();
		float total = 0;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] += troopAllocPlayerInput[i];
			if (Map.ins.state_populations[i] < 1)
			{
				troopAllocations[i] = 0;
				if (troopAllocPlayerInput[i] > 0) {
					troopAllocPlayerInput[i] = 0;
				}

			}
			total += Mathf.Max(0, troopAllocations[i]);
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] = Mathf.Max(0, troopAllocations[i] / total);
		}
	}

}
