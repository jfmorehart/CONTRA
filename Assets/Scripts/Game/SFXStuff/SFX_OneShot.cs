using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX_OneShot : MonoBehaviour
{
    AudioSource src;
    float life = 1;
	float baseVolume;

	//for moving targets (planes)
	bool flying;
	Transform pilot;

	//for fixed points (nukes)
	Vector3? track;
	bool tracking;

	public float decayAmt;

	private void Awake()
	{
		src = GetComponent<AudioSource>();
	}

	public void Play(AudioClip clip, float volume, bool loop = false, Vector3? toTrack = null, float distanceDecay = 0.05f)
	{
		track = toTrack;
		if(track != null) {
			tracking = true;
			RecreateMapVector((Vector3)track, distanceDecay);
			decayAmt = distanceDecay;
		}
		gameObject.name = "ChatterBox";
		src = GetComponent<AudioSource>();
		src.clip = clip;
		src.volume = volume;
		baseVolume = volume;
		life = clip.length + 0.1f + (loop? Mathf.Infinity : 0); //lmao
		src.loop = loop;
		src.Play();
	}

	public void Pilot(Transform track, AudioClip clip, float volume, bool loop = false) {

		gameObject.name = "Pilot";
		src = GetComponent<AudioSource>();
		flying = true;
		pilot = track;
		src.clip = clip;
		src.volume = volume;
		baseVolume = volume;
		life = clip.length + 0.1f + (loop ? Mathf.Infinity : 0); //lmao
		src.loop = loop;

		RecreateMapVector(pilot.transform.position, SFX.ins.pilotChatterMult);
		src.Play();
	}
		

	private void Update()
	{
        life -= Time.deltaTime;
        if (life < 0) Destroy(gameObject);
		src.volume = baseVolume * SFX.ins.globalVolume;

		if (tracking) {
			RecreateMapVector((Vector3)track, decayAmt);
			//hack
			src.volume *= life / src.clip.length;
			return;
		}
		if (!flying) return;
		if (pilot == null)
		{
			Destroy(gameObject);
			return;
		}

		RecreateMapVector(pilot.position, SFX.ins.pilotChatterMult);

	}

	public void RecreateMapVector(Vector3 pos, float rangeMult) {
		Vector3 delta = pos - MoveCam.ins.transform.position;
		delta.z += Camera.main.orthographicSize * 2;
		float mag = SFX.ins.p_deltaMult * Mathf.Pow(delta.magnitude, SFX.ins.p_deltaExponent);
		src.spatialBlend = 1;
		transform.localPosition = mag * delta.normalized * rangeMult;
	}
}
