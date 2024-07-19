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
		State_Enemy brain = Diplomacy.states[team] as State_Enemy;
		string status = "";

		if (brain.opinion[0] < 0.4)
		{
			if (brain.opinion[0] < 0.15)
			{
				status = "<color=\"red\"> hates </color> you";
			}
			else
			{
				status = "<color=\"yellow\"> dislikes </color> you";
			}
		}
		else if (brain.opinion[0] > 0.6)
		{
			if (brain.opinion[0] > 0.8f)
			{
				status = "<color=\"blue\"> trusts </color> you";
			}
			else
			{
				status = "<color=\"green\"> likes </color> you";
			}
		}
		else
		{
			status = "neutral";
		}
		statusText.text = status;
	}
}
