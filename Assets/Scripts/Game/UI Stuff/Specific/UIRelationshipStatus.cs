using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIRelationshipStatus : MonoBehaviour
{
	public TMP_Text statusText;

	// Update is called once per frame
	void Update()
	{
		int team = UI.ins.targetNation;
		if (Diplomacy.states[team] is State_Enemy) {
			statusText.text = Diplomacy.OpinionText(team, 0);
			if (statusText.text != "neutral")
			{
				statusText.text += ConsolePanel.you;
			}
		}
		else {
			statusText.text = "";
		}

	}
}
