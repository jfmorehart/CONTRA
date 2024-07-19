using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAirDoctrine : UIMenu
{
	State_AI state;
	private void Start()
	{
		state = Diplomacy.states[0] as State_AI;
		for (int i = 0; i < state.airdoctrine.Length; i++)
		{
			if ((children[i].value == 1) != state.airdoctrine[i])
			{
				children[i].value = state.airdoctrine[i] ? 1 : 0;

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

	private void Update()
	{
		for (int i = 0; i < state.airdoctrine.Length; i++)
		{
			if ((children[i].value == 1) != state.airdoctrine[i])
			{
				state.airdoctrine[i] = children[i].value == 1;
			}
		}
	}
}
