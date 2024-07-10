using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class State_Player : State_AI
{
	// This class is barely modded from State_AI.
	// it only adds some player guidance for troop allocations

	// player input for troop allocations, will be blended with internal figures
	// to minimize necessary slider micromanagement

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

	public override void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim, bool provoked)
	{
		base.LaunchDetect(launcher, target, perp, victim, provoked);
		if(perp == team) {
			Debug.Log("LAUNCH DETECT  " + launcher + "   " + target);
			string str = "<color=\"red\">" + " Launching Missile" + "</color> at ";
			str += ConsolePanel.ColoredName(victim);
			ConsolePanel.Log(str);
		}

		if(victim == team)
		{
			string str = "<color=\"red\">" + " Launch Detected" + "</color>" + " from: ";
			str += ConsolePanel.ColoredName(perp);
			ConsolePanel.Log(str);
		}
	}

	public override void WarStarted(int by)
	{
		base.WarStarted(by);
	}
}
