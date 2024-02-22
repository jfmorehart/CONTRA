using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchSiren : MonoBehaviour
{
	public Transform spin;
	public float accel;
	public float drag;
	public float avel;

	public GameObject[] lights;
 
	bool on;

	private void Start()
	{
		Off();
	}
	private void Update()
	{
		//Debug.Log(UI.ins.incoming);
		if (!on) { 
			if(UI.ins.incoming > 0) {
				on = true;
				SpinUp();
			}
		}
		else {
			if (UI.ins.incoming < 1)
			{
				on = false;
				Off();
			}
			avel += accel + Time.deltaTime;

		}

		if (avel < 0.1f) return;
		spin.transform.Rotate(Vector3.up, avel * Time.deltaTime);
		avel *= 1 - Time.deltaTime * drag;
	}

	public void SpinUp() {
		ToggleLights(true);
    }

	public void Off()
	{
		ToggleLights(false);
	}

	void ToggleLights(bool t_on) { 
		foreach(GameObject g in lights) {

			g.SetActive(t_on);
			Debug.Log(t_on);
		}
    }
}
