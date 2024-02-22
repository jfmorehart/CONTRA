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
	public override void GenerateTroopAllocations()
	{
		base.GenerateTroopAllocations();
		float total = 0;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] += troopAllocPlayerInput[i];
			total += Mathf.Max(0, troopAllocations[i]);
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] = Mathf.Max(0, troopAllocations[i] / total);
		}
	}

}
