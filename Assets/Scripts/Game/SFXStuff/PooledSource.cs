using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledSource : MonoBehaviour
{
	AudioSource src;
	public float life = 1;
	float baseVolume;

	//for fixed points (nukes)
	Vector3? track;
	bool live;

	public float decayAmt;

	private void Awake()
	{
		src = GetComponent<AudioSource>();
		Toggle(false);
	}

	public void Play(AudioClip clip, float volume, Vector3? toTrack = null, float distanceDecay = 0.05f)
	{
		track = toTrack;
		RecreateMapVector((Vector3)track, distanceDecay);

		decayAmt = distanceDecay;
		gameObject.name = "PooledTracker";
		src = GetComponent<AudioSource>();
		src.pitch = 1;
		src.clip = clip;
		src.volume = volume;
		baseVolume = volume;
		life = clip.length + 0.1f;
		live = true;
		Toggle(true);
	}

	public void RecreateMapVector(Vector3 pos, float rangeMult)
	{
		Vector3 delta = pos - MoveCam.ins.transform.position;
		delta.z += Camera.main.orthographicSize * 2;
		float mag = SFX.ins.p_deltaMult * Mathf.Pow(delta.magnitude, SFX.ins.p_deltaExponent);
		src.spatialBlend = 1;
		transform.localPosition = mag * delta.normalized * rangeMult;
	}

	public void FUpdate()
	{
		if (!live) return;
		life -= Time.deltaTime;
		if (life < 0) Toggle(false);
		src.volume = baseVolume * SFX.globalVolume;
		RecreateMapVector((Vector3)track, decayAmt);
	}

	void Toggle(bool on) {
		live = on;
		if (on) {
			src.Play();
		}
		else {
			src.Pause();
		}
    }
}
