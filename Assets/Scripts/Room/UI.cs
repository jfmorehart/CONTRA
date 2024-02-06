using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ArmyUtils;

public class UI : MonoBehaviour
{
	public static UI ins;


	[System.Serializable]
	public enum Menu
	{
		main,
		state,
		strike,
	}
	public Menu currentMenu = Menu.strike;

	public List<GameObject> menus;
	public GameObject strikeMenu;
	public GameObject nationMenu;
	public GameObject mainMenu;

	//Incoming menu
	public Image backgroundimg;
    public Color red;
	float ramt;

	public float dr;
	public bool osc;
	bool up;

	int incoming;
	List<int> liveIncoming;
	int playerTeam = 0;

	//StrikeMenu


	public int nationSelected;
	public List<TMP_Text>[] options;
	public List<TMP_Text> strikeoptions;
	public List<TMP_Text> nationoptions;
	public List<TMP_Text> mainoptions;
	public TMP_Text strikeNationText;
	public bool[] values;
   
	public int selected;
	public bool UIcontrol;

	//sliders
	float lastAD;
	readonly float ADtick = 0.05f;

	private void Awake()
	{
		ins = this;
		ramt = red.r;
		LaunchDetection.launchDetectedAction += LaunchDetect;
		liveIncoming = new List<int>();
		backgroundimg.color = Color.black;
		values = new bool[strikeoptions.Count];
		menus.Add(mainMenu);
		menus.Add(nationMenu);
		menus.Add(strikeMenu);
		options = new List<TMP_Text>[3] { mainoptions, nationoptions, strikeoptions };	
	}

	private void Start()
	{
		for(int i = 0; i < Map.ins.numStates - 1; i++){
			TMP_Text t = options[0][i];
			t.color = Map.ins.state_colors[i + 1];
		}

		for(int i = 0; i < 3; i++) {
			strikeoptions[i].text = strikeoptions[i].text.Replace('x', ' ');
		}
	}
	public void UIScreenToggle(bool on) {
		UIcontrol = on;
		ChangeSelected(0);
    }

	private void Update()
	{
		if (osc) {
			Oscillate();
		}
		if (!UIcontrol) return;

		if (Input.GetKeyDown(KeyCode.Escape)) { 
			//Go back a menu
			if((int)currentMenu > 0) {
				SwitchMenu((int)currentMenu - 1);
			}
		}

		if (Input.GetKeyDown(KeyCode.W)) {
			ChangeSelected(-1);
		}
		if (Input.GetKeyDown(KeyCode.S)){
			ChangeSelected(1);
		}

		if (Input.GetKey(KeyCode.A))
		{
			if (options[(int)currentMenu][selected].transform.childCount > 0)
			{
				if (Time.unscaledTime - lastAD > ADtick)
				{
					if (options[(int)currentMenu][selected].transform.GetChild(0).TryGetComponent(out Slider sl))
					{
						sl.value -= 0.05f;
						lastAD = Time.unscaledTime;
					}
				}
			}
		}
		if (Input.GetKey(KeyCode.D))
		{
			if (Time.unscaledTime - lastAD > ADtick)
			{
				if (options[(int)currentMenu][selected].transform.childCount > 0) {
					if (options[(int)currentMenu][selected].transform.GetChild(0).TryGetComponent(out Slider sl))
					{
						sl.value += 0.05f;
						lastAD = Time.unscaledTime;
					}
				}

			}
		}
		if (Input.GetKeyDown(KeyCode.Return)) {


			if (values[selected] == true)
			{
				values[selected] = false;
				strikeoptions[selected].text = strikeoptions[selected].text.Replace('x', ' ');
			}
			else if (values[selected] == false)
			{
				strikeoptions[selected].text = strikeoptions[selected].text.Replace(' ', 'x');
				Debug.Log("mark X");
				values[selected] = true;
			}

			switch (currentMenu) {
				case Menu.main:
					nationSelected = selected + 1;
					SwitchMenu((int)Menu.state);
					break;
				case Menu.state:
					if(selected == 1) { // magic number for preemptive strike
						SwitchMenu((int)Menu.strike);
					}
					break;
				case Menu.strike:
					if(selected == 4) { //magic number for launch
						float sat = 1;
						if (options[(int)currentMenu][3].transform.GetChild(0).TryGetComponent(out Slider sl))
						{
							sat = sl.value * 20;
						}
						else {
							Debug.LogError("set up wrong");
						}
						Launch(values[0], values[1], values[2], (int)sat);
					}
					break;
			}
		}
	}

	void SwitchMenu(int newMenu) {
		if(currentMenu != Menu.main) {
			options[(int)currentMenu][selected].color = Color.white;
		}
		strikeNationText.color = Map.ins.state_colors[nationSelected];
		menus[(int)currentMenu].SetActive(false);
		currentMenu = (Menu)newMenu;
		menus[(int)currentMenu].SetActive(true);
		values = new bool[options[(int)currentMenu].Count];
		selected = 0;
		ChangeSelected(0);
		for (int i = 0; i < 3; i++)
		{
			strikeoptions[i].text = strikeoptions[i].text.Replace('x', ' ');
		}
	}

	void Launch(bool nuclear, bool conventional, bool cities, int saturation) {
		State_AI player = Diplo.states[0] as State_AI;
		List<Target> tars = new List<Target>();
		if (nuclear) {
			tars.AddRange(NuclearTargets(nationSelected));
			Debug.Log("AAA");
		}
		if (conventional)
		{
			tars.AddRange(ConventionalTargets(nationSelected));
			Debug.Log("BBB");
		}
		if (cities)
		{
			tars.AddRange(CivilianTargets(nationSelected));
			Debug.Log("CCC");
		}
		player.ICBMStrike(saturation, TargetSort(tars.ToArray()).ToList());
	}
	void ChangeSelected(int change) {
		TMP_Text t = options[(int)currentMenu][selected];
		HKSel(change);
		TMP_Text t2 = options[(int)currentMenu][selected];
		if (currentMenu == Menu.main) {
			t.fontStyle = FontStyles.Bold;
			t2.fontStyle = FontStyles.Underline | FontStyles.Bold;
			return;
		}
		t.color = Color.white;
		t2.color = Color.yellow;
	}
	void HKSel(int change) {
		selected += change;
		if (selected < 0)
		{
			selected = options[(int)currentMenu].Count - 1;
		}
		if (selected >= options[(int)currentMenu].Count)
		{
			selected = 0;
		}
	}

	void LaunchDetect(Vector2 launchPos, Vector2 targetPos, int perp, int victim) { 
		if(victim == playerTeam) {
			osc = true;
			//PromptNuclear();
			incoming++;
			Invoke(nameof(RemoveIncoming), MapUtils.Tau(launchPos, targetPos));
			nationSelected = perp;
			SwitchMenu((int)Menu.strike);
		}
    }
	void RemoveIncoming() {
		incoming--;
		if(incoming < 1) {
			osc = false;
			backgroundimg.color = Color.black;
			//ClearDisplay();
		}
    }

	void Oscillate() {
		if (up)
		{
			red.r += dr * Time.deltaTime;
			if (red.r > ramt) up = false;
		}
		else
		{
			red.r -= dr * Time.deltaTime;
			if (red.r < 0) up = true;
		}
		backgroundimg.color = red;
	}
}
