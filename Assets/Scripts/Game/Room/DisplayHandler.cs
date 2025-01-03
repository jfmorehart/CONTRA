using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class DisplayHandler : MonoBehaviour
{
	public static DisplayHandler ins;
	public RenderTexture[] cameraOutputs;
	public Screen[] screens;

	public Camera pauseCam;
	public bool paused;

	public static Action resetGame;

	public bool menuNotGame;
	[HideInInspector]
	public bool locked;
	private void Awake()
	{
		ins = this;

		if (!menuNotGame)
		{
			TimePanel.timesUp += EndScreens;
		}
	}
	private void Start()
	{
		//UI.ins.UIScreenToggle(true);

		if (menuNotGame) {
			foreach (Screen s in screens)
			{
				//s.Switch(0);
				if (s.wideFormat)
				{
					s.Switch(0);
				}
				else
				{
					s.Switch(1);
				}

			}
		}
		else if(!Simulator.tutorialOverride){
			MoveCam.ins.canMove = true;
		}

	}
	public void TutorialBlack() { 
		for(int i =1 ; i < screens.Length; i++) {
			if (i == 4) continue;
			screens[i].Switch(-2);
		}
    }
	public void ResetAll()
	{
		for (int i = 1; i < screens.Length; i++)
		{
			//if (i == 4) continue;
			screens[i].Switch(-1);
		}
	}
	public void EndScreens() {
		EndPanel.ins.Enable();

	}
	public void Pause() {
		if (!Map.multi) {
			Time.timeScale = 0;
		}
		UI.ins.locked = true;
		paused = true;
		PausePanel.pauseInstance.Boot();
		WideScreenCam.ins.Refresh();
		pauseCam.enabled = true;
		foreach (Screen s in screens)
		{
			if (s.wideFormat) {
				s.Switch(2);
			}
			else {
				s.Switch(5);
			}

		}
	}

	public void UnPause() {
		if(Simulator.tutorialOverride) {
			if (!TutorialOverride.railroad) {
				UI.ins.locked = false;
			}
			paused = false;
			PausePanel.pauseInstance.Shutdown();
			Time.timeScale = 1;
			WideScreenCam.ins.Refresh();
			pauseCam.enabled = false;

			// i know this is stupid, im sorry
			for (int i = 0; i < screens.Length; i++)
			{
				//if (i == 0)
				//{
				//	screens[i].Switch(-1);
				//	continue;
				//}
				if (i == 4 || i == 0) {
					screens[i].Switch(-1);
					continue;
				}
				if(i == 3 && TutorialOverride.showMenu) {
					screens[i].Switch(-1);
					continue;
				}
				if (TutorialOverride.showAllPanels) {
					screens[i].Switch(-1);
				}
				else {
					screens[i].Switch(-2);
				}

			}
		}
		else {
			UI.ins.locked = false;
			paused = false;
			PausePanel.pauseInstance.Shutdown();
			Time.timeScale = 1;
			WideScreenCam.ins.Refresh();
			foreach (Screen s in screens)
			{
				s.Switch(-1);
			}
			pauseCam.enabled = false;
		}
	}

	public void TogglePopStrikeScreen(bool strike) {
		if (strike) {
			screens[1].Switch(7);
		}
		else {
			StrikePlan.ins.ErasePlan();
			screens[1].Switch(6);
		}
    }

	private void Update()
	{
		//if (locked) return;
		if (Input.GetKeyDown(KeyCode.Escape) && !EndPanel.over && !menuNotGame)
		{
			if (paused) {
				UnPause();
			}
			else {
				Pause();
			}

		}
		//if (Input.GetKeyDown(KeyCode.P)) {
		//	EndScreens();
		//}
	}

	public void LoadMenu() {
		resetGame?.Invoke();
		TimePanel.timesUp -= EndScreens;
		if (Map.multi) {

			if (Map.host) {
				MultiplayerVariables.ins.EndGameClientRPC();
			}
			else {
				MultiplayerVariables.ins.EndGameServerRPC();
			}
			NetworkManager.Singleton.Shutdown();
			SceneManager.LoadScene("Menu");
		}
		else {
			SceneManager.LoadScene("Menu");
		}
	}
	public void ReloadGame()
	{
		resetGame?.Invoke();
		TimePanel.timesUp -= EndScreens;
		SceneManager.LoadScene("Game");
	}
}
