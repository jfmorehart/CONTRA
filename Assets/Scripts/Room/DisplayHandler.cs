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
	}
	private void Start()
	{
		UI.ins.UIScreenToggle(true);
		MoveCam.ins.canMove = true;
	}
	void Pause() {
		paused = true;
		Time.timeScale = 0;
		pauseCam.enabled = true;
		foreach (Screen s in screens)
		{
			s.Switch(5);
		}
	}

	void UnPause() {
		paused = false;
		Time.timeScale = 1;
		foreach (Screen s in screens) {
			s.Switch(-1);
		}
		pauseCam.enabled = false;
	}

	public void TogglePopStrikeScreen(bool strike) {
		if (strike) {
			screens[1].Switch(7);
		}
		else {
			screens[1].Switch(6);
		}
    }

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
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
            if (paused)
            {
                resetGame?.Invoke();
                SceneManager.LoadScene(1);
            }
        }

        //if (Input.GetKeyDown(KeyCode.Space)) {
        //	int c = screens[0].currentScreen + 1;
        //	if (c > cameraOutputs.Length - 1) c = 0;
        //	screens[0].Switch(c);
        //	if(c == 0) {
        //		MoveCam.ins.canMove = true;
        //	}
        //	else {
        //		MoveCam.ins.canMove = false;
        //	}
        //	if(c == 3) {
        //		UI.ins.UIScreenToggle(true);
        //	}
        //	else {
        //		UI.ins.UIScreenToggle(false);
        //	}
        //}
    }
}
