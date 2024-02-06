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
		nukeRatio = (1 + nuclearCount[enemy]) / (float)(nuclearCount[team] + 1f);
		armyRatio = (1 + conventionalCount[enemy]) / (float)(conventionalCount[team] + 1f);
		pVictory = Mathf.Pow(0.5f, Mathf.Pow((nukeRatio + armyRatio) * 0.5f, 2));
		relationship = Diplo.relationships[team, enemy];
		isHotWar = (int)relationship > 4; // covers limited and total war
	}
}

