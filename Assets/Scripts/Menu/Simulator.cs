using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Simulator
{
    public static bool IsSetup = false;
	public static Scenario[] scenarios;

    public static Scenario activeScenario;

    public static bool tutorialOverride;

	public static void Setup() {
        scenarios = new Scenario[3];

        //basic tutorial, overwhelm
        double[] ally = new double[] {0.8, 0.2}; 
        int[][] teams = new int[2][];
        teams[0] = new int[]{0}; //team A is the player
		teams[1] = new int[] {1}; //team B is the enemy
		scenarios[0] = new Scenario(0, 2, ally, teams);

        //lvl 2
        double[] duoShowdown = new double [] {0.25, 0.25, 0.25, 0.25}; 
		teams = new int[2][];
		teams[0] = new int[] { 0, 3 }; //team A is the player
		teams[1] = new int[] { 1, 2 }; //team B is the enemy
		scenarios[1] = new Scenario(0, 4, duoShowdown, teams);

        //standard
        double[] tsizes = null;
		teams = new int[2][];
		teams[0] = new int[] { }; //team A is the player
		teams[1] = new int[] { }; //team B is the enemy
		scenarios[2] = new Scenario(0, 5, tsizes, teams);

        activeScenario = scenarios[0];
        IsSetup = true;
	}

    public static int AffiliatedCheck(int team) { 
        for(int i = 0; i < activeScenario.affiliations.Length; i++) {
            if (activeScenario.affiliations[i].Contains(team)){
                Debug.Log("afil " + i + " contains " + team);
                return i;
	        }
		}
        return -1;
    }
}
