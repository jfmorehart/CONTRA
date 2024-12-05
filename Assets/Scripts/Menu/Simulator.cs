using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Simulator
{
    public static bool IsSetup = false;
	public static List<Scenario> scenarios;

    public static Scenario activeScenario;

    public static bool tutorialOverride;

	public static void Setup() {
        scenarios = new();

		Scenario sc;
		string description = "";
		//basic tutorial, overwhelm
		Scenario.ScenarioConditions conditions = new Scenario.ScenarioConditions();
		conditions.unlockedupgrades = new int[4][];
		conditions.unlockedupgrades[0] = new int[] { 5, 5, 5, 5 };
		conditions.unlockedupgrades[1] = new int[] { 0, 0, 0, 0 };
		double[] overmatch = new double[] { 0.8, 0.2 };
		int[][] teams = new int[2][];
		teams[0] = new int[] { 0 }; //team A is the player
		teams[1] = new int[] { 1 }; //team B is the enemy
		description = "tutorial instructs the candidate in the absolute fundamentals of nuclear war";
		sc = new Scenario("tutorial", description, 2, overmatch, teams, conditions);
		sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		sc.tutorial = 1;
		scenarios.Add(sc);

		//lvl 2
		double[] almosteven = new double[] { 0.7, 0.3};
		teams = new int[2][];
		teams[0] = new int[] { 0 }; //team A is the player
		teams[1] = new int[] { 1 }; //team B is the enemy
		conditions = new Scenario.ScenarioConditions();
		conditions.unlockedupgrades = new int[4][];
		conditions.unlockedupgrades[0] = new int[] { 5, 4, 0, 0 };
		conditions.unlockedupgrades[1] = new int[] { 0, 0, 3, 3};
		conditions.airbases = new int[2] { 0, 2};
		conditions.silos = new int[2] {0, 2};
		conditions.batteries = new int[2] { 0, 0};
		description = "scenario a familiarizes the player with aspects of research and conscription";
		sc = new Scenario("scenario a", description, 2, almosteven, teams, conditions);
		sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		sc.tutorial = 2;
		scenarios.Add(sc);

		//lvl 2
		double[] duoShowdown = new double [] {0.4, 0.2, 0.4}; 
		teams = new int[2][];
		teams[0] = new int[] { 0, 1 }; //team A is the player
		teams[1] = new int[] { 2 }; //team B is the enemy
		conditions = new Scenario.ScenarioConditions();
		//conditions.silos = new int[3] { 1, 0, 1 };
		//seeds where the borders engender the sorta thing im going for
		conditions.seedBucket = new int[]{585, 905, 874, 930, 494, 92};
		description = "'proxy' explores alliance dynamics by giving the player both a steadfast ally and a steadfast enemy";
		sc = new Scenario("proxy", description, 3, duoShowdown, teams, conditions);
		sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		scenarios.Add(sc);


		double[] tsizes = new double[] {0.5};
		teams = new int[2][];
		teams[0] = new int[] { }; //team A is the player
		teams[1] = new int[] { }; //team B is the enemy
		description = "'easy' gives the player a strong position, but also a large number of rival states to contend with";
		sc = new Scenario("easy", description, 5, tsizes, teams, null);
		sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		scenarios.Add(sc);

		//double[] buddy = new double[] {0.4, 0.1, 0.1, 0.1};
		//teams = new int[2][];
		//teams[0] = new int[] {0, 1}; //team A is the player
		//teams[1] = new int[] {2, 3}; //team B is the enemy
		//description = "scenario d gives the player two enemies, and an ally they'll likely need to protect in order to succeed";
		//sc = new Scenario("scenario d", description, 4, buddy, teams, null);
		//sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		//scenarios.Add(sc);

		//lvl 2
		double[] puppet = new double[] { 0.1, 0.3, 0.3, 0.3};
		teams = new int[2][];
		teams[0] = new int[] { 0, 2 }; //team A is the player
		teams[1] = new int[] { 1 }; //team B is the enemy
		conditions = new Scenario.ScenarioConditions();
		conditions.silos = new int[] { 0, 2, 2, 0};
		conditions.airbases = new int[] { 0, 1, 0, 0};
		conditions.unlockedupgrades = new int[4][];
		conditions.unlockedupgrades[0] = new int[] { 0, 0, 0, 0 };
		conditions.unlockedupgrades[1] = new int[] { 1, 0, 1, 1 };
		conditions.unlockedupgrades[2] = new int[] { 0, 0, 1, 0 };
		conditions.unlockedupgrades[3] = new int[] { 0, 0, 0, 0 };
		description = "'puppet' is a difficult scenario where the player has a strong enemy and a strong ally";
		sc = new Scenario("puppet", description, 4, puppet, teams, conditions);
		sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		scenarios.Add(sc);

		double[] fair = new double[] { 0.2};
		teams = null;
		description = "fair is a fair match";
		sc = new Scenario("fair", description, 5, fair, teams, null);
		sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		scenarios.Add(sc);

		double[] hard = new double[] {0.1, 0.2, 0.2, 0.1, 0.4};
		description = "hard makes you a small country";
		sc = new Scenario("hard", description, 5, hard, null, null);
		sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		scenarios.Add(sc);

		double[] random = null;
		teams = null;
		description = "random has no safeguards on nation sizes";
		sc = new Scenario("random", description, 5, random, teams, null);
		sc.completed = PlayerPrefs.GetInt(sc.name, 0) == 1;
		scenarios.Add(sc);

		activeScenario = scenarios[2]; //default to scenario c
		IsSetup = true;
	}

    public static int AffiliatedCheck(int team) {
        if (activeScenario.affiliations == null) return -1;
        for(int i = 0; i < activeScenario.affiliations.Length; i++) {
            if (activeScenario.affiliations[i].Contains(team)){
                //Debug.Log("afil " + i + " contains " + team);
                return i;
	        }
		}
        return -1;
    }
}
