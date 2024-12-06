using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialOverride : MonoBehaviour
{
	//this class has the difficult job of managing to stop the gameplay and teach the player how cities work
	public static bool hasLaunchedMissiles;
	public static bool railroad; //locking input and menus
	public static bool showMenu; //specifically unlock the left menu
	public static bool showAllPanels;

	private void Start()
	{
		if (!Simulator.tutorialOverride) return;
		if(Simulator.activeScenario.tutorial == 1) {
			StartCoroutine(nameof(TutorialProcedure));
		}
		else if(Simulator.activeScenario.tutorial == 2) {
			StartCoroutine(nameof(Tutorial2));
		}

	}
	IEnumerator Tutorial2() {
		yield return new WaitForSecondsRealtime(0.1f); //wait for the old scene to unload
		DisplayHandler.ins.Pause();
		DisplayHandler.ins.UnPause();
		railroad = true;
		showMenu = false;
		showAllPanels = false;
		DisplayHandler.ins.locked = true;
		UI.ins.locked = true;
		ConsolePanel.ins.toolTipLockout = true;
		ConsolePanel.ins.toolhead.text = "";
		ConsolePanel.ins.tooltext.text = "";
		//ConsolePanel.ins. = "";
		DisplayHandler.ins.TutorialBlack(); //black out distracting screens
		MoveCam.ins.canMove = false;

		Time.timeScale = 1;
		Map.ins.populationGrowthTickDelay = 0f;
		Map.ins.growth_tutorialManualValues = true;
		Map.ins.growth_deltaOverride = 0.0015f;
		Map.ins.growth_stateGrowthTickOverride = 1;
		//yield return new WaitForSecondsRealtime(2);

		//reposition camera onto state center
		Vector3 center = MapUtils.CoordsToPoint(Map.ins.state_centers[0]);
		MoveCam.ins.transform.position = center - Vector3.forward * 20;
		Camera.main.orthographicSize = 400;

		ConsolePanel.Log("your country is strong");
		yield return new WaitForSecondsRealtime(1);

		//fix growth
		Map.ins.populationGrowthTickDelay = 0.25f;
		Map.ins.growth_tutorialManualValues = false;

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Clear();
		ConsolePanel.Log("it can sustain a powerful army");
		yield return new WaitForSecondsRealtime(1);
		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";

		List<Unit> bases = new();
		bases.AddRange(ArmyUtils.GetSilos(1));
		List<Unit> air = new();
		air.AddRange(ArmyUtils.GetAirbases(1));
		List <Unit> planes = new();
		planes = ArmyUtils.GetAircraftList(1);
		ConsolePanel.Clear();
		ConsolePanel.Log("but your enemies have sophisticated weaponry");
		yield return new WaitForSecondsRealtime(1f);

		float t = -0.49f;
		int n = -1;
		while (!Input.GetKeyDown(KeyCode.Space) || (n < 3)) {
			t += Time.deltaTime;
			int pn = n;
			n = Mathf.RoundToInt(t * 0.33f);
			//Debug.Log(n);
			if(n % 3 == 0) {
				center = bases[n % bases.Count].transform.position;
				if (pn != n && n == 0)
				{
					ConsolePanel.Log("a mix of missile silos");
				}
			}
			if (n % 3 == 1)
			{
				center = air[n % air.Count].transform.position;
				if (pn != n && n == 1)
				{
					ConsolePanel.Log("airbases");
				}
			}
			if (n % 3 == 2)
			{
				center = planes[n % planes.Count].transform.position;
				if (pn != n && n == 2)
				{
					ConsolePanel.Log("and nuclear capable fighter-bombers");
				}
			}
			if (pn != n && n == 3)
			{
				ConsolePanel.ins.tooltext.text = "press space to continue";
			}
			
			MoveCam.ins.transform.position = center - Vector3.forward * 20;
			Camera.main.orthographicSize = 100;
			yield return null;
		}

		//reposition camera onto enemy center
		center = MapUtils.CoordsToPoint(Map.ins.state_centers[1]) + MapUtils.CoordsToPoint(Map.ins.state_centers[0]);
		MoveCam.ins.transform.position = (center * 0.5f) - Vector3.forward * 20;
		Camera.main.orthographicSize = 400;

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Clear();
		ConsolePanel.Log("a war with them risks unsustainable losses");
		yield return new WaitForSecondsRealtime(1);
		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
		ConsolePanel.ins.tooltext.text = "";

		ConsolePanel.Clear();
		ConsolePanel.Log("to protect your invasion forces");
		ConsolePanel.Log("you will build: ");
		ConsolePanel.Log("anti aircraft artillery batteries");
	
		yield return new WaitForSecondsRealtime(3);
		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
		ConsolePanel.ins.tooltext.text = "";

		DisplayHandler.ins.ResetAll();
		showAllPanels = true;
		railroad = false;
		showMenu = false;

		ConsolePanel.Clear();
		ConsolePanel.Log("first, research air defense");
		ConsolePanel.Log("for demonstration, we've already given you the first four tiers of research");
		yield return new WaitForSecondsRealtime(1);
		State state = Diplomacy.states[0];
		while (Research.currentlyResearching[0].x != (int)Research.Branch.aaa)
		{
			if (UI.ins.currentMenu != UI.ins.menu_rtopic)
			{
				NavigateToResearch();
			}
			else
			{
				ConsolePanel.ins.toolhead.text = "select abm capable";
				ConsolePanel.ins.tooltext.text = "press space to select";
				if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
				{
					UI.ins.currentMenu.children[UI.ins.selected].onSelect?.Invoke();
				}
			}

			yield return null;
		}
		DisplayHandler.ins.locked = false;
		UI.ins.locked = false;
		MoveCam.ins.canMove = true;
		ConsolePanel.Clear();
		ConsolePanel.Log("<color=\"green\"> research started </color>");
		ConsolePanel.Log("well done");
		ConsolePanel.Log("now, construct three air defense batteries");
		yield return new WaitForSecondsRealtime(1);
		ConsolePanel.Log("these sites will defend your cities and armies from aerial bombardment");
		string numText = "";
		while (state.construction_sites.Count + ArmyUtils.batteries[0].Count < 3)
		{
			if (UI.ins.currentMenu == UI.ins.menu_build_confirm)
			{
				int built = state.construction_sites.Count + ArmyUtils.batteries[0].Count;
				if (built == 0)
				{
					numText = "3";
				}
				if (built == 1)
				{
					if (numText != "3")
					{
						ConsolePanel.Clear();
						ConsolePanel.Log("Well done. Now two more.");
					}
					numText = "2";
				}
				if (built == 2)
				{
					if (numText != "1")
					{
						ConsolePanel.Log("Well done. Now one more.");
					}
					numText = "1";
				}
				ConsolePanel.ins.toolhead.text = "designate " + numText + " build sites";
				ConsolePanel.ins.tooltext.text = "use 'wasdqe' keys to move cursor";
			}
			else
			{
				NavigateToBuildItem("air defense");
			}

			yield return null;
		}
		ConsolePanel.Clear();
		ConsolePanel.Log("well done");
		ConsolePanel.Log("now, conscript 150 thousand soldiers");
		ConsolePanel.Log("you can see your army count on the panel to the right");
		while (ArmyUtils.armies[0].Count < 150)
		{
			if (UI.ins.currentMenu == UI.ins.menu_defense)
			{
				ConsolePanel.ins.toolhead.text = "conscript 150k soldiers";
				UpDownSpace(0);
			} 
			else
			{
				NavigateToDefense();
			}

			yield return null;
		}
		ConsolePanel.Clear();
		ConsolePanel.Log("well done");
		ConsolePanel.Log("invade when ready");
		ConsolePanel.ins.toolhead.text = "";
		ConsolePanel.ins.tooltext.text = "";
		while (ROE.AreWeAtWar(0, 1) == false)
		{
			if (UI.ins.currentMenu == UI.ins.menu_nation)
			{
				ConsolePanel.ins.toolhead.text = "declare war";
				UpDownSpace(1);
			}
			else
			{
				NavigateToEnemy();
			}

			yield return null;
		}
		ConsolePanel.Clear();
		ConsolePanel.Log("good");
		ConsolePanel.Log("keep your army strength above 150");
		ConsolePanel.Log("build more air defenses if necessary");
		ConsolePanel.ins.toolhead.text = "continue the attack";
		ConsolePanel.ins.tooltext.text = "keep army count over 150k";
	}
	IEnumerator TutorialProcedure() {

		yield return null; //wait for the old scene to unload

		railroad = true;
		showMenu = false;
		DisplayHandler.ins.locked = true;
		UI.ins.locked = true;
		ConsolePanel.ins.toolTipLockout = true;
		ConsolePanel.ins.toolhead.text = "";
		ConsolePanel.ins.tooltext.text = "";
		//ConsolePanel.ins. = "";
		WideScreenCam.ins.Refresh();
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

		ConsolePanel.Log("this is your city.");
		yield return new WaitForSecondsRealtime(0.5f);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("the brightness represents population density.");
		yield return new WaitForSecondsRealtime(1);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("your city can grow, like this:");
		Time.timeScale = 1;
		Map.ins.populationGrowthTickDelay = 0.01f;
		Map.ins.growth_tutorialManualValues = true;
		Map.ins.growth_deltaOverride = 0.0015f;
		Map.ins.growth_stateGrowthTickOverride = 1;

		yield return new WaitForSecondsRealtime(3);
		Map.ins.growth_tutorialManualValues = false;
		Map.ins.populationGrowthTickDelay = 0.25f;
		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("or shrink, like this:");
		Map.ins.populationGrowthTickDelay = 0f;
		Map.ins.growth_tutorialManualValues = true;
		Map.ins.growth_deltaOverride = 10f;
		Map.ins.growth_stateGrowthTickOverride = -1;

		yield return new WaitForSecondsRealtime(5);
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
		ConsolePanel.Log("you have many cities.");
		//grow back a little from the shrinkage
		Map.ins.populationGrowthTickDelay = 0f;
		Map.ins.growth_tutorialManualValues = true;
		Map.ins.growth_deltaOverride = 0.015f;
		Map.ins.growth_stateGrowthTickOverride = 1;
		yield return new WaitForSecondsRealtime(1);

		Map.ins.populationGrowthTickDelay = 0.25f;
		Map.ins.growth_tutorialManualValues = false;
		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("it's <color=#ff0000> your </color> responsibility to make sure it stays that way.");
		yield return new WaitForSecondsRealtime(1);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
		ConsolePanel.ins.tooltext.text = "";
		Camera.main.cullingMask = PlayerInput.ins.regularMask;
		ConsolePanel.Clear();
		ConsolePanel.Log("the white dots are armies.");
		yield return new WaitForSecondsRealtime(0.5f);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		//reposition camera onto state center
		MoveCam.ins.canMove = false;
		center = MapUtils.CoordsToPoint(Map.ins.state_centers[1]);
		MoveCam.ins.transform.position = center - Vector3.forward * 20;
		Camera.main.orthographicSize = 300;

		ConsolePanel.Clear();
		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("this is a rival nation.");
		yield return new WaitForSecondsRealtime(1);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("they pose a threat to you.");
		yield return new WaitForSecondsRealtime(1);

		ConsolePanel.ins.tooltext.text = "press space to continue";
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

		//UI TUTORIAL
		Research.unlockedUpgrades[0][(int)Research.Branch.silo] = 5;
		Research.unlockedUpgrades[0][(int)Research.Branch.ground] = 5;
		Research.ResearchChange[0]?.Invoke(); //tell armies they're big ups
		Camera.main.orthographicSize = 600;
		UI.ins.targetNation = 1;

		//new railroad attempt
		//UI.ins.locked = false;
		//railroad = false;


		ConsolePanel.Clear();
		ConsolePanel.ins.tooltext.text = "";
		ConsolePanel.Log("you have been granted access to the computer controls");
		yield return new WaitForSecondsRealtime(1);
		ConsolePanel.Log("construct three missile silos", 9999);
		MoveCam.ins.canMove = true;
		DisplayHandler.ins.screens[3].Switch(-1);
		showMenu = true;

		yield return new WaitForSecondsRealtime(1);

		State_AI state = Diplomacy.states[0] as State_AI;

		string numText = "";
		while (state.construction_sites.Count + ArmyUtils.silos[0].Count < 3) {

			if (UI.ins.currentMenu == UI.ins.menu_build_confirm)
			{
				if (UI.ins.selected == 0) {
					int built = state.construction_sites.Count + ArmyUtils.silos[0].Count;
					if(built == 0) {
						numText = "3";
					}
					if (built == 1)
					{
						if(numText != "2") {
							ConsolePanel.Log("Well done. Now two more.");
						}
						numText = "2";
					}
					if (built == 2)
					{
						if (numText != "1")
						{
							ConsolePanel.Log("Well done. Now one more.");
						}
						numText = "1";
					}
					ConsolePanel.ins.toolhead.text = "designate " + numText + " build sites";
					ConsolePanel.ins.tooltext.text = "use 'wasdqe' keys to move cursor";
				}
				else {
					ConsolePanel.ins.tooltext.text = "navigate to 'confirm'";
				}
			}
			else
			{
				NavigateToBuildItem("icbm silo");
			}
			yield return null;
		}
		UI.ins.locked = false;
		ConsolePanel.Clear();
		ConsolePanel.Log("well done.");
		ConsolePanel.Log("now launch a pre-emptive strike.");
		ConsolePanel.Log("press 'tab' three times to return to the home menu");
		ConsolePanel.ins.toolhead.text = "launch a preemptive strike";
		bool r1 = false, r2 = false, r3 = false;
		while (!hasLaunchedMissiles)
		{
			if(!r1 && UI.ins.currentMenu == UI.ins.menu_home) {
				r1 = true;
				ConsolePanel.Clear();
				ConsolePanel.Log("now select 'diplomacy'");
			}
			if (!r2 && UI.ins.currentMenu == UI.ins.menu_nation)
			{
				r2 = true;
				ConsolePanel.Clear();
				ConsolePanel.Log("select your enemy");
			}
			if (!r3 && UI.ins.currentMenu == UI.ins.menu_nation)
			{
				r3 = true;
				ConsolePanel.Clear();
				ConsolePanel.Log("launch a pre-emptive strike");
			}
			if (UI.ins.currentMenu != UI.ins.menu_strike) {
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

	void NavigateToBuildItem(string itemname)
	{
		if(UI.ins.currentMenu == UI.ins.menu_home) {
			ConsolePanel.ins.toolhead.text = "navigate to 'defense'";
			if  (UI.ins.locked) {
				UpDownSpace_Restrictive(1);
			}
			else {
				UpDownSpace(1);
			}
		}
		else if(UI.ins.currentMenu == UI.ins.menu_defense)
		{
			ConsolePanel.ins.toolhead.text = "navigate to 'new base'";
			if (UI.ins.locked)
			{
				UpDownSpace_Restrictive(2);
			}
			else
			{
				UpDownSpace(2);
			}
		}
		else if (UI.ins.currentMenu == UI.ins.menu_build)
		{
			UI.ins.locked = false;
		
			ConsolePanel.ins.toolhead.text = "select '" + itemname + "'";
			ConsolePanel.ins.tooltext.text = "";
		}
		else 
		{
			UI.ins.locked = false;
			ConsolePanel.ins.toolhead.text = "navigate back to the home menu";
			ConsolePanel.ins.tooltext.text = "press 'tab' to go back";
		}
	}
	void NavigateToStrike()
	{ 
		if(UI.ins.currentMenu == UI.ins.menu_home) {
			ConsolePanel.ins.toolhead.text = "navigate to 'diplomacy'";
			UpDownSpace(0);
		} else if (UI.ins.currentMenu == UI.ins.menu_diplo)
		{
			ConsolePanel.ins.toolhead.text = "select the enemy";
			UpDownSpace(0);
		}
		else if (UI.ins.currentMenu == UI.ins.menu_nation)
		{
			ConsolePanel.ins.toolhead.text = "select 'pre-emptive strike'";
			UpDownSpace(3);
		}
		else {
			ConsolePanel.ins.toolhead.text = "navigate back to the home menu";
			ConsolePanel.ins.tooltext.text = "press 'tab' to go back";
		}
	}

	void NavigateToEnemy()
	{
		if (UI.ins.currentMenu == UI.ins.menu_home)
		{
			ConsolePanel.ins.toolhead.text = "navigate to 'diplomacy'";
			UpDownSpace(0);
		}
		else if (UI.ins.currentMenu == UI.ins.menu_diplo)
		{
			ConsolePanel.ins.toolhead.text = "select the enemy";
			UpDownSpace(0);
		}
		else
		{
			ConsolePanel.ins.toolhead.text = "navigate back to the home menu";
			ConsolePanel.ins.tooltext.text = "press 'tab' to go back";
		}
	}

	void NavigateToResearch()
	{
		if (UI.ins.currentMenu == UI.ins.menu_home)
		{
			ConsolePanel.ins.toolhead.text = "navigate to 'research'";
			UpDownSpace_Restrictive(2);
		}
		else if (UI.ins.currentMenu == UI.ins.menu_research)
		{
			ConsolePanel.ins.toolhead.text = "select air defense";
			UpDownSpace_Restrictive(1);
		}
		else
		{
			ConsolePanel.ins.toolhead.text = "navigate back to the home menu";
			ConsolePanel.ins.tooltext.text = "press 'tab' to go back";
			if (Input.GetKeyDown(KeyCode.Return))
			{
				UI.ins.Cancel();
			}
		}
	}
	void NavigateToDefense()
	{
		if (UI.ins.currentMenu == UI.ins.menu_home)
		{
			ConsolePanel.ins.toolhead.text = "navigate to 'defense'";
			UpDownSpace(1);
		}
		else
		{
			ConsolePanel.ins.toolhead.text = "navigate back to the home menu";
			ConsolePanel.ins.tooltext.text = "press 'tab' to go back";
			if (Input.GetKeyDown(KeyCode.Return))
			{
				UI.ins.Cancel();
			}
		}
	}



	void UpDownSpace(int desired) {
		if (UI.ins.selected > desired)
		{
			ConsolePanel.ins.tooltext.text = "press 'up' on the arrow keys";
		}
		else if (UI.ins.selected < desired)
		{
			ConsolePanel.ins.tooltext.text = "press 'down' on the arrow keys";
		}
		else
		{
			ConsolePanel.ins.tooltext.text = "press 'space' to select";
		}
	}

	void UpDownSpace_Restrictive(int desired)
	{
		if (UI.ins.selected > desired)
		{
			ConsolePanel.ins.tooltext.text = "press 'up' on the arrow keys";
			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				UI.ins.ChangeSelected(-1);
			}
		}
		else if (UI.ins.selected < desired)
		{
			ConsolePanel.ins.tooltext.text = "press 'down' on the arrow keys";
			if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				UI.ins.ChangeSelected(1);
			}
		}
		else
		{
			ConsolePanel.ins.tooltext.text = "press 'space' to select";
			if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				UI.ins.currentMenu.children[UI.ins.selected].onSelect?.Invoke();
			}
		}
	}
}
