using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class TypingInterface : MonoBehaviour
{
	public static TypingInterface interfaceInstance;
	public Transform textHolder;

    string pstring = "";
	public int maxLineLength;

	public float cTimer;
	float cflop;
	bool hascursor;

	public GameObject linePrefab;
	public float fontSize;
	public TMP_Text[] lineObjs;
	public int numLines;
	public string[] lines;
	public bool[] permanent;
	public float lineSpacer;

	protected int activeLine = 0;
	public int lettersPerTick = 1;

	[SerializeField]
	bool[] finishedWriting;
	[SerializeField]
	int[] lengths;

	public float typingDelay = 0.001f;
	public float boopDelay = 0.07f;
	float lastcharTime;
	bool newchar;

	public List<string> unwritten;

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

	public bool lockout;

	public float textScaleOverride = 1;

	public virtual void Awake()
	{
		interfaceInstance = this;
		if (textHolder == null) textHolder = transform;
		Cursor.lockState = CursorLockMode.Locked;
		if (!Simulator.IsSetup) Simulator.Setup();
		Time.timeScale = 1;
		
		lineObjs = new TMP_Text[numLines];
		lines = new string[numLines];
		finishedWriting = new bool[numLines];
		lengths = new int[numLines];
		permanent = new bool[numLines];
		unwritten = new List<string>();

		for (int i =0; i < numLines; i++) {
			Vector2 pos = new Vector2(0, -lineSpacer * i);
			lineObjs[i] = Instantiate(linePrefab, (Vector2)textHolder.position + pos, Quaternion.identity, textHolder).GetComponent<TMP_Text>();
			lineObjs[i].text = "";
			lineObjs[i].fontSize = fontSize;
			lineObjs[i].transform.localScale = lineObjs[i].transform.localScale * textScaleOverride;
			lines[i] = new string("");
			finishedWriting[i] = true;
			lengths[i] = 0;
		}

		boopsources = new AudioSource[booplength];
		for(int i = 0; i < booplength; i++) {
			boopsources[i] = Instantiate(pooledSourcePrefab, transform).GetComponent<AudioSource>();
		}
	}

	SFX_OneShot KeyPressSound() {

		GameObject go = Instantiate(oneshotPrefab, transform);
		SFX_OneShot os = go.GetComponent<SFX_OneShot>();
		if (keyclick.Length < 1) return null;
		os.Play(keyclick[Random.Range(0, keyclick.Length)], 0.01f);
		os.GetComponent<AudioSource>().pitch = Random.Range(0.95f, 1.05f);
		Debug.Log("click " + (0.01f) + os.GetComponent<AudioSource>().volume);
		return os;
	}
	public void BoopSound(float minPitch = 1, float maxPitch = 1, float volume = 0.01f)
	{
		if (Time.unscaledTime- lastBoop < boopDelay) return;
		lastBoop = Time.unscaledTime;
		bcham++;
		if (bcham >= booplength) bcham = 0;
		boopsources[bcham].clip = boops[Random.Range(0, boops.Length)];
		boopsources[bcham].pitch = Random.Range(minPitch, maxPitch);
		boopsources[bcham].volume = volume * SFX.globalVolume;
		boopsources[bcham].Play();
	}

	// Update is called once per frame
	public virtual void Update()
    {
		lettersPerTick = Mathf.Max(1, Mathf.RoundToInt(120 / (1 / Time.deltaTime)) + unwritten.Count);

		//do we type?
		if(Time.unscaledTime- lastcharTime > typingDelay) {
			newchar = true;
			lastcharTime = Time.unscaledTime;
		}

		//update whether or not we're finished typing
		bool doneWriting = true;
		for(int i =0; i < numLines; i++) {
			if (i == activeLine) continue;
			if (!finishedWriting[i]) {
				doneWriting = false;
				break;
			}
		}

		//read input
		foreach (char c in Input.inputString) {
			if (lockout) break;
			if (!doneWriting) break;

			KeyPressSound();

			//we weren't pressing this last frame (new input)
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
				//special cases, clean input
				else if (c == '\n') break;
				else if ((c > 'z' || c < '0') && c != ' ' && c!= '.') break; //only 0-9, a-z, period, and space
				else if (c == '\u001B') break; //esc char
				else
				{
					
					char o = c;
					if (o > 64 && o < 91)
					{
						o = (char)(o + ('a' - 'A'));
					}
					lines[activeLine] += "\u200B" + o;//zero width space lmao
													  //the zero width space tricks the wrapping algorithm into splitting the word here

				}
	        }
	    }
		//if we've finished the current line
		if (doneWriting) {
			//check to see if theres something new we need to write
			if (unwritten.Count > 0)
			{
				//prep the new line for writing
				lines[activeLine] = unwritten[0];

				//disregard tags on written length

				int lengthWithoutTags = LengthWithoutTags(unwritten[0]);
				//Debug.Log("length of new line = " + unwritten[0].Length + " without tags: " + lengthWithoutTags);
				int skiplines = Mathf.CeilToInt(lengthWithoutTags / (float)maxLineLength);

				lengths[activeLine] = 0;
				finishedWriting[activeLine] = false;
				unwritten.RemoveAt(0);
				//create proper amount of space for it
				for (int i = 0; i < skiplines; i++)
				{
					NewLine();
					finishedWriting[activeLine] = true;
				}
			}
			else {
				//create user-input carat
				if (!lines[activeLine].Contains(">") && !lockout) {
					NewLine();
					lines[activeLine] = ">";
				}
			}
		}

		if (Input.GetKeyDown(KeyCode.Return) && !lockout)
		{
			string outString = lines[activeLine];
			lines[activeLine] = outString.ToLower() + '\n';
			finishedWriting[activeLine] = true;

			int skiplines = Mathf.CeilToInt(lines[activeLine].Length / (float)maxLineLength);
			for(int i = 0; i < skiplines; i++) {
				NewLine();
				finishedWriting[activeLine] = true;
			}
			finishedWriting[activeLine] = true;

			ProcessText(outString);
		}

		//refresh whats on screen
		for(int i = 0; i < numLines; i++) {

			//skip writing animation
			if (Input.GetKey(KeyCode.LeftShift) || unwritten.Count > lines.Length) {
				lineObjs[i].text = lines[i];
				lengths[i] = lines[i].Length;
				finishedWriting[i] = true;
			}

			//handle writing stuff
			string s = lines[i];
			if (!finishedWriting[i] && lines[i].Length > 0) {

				if (newchar)
				{
					for(int t = 0; t < lettersPerTick; t++) {
						if (lines[i].Length <= lengths[i]) break;
						//write tags in one keypress
						if (lines[i][lengths[i]] == '<')
						{
							int add = 0;
							while (add < 30)
							{
								add++;
								//end if we ran out of room
								if (lines[i].Length <= lengths[i] + add)
								{
									Debug.Log("out of space" + lines[i].Length + " vs" + lengths[i] + add);
									break;
								}

								//end if we found the end character
								if (lines[i][lengths[i] + add] == '>')
								{
									break;
								}
							}
							lengths[i] += add;
						}
					lengths[i]++;
					}

					BoopSound(0.95f, 1.05f);
					if (lengths[i] >= lines[i].Length)
					{
						Debug.Log("fin " + i);
						lengths[i] = lines[i].Length;
						finishedWriting[i] = true;
					}
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
		if (lockout) {
			hascursor = false;
		}else if (Time.unscaledTime- cflop > cTimer)
		{
			hascursor = !hascursor;
			cflop = Time.unscaledTime;
		}
	}
	int LengthWithoutTags(string str) {
		int add = 0;
		int l = 0;
		while (add < str.Length)
		{
			if (str[add] != '<')
			{
				l++;
				add++;
			}
			else
			{
				while (str[add] != '>')
				{
					add++;
					if (add >= str.Length) break;
				}
			}
		}
		if(l == add) {
			return l;
		}
		else {
			return l - 2;
		}

	}
	void NewLine() {
		activeLine++;
		if (activeLine >= numLines) {
			for(int i = 0; i < numLines - 1; i++) {
				if (permanent[i]) continue;
				lines[i] = lines[i + 1].ToString();
				if (permanent[i + 1]) {
					permanent[i] = true;
					permanent[i + 1] = false;
				}
				finishedWriting[i] = finishedWriting[i + 1];
				lengths[i] = lengths[i + 1];
			}
			activeLine = numLines - 1;
		}
		lines[activeLine] = "";
		lengths[activeLine] = 0;
		finishedWriting[activeLine] = false;

	}
	public void WriteOut(string message, bool greenify = false, bool instant = false, bool setpermanent = false) {

		if (greenify) {
			message = GreenText(message);
		}
		if (instant) {
			lines[activeLine] += message + '\n';
			finishedWriting[activeLine] = true;
			if (setpermanent) {
				permanent[activeLine] = true;
			}
			int skiplines = Mathf.CeilToInt(message.Length / (float)maxLineLength);
			for (int i = 0; i < skiplines; i++)
			{
				NewLine();
				finishedWriting[activeLine] = true;
			}
		}
		else {
			unwritten.Add(message + '\n');
		}
	}
	public void WriteBracket(bool greenify = false) {
		WriteOut("_______________________________________", greenify);
	}
	public virtual bool ProcessText(string message) {
		//message = message.Replace("\u200B", "");
		//universal commands
		if (message.Contains("ocean=") || message.Contains("ocean ="))
		{
			int f = message.IndexOf('=') + 1;
			string volume = message[f..];
			int.TryParse(volume, out int v);
			v = Mathf.Max(v, 0);
			v = Mathf.Min(v, 1);
			PlayerPrefs.SetInt("ocean", v);
			WriteOut("ocean set to: " + v);
			return true;
		}
		if (message.Contains("volume=") || message.Contains("volume ="))
		{
			int f = message.IndexOf('=') + 1;
			string volume = message[f..];
			float.TryParse(volume, out float v);
			v = Mathf.Max(v, 0);
			v = Mathf.Min(v, 10);
			SFX.globalVolume = v;
			PlayerPrefs.SetFloat("volume", v);
			WriteOut("volume set to: " + v);
			return true;
		}
		if(message.Contains("bloom =")|| message.Contains("bloom=")) {
			int f = message.IndexOf('=') + 1;
			string volume = message[f..];
			int.TryParse(volume, out int v);
			v = Mathf.Max(v, 0);
			v = Mathf.Min(v, 1);
			PlayerPrefs.SetInt("bloom", v);
			WriteOut("bloom set to: " + v);
			return true;
		}
		if (message.Contains("settings")) {
			WriteBracket();
			WriteOut("volume = " + SFX.globalVolume);
			WriteOut("bloom = " + PlayerPrefs.GetInt("bloom", 0));
			WriteOut("ocean = " + PlayerPrefs.GetInt("ocean", 0));
			WriteBracket();
		}
	return false;
	}

	string GreenText(string message) {
		return "<color=\"green\">" + message + "</color>";
	}

	public void ClearConsole() {
		unwritten.Clear();
		for(int i =0;i < lines.Length; i++) {
			lines[i] = "";
			finishedWriting[i] = true;
			lengths[i] = 0;
			activeLine = 0;
		}
    }
	void LoadGame(bool online = false)
	{
		if (online) {

			NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
			return;
		}
		Destroy(NetworkManager.Singleton.gameObject);
		Destroy(MultiplayerVariables.ins);
		lockout = true;
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

	public void Dot()
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
