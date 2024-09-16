using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Research
{
	public enum Branch {
		ground,
		aaa,
		air,
		silo
	}

	public static string[] headers = { "groundforces", "air defense" , "aircraft", "icbms"};
	public static string[] groundUpgrades = { "strength i", "damage i", "strength ii", "damage ii", "modernization" };
	public static string[] airUpgrades = { "unlock", "production", "missiles i", "range", "missiles ii" };
	public static string[] icbmUpgrades = { "unlock", "warhead i", "production", "warhead ii", "mirv" };
	public static string[] aaaUpgrades = { "unlock", "production i", "missile tech", "production ii", "abm capable" };
	public static string[][] names = { groundUpgrades, aaaUpgrades, airUpgrades, icbmUpgrades};

	public static int[][] unlockedUpgrades; //x = team, y = branch, value = next thing to research
	public static float[] unlockProgress; //x = team, value = 0-1 percent progress
	public static float[] unlockSpeed; //x = team
	public static Vector2Int[] currentlyResearching; //x = branch, y = upgrade

	public static float[] baseCosts = { 20, 20, 30, 40 };
	public static float[] rankMultipliers = { 1, 2f, 3f, 4f, 5f};
	public static float[][] costs;

	public static void Setup() {
		unlockedUpgrades = new int[Map.ins.numStates][];
		unlockProgress = new float[Map.ins.numStates];
		unlockSpeed = new float[Map.ins.numStates];
		currentlyResearching = new Vector2Int[Map.ins.numStates];

		for (int i =0; i < Map.ins.numStates; i++) {
			unlockedUpgrades[i] = new int[4];
		}

		costs = new float[4][];
		for(int x =0; x < costs.Length; x++) {
			costs[x] = new float[5];
			for(int y = 0; y < 5; y++) {
				costs[x][y] = baseCosts[x] * rankMultipliers[y];
			}	
		}
	}

	public static void ConductResearch(int teamOf, float manHours) {

		unlockSpeed[teamOf] = manHours / State.stateUpdateDelay;
	}

	public static void PerFrameResearch() {
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (currentlyResearching[i].x == -1) continue;
			if (unlockSpeed[i] < 0f) continue;
			unlockProgress[i] += unlockSpeed[i] / costs[currentlyResearching[i].x][currentlyResearching[i].y] * Time.deltaTime;

			if (unlockProgress[0] > 1)
			{
				unlockedUpgrades[i][currentlyResearching[i].x]++;
				unlockProgress[currentlyResearching[i].x]++;
				unlockProgress[i] = 0;
				currentlyResearching[i] = new Vector2Int(-1, -1);
			}
		}
	}
}
