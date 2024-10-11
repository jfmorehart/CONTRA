using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWarAndPeaceButton : MonoBehaviour
{
    UIOption button;
	private void Awake()
	{
		button = GetComponent<UIOption>();
	}
	// Update is called once per frame
	void Update()
    {
		int t2 = UI.ins.targetNation;
		if (ROE.AreWeAtWar(0,t2)) { 
			if(Diplomacy.peaceOffers[0, t2]) {
				SetText("cancel peace offer");
				button.tooltip_headerText = "cancel peace offer button";
				button.tooltip_bodyText = "revoke your offer of peace";
			}
			else if(Diplomacy.peaceOffers[t2, 0]){
				SetText("accept peace offer");
				button.tooltip_headerText = "accept peace offer button ";
				button.tooltip_bodyText = "this nation has offered you peace, do you accept?";
			}
			else {
				SetText("offer peace");
				button.tooltip_headerText = "peace offer button";
				button.tooltip_bodyText = "do you want to offer peace to this country?";
			}
		}
		else {
			SetText("declare war");
			button.tooltip_headerText = "declare war button";
			button.tooltip_bodyText = "be careful!";
		}
    }

	void SetText(string text) {
		if (button.highlighted) {
			button.text.text = ">" + text + "<";
		}
		else {
			button.text.text = text;
		}
		button.plaintext = text;
	}
}
