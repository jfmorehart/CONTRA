using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArmyUtils;
using static MapUtils;


public struct StateDynamic
{
	public static float armyWeight = 0.2f;
	public static float nukeWeight = 0.2f;
	public static float airWeight = 0.15f;
	public static float popWeight = 0.45f;

	public float popRatio;
	public float nukeRatio;
	public float armyRatio;
	public float airRatio;

	public float pVictory;
	public Diplomacy.Relationship relationship;
	public bool isHotWar;

	public StateDynamic(int team, int enemy)
	{
		popRatio = (1 + Map.ins.state_populations[enemy]) / (float)(Map.ins.state_populations[team] + 1f);
		int enemyNukes = nuclearCount[enemy];
		int myNukes = nuclearCount[team];

		int myAirbases = airbases[team].Count;
		int enemyAirbases = airbases[enemy].Count;

		if (myNukes < 1)
		{
			//having a silo is a huge deal
			nukeRatio = enemyNukes + 1;
		}
		else
		{
			//but once you have one silo, you might as well have 3
			nukeRatio = (5f + enemyNukes) / (float)(myNukes + 5f);
		}

		airRatio = (enemyAirbases + 1) / (myAirbases + 1);

		armyRatio = (10 + conventionalCount[enemy]) / (float)(conventionalCount[team] + 10f);
		float lerpTerm = nukeRatio * nukeWeight + armyRatio * armyWeight + airRatio * airWeight + popRatio * popWeight;
		pVictory = Mathf.Clamp(Mathf.Pow(0.08f, Mathf.Pow(lerpTerm * 0.5f, 2)), 0.01f, 0.99f);
		relationship = Diplomacy.relationships[team, enemy];
		isHotWar = (int)relationship > 4; // covers limited and total war
	}
}

