using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class DisplayHandler : MonoBehaviour
{
	public static DisplayHandler ins;
	public RenderTexture[] cameraOutputs;
	public Screen[] screens;

	public Camera pauseCam;
	public bool paused;

	public static Action resetGame;


	private void Awake()
	{
		ins = this;
		TimePanel.timesUp += EndScreens;
	}
	private void Start()
	{
		//UI.ins.UIScreenToggle(true);
		MoveCam.ins.canMove = true;
	}
	public void EndScreens() {
		EndPanel.ins.Enable();
		TallScreenCam.ins.End();
		foreach (Screen s in screens)
		{
			if (s.wideFormat) {
				s.Switch(9); //endscreen;
			}
			else {
				s.Switch(4); //tallscreen (economy)
			}
	
		}
	}
	void Pause() {
		paused = true;
		Time.timeScale = 0;
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

	void UnPause() {
		paused = false;
		Time.timeScale = 1;
		WideScreenCam.ins.Refresh();
		foreach (Screen s in screens) {
			s.Switch(-1);
		}
		pauseCam.enabled = false;
	}

	public void TogglePopStrikeScreen(bool strike) {
		Debug.Log("toggle");
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
		if (Input.GetKeyDown(KeyCode.Escape) && !EndPanel.over)
		{
			if (paused) {
				UnPause();
			}
			else {
				Pause();
			}

		}

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (paused || EndPanel.over)
            {
                resetGame?.Invoke();
				TimePanel.timesUp -= EndScreens;
				SceneManager.LoadScene(1);
            }
        }
	}
}
