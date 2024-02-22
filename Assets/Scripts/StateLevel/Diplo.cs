using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

	public static string[] state_names;

	static int numreg = 0;

	public static void SetupDiplo()
	{
		numreg = 0;
		states = new State[Map.ins.numStates];
		state_names = new string[Map.ins.numStates];
		relationships = new Relationship[states.Length, states.Length];
		for (int i = 0; i < states.Length; i++)
		{
			for (int j = 0; j < states.Length; j++)
			{
				relationships[i, j] = Relationship.Neutral;
			}
		}
        ftaken = new List<int>();
        staken = new List<int>();
		ttaken = new List<int>();
	}
	public static void RegisterState(State st) {
		states[st.team] = st;
		state_names[st.team] = RandomName();
		numreg++;
		if(numreg == Map.ins.numStates) {
			StatesReady?.Invoke();
		}
    }


	public static void Nuked(int perp, int victim, uint dead) { 
		
    }
	public static string RandomName() {
		string name = "";
		int index;
		do
		{
			index = UnityEngine.Random.Range(0, firsts.Length);
		} while (ftaken.Contains(index));
		name += firsts[index];
		ftaken.Add(index);
		do
		{
			index = UnityEngine.Random.Range(0, seconds.Length);
		} while (staken.Contains(index));
		name += seconds[index];
		staken.Add(index);
		do
		{
			index = UnityEngine.Random.Range(0, thirds.Length);
		} while (ftaken.Contains(index));
		name += thirds[index];
		ttaken.Add(index);
		return name;
	}
	static List<int> ftaken = new List<int>();
	public static string[] firsts = new string[] {
		"p",
		"rus",
		"am",
		"sr",
		"obr",
		"art",
		"corb",
		"fl"
	};
	static List<int> staken = new List<int>();
	public static string[] seconds = new string[] {
		"a",
		"o",
		"oli",
		"ertu",
		"eri",
		"i",
		"us",
		"is",
		"an",
		"ori",
		"ooli",
	};
	static List<int> ttaken = new List<int>();
	public static string[] thirds = new string[] {
		"land",
		"ca",
		"stan",
		"nia",
		"nce",
		"any",
		"da",
	};
}
