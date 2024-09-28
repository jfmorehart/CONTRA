using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerminalMissileRegistry
{
	public static List<Missile>[] registry;

	public static void Setup() {
		registry = new List<Missile>[Map.ins.numStates];
		for(int i = 0; i < Map.ins.numStates; i++) {
			registry[i] = new List<Missile>();
		}
    }

	public static void Register(Missile mis, int victimTeam) {
		if (victimTeam == -1) return;
		registry[victimTeam].Add(mis);
    }
	public static void DeRegister(Missile mis, int victimTeam)
	{
		if (victimTeam == -1) return;
		registry[victimTeam].Remove(mis);
	}
}
