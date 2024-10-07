using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;


public class SFX : MonoBehaviour
{
	public static SFX ins;
	public float pilotChatterMult;
	public float p_deltaExponent;
	public float p_deltaMult;

	[HideInInspector]
	public bool reloadAudio;

	public AudioSource main;
	public AudioClip[] chatter;
	public AudioClip[] pilot_war;
	public AudioClip[] pilot_peace;
	public AudioClip newWar;
	public AudioClip newPeace;
	public AudioClip ambient;
	public AudioClip nukeSound;
	public AudioClip missileSound;
	public AudioClip sidewinderSound;
	public AudioClip launchSound;
	public AudioClip geiger;

	public GameObject oneshotPrefab;
	public GameObject pooledSourcePrefab;
	public int poolSize;
	PooledSource[] pool;

	//string soundpath = "Sounds/";

	float chatterVolume = 0.02f;
	public float globalVolume;

	float delay = 5;

	public float testPitch;

	private void Awake()
	{
		ins = this;
		main = GetComponent<AudioSource>();

		main.volume = 0.02f * globalVolume;
		main.clip = ambient;
		main.loop = true;
		main.Play();
		pool = new PooledSource[poolSize];
		for(int i = 0; i < poolSize; i++) {
			pool[i] = Instantiate(pooledSourcePrefab, transform).GetComponent<PooledSource>();
		}
	}

	public void LoadAllAudioFiles() {
		chatter = LoadFolder("chatter");
		pilot_war = LoadFolder("pilot_war");
		pilot_peace = LoadFolder("pilot_peace");
	}

	AudioClip[] LoadFolder(string nof) {


		DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/Resources/Sounds/" + nof);
		FileInfo[] f = dir.GetFiles("*.mp3");
		AudioClip[] arr = new AudioClip[f.Length];
		for (int i = 0; i < f.Length; i++)
		{
			arr[i] = LoadAudio(f[i].Name, nof);
		}
		return arr;
	}

	private void Update()
	{
		if(delay > 0) {
			delay -= Time.deltaTime;
			return;
		}
		Chatter();
	}

	void Chatter() {
		AudioClip clip = chatter[Random.Range(0, chatter.Length)];
		AudioSource nsc = NewSource(clip, chatterVolume * globalVolume, false);
		nsc.panStereo = Random.value;
		delay = main.clip.length + Random.Range(1, 5f);
	}

	public void DeclareWarAlarm() {
		NewSource(newWar, 0.2f);
    }
	public void MakePeaceAlarm()
	{
		NewSource(newPeace, 0.1f);
	}

	AudioSource NewSource(AudioClip clip, float volume, bool loop = false) {
		GameObject go = Instantiate(oneshotPrefab, transform);
		SFX_OneShot os = go.GetComponent<SFX_OneShot>();
		os.Play(clip, volume, loop);
		return os.GetComponent<AudioSource>();
    }
	public void NukePosition(Vector3 wpos, float size) {
		GameObject go = Instantiate(oneshotPrefab, transform);
		AudioSource src = go.GetComponent<AudioSource>();
		src.pitch = Random.Range(0.7f, 1.3f);
		SFX_OneShot sf = src.GetComponent<SFX_OneShot>();
		sf.Play(nukeSound, 5f * size, false, wpos, 0.008f);
    }
	public SFX_OneShot PilotChatter(bool peace, Unit pilot) {
		AudioClip[] sounds = peace ? pilot_peace : pilot_war;
		if (sounds.Length < 1) return null;
		AudioClip clip = sounds[Random.Range(0, sounds.Length)];

		GameObject go = Instantiate(oneshotPrefab, transform);
		SFX_OneShot os = go.GetComponent<SFX_OneShot>();
		os.Pilot(pilot.transform, clip, 0.3f);
		return os;
	}
	public SFX_OneShot MissileLaunch(Transform missile, float vmult = 1) {
		VectorLockNewSound(launchSound, 1f * vmult, missile.transform.position, 0.4f, 0.01f * (1 / vmult)); 

        GameObject go = Instantiate(oneshotPrefab, transform);
		SFX_OneShot os = go.GetComponent<SFX_OneShot>();
		os.Pilot(missile, missileSound, 0.3f, true);
		os.GetComponent<AudioSource>().pitch = Random.Range(2f, 3f);
		return os;
	}
	public SFX_OneShot ATAMLaunch(Transform atam) {
		VectorLockNewSound(launchSound, 1f * 0.4f, atam.transform.position, 0.4f, 0.01f * 2);

		GameObject go = Instantiate(oneshotPrefab, transform);
		SFX_OneShot os = go.GetComponent<SFX_OneShot>();
		os.Pilot(atam, sidewinderSound, 0.8f, true);
		os.GetComponent<AudioSource>().pitch = Random.Range(0.9f, 1.1f);
		return os;
	}
	public void PlaneLaunch(Transform plane)
	{
		GameObject go = Instantiate(oneshotPrefab, transform);
		SFX_OneShot os = go.GetComponent<SFX_OneShot>();
		os.Pilot(plane, missileSound, 0.3f, true);
		os.GetComponent<AudioSource>().pitch = Random.Range(0.3f, 0.5f);
	}
	public void VectorLockNewSound(AudioClip clip, float volume, Vector3 pos, float pitchVariation, float decayAmt){
        GameObject go = Instantiate(oneshotPrefab, transform);
        SFX_OneShot os = go.GetComponent<SFX_OneShot>();
		os.Play(clip, volume, false, pos, decayAmt);
        os.GetComponent<AudioSource>().pitch = 1 + Random.Range(-pitchVariation * 0.5f, pitchVariation * 0.5f);
    }
	AudioClip LoadAudio(string name, string arrayname)
	{
		name = name.Replace(".mp3", "");
		Debug.Log("loading " + "Sounds/" + arrayname + "/" + name);
		return Resources.Load<AudioClip>("Sounds/"+ arrayname + "/" + name);
	}
	public void Shoot(Vector2 pos) {
		PooledVLock(launchSound, 0.2f, pos, 0.1f, 0.3f);
    }

	public void PooledVLock(AudioClip clip, float volume, Vector2 pos, float decayAmt = 0.05f, float pitchVariation = 0.1f) {
		PooledSource os = GetPooledSource();
		os.Play(clip, volume, pos, decayAmt);
		os.GetComponent<AudioSource>().pitch = testPitch;// 1 + Random.Range(-pitchVariation * 0.5f, pitchVariation * 0.5f);
	}

	int cham;
	PooledSource GetPooledSource() {
		cham++;
		if (cham > pool.Length - 1) cham = 0;
		return pool[cham];
    }

}
