using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Diplo
{
	public enum Relationship { 
		Ally,
		Friendly, 
		Neutral, 
		Hostile,
		ConventionalWar,
		NuclearWar,
		TotalWar
    }

	public static State[] states;
	public static Relationship[,] relationships;
	//public delegate void DiploCall();
	public static Action StatesReady;
	//public static DiploCall StatesReady;

	static int numreg = 0;

	public static void SetupDiplo()
	{
		states = new State[Map.ins.numStates];
		relationships = new Relationship[states.Length, states.Length];
		for (int i = 0; i < states.Length; i++)
		{
			for (int j = 0; j < states.Length; j++)
			{
				relationships[i, j] = Relationship.Neutral;
			}
		}

	}
	public static void RegisterState(State st) {
		states[st.team] = st;
		numreg++;
		if(numreg == Map.ins.numStates) {
			StatesReady?.Invoke();
		}
    }


	public static void Nuked(int perp, int victim, uint dead) { 
		
    }
}
