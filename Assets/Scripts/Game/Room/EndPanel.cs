using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndPanel : MonoBehaviour
{
	public static EndPanel ins;
	public static bool over;

	public Camera endCam;
	bool live;

	public TMP_Text header;
	public TMP_Text scoreText;

	private void Awake()
	{
		over = false;
		ins = this;
		endCam.enabled = false;
	}

	private void Update()
	{
		for(int i =1; i < Map.ins.numStates; i++) {
			if (Diplomacy.states[i].alive) {
				return;
			}
		}

		Enable();
	}

	public void Enable() {
		over = true;
		endCam.enabled = true;
		float score = Mathf.RoundToInt(Diplomacy.score[0]);
		scoreText.text = score.ToString() + " pts";
		if(score > 15f) {
			if(score > 50) {
				header.text = "major victory";
			}
			else {
				header.text = "victory";
			}
		}else if (score > -15) {
			header.text = "neutral";
		}
		else { 
			if(score < -50) {
				header.text = "major defeat";
			}
			else {
				header.text = "defeat";
			}
		}
    }
}
