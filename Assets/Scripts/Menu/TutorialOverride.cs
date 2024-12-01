using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialOverride : MonoBehaviour
{
	//this class has the difficult job of managing to stop the gameplay and teach the player how cities work
	public static bool hasLaunchedMissiles;
	public static bool railroad; //lock everything

	private void Start()
	{
		if (!Simulator.tutorialOverride) return;
		StartCoroutine(nameof(TutorialProcedure));
	}

	IEnumerator TutorialProcedure() {

		yield return null; //wait for the old scene to unload
		railroad = true;
		DisplayHandler.ins.locked = true;
		UI.ins.locked = true;
		ConsolePanel.ins.toolTipLockout = true;
		ConsolePanel.ins.toolhead.text = "";
		ConsolePanel.ins.tooltext.text = "";
		//ConsolePanel.ins. = "";
		DisplayHandler.ins.TutorialBlack(); //black out distracting screens
		MoveCam.ins.canMove = false;

		//spawn troops for later invasion
		for (int i = 0; i < 100; i++)
		{
			Vector2Int wp = MapUtils.RandomPointInState(0);

			if (Map.ins.GetPixTeam(wp) < 0) continue;
			Transform t = Instantiate(ArmyManager.ins.armyPrefab, MapUtils.CoordsToPoint(wp), Quaternion.identity, ArmyManager.ins.transform).transform;
		}


		//INTRODUCTION TO CITIES

		int tries = 0;
		City focus;
		List<City> ignore = new List<City>();
		int numCities = ArmyUtils.GetCities(0).Count;
		do
		{
			tries++;
			focus = ArmyUtils.NearestCity(Vector2.one * 0.5f * Map.localScale, 0, ignore);
			ignore.Add(focus);

		} while (focus.truepop > 5 && tries < numCities);
		Debug.Log("truepop" + focus.truepop);
		MoveCam.ins.transform.position = (Vector3)focus.wpos - Vector3.forward * 20;
		Camera.main.orthographicSize = 30;
		Camera.main.cullingMask = PlayerInput.ins.tutorialMask;
		Debug.Log("set camera to: " + Camera.main.orthographicSize);
		Time.timeScale = 0.1f;
		yield return new WaitForSecondsRealtime(1);

		ConsolePanel.Log("this is your city.", float.PositiveInfinity);
		yield return new WaitForSecondsRealtime(2);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("the brightness represents population density.", float.PositiveInfinity);
		yield return new WaitForSecondsRealtime(2);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("your city can grow, like this:");
		Time.timeScale = 1;
		Map.ins.populationGrowthTickDelay = 0f;
		Map.ins.growth_tutorialManualValues = true;
		Map.ins.growth_deltaOverride = 0.0015f;
		Map.ins.growth_stateGrowthTickOverride = 1;

		yield return new WaitForSecondsRealtime(5);
		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("or shrink, like this:");
		Map.ins.populationGrowthTickDelay = 0f;
		Map.ins.growth_tutorialManualValues = true;
		Map.ins.growth_deltaOverride = 10f;
		Map.ins.growth_stateGrowthTickOverride = -1;

		yield return new WaitForSecondsRealtime(8);
		ConsolePanel.ins.tooltext.text = "press space to continue";
		Map.ins.populationGrowthTickDelay = 0.25f;
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		//NOW MOVING ON TO COUNTRIES

		//fix growth
		ConsolePanel.ins.tooltext.text = "";
		Map.ins.growth_tutorialManualValues = false;
		Map.ins.populationGrowthTickDelay = 0.25f;

		//reposition camera onto state center
		MoveCam.ins.canMove = false;
		Vector3 center = MapUtils.CoordsToPoint(Map.ins.state_centers[0]);
		MoveCam.ins.transform.position = center - Vector3.forward * 20;
		Camera.main.orthographicSize = 300;

		ConsolePanel.Clear();
		ConsolePanel.Log("you have many cities.", float.PositiveInfinity);
		//grow back a little from the shrinkage
		Map.ins.populationGrowthTickDelay = 0f;
		Map.ins.growth_tutorialManualValues = true;
		Map.ins.growth_deltaOverride = 0.015f;
		Map.ins.growth_stateGrowthTickOverride = 1;
		yield return new WaitForSecondsRealtime(2);

		Map.ins.populationGrowthTickDelay = 0.25f;
		Map.ins.growth_tutorialManualValues = false;
		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("it's <color=#ff0000> your </color> responsibility to make sure it stays that way.", float.PositiveInfinity);
		yield return new WaitForSecondsRealtime(2);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		Camera.main.cullingMask = PlayerInput.ins.regularMask;
		ConsolePanel.Clear();
		ConsolePanel.Log("the white dots are armies.", float.PositiveInfinity);
		yield return new WaitForSecondsRealtime(2);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		//reposition camera onto state center
		MoveCam.ins.canMove = false;
		center = MapUtils.CoordsToPoint(Map.ins.state_centers[1]);
		MoveCam.ins.transform.position = center - Vector3.forward * 20;
		Camera.main.orthographicSize = 300;

		ConsolePanel.Clear();
		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("this is a rival nation.", float.PositiveInfinity);
		yield return new WaitForSecondsRealtime(2);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("they pose a threat to you.", float.PositiveInfinity);
		yield return new WaitForSecondsRealtime(2);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		//UI TUTORIAL
		Research.unlockedUpgrades[0][(int)Research.Branch.silo] = 5;
		Research.unlockedUpgrades[0][(int)Research.Branch.ground] = 5;
		Research.ResearchChange[0]?.Invoke(); //tell armies they're big ups
		Camera.main.orthographicSize = 600;
		UI.ins.targetNation = 1;
		UI.ins.locked = false;
		railroad = false;

		ConsolePanel.Clear();
		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("you have been granted access to the computer controls");
		ConsolePanel.Log(" ");
		yield return new WaitForSecondsRealtime(1);
		ConsolePanel.Log("construct three missile silos", 9999);
		ConsolePanel.Log(" ");
		MoveCam.ins.canMove = true;
		DisplayHandler.ins.screens[3].Switch(-1);

		yield return new WaitForSecondsRealtime(1);

		State_AI state = Diplomacy.states[0] as State_AI;

		string numText = "";
		while (state.construction_sites.Count + ArmyUtils.silos[0].Count < 3) {

			if (UI.ins.currentMenu == UI.ins.menu_build_confirm)
			{
				if (UI.ins.selected == 0) {
					int built = state.construction_sites.Count + ArmyUtils.silos[0].Count;
					if(built == 0) {
						numText = "three";
					}
					if (built == 1)
					{
						if(numText != "two") {
							ConsolePanel.Log("Well done. Now two more.");
						}
						numText = "two";
					}
					if (built == 2)
					{
						if (numText != "one")
						{
							ConsolePanel.Log("Well done. Now one more.");
						}
						numText = "one";
					}
					ConsolePanel.ins.toolhead.text = "designate " + numText + " construction sites";
					ConsolePanel.ins.tooltext.text = "move the cursor with w, a, s, and d keys. q and e to zoom";
				}
				else {
					ConsolePanel.ins.tooltext.text = "navigate to 'confirm'";
				}
			}
			else
			{
				NavigateToBuildSilos();
			}
			yield return null;
		}
		ConsolePanel.Clear();
		ConsolePanel.Log("well done.");
		ConsolePanel.Log("now launch a pre-emptive strike.");
		ConsolePanel.Log("press 'tab' three times to return to the home menu");
		ConsolePanel.ins.toolhead.text = "launch a preemptive strike";

		while (!hasLaunchedMissiles)
		{
			if(UI.ins.currentMenu != UI.ins.menu_strike) {
				NavigateToStrike();
			}
			else { 
				//tick boxes and launch
				if (UI.ins.currentMenu.children[2].value != 1) {
					ConsolePanel.ins.toolhead.text = "select targets";
					ConsolePanel.ins.tooltext.text = "tick all the boxes";
				}
				else if (UI.ins.selected != 4)
				{
					ConsolePanel.ins.toolhead.text = "navigate to 'launch'";
					ConsolePanel.ins.tooltext.text = "press 'down' on the arrow keys";
				}
				else {
					ConsolePanel.ins.toolhead.text = "launch a preemptive strike";
					ConsolePanel.ins.tooltext.text = "press space to launch";
				}
			}

			yield return null;
		}
		yield return new WaitForSecondsRealtime(5);
		ConsolePanel.Clear();
		ConsolePanel.Log("well done");
		ConsolePanel.ins.toolhead.text = "finish this";
		ConsolePanel.ins.tooltext.text = "do not allow the possibility of retaliation";
		//DisplayHandler.ins.UnPause();
		DisplayHandler.ins.locked = false;

		////NUKE TIME
		//ConsolePanel.ins.tooltext.text = "";
		//Camera.main.orthographicSize = 600;
		//UI.ins.targetNation = 1;
		//UI.ins.StrikeNation();
		//UI.ins.currentMenu.children[0].value = 1;
		//UI.ins.currentMenu.children[0].BoxTick();
		//UI.ins.currentMenu.children[1].value = 1;
		//UI.ins.currentMenu.children[1].BoxTick();
		//UI.ins.currentMenu.children[2].value = 1;
		//UI.ins.currentMenu.children[2].BoxTick();
		//UI.ins.currentMenu.children[3].value = 1;
		//UI.ins.currentMenu.children[UI.ins.selected].UnHighlight();
		//UI.ins.selected = 4;
		//UI.ins.currentMenu.children[4].Highlight();

		////ConsolePanel.ins.toolTipLockout = false;
		//DisplayHandler.ins.UnPause();
		//DisplayHandler.ins.locked = false;

		//war declared, missiles being launched
		InvokeRepeating(nameof(Conscript), 0, 5);
	}

	void Conscript() {
		Diplomacy.states[0].SpawnTroops(5);
    }

	void NavigateToBuildSilos()
	{
		if(UI.ins.currentMenu == UI.ins.menu_home) {
			ConsolePanel.ins.toolhead.text = "navigate to 'defense'";
			if (UI.ins.selected > 1)
			{
				ConsolePanel.ins.tooltext.text = "press 'up' on the arrow keys";
			}
			else if (UI.ins.selected < 1)
			{
				ConsolePanel.ins.tooltext.text = "press 'down' on the arrow keys";
			}
			else
			{
				ConsolePanel.ins.tooltext.text = "press 'space' to select";
			}
		}else if(UI.ins.currentMenu == UI.ins.menu_defense)
		{
			ConsolePanel.ins.toolhead.text = "navigate to 'new base'";
			if (UI.ins.selected > 2)
			{
				ConsolePanel.ins.tooltext.text = "press 'up' on the arrow keys";
			}
			else if (UI.ins.selected < 2)
			{
				ConsolePanel.ins.tooltext.text = "press 'down' on the arrow keys";
			}
			else
			{
				ConsolePanel.ins.tooltext.text = "press 'space' to select";
			}
		}
		else if (UI.ins.currentMenu == UI.ins.menu_build)
		{
			ConsolePanel.ins.toolhead.text = "select 'silo'";
			ConsolePanel.ins.tooltext.text = "";
		}
		else 
		{
			ConsolePanel.ins.toolhead.text = "return to correct menu";
			ConsolePanel.ins.tooltext.text = "press 'tab' to go back";
		}
	}
	void NavigateToStrike()
	{ 
		if(UI.ins.currentMenu == UI.ins.menu_home) {
			ConsolePanel.ins.toolhead.text = "navigate to 'diplomacy'";
			if (UI.ins.selected > 0)
			{
				ConsolePanel.ins.tooltext.text = "press 'up' on the arrow keys";
			}
			else if (UI.ins.selected < 0)
			{
				ConsolePanel.ins.tooltext.text = "press 'down' on the arrow keys";
			}
			else
			{
				ConsolePanel.ins.tooltext.text = "press 'space' to select";
			}
		} else if (UI.ins.currentMenu == UI.ins.menu_diplo)
		{
			ConsolePanel.ins.toolhead.text = "select the enemy";
			if (UI.ins.selected > 0)
			{
				ConsolePanel.ins.tooltext.text = "press 'up' on the arrow keys";
			}
			else if (UI.ins.selected < 0)
			{
				ConsolePanel.ins.tooltext.text = "press 'down' on the arrow keys";
			}
			else
			{	
				ConsolePanel.ins.tooltext.text = "press 'space' to select";
			}
		}
		else if (UI.ins.currentMenu == UI.ins.menu_nation)
		{
			ConsolePanel.ins.toolhead.text = "select 'pre-emptive strike'";
			if (UI.ins.selected > 3)
			{
				ConsolePanel.ins.tooltext.text = "press 'up' on the arrow keys";
			}
			else if (UI.ins.selected < 3)
			{
				ConsolePanel.ins.tooltext.text = "press 'down' on the arrow keys";
			}
			else
			{
				ConsolePanel.ins.tooltext.text = "press 'space' to select";
			}
		}
		else {
			ConsolePanel.ins.toolhead.text = "navigate back to the home menu";
			ConsolePanel.ins.tooltext.text = "press 'tab' to go back";
		}
	}
}
