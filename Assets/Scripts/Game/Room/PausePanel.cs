using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PausePanel : MonoBehaviour
{
	public static PausePanel ins;
	public TMP_Text penaltyScore;

	private void Awake()
	{
		ins = this;
	}
	private void Update()
	{
		if(Time.timeScale == 0f) {
			float score = (Diplomacy.score[0] - 30);
			penaltyScore.text = Mathf.RoundToInt(score).ToString() + " pts" ;
		}
	}
}
