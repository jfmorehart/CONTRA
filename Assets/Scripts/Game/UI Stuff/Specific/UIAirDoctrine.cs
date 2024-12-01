using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAirDoctrine : UIMenu
{
	State_AI state;
	float lastUpdate;

	private void Start()
	{
		state = Diplomacy.states[Map.localTeam] as State_AI;
		UpdateFromArray();
	}

	private void Update()
	{
		StrikePlan.ins.DrawAirPlan();

		if (Time.time - lastUpdate > 0.1f) {
			UpdateFromArray();
		}
		else {
			int en = UI.ins.targetNation;
			for (int i = 0; i < state.airdoctrine[en].Length; i++)
			{
				//this makes the data match the UI, for player input
				if ((children[i].value == 1) != state.airdoctrine[en][i])
				{
					state.airdoctrine[en][i] = children[i].value == 1;
				}
			}
		}

		lastUpdate = Time.time;
	}

	void UpdateFromArray() {
		//this makes the UI match the data, when swapping screens
		int en = UI.ins.targetNation;
		for (int i = 0; i < state.airdoctrine[en].Length; i++)
		{
			if ((children[i].value == 1) != state.airdoctrine[en][i])
			{
				children[i].value = state.airdoctrine[en][i] ? 1 : 0;

				if (children[i].highlighted)
				{
					children[i].UnHighlight();
					children[i].Highlight();
				}
				else
				{
					children[i].UnHighlight();
				}
			}
		}
	}
}
