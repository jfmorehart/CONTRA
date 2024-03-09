using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArmyUtils;
using static MapUtils;


public struct StateEval
{
	public float popRatio;
	public float nukeRatio;
	public float armyRatio;

	public float pVictory;
	public Diplo.Relationship relationship;
	public bool isHotWar;

	public StateEval(int team, int enemy) {
		popRatio = (1 + Map.ins.state_populations[enemy]) / (float)(Map.ins.state_populations[team] + 1f);
		int enemyNukes = nuclearCount[enemy];
		int myNukes = nuclearCount[team];


		if(myNukes < 1) {
			//having a silo is a huge deal
			nukeRatio = enemyNukes + 1;
		}
		else {
			//but once you have one silo, you might as well have 3
			nukeRatio = (5f + enemyNukes) / (float)(myNukes + 5f);
		}

		armyRatio = (1 + conventionalCount[enemy]) / (float)(conventionalCount[team] + 1f);
		float lerpTerm = nukeRatio * 0.65f + armyRatio * 0.35f;
		pVictory = Mathf.Pow(0.08f, Mathf.Pow(lerpTerm * 0.5f, 2));
		relationship = Diplo.relationships[team, enemy];
		isHotWar = (int)relationship > 4; // covers limited and total war
	}
}

