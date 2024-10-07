using UnityEngine;
using static ArmyUtils;


public struct StateEval
{
	public static float armyWeight = 0.2f;
	public static float nukeWeight = 0.2f;
	public static float airWeight = 0.15f;
	public static float popWeight = 0.45f;

	public float str_pop;
	public float str_nuke;
	public float str_army;
	public float str_air;

	public float strength;

	public StateEval(int team) {
		str_pop = Map.ins.state_populations[team];
		str_nuke = nuclearCount[team];
		str_air = airbases[team].Count;

		str_army = ArmyUtils.armies[team].Count;

		strength = str_nuke * nukeWeight + str_army * armyWeight + str_air * airWeight + str_pop * popWeight;
		strength *= Diplomacy.states[team].alive ? 1 : 0;
	}
}

