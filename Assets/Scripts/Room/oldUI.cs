using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static ArmyUtils;

public class oldUI : MonoBehaviour
{
	//There is an ungodly amount of magic number hardcoded bullshit in this class

	public static oldUI ins;


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

	[HideInInspector]
	public int incoming;
	List<int> liveIncoming;
	int playerTeam = 0;

	//StrikeMenu
	float lastStikeUpdate;
	float strikeUpdateTickDelay = 0.1f;

	public int nationSelected;
	public List<TMP_Text>[] options;
	public List<TMP_Text> strikeoptions;
	public List<TMP_Text> nationoptions;
	public List<TMP_Text> mainoptions;
	public TMP_Text strikeNationText;
	public TMP_Text diploNationText;
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
		Cursor.lockState = CursorLockMode.Locked;
	}
	void Reset() {
		DisplayHandler.resetGame -= Reset;
		LaunchDetection.launchDetectedAction -= LaunchDetect;
	}
	private void Start()
	{
		DisplayHandler.resetGame += Reset;

		for (int i = 1; i < Map.ins.numStates; i++){
			TMP_Text t = options[0][i - 1];
			t.color = Map.ins.state_colors[i];
			t.text = Diplomacy.state_names[i];
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

		if(currentMenu == Menu.strike) {
			if(Time.time - lastStikeUpdate > strikeUpdateTickDelay) {
				UpdateStrikePlanning();
				lastStikeUpdate = Time.time;
			}
		}

		if (osc) {
			Oscillate();
		}
		if (!UIcontrol) return;

		if (Input.GetKeyDown(KeyCode.Tab)) { 
			//Go back a menu
			if((int)currentMenu > 0) {
				SwitchMenu((int)currentMenu - 1);
			}
		}

		if (Input.GetKeyDown(KeyCode.UpArrow)) {
			ChangeSelected(-1);
		}
		if (Input.GetKeyDown(KeyCode.DownArrow)){
			ChangeSelected(1);
		}

		if (Input.GetKey(KeyCode.LeftArrow))
		{
			if (options[(int)currentMenu][selected].transform.childCount > 0)
			{
				if (Time.unscaledTime - lastAD > ADtick)
				{
					if (options[(int)currentMenu][selected].transform.GetChild(0).TryGetComponent(out Slider sl))
					{
						sl.value -= 0.05f;
						lastAD = Time.unscaledTime;
						UpdateSlider(sl);
					}
				}
			}
		}
		if (Input.GetKey(KeyCode.RightArrow))
		{
			if (Time.unscaledTime - lastAD > ADtick)
			{
				if (options[(int)currentMenu][selected].transform.childCount > 0) {
					if (options[(int)currentMenu][selected].transform.GetChild(0).TryGetComponent(out Slider sl))
					{
						sl.value += 0.05f;
						lastAD = Time.unscaledTime;
						UpdateSlider(sl);
					}
				}

			}
		}
		void UpdateSlider(Slider sl) { 
			if(currentMenu == Menu.state) {
				//this is the troop slider
				PlayerState pl = Diplomacy.states[0] as PlayerState;
				pl.troopAllocPlayerInput[nationSelected] = sl.value - 0.5f;
			}
		}
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) {

			if (values[selected] == true)
			{
				values[selected] = false;
				strikeoptions[selected].text = strikeoptions[selected].text.Replace('x', ' ');
			}
			else if (values[selected] == false)
			{
				strikeoptions[selected].text = strikeoptions[selected].text.Replace(' ', 'x');
				values[selected] = true;
			}

			switch (currentMenu) {
				case Menu.main:
					nationSelected = selected + 1;
					SwitchMenu((int)Menu.state);
				break;
				case Menu.state:
					if (selected == 1)
					{ // magic number for declaring war
						ROE.DeclareWar(0, nationSelected);
					}
					if (selected == 2) { // magic number for preemptive strike
						SwitchMenu((int)Menu.strike);
					}
				break;
				case Menu.strike:

					if (options[(int)currentMenu][3].transform.GetChild(0).TryGetComponent(out Slider sl))
					{
						if (selected == 4)
						{ //magic number for launch
							float sat = 1;
							sat = sl.value * 20;
							int sati = Mathf.CeilToInt(Mathf.Max(1, sat));
							List<Target> tars = GetTargets(nationSelected, sati, values[0], values[1], values[2]);
							Launch(sati, tars);
						}
					}
					else
					{
						Debug.LogError("set up wrong");
					}
				break;
			}
		}
	}

	void SwitchMenu(int newMenu) {
		DeselectCurrent();	
		if(currentMenu != Menu.main) {
			options[(int)currentMenu][selected].color = Color.white;
		}
		strikeNationText.color = Map.ins.state_colors[nationSelected];
		strikeNationText.text = Diplomacy.state_names[nationSelected];
		diploNationText.color = Map.ins.state_colors[nationSelected];
		diploNationText.text = Diplomacy.state_names[nationSelected];
		menus[(int)currentMenu].SetActive(false);
		currentMenu = (Menu)newMenu;
		menus[(int)currentMenu].SetActive(true);
		values = new bool[options[(int)currentMenu].Count];
		if(newMenu == (int)Menu.main) {
			selected = nationSelected - 1;
		}
		else {
			selected = 0;
		}
		if(newMenu == (int)Menu.state) {
			DisplayHandler.ins.TogglePopStrikeScreen(false);
			if (options[(int)currentMenu][0].transform.GetChild(0).TryGetComponent(out Slider sl))
			{
				PlayerState pl = Diplomacy.states[0] as PlayerState;
				sl.value = pl.troopAllocPlayerInput[nationSelected] + 0.5f;
			}
		}
		ChangeSelected(0);
		for (int i = 0; i < 3; i++)
		{
			strikeoptions[i].text = strikeoptions[i].text.Replace('x', ' ');
		}
		if (newMenu == (int)Menu.strike)
		{
			strikeoptions[2].text = strikeoptions[2].text.Replace(' ', 'x');
			values[2] = true;
			DisplayHandler.ins.TogglePopStrikeScreen(true);
			if (options[(int)currentMenu][3].transform.GetChild(0).TryGetComponent(out Slider sl))
			{
				sl.value = 0.5f;
			}
		}


	}


	void Launch(int saturation, List<Target> tars) {
		State_AI player = Diplomacy.states[0] as State_AI;
		player.ICBMStrike(saturation, TargetSort(tars.ToArray()).ToList());
	}
	void UpdateStrikePlanning() {
		if (options[(int)currentMenu][3].transform.GetChild(0).TryGetComponent(out Slider sl))
		{
			float sat = 1;
			sat = sl.value * 20;
			int sati = Mathf.CeilToInt(Mathf.Max(1, sat));

			List<Target> tars = GetTargets(nationSelected, sati, values[0], values[1], values[2]);
			StrikePlan.ins.DrawPlan(sati, tars);
		}
	}

	void ChangeSelected(int change) {
		TMP_Text t = options[(int)currentMenu][selected];
		HKSel(change);
		TMP_Text t2 = options[(int)currentMenu][selected];
		t.text = t.text.Replace(">", "");
		t.text = t.text.Replace("<", "");
		t2.text = ">" + t2.text + "<";


		if (currentMenu == Menu.main) {
			t.fontStyle = FontStyles.Bold;
			t2.fontStyle = FontStyles.Underline | FontStyles.Bold;

			return;
		}
		t.color = Color.white;
		t2.color = Color.yellow;
	}
	void DeselectCurrent() {
		TMP_Text t = options[(int)currentMenu][selected];
		t.text = t.text.Replace(">", "");
		t.text = t.text.Replace("<", "");

		if (currentMenu == Menu.main)
		{
			t.fontStyle = FontStyles.Bold;

			return;
		}
		t.color = Color.white;
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
		if (victim == playerTeam)
		{
			//osc = true;
			//PromptNuclear();
			incoming++;
			Invoke(nameof(RemoveIncoming), MapUtils.Tau(launchPos, targetPos));

			//annoying as fuck
			//nationSelected = perp;
			//SwitchMenu((int)Menu.strike);
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
