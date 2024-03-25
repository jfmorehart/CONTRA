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

	AudioSource src;
	public bool sirenPlaying;
	public float lowPitch, highPitch;
	public float spinUpAmt, noiseAccel, maxVolume, ndrag;
 
	public bool on;

	private void Awake()
	{
		src = GetComponent<AudioSource>();
		highPitch += (Random.value - 0.5f) * 0.5f;
	}
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
			spinUpAmt *= 1 - Time.deltaTime * ndrag;
		}
		else {
			if (UI.ins.incoming < 1)
			{
				on = false;
				Off();
			}
			avel += accel + Time.deltaTime;
			if(spinUpAmt < 1) {
				spinUpAmt += noiseAccel * Time.deltaTime;
			}
		
			
		}
		if (sirenPlaying) {
			src.pitch = Mathf.Lerp(lowPitch, highPitch, spinUpAmt);
			src.volume = Mathf.Lerp(0, maxVolume, spinUpAmt * spinUpAmt);
			if (spinUpAmt < 0) {
				src.Stop();
				src.volume = 0;
				sirenPlaying = false;
			}
		}

		if (avel < 0.1f) return;
		spin.transform.Rotate(Vector3.up, avel * Time.deltaTime);
		avel *= 1 - Time.deltaTime * drag;
	}

	public void SpinUp() {
		ToggleLights(true);
		src.pitch = lowPitch;
		src.volume = 0;
		spinUpAmt = 0.01f;
		sirenPlaying = true;
		src.Play();
    }

	public void Off()
	{
		ToggleLights(false);
	}

	void ToggleLights(bool t_on) { 
		foreach(GameObject g in lights) {

			g.SetActive(t_on);
		}
    }
}
