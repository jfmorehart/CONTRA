using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchSiren : MonoBehaviour
{
	public bool on;


	public Transform spin;
	public float accel;
	public float drag;
	public float avel;

	public Light[] lights;

	AudioSource src;
	public bool sirenPlaying;
	public float lowPitch, highPitch;
	public float speed, maxSpeed, noiseAccel, maxVolume, ndrag;
	public float maxBrightness;
	Renderer ren;
	Renderer ren2;

 


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

		if(UI.ins.incomingMissiles > 0) {
			if (on) {
				if(speed < maxSpeed) {
					speed += accel * Time.deltaTime;
				}
				else {
					speed = maxSpeed;
				}

				Color c = Color.Lerp(Color.black, Color.red, speed / maxSpeed);
				ren.material.SetColor("_EmissionColor", c);
				ren2.material.SetColor("_EmissionColor", c);

				foreach (Light l in lights) {
					l.intensity = Mathf.Lerp(0, maxBrightness, speed/maxSpeed);
				}
			}
			else {
				//turn on
				On();
			}
		}
		else { 
			if(speed > 0) {
				speed -= accel * Time.deltaTime;
			}
		}

		if (sirenPlaying)
		{
			src.pitch = Mathf.Lerp(lowPitch, highPitch, speed / maxSpeed);
			src.volume = Mathf.Lerp(0, maxVolume, (speed / maxSpeed) * (speed /maxSpeed) ) * SFX.globalVolume;
			if (speed < 0)
			{
				Off();
				src.Stop();
				src.volume = 0;
				sirenPlaying = false;
			}
		}
		//avel += speed * Time.deltaTime;
		spin.transform.Rotate(Vector3.up, speed * Time.deltaTime);
		//speed *= 1 - Time.deltaTime * drag;
	}
	Vector3 C2V (Color c) {
		return new Vector3(c.r, c.g, c.b);
    }
	Color V2C (Vector3 v) {
		return new Color(v.x, v.y, v.z, 1);
    }
	public void SpinUp()
	{
		ToggleLights(true);
		src.pitch = lowPitch;
		src.volume = 0;
		speed = 0.01f;
		sirenPlaying = true;
		src.Play();
		ren.material.SetColor("_EmissionColor", Color.red);
		ren2.material.SetColor("_EmissionColor", Color.red);
	}

	public void Off()
	{
		on = false;
		avel = 0;
		ToggleLights(false);
		ren.material.SetColor("_EmissionColor", Color.black);
		ren2.material.SetColor("_EmissionColor", Color.black);
	}
	public void On()
	{
		Debug.Log("set on");
		src.pitch = lowPitch;
		src.volume = 0;
		sirenPlaying = true;
		on = true;
		ToggleLights(true);
	}

	void ToggleLights(bool t_on)
	{
		Debug.Log(lights);
		for(int i = 0; i < lights.Length; i++)
		{
			lights[i].intensity = t_on ? 1 : 0;
		}
	}
}
