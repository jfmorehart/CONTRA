using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class TypingInterface : MonoBehaviour
{
    public TMP_Text dialogue;

    string pstring = "";
	public int maxLineLength;

	public float cTimer;
	float cflop;
	bool hascursor;

	public GameObject linePrefab;
	public TMP_Text[] lineObjs;
	public int numLines;
	public string[] lines;
	public float lineSpacer;

	int activeLine = 0;

	[SerializeField]
	bool[] finishedWriting;
	[SerializeField]
	int[] lengths;

	public float typingDelay = 0.001f;
	public float boopDelay = 0.07f;
	float lastcharTime;
	bool newchar;

	List<string> unwritten;

	//sound stuff
	public GameObject oneshotPrefab;
	public GameObject pooledSourcePrefab;
	public AudioSource src;
	public AudioClip[] keyclick;
	public AudioClip[] boops;
	AudioSource[] boopsources;
	float lastBoop;
	int booplength = 50;
	int bcham;


	private void Awake()
	{
		Cursor.lockState = CursorLockMode.Locked;
		if (!Simulator.IsSetup) Simulator.Setup();
		Time.timeScale = 1;
		
		lineObjs = new TMP_Text[numLines];
		lines = new string[numLines];
		finishedWriting = new bool[numLines];
		lengths = new int[numLines];
		unwritten = new List<string>();

		for (int i =0; i < numLines; i++) {
			Vector2 pos = new Vector2(0, -lineSpacer * i);
			lineObjs[i] = Instantiate(linePrefab, (Vector2)transform.position + pos, Quaternion.identity, transform).GetComponent<TMP_Text>();
			lineObjs[i].text = "";
			lines[i] = new string("");
			finishedWriting[i] = true;
			lengths[i] = 0;
		}

		boopsources = new AudioSource[booplength];
		for(int i = 0; i < booplength; i++) {
			boopsources[i] = Instantiate(pooledSourcePrefab, transform).GetComponent<AudioSource>();
		}
		WriteOptions();
	}

	SFX_OneShot KeyPressSound() {

		GameObject go = Instantiate(oneshotPrefab, transform);
		SFX_OneShot os = go.GetComponent<SFX_OneShot>();
		os.Play(keyclick[Random.Range(0, keyclick.Length)], 0.01f * SFX.globalVolume);
		os.GetComponent<AudioSource>().pitch = Random.Range(0.95f, 1.05f);
		return os;
	}
	void BoopSound(float minPitch = 1, float maxPitch = 1)
	{
		if (Time.time - lastBoop < boopDelay) return;
		lastBoop = Time.time;
		bcham++;
		if (bcham >= booplength) bcham = 0;
		boopsources[bcham].clip = boops[Random.Range(0, boops.Length)];
		boopsources[bcham].pitch = Random.Range(minPitch, maxPitch);
		boopsources[bcham].volume = 0.01f * SFX.globalVolume;
		boopsources[bcham].Play();
	}

	void WriteOptions() {
		Debug.Log("write options " + Time.time);
		WriteOut("_________________________________________", false);
		WriteOut("select a simulation to load", false);
		WriteOut("");
		WriteOut("scenario a", false);
		WriteOut("scenario b", false);
		WriteOut("scenario c",false);
		WriteOut("");
		WriteOut("type 'help' for more info",false);
		WriteOut("_______________________________________", false);
		WriteOut("");
	}

	// Update is called once per frame
	void Update()
    {
		if(Time.time - lastcharTime > typingDelay) {
			newchar = true;
			lastcharTime = Time.time;
		}
		bool doneWriting = true;
		for(int i =0; i < numLines; i++) {
			if (i == activeLine) continue;
			if (!finishedWriting[i]) {
				doneWriting = false;
				break;
			}
		}

		foreach (char c in Input.inputString) {
			if (!doneWriting) break;

			KeyPressSound();

            if (!pstring.Contains(c)) {
				if (c == '\b')
				{
					if (lines[activeLine].Length > 1)
					{
						lines[activeLine] = lines[activeLine].Remove(lines[activeLine].Length - 1);
						if (lines[activeLine].Length > 1 && lines[activeLine][^1].ToString() == "\u200B")
						{
							lines[activeLine] = lines[activeLine].Remove(lines[activeLine].Length - 1);
						}
					}
				}
				else if (c == '\n') break;
				else
				{
					char o = c;
					if (o > 64 && o < 91) {
						o = (char)(o + ('a' - 'A'));
					}
					lines[activeLine] += "\u200B" + o;//zero width space lmao
					//the zero width space tricks the wrapping algorithm into splitting the word here

				}
	        }
	    }
		if (doneWriting) { 
			if(unwritten.Count > 0) {
				NewLine();
				lines[activeLine] = unwritten[0];
				lengths[activeLine] = 0;
				finishedWriting[activeLine] = false;
				unwritten.RemoveAt(0);
			}
			else {
				if (!lines[activeLine].Contains(">")) {
					NewLine();
					lines[activeLine] = ">";
				}
			}
		}

		if (Input.GetKeyDown(KeyCode.Return))
		{
			string outString = lines[activeLine];
			lines[activeLine] = outString.ToLower() + '\n';
			finishedWriting[activeLine] = true;

			int skiplines = Mathf.CeilToInt(lines[activeLine].Length / maxLineLength);
			Debug.Log(lines[activeLine].Length);
			for(int i = 0; i < skiplines; i++) {
				Debug.Log("skip " + skiplines);
				NewLine();
				finishedWriting[activeLine] = true;
			}
			NewLine();
			finishedWriting[activeLine] = true;

			ProcessText(outString);
		}

		for(int i = 0; i < numLines; i++) {

			string s = lines[i];
			if (!finishedWriting[i] && lines[i].Length > 0) {

				if (newchar)
				{
					lengths[i]++;
					BoopSound(0.95f, 1.05f);
					if (lengths[i] >= lines[i].Length) finishedWriting[i] = true;
					newchar = false;
				}
				s = Reverse(lines[i]);
				s = s.Substring(Mathf.Max(0, s.Length - lengths[i]));
				s = Reverse(s);// (string)s
			}
			lineObjs[i].text = s;

			if (i == activeLine && hascursor)
			{
				lineObjs[i].text += "\u200B" + "|"; //add cursor superficially (not really stored)
			}

		}


		pstring = Input.inputString;

		if (Time.time - cflop > cTimer)
		{
			hascursor = !hascursor;
			cflop = Time.time;
		}
	}
	void NewLine() {
		activeLine++;
		if (activeLine >= numLines) {
			Debug.Log("shift");
			for(int i = 0; i < numLines - 1; i++) {
				lines[i] = lines[i + 1].ToString();
				finishedWriting[i] = finishedWriting[i + 1];
				lengths[i] = lengths[i + 1];
			}
			activeLine = numLines - 1;
		}
		lines[activeLine] = "";
		lengths[activeLine] = 0;
		finishedWriting[activeLine] = false;

	}
	void WriteOut(string message, bool greenify = false, bool instant = false) {

		if (greenify) {
			message = GreenText(message);
		}
		if (instant) {
			lines[activeLine] += message + '\n';
			finishedWriting[activeLine] = true;
			NewLine();
		}
		else {
			unwritten.Add(message + '\n');
		}
	}
	void WriteBracket(bool greenify = false) {
		WriteOut("_________________________________________", greenify);
	}
	void ProcessText(string message) {
		message = message.Replace("\u200B", "");

		if (message.Contains("hello joshua", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("greetings professor falken");
		}
		if (message.Contains("tears in rain", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("time to die");
		}
		if (message == ">repeat")
		{
			WriteOptions();
		}
		if (message == ">list games")
		{
			WriteOut("tic-tac-toe");
			WriteOut("falkens maze");
			WriteOut("black jack");
			WriteOut("gin rummy");
			WriteOut("hearts");
			WriteOut("bridge");
			WriteOut("checkers");
			WriteOut("chess");
			WriteOut("poker");
			WriteOut("fighter combat");
			WriteOut("guerrilla engagement");
			WriteOut("desert warfare");
			WriteOut("air-to-ground actions");
			WriteOut("theaterwide tactical warfare");
			WriteOut("biotoxic and chemical warfare");
			WriteOut("");
			WriteOut("global thermonuclear war");
			WriteOut("");
		}
		if (message == ">back")
		{
			WriteOut("you are already in the root menu", false);
		}
		if (message == ">help")
		{
			WriteOut("_______________________________________");
			WriteOut("");
			WriteOut("general commands");
			WriteOut("");
			WriteOut("'repeat' to see current options");
			WriteOut("'back' to go back");
			WriteOut("'controls' for list of controls");
			WriteOut("'help' for commands");
			WriteOut("");
			WriteOut("_______________________________________");
		}
		if (message == ">controls")
		{
			WriteOut("_______________________________________");
			WriteOut("");
			WriteOut("simulation controls");
			WriteOut("");
			WriteOut("arrow keys - menu navigation");
			WriteOut("spacebar or return - select");
			WriteOut("tab - back");
			WriteOut("w, a, s, d - pan camera");
			WriteOut("q, e - zoom camera");
			WriteOut("");
			WriteOut("_______________________________________");
		}

		if (message.Contains("scenario a", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("very well");
			Simulator.activeScenario = Simulator.scenarios[0];
			LoadGame();
		}
		if (message.Contains("scenario b", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("very well");
			Simulator.activeScenario = Simulator.scenarios[1];
			LoadGame();
		}
		if (message.Contains("scenario c", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("very well");
			Simulator.activeScenario = Simulator.scenarios[2];
			LoadGame();


		}

	}

	string GreenText(string message) {
		return "<color=\"green\">" + message + "</color>";
	}


	void LoadGame()
	{
		InvokeRepeating(nameof(Dot), 0, 0.01f);
		StartCoroutine(nameof(AsyncLoadScene), 1);
	}

	IEnumerator AsyncLoadScene(float delay = 1) {

		float startTime = Time.unscaledTime;

		AsyncOperation asyncLoad;
		asyncLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
		asyncLoad.allowSceneActivation = false;
		//wait until the asynchronous scene fully loads
		while (!asyncLoad.isDone)
		{
			//scene has loaded as much as possible,
			// the last 10% can't be multi-threaded

			if (asyncLoad.progress >= 0.9f && Time.unscaledTime - startTime > delay)
			{
				asyncLoad.allowSceneActivation = true;
			}
			Debug.Log(asyncLoad.isDone);
			yield return null;
		}

		AsyncOperation asyncUnLoad;
		asyncUnLoad = SceneManager.UnloadSceneAsync("Menu");
		while (!asyncLoad.isDone)
		{
			Debug.Log("unloading " + asyncUnLoad.progress);
			yield return null;
		}
	}

	void Dot()
	{
		string s = "";
		for (int i = 0; i < Random.Range(0, 128); i++)
		{
			s += ".";
		}
		BoopSound(3f, 3.1f);
		WriteOut(s, true, true);
	}

	public static string Reverse(string s)
	{
		char[] charArray = s.ToCharArray();
		Array.Reverse(charArray);
		return new string(charArray);
	}
}
