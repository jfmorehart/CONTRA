using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class TimePanel : MonoBehaviour
{
    public static TimePanel ins;
    public float timer = 300;
    public TMP_Text time;
    public static Action timesUp;

	private void Awake()
	{
        ins = this;

	}

	// Update is called once per frame
	void Update()
    {
        if (timer < 0.1f) return;
        timer -= Time.deltaTime;
        int minutes = Mathf.FloorToInt(timer / 59.99f);
        int seconds = Mathf.RoundToInt((timer - (minutes * 60)));
        time.text = minutes.ToString() + ":" + DoubleZero(seconds);
        if(timer < 0.1f) {
            EndGame();
	    }
    }

    public void EndGame() {
        time.color = Color.red;
        Diplomacy.CalculateStatePowerRankings();

		Time.timeScale = 0;
		timesUp?.Invoke();
		//EndPanel.ins.Enable(); this is invoked by timesup anyhow
	}

	string DoubleZero(int seconds) { 
        if(seconds < 10) {
            return "0" + seconds.ToString();
        }
        if(seconds == 60) {
			return "00";
		}
        else {
            return seconds.ToString();
		}
    }
}
