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
		popRatio = (1 + Map.ins.state_populations[enemy]) / (Map.ins.state_populations[team] + 1);
		nukeRatio = (1 + nuclearCount[enemy]) / (nuclearCount[team] + 1);
		armyRatio = (1 + conventionalCount[enemy]) / (conventionalCount[team] + 1);
		pVictory = Mathf.Pow(0.5f, (nukeRatio + armyRatio) * 0.5f);
		relationship = Diplo.relationships[team, enemy];
		isHotWar = (int)relationship > 4; // covers limited and total war
	}
}

