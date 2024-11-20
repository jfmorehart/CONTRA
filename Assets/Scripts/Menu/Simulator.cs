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

		string description = "";
        //basic tutorial, overwhelm
        double[] ally = new double[] {0.8, 0.2}; 
        int[][] teams = new int[2][];
        teams[0] = new int[]{0}; //team A is the player
		teams[1] = new int[] {1}; //team B is the enemy
		description = "scenario a is two-nation scenario with a vast advantage to the player";
		scenarios.Add(new Scenario("scenario a", description, 2, ally, teams));

        //lvl 2
        double[] duoShowdown = new double [] {0.25, 0.25, 0.5}; 
		teams = new int[2][];
		teams[0] = new int[] { 0, 1 }; //team A is the player
		teams[1] = new int[] { 2 }; //team B is the enemy
		description = "scenario b explores allianae dynamics by giving the player both a steadfast ally and a steadfast enemy";
		scenarios.Add(new Scenario("scenario b", description, 4, duoShowdown, teams));

        double[] tsizes = new double[] {0.5};
		teams = new int[2][];
		teams[0] = new int[] { }; //team A is the player
		teams[1] = new int[] { }; //team B is the enemy
		description = "scenario c gives the player a strong position, but also a large number of rival states to contend with";
		scenarios.Add(new Scenario("scenario c", description, 5, tsizes, teams));

		double[] buddy = new double[] {0.4, 0.1, 0.1, 0.1};
		teams = new int[2][];
		teams[0] = new int[] {0, 1}; //team A is the player
		teams[1] = new int[] {2, 3}; //team B is the enemy
		description = "scenario d gives the player two enemies, and an ally they'll likely need to protect in order to succeed";
		scenarios.Add(new Scenario("scenario d", description, 4, buddy, teams));

		activeScenario = scenarios[2]; //default to scenario c
		IsSetup = true;
	}

    public static int AffiliatedCheck(int team) {
        if (activeScenario.affiliations == null) return -1;
        for(int i = 0; i < activeScenario.affiliations.Length; i++) {
            if (activeScenario.affiliations[i].Contains(team)){
                Debug.Log("afil " + i + " contains " + team);
                return i;
	        }
		}
        return -1;
    }
}
