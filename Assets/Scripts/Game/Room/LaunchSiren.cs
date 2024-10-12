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
	Renderer ren;
	Renderer ren2;

 
	public bool on;

	private void Awake()
	{
		src = GetComponent<AudioSource>();
		ren = GetComponent<Renderer>();
		ren2 = transform.GetChild(0).GetChild(0).GetComponent<Renderer>();
		highPitch += (Random.value - 0.5f) * 0.5f;
	}
	private void Start()
	{
		Off();
	}
	private void Update()
	{
		if (UI.ins == null) return;

		if (!on) { 
			if(UI.ins.incomingMissiles > 0) {
				on = true;
				SpinUp();
			}
			spinUpAmt *= 1 - Time.deltaTime * ndrag;
			spinUpAmt = 0;
		}
		else {
			if (UI.ins.incomingMissiles < 1)
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
			src.volume = Mathf.Lerp(0, maxVolume, spinUpAmt * spinUpAmt) * SFX.globalVolume;
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
		ren.material.SetColor("_EmissionColor", Color.red);
		ren2.material.SetColor("_EmissionColor", Color.red);

	}

	public void Off()
	{
		ToggleLights(false);
		ren.material.SetColor("_EmissionColor", Color.black);
		ren2.material.SetColor("_EmissionColor", Color.black);
	}

	void ToggleLights(bool t_on) { 
		foreach(GameObject g in lights) {

			g.SetActive(t_on);
		}
    }
}
