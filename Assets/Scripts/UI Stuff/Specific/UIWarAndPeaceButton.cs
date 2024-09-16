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
			}
			else if(Diplomacy.peaceOffers[t2, 0]){
				SetText("accept peace offer");
			}
			else {
				SetText("offer peace");
			}
		}
		else {
			SetText("declare war");
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
