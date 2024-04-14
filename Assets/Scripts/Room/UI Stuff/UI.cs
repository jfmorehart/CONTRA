using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
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

	public UIMenu menu_main;
	public UIMenu menu_nation;
	public UIMenu menu_strike;

	public UIMenu currentMenu;

	public Vector2 textOrigin;
	public float infoSpacer;

	public void Start()
	{
		ins = this;
		LaunchDetection.launchDetectedAction += LaunchDetect;
		for(int i = 0; i < menu_main.children.Length; i++) {
			UIOption op = menu_main.children[i];
			Debug.Log(i);
			op.text.color = Map.ins.state_colors[(int)op.value];
			op.defaultColor = Map.ins.state_colors[(int)op.value];
			op.plaintext = Diplo.state_names[(int)op.value];
			op.text.text = Diplo.state_names[(int)op.value];
		}
		Cursor.lockState = CursorLockMode.Locked;
		currentMenu.children[selected].Highlight();
	}
	private void Update()
	{
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
				currentMenu.children[selected].value += 1f * Time.deltaTime;
				if (currentMenu.children[selected].value > 1)
				{
					currentMenu.children[selected].value = 1;
				}
			}
		}

		if (Input.GetKey(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
		{
			switch(currentMenu.children[selected].kind) {
				case UIOption.Kind.Button:
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
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			if(currentMenu.parent != null) {
				SwitchMenus(currentMenu, currentMenu.parent);
			}

		}
	}
	public void SelectNation() {
		targetNation = (int)currentMenu.children[selected].value;
		SwitchMenus(currentMenu, menu_nation);
	}
	public void StrikeNation()
	{
		SwitchMenus(currentMenu, menu_strike);
	}
	void SwitchMenus(UIMenu start, UIMenu end) {
		DisplayHandler.ins.TogglePopStrikeScreen(end == menu_strike);
		start.children[selected].UnHighlight();
		start.gameObject.SetActive(false);
		currentMenu = end;
		currentMenu.gameObject.SetActive(true);
		selected = currentMenu.lastSelected;
		if (selected > currentMenu.children.Length) selected = currentMenu.children.Length - 1;
		currentMenu.children[selected].Highlight();
		if (currentMenu.stateColor != null)
		{
			currentMenu.stateColor.color = Map.ins.state_colors[targetNation];
			currentMenu.stateColor.text = Diplo.state_names[targetNation];
		}
	}
	public void DeclareWar() {
		ROE.DeclareWar(0, targetNation);
	}
	public void LaunchMissiles() {
		float sat = (menu_strike as UIStrikeMenu).saturationSlider.value * 20;
		int sati = Mathf.CeilToInt(Mathf.Max(1, sat));

		List<Target> tars = GetTargets(UI.ins.targetNation, sati, 
	    menu_strike.children[0].value == 1,
		menu_strike.children[1].value == 1,
		menu_strike.children[2].value == 1);

		State_AI player = Diplo.states[0] as State_AI;
		player.ICBMStrike(sati, TargetSort(tars.ToArray()).ToList());
	}
	void ChangeSelected(int dir) {
		int osel = selected;
		int nsel = selected + dir;
		if (nsel < 0) nsel = 0;
		if (nsel > currentMenu.children.Length - 1) nsel = currentMenu.children.Length - 1;
		currentMenu.children[osel].UnHighlight();
		currentMenu.children[nsel].Highlight();
		selected = nsel;
		currentMenu.lastSelected = selected;
	}
	public void ReconsiderStatehood() {
		List<UIOption> toadd = new();
		bool reselect = false;
		for(int i = 0; i < menu_main.children.Length; i++) {
			int team = (int)menu_main.children[i].value;
			if (Map.ins.state_populations[team] > 0) {
				toadd.Add(menu_main.children[i]);
			}
			else {
				Destroy(menu_main.children[i].gameObject);
				reselect = true;
			}
		}
		menu_main.children = toadd.ToArray();
		if (reselect) ChangeSelected(-1);
		RedrawDiploMenu();

    }
	void RedrawDiploMenu() {
		for (int i = 0; i < menu_main.children.Length; i++)
		{
			Vector2 pos = textOrigin;
			pos.y -= i * infoSpacer;
			menu_main.children[i].transform.localPosition = pos;
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
