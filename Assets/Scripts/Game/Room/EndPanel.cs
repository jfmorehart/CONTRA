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

	public bool scenarioEndOffered;
	private void Awake()
	{
		over = false;
		ins = this;
		endCam.enabled = false;
	}

	private void Update()
	{
		if (scenarioEndOffered) return;
		if (WinCheck()) TimePanel.ins.EndGame();
		float teamPop = Map.ins.state_populations[Map.localTeam];
		float enemyPop = 0;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (i == Map.localTeam) continue;
			if (Diplomacy.states[i] is State_Enemy) {
				//disregard sworn allies
				if ((Diplomacy.states[i] as State_Enemy).opinion[Map.localTeam] > 0.8) continue;
			}
			enemyPop += Map.ins.state_populations[i];
		}
		if(teamPop > enemyPop * 2) {
			OfferScenarioEnd();
		}
	}
	void OfferScenarioEnd() {
		scenarioEndOffered = true;
		ConsolePanel.Clear();
		ConsolePanel.Log("  ", Mathf.Infinity);
		ConsolePanel.Log("[scenario stabilized: player victory]", Mathf.Infinity);
		ConsolePanel.Log("press 'escape' when ready to leave", Mathf.Infinity);
		ConsolePanel.Log(" ", Mathf.Infinity);
	}
	bool WinCheck() {
		for (int i = 1; i < Map.ins.numStates; i++)
		{
			if (Diplomacy.states[i].alive)
			{
				return false;
			}
		}
		return true;
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
