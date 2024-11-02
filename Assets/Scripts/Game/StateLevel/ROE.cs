using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static ConsolePanel;

public static class ROE
{
	//this class is for the more registration-y side of war and peace stuff, less readable. 

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
		if (t1 == -1 || t2 == -1) return false;
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

	public static List<int> GetEnemies(int team) {
		List<int> enemies = new();
		for(int i = 0; i < Map.ins.numStates; i++) {
			if (team == i) continue;
			if(AreWeAtWar(team, i)) {
				enemies.Add(i);
			}
		}
		return enemies;
    }

	//More readable interfaces
	public static void DeclareWar(int t1, int t2) {
		if (AreWeAtWar(t1, t2)) return;
		Diplomacy.RemovePeaceOffer(t1, t2);
		Diplomacy.AnnounceNews(Diplomacy.NewsItem.War, t1, t2);

		SetState(t1, t2, 1);
		if (t1 == t2) return;
		if (!Diplomacy.states[t1].alive) return;

		if (t1 == 0 || t2 == 0)
		{
			SFX.ins.DeclareWarAlarm();
		}

		if (Diplomacy.IsMyAlly(t1, t2)) {
			Debug.LogError("trying to invade current ally :(");
		}
		Diplomacy.states[t2].WarStarted(t1);
		Diplomacy.states[t1].WarStarted(t2);


		if (t1 == 0)
		{
			Log(you + " declared war on " + ColoredName(t2), 30);
		}
		else if(t2 == 0) { 
			Log(ColoredName(t1) + " has declared war on " + you, 30);
		}
		else { 
			if(t1 == 0) {
				Log(ColoredName(t1) + " have declared war on " + ColoredName(t2), 30);
			}
			else {
				Log(ColoredName(t1) + " has declared war on " + ColoredName(t2), 30);
			}

		}
	}
	public static void MakePeace(int t1, int t2)
	{
		SetState(t1, t2, 0);

		if (t1 == 0 || t2 == 0)
		{
			SFX.ins.MakePeaceAlarm();
		}
		if (!Diplomacy.states[t1].alive) return;
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

	static List<int> pas = new ();
	public static int[] Passables(int team, bool includeOcean = false) {
		pas.Clear();
		// Return countries that we are at war with or have open borders with
		if (Diplomacy.HasAllies(team)) {
			pas.AddRange(Diplomacy.AlliesOf(team));
		}

		for(int i = 0; i < Map.ins.numStates; i++) {
			if (AreWeAtWar(team, i)) {
				pas.Add(i);
			}
		}
		if (includeOcean) {
			pas.Add(-1);	
		}
		return pas.ToArray();
    }
	public static List<int> ListPassables(int team, bool includeOcean = false)
	{
		List<int> pas = new();
		// Return countries that we are at war with or have open borders with
		if (Diplomacy.HasAllies(team))
		{
			pas.AddRange(Diplomacy.AlliesOf(team));
		}

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (AreWeAtWar(team, i))
			{
				pas.Add(i);
			}
		}
		if (includeOcean)
		{
			pas.Add(-1);
		}
		return pas;
	}
}
