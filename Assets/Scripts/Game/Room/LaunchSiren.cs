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
	bool lastOnWasOurLaunch;

	float plTime;
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
	public void PlayerLaunch(float dur) {
		plTime = dur;
    }
	private void Update()
	{
		if (UI.ins == null) return;
		plTime -= Time.deltaTime;
		if (UI.ins.incomingMissiles > 0) {
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
				lastOnWasOurLaunch = false;
				foreach (Light l in lights) {
					l.intensity = Mathf.Lerp(0, maxBrightness, speed/maxSpeed);
					l.color = c;
				}
			}
			else {
				//turn on
				On();
			}
		}
		else if (plTime > 0)
		{
			if (on)
			{
				if (speed < maxSpeed)
				{
					speed += accel * Time.deltaTime;
				}
				else
				{
					speed = maxSpeed;
				}

				Color c = Color.Lerp(Color.black, Color.white, speed / maxSpeed);
				ren.material.SetColor("_EmissionColor", c);
				ren2.material.SetColor("_EmissionColor", c);
				lastOnWasOurLaunch = true;
				float modi = speed / maxSpeed;
				modi /= 0.01f + Mathf.Abs(plTime - Mathf.Round(plTime));
				foreach (Light l in lights)
				{
					l.intensity = Mathf.Lerp(0, maxBrightness, modi);
					l.color = c;
				}
			}
			else
			{
				//turn on
				On();
				speed = maxSpeed * 0.3f;
			}

		}else {
			if (speed > 0)
			{
				speed -= accel * Time.deltaTime * 3;
				if (speed < 0) speed = 0;
				foreach (Light l in lights)
				{
					l.intensity = Mathf.Lerp(0, maxBrightness, speed / maxSpeed);
				}
				Color c;
				if (lastOnWasOurLaunch) {
					c = Color.Lerp(Color.black, Color.white, speed / maxSpeed);
				}
				else {
					c = Color.Lerp(Color.black, Color.red, speed / maxSpeed);
				}
				ren.material.SetColor("_EmissionColor", c);
				ren2.material.SetColor("_EmissionColor", c);
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
				speed = 0;
			}
		}
		//avel += speed * Time.deltaTime;
		//if(plTime > 0 && UI.ins.incomingMissiles < 1) {
		//	float mod = Mathf.Lerp(0, speed / (0.01f + Mathf.Abs(plTime - Mathf.Round(plTime))), 0.1f);
		//	avel = mod;
		//}
		spin.transform.Rotate(Vector3.up, speed * Time.deltaTime);
		//speed *= 1 - Time.deltaTime * drag;
	}
	Vector3 C2V (Color c) {
		return new Vector3(c.r, c.g, c.b);
    }
	Color V2C (Vector3 v) {
		return new Color(v.x, v.y, v.z, 1);
    }
	//public void SpinUp()
	//{
	//	ToggleLights(true);
	//	src.pitch = lowPitch;
	//	src.volume = 0;
	//	speed = 0.01f;
	//	sirenPlaying = true;
	//	src.Play();
	//	ren.material.SetColor("_EmissionColor", Color.red);
	//	ren2.material.SetColor("_EmissionColor", Color.red);
	//}

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
		src.Play();
		on = true;
		ToggleLights(true);
	}

	void ToggleLights(bool t_on)
	{
		//Debug.Log(lights);
		for(int i = 0; i < lights.Length; i++)
		{
			lights[i].intensity = t_on ? 1 : 0;
		}
	}
}
