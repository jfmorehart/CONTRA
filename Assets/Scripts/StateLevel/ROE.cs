using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ROE
{
	//2D array of 0 or 1 shorts
    //Not bools for ease of use with compute shaders
	public static int[] atWar;
	public static Action roeChange;

	public static void SetUpRoe()
	{
		atWar = new int[Map.ins.numStates * Map.ins.numStates];

		// The nature of man... this is so deep
		for(int i = 0; i < Map.ins.numStates; i++) {
			DeclareWar(i, i);
		}

		//Subscribe to delegate that will tell us when all the states
		// have registered so we can check who touches who. 
		// unclear when we should update this info
	}

	//With specified player
	public static bool AreWeAtWar(int t1, int t2) {
		int index = t1 * Map.ins.numStates + t2;
		return (atWar[index] == 1);
    }
	//With any player
	public static bool AreWeAtWar(int t1)
	{
		int index = t1 * Map.ins.numStates;
		for (int i = 0; i < Map.ins.numStates; i++) {
			if (t1 == i) continue;
			if (atWar[index + i] != 0) return true;
		}

		return false;
	}

	//More readable interfaces
	public static void DeclareWar(int t1, int t2) {
		SetState(t1, t2, 1);
	}
	public static void MakePeace(int t1, int t2)
	{
		SetState(t1, t2, 0);
	}

	public static void SetState(int t1, int t2, int toSet) {

		//No self peace, only self war
		if (t1 == t2 && toSet == 0) return;

		int index = t1 * Map.ins.numStates + t2;
		atWar[index] = toSet;
		int index2 = t2 * Map.ins.numStates + t1;
		atWar[index2] = toSet;

		roeChange?.Invoke();

	}

	public static void DebugAll() { 
		for(int i = 0; i < atWar.Length; i++) {
			Debug.Log(i + " - " + atWar[i]);
		}
    }

	public static void SetAllExceptIdentity(int toSet) { 
		for(int i = 0; i < Map.ins.numStates; i++) {
			for (int j = 0; j < Map.ins.numStates; j++)
			{
				if (i == j) continue;
				SetState(i, j, toSet);
			}
		}
    }

	public static int[] Passables(int team) {
		List<int> pas = new List<int>();
		// Return countries that we are at war with or have open borders with
		for(int i = 0; i < Map.ins.numStates; i++) {
			if (AreWeAtWar(team, i)) {
				pas.Add(i);
			}
		}
		return pas.ToArray();
    }
}
