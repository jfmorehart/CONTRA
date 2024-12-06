using System;
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
	public static string[] groundUpgrades = { "training", "armor support", "mechanization" , "combined arms", "modernization" };
	public static string[] airUpgrades = { "unlock", "production", "payload i", "engines", "payload ii" };
	public static string[] icbmUpgrades = { "unlock", "warhead i", "production", "warhead ii", "mirv" };
	public static string[] aaaUpgrades = { "unlock", "production i", "missile tech", "production ii", "abm capable" };
	public static string[][] names = { groundUpgrades, aaaUpgrades, airUpgrades, icbmUpgrades};

	public static int[][] unlockedUpgrades; //x = team, y = branch, value = next thing to research
	public static float[] unlockProgress; //x = team, value = 0-1 percent progress
	public static float[] unlockSpeed; //x = team
	public static Vector2Int[] currentlyResearching; //x = branch, y = upgrade

	//								ground, AAA, air, icbm
	public static float[] baseCosts = {200, 100, 200, 200};
	public static float[] rankMultipliers = { 1, 2f, 2.5f, 3f, 3.5f};
	public static float[][] costs;
	public static float[] budget;

	public static Action[] ResearchChange;

	public static void Setup() {
		unlockedUpgrades = new int[Map.ins.numStates][];
		unlockProgress = new float[Map.ins.numStates];
		unlockSpeed = new float[Map.ins.numStates];
		budget = new float[Map.ins.numStates];
		currentlyResearching = new Vector2Int[Map.ins.numStates];

		ResearchChange = new Action[Map.ins.numStates];
		for (int i =0; i < Map.ins.numStates; i++) {
			currentlyResearching[i] = -Vector2Int.one;
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

	public static void DeclareResearchTopic(int teamOf, Research.Branch branch) {
		int progress = unlockedUpgrades[teamOf][(int)branch];
		currentlyResearching[teamOf] = new Vector2Int((int)branch, progress);
		unlockProgress[teamOf] = 0;
    }

	public static void PerFrameResearch() {
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (currentlyResearching[i].x == -1) continue;
			if (unlockSpeed[i] < 0f) continue;
			if (currentlyResearching[i].x >= costs.Length) { 
				currentlyResearching[i] = new Vector2Int(-1, -1);
				Debug.LogError("over index x");
				continue;
			}
			if (currentlyResearching[i].y >= costs[currentlyResearching[i].x].Length)
			{
				currentlyResearching[i] = new Vector2Int(-1, -1);
				Debug.LogError("over index y");
				continue;
			}

			if (currentlyResearching[i].x > costs.Length) currentlyResearching[i] = new Vector2Int(-1, -1);
			unlockProgress[i] += unlockSpeed[i] / costs[currentlyResearching[i].x][currentlyResearching[i].y] * Time.deltaTime;

			if (unlockProgress[i] > 1)
			{
				ResearchCompleted(i);
			}
		}
	}

	public static void ResearchCompleted(int i) {
		Debug.Log(i + "completed research");
		if(i == Map.localTeam) {
			Vector2Int completed = currentlyResearching[i];
			if(completed.x > 0 && completed.y == 0) {
				ConsolePanel.Log("new base type unlocked: " + headers[completed.x]);
			}
			else {
				
				ConsolePanel.Log("<color=\"green\"> research complete </color>");
			}

		}
		unlockedUpgrades[i][currentlyResearching[i].x]++;
		unlockProgress[i] = 0;
		currentlyResearching[i] = -Vector2Int.one;
		ResearchChange[i]?.Invoke();
		if (Map.multi) { 
			MultiplayerVariables.ins.ResearchUpdateServerRPC(MultiplayerVariables.ins.clientIDs[i], unlockedUpgrades[i]);
		}
	}
}
