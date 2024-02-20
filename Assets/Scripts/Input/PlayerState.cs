using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : State_AI
{
	public override void GenerateTroopAllocations()
	{
		base.GenerateTroopAllocations();
		//for(int i = 0; i < Map.ins.numStates; i++) { 
		//	troopAllocations += TODO player controlled weight modifier
		//}
	}

}
