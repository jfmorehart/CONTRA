using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePanel : TypingInterface
{
	public TMP_Text penaltyScore;
	public static PausePanel pauseInstance;
	public override void Awake()
	{
		base.Awake();
		pauseInstance = this;
	}
	//private void Update()
	//{
	//	if(Time.timeScale == 0f) {
	//		float score = (Diplomacy.score[0] - 30);
	//		penaltyScore.text = Mathf.RoundToInt(score).ToString() + " pts" ;
	//	}
	//}

	public void Boot() {
		lockout = false;
		//ClearConsole();
		if (Map.multi) {
			WriteOut("____________________________________");
			WriteOut("multiplayer sessions cannot be paused");
			WriteOut("the game is still live");
			WriteOut("hit 'esc' to return to the game");
			WriteOut("type 'end' to end the game");
			WriteOut("____________________________________");
		}
		else if (EndPanel.ins.scenarioEndOffered) {
			WriteOut("____________________________________");
			if (Diplomacy.score[0] > 30) {
				WriteOut("scenario result:             victory");
			}else if (Diplomacy.score[0] > 5) {
				WriteOut("scenario result:            positive");
			}
			else if (Diplomacy.score[0] > -5)
			{
				WriteOut("scenario result:            stagnant");
			}
			else if (Diplomacy.score[0] > -30)
			{
				WriteOut("scenario result:            negative");
			}
			else {
				WriteOut("scenario result:              defeat");
			}

			WriteOut("completion:                     100%");
			WriteOut("score:                        " + Mathf.RoundToInt(Diplomacy.score[0]).ToString() + "pts");
			WriteOut("type 'back' to exit to the home menu");
			WriteOut("____________________________________");
		}
		else {
			WriteOut("____________________________________");
			WriteOut("game in progress - paused");
			WriteOut("type 'help' for options");
			WriteOut("____________________________________");
		}
	}
	public void Shutdown() {
		//lockout = true;
		ClearConsole();
	}

	public override void ProcessText(string message)
	{
		message = message.Replace("\u200B", "");
		if (message.Contains("quit") || message.Contains("exit"))
		{
			Application.Quit();
			return;
		}

		if (message.Contains("unpause", System.StringComparison.CurrentCultureIgnoreCase))
		{
			if (EndPanel.over) {
				WriteOut("action not possible: session has terminated");
				return;
			}
			else {
				DisplayHandler.ins.UnPause();
				return;
			}

		}
		if (message.Contains("retry", System.StringComparison.CurrentCultureIgnoreCase))
		{
			if (Map.multi && !Map.host)
			{
				WriteOut("retry is not avaliable in multiplayer");
				return;
			}
			StartCoroutine(nameof(PausedDot));
			DisplayHandler.ins.ReloadGame();
			return;
		}
		if (message.Contains("back") || (message.Contains("menu")) || (message.Contains("end")))
		{
			//if (Map.multi && !Map.host)
			//{
			//	WriteOut("only the host can end the game");

			//	return;
			//}
			StartCoroutine(nameof(PausedDot));
			DisplayHandler.ins.LoadMenu();
			return;
		}
		if (message.Contains("help") || message.Contains("options"))
		{
			WriteOut("____________________________________");
			WriteOut("options");
			WriteOut("esc key or 'unpause' - back to game");
			WriteOut("'controls' - how to play");
			WriteOut("'retry' - restart current scenario");
			WriteOut("'back' - terminate current scenario");
			WriteOut("'quit' - exit game");
			WriteOut("____________________________________");
			return;
		}
		if (message.Contains("controls"))
		{
			WriteOut("____________________________________");
			WriteOut("simulation controls");
			WriteOut("arrow keys - menu navigation");
			WriteOut("spacebar or return - select");
			WriteOut("tab - back");
			WriteOut("w, a, s, d - pan camera");
			WriteOut("q, e - zoom camera");
			WriteOut("_______________________________________");
			return;
		}
		WriteOut("unknown command");
	}

	IEnumerator PausedDot() { 
		for(int i = 0; i < 500; i++) {
			Dot();
			yield return null;
		}
    }
}
