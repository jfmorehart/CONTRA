using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static ArmyUtils;

public class UI : MonoBehaviour
{
	public static UI ins;
	int playerTeam = 0;

	[HideInInspector]
	public int incomingMissiles;

	public int selected;
	public int targetNation;

	public UIMenu menu_home;
	public UIMenu menu_diplo;
	public UIMenu menu_defense;
	public UIMenu menu_bases;
	public UIMenu menu_build;
	public UIMenu menu_build_confirm;
	public UIMenu menu_nation;
	public UIMenu menu_feelings;
	public UIMenu menu_strike;
	public UIMenu menu_airdoctrine;
	public UIMenu menu_research;
	public UIMenu menu_rtopic;

	public UIMenu currentMenu;

	public Vector2 textOrigin;
	public float infoSpacer;

	public GameObject diploStateOptionPrefab;

	float sliderSoundDelay = 0.05f;
	float lastSliderSound;

	public bool locked;

	private void Awake()
	{
		ins = this;
	}
	public void Start()
	{
		LaunchDetection.launchDetectedAction += LaunchDetect;
		DisplayHandler.resetGame += Reset;
		menu_diplo.gameObject.SetActive(true);

		menu_diplo.children = new UIOption[Map.ins.numStates - 1]; //skip player
		for (int i = 0; i < Map.ins.numStates - 1; i++)
		{
			GameObject g = Instantiate(diploStateOptionPrefab, menu_diplo.transform);
			g.GetComponent<RectTransform>().position = new Vector3(200, -50, 0);
			menu_diplo.children[i] = g.GetComponent<UIOption>();
			UIOption op = menu_diplo.children[i];
			op.onSelect.AddListener(SelectNation);
			op.value = i + 1; //skip 0 (player state)
			op.text.color = Map.ins.state_colors[(int)op.value];
			op.defaultColor = Map.ins.state_colors[(int)op.value];
			op.plaintext = Diplomacy.state_names[(int)op.value];
			op.text.text = Diplomacy.state_names[(int)op.value];
		}

		//for (int i = 0; i < menu_diplo.children.Length; i++) {
		//	UIOption op = menu_diplo.children[i];
		//	//Debug.Log(op.name);
		//	//Debug.Log(op.name + " " + op.text.color + op.GetComponent<TMP_Text>().color);
		//	op.text.color = Map.ins.state_colors[(int)op.value];
		//	op.defaultColor = Map.ins.state_colors[(int)op.value];
		//	op.plaintext = Diplomacy.state_names[(int)op.value];
		//	op.text.text = Diplomacy.state_names[(int)op.value];
		//}
		menu_home.gameObject.SetActive(true);
		menu_diplo.gameObject.SetActive(false);
		menu_build.gameObject.SetActive(false);
		menu_defense.gameObject.SetActive(false);
		menu_build_confirm.gameObject.SetActive(false);
		menu_nation.gameObject.SetActive(false);
		menu_strike.gameObject.SetActive(false);
		menu_airdoctrine.gameObject.SetActive(false);
		menu_bases.gameObject.SetActive(false);
		menu_feelings.gameObject.SetActive(false);
		menu_research.gameObject.SetActive(false);
		menu_rtopic.gameObject.SetActive(false);

		Cursor.lockState = CursorLockMode.Locked;
		currentMenu.children[0].Highlight();
	}
	private void Update()
	{
		if (locked) return;
		if (EndPanel.over) return;

		ReconsiderStatehood();
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			ChangeSelected(-1);
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			ChangeSelected(1);
		}
		if (Input.GetKey(KeyCode.LeftArrow))
		{
			if(currentMenu.children[selected].kind == UIOption.Kind.Slider) {

				if(Time.time - lastSliderSound > sliderSoundDelay) {
					PlaySliderSound();
				}

				currentMenu.children[selected].value -= 1f * Time.deltaTime;
				if(currentMenu.children[selected].value < 0) {
					currentMenu.children[selected].value = 0;
				}
			}
		}
		if (Input.GetKey(KeyCode.RightArrow))
		{
			if (currentMenu.children[selected].kind == UIOption.Kind.Slider)
			{
				if (Time.time - lastSliderSound > sliderSoundDelay)
				{
					PlaySliderSound();
				}

				currentMenu.children[selected].value += 1f * Time.deltaTime;
				if (currentMenu.children[selected].value > 1)
				{
					currentMenu.children[selected].value = 1;
				}
			}
		}

		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.X))
		{
			switch(currentMenu.children[selected].kind) {
				case UIOption.Kind.Button:
					if (currentMenu.children[selected].locked) break;
					//Debug.Log(currentMenu.children[selected].text.text);
					currentMenu.children[selected].onSelect?.Invoke();
					break;
				case UIOption.Kind.Slider:
					break;
				case UIOption.Kind.Switch:
					//flip current value
					currentMenu.children[selected].value = (currentMenu.children[selected].value == 0) ? 1 : 0;
					//tick box
					currentMenu.children[selected].Highlight();
					break;	
			}

		}
		if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Z))
		{
			Cancel();
		}
	}
	void PlaySliderSound() {
		lastSliderSound = Time.time;
		PlayerInput.ins.KeyClick();
    }
	public void DiplomacyScreen()
	{
		SwitchMenus(currentMenu, menu_diplo);
	}
	public void DefenseScreen() {
		SwitchMenus(currentMenu, menu_defense);
    }
	public void BuildScreen()
	{
		SwitchMenus(currentMenu, menu_build);
		PlayerInput.ins.ToggleBuildMode(true);
	}
	public void SelectBuildConfirm() {
		UIBuildConfirm.buildType = Mathf.RoundToInt(currentMenu.children[selected].value);
		SwitchMenus(currentMenu, menu_build_confirm);
		PlayerInput.ins.ToggleBuildMode(true);
	}
	public void SelectNation() {
		targetNation = (int)currentMenu.children[selected].value;
		SwitchMenus(currentMenu, menu_nation);
	}
	public void StrikeNation()
	{
		SwitchMenus(currentMenu, menu_strike);
		PlayerInput.ins.ToggleAirMode(true);
	}
	public void AirDoctrine()
	{
		SwitchMenus(currentMenu, menu_airdoctrine);
		PlayerInput.ins.ToggleAirMode(true);
	}
	public void BasesMenu() {
		SwitchMenus(currentMenu, menu_bases);
		PlayerInput.ins.ToggleBuildMode(true);
	}
	public void FeelingsMenu()
	{
		SwitchMenus(currentMenu, menu_feelings);
	}
	public void ResearchMenu()
	{
		SwitchMenus(currentMenu, menu_research);
	}
	public void RTopicMenu()
	{
		menu_rtopic.GetComponent<RTopicMenu>().branch = (Research.Branch)currentMenu.children[selected].value;
		SwitchMenus(currentMenu, menu_rtopic);
	}
	public void Cancel() {
		if (currentMenu.parent != null)
		{
			SwitchMenus(currentMenu, currentMenu.parent);
		}
	}
	public void SwitchMenus(UIMenu start, UIMenu end) {

		DisplayHandler.ins.TogglePopStrikeScreen(end == menu_strike);
		PlayerInput.ins.ToggleBuildMode(end == menu_build || end == menu_build_confirm);
		PlayerInput.ins.ToggleAirMode(end == menu_airdoctrine);
		if (start == menu_feelings) Relationships.ins.Clear();
		if (start == menu_diplo) Relationships.ins.Clear();
		if (end == menu_feelings) Relationships.ins.Draw();
		if (end == menu_diplo) Relationships.ins.Draw();
		if (end == menu_diplo) targetNation = 0;

		if(start.children.Length > 0) {
			start.children[selected].UnHighlight();
		}

		start.gameObject.SetActive(false);
		currentMenu = end;
		currentMenu.gameObject.SetActive(true);

		if (currentMenu.preserveLastSelected)
		{ 
			selected = currentMenu.lastSelected;
		}
		else {
			selected = currentMenu.defaultSelected;
		}

		if (selected >= currentMenu.children.Length) selected = currentMenu.children.Length - 1;

		for(int i = 0; i < end.children.Length; i++)
		{
			end.children[i].UnHighlight();
		}
		currentMenu.children[selected].Highlight();

		if (currentMenu.stateColor != null)
		{
			currentMenu.stateColor.color = Map.ins.state_colors[targetNation];
			currentMenu.stateColor.text = Diplomacy.state_names[targetNation];
		}

		InfoPanel.instance.RefreshAtWarScreen();
	}

	public void WarToggle() {
		if(ROE.AreWeAtWar(0, targetNation)) {
			if (Diplomacy.peaceOffers[0, targetNation]) {
				//withdraw peace offer
				Diplomacy.peaceOffers[0, targetNation] = false;
			}
			else { 
				Diplomacy.OfferPeace(0, targetNation);
			}
		}
		else {
			ROE.DeclareWar(0, targetNation);
		}
	}
	public void LaunchMissiles() {
		//ROE.DeclareWar(0, targetNation);
		float sat = (menu_strike as UIStrikeMenu).saturationSlider.value * 20;
		int sati = Mathf.CeilToInt(Mathf.Max(1, sat));

		List<Target> tars = GetTargets(UI.ins.targetNation, sati, 
	    menu_strike.children[0].value == 1,
		menu_strike.children[1].value == 1,
		menu_strike.children[2].value == 1);

		State_AI player = Diplomacy.states[0] as State_AI;
		int missilesAway = player.ICBMStrike(sati, TargetSort(tars.ToArray()).ToList(), targetNation);
		if(missilesAway < 1) {
			return;
		}
		SFX.ins.NewSource(SFX.ins.countdown, 0.05f);
		(menu_strike as UIStrikeMenu).UpdateStrikePlanScreen();
		FinalCam.ins.MissileLaunch();
		if (Simulator.tutorialOverride) {
			TutorialOverride.hasLaunchedMissiles = true;
		}

	}
	void ChangeSelected(int dir) {
		int osel = selected;
		int nsel = selected + dir;
		if (nsel < 0) nsel = 0;
		if (nsel > currentMenu.children.Length - 1) nsel = currentMenu.children.Length - 1;
		if (osel > currentMenu.children.Length - 1) osel = currentMenu.children.Length - 1;
		currentMenu.children[osel].UnHighlight();
		currentMenu.children[nsel].Highlight();
		selected = nsel;
		currentMenu.lastSelected = selected;
		if (selected > currentMenu.children.Length) selected = currentMenu.children.Length - 1;
	}
	public void ReconsiderStatehood() {
		List<UIOption> toadd = new();
		bool reselect = false;
		for(int i = 0; i < menu_diplo.children.Length; i++) {
			if (Diplomacy.states[Mathf.RoundToInt(menu_diplo.children[i].value)].alive) {
				toadd.Add(menu_diplo.children[i]);
			}
			else {
				Destroy(menu_diplo.children[i].gameObject);
				reselect = true;
			}
		}
		menu_diplo.children = toadd.ToArray();
		if (reselect && currentMenu == menu_diplo) ChangeSelected(-1);
		RedrawDiploMenu();

    }
	void RedrawDiploMenu() {
		for (int i = 0; i < menu_diplo.children.Length; i++)
		{
			Vector2 pos = textOrigin;
			pos.y -= i * infoSpacer;
			menu_diplo.children[i].transform.localPosition = pos;
		}
	}

	void Reset()
	{
		DisplayHandler.resetGame -= Reset;
		LaunchDetection.launchDetectedAction -= LaunchDetect;
	}

	void LaunchDetect(Vector2 launchPos, Vector2 targetPos, int perp, int victim)
	{
		if (victim == playerTeam)
		{
			//osc = true;
			//PromptNuclear();
			incomingMissiles++;
			Invoke(nameof(RemoveIncoming), MapUtils.Tau(launchPos, targetPos));

			//annoying as fuck
			//nationSelected = perp;
			//SwitchMenu((int)Menu.strike);
		}
	}
	void RemoveIncoming()
	{
		incomingMissiles--;
	}
}
