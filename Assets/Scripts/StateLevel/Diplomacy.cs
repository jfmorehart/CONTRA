using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ConsolePanel;

public static class Diplomacy
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

	public static bool[,] peaceOffers;

	public static string[] state_names;

	static int numreg = 0;

	public static List<int>[] alliances;

	public static void SetupDiplo()
	{
		numreg = 0;
		states = new State[Map.ins.numStates];
		state_names = new string[Map.ins.numStates];
		relationships = new Relationship[states.Length, states.Length];
		peaceOffers = new bool[states.Length, states.Length];
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

		alliances = new List<int>[Mathf.CeilToInt(states.Length / 2)];
		for(int i = 0; i < alliances.Length; i++) {
			alliances[i] = new List<int>();
		}
	}

	public static void OfferPeace(int t1, int t2) {
		if (peaceOffers[t1, t2]) return; //already true;

			peaceOffers[t1, t2] = true;

		if (peaceOffers[t2, t1])
		{
			ROE.MakePeace(t1, t2);
			//reset for the next war lmao
			peaceOffers[t1, t2] = false;
			peaceOffers[t2, t1] = false;
			Log(ColoredName(t1) + " and " + (ColoredName(t2) + " have made peace"), 30);
		}
		else {
			Log(ColoredName(t1) + " has offered " + (ColoredName(t2) + " peace"), 30);
		}
	}
	public static void RemovePeaceOffer(int t1, int t2)
	{
		peaceOffers[t1, t2] = false;
	}

	public static void JoinAlliance(int joining, int host) {
		if (HasAllies(joining)) {
			Debug.LogError("you already have a team silly");
			return;
		}
		int al = AllianceOfTeam(host);
		if(al == -1) {
			//new alliance
			al = FindOpenAlliance();
			alliances[al].Add(host);
			alliances[al].Add(joining);
		}
		else {
			if (alliances[al].Contains(joining)) {
				//already there dipshit
				return;
			}
			alliances[al].Add(joining);
		}
		AlliancePanel.ins.AlliancePanelUpdate();
		AllianceWarsUpdate(al);
    }

	public static void RegisterState(State st) {
		states[st.team] = st;
		state_names[st.team] = RandomName();
		numreg++;
		if(numreg == Map.ins.numStates) {
			StatesReady?.Invoke();
		}
    }

	public static void AllianceWarsUpdate(int al) { 
		foreach(int mem in alliances[al]) {
			JoinAllianceWars(mem);
		}
    }
	public static void JoinAllianceWars(int team) {
		int al = AllianceOfTeam(team);
		if (al == -1) return;
		foreach (int en in GetAllianceEnemies(al))
		{
			if(!ROE.AreWeAtWar(team, en)) {
				Debug.Log(team + " joining active war against " + en);
				ROE.DeclareWar(team, en);
			}
		}

	}

	public static List<int> GetAllianceEnemies(int al) {
		List<int> enemies = new();
		foreach(int mem in alliances[al]) {
			enemies.AddRange(ROE.GetEnemies(mem));
		}
		return enemies;
    }

	static int FindOpenAlliance()
	{
		for (int i = 0; i < alliances.Length; i++)
		{
			if (alliances[i].Count < 1)
			{
				return i;
			}
		}
		Debug.LogError("not enough alliance slots");
		return -1; //should never run
	}

	//dumb as shit
	public static int CanIReachEnemyThroughAllies(int team, int enemy) {
		//returns the weight of how important it is to supply the frontline ally
		if (AsyncPath.ins.SharesBorder(team, enemy)) return 3;
		if (!HasAllies(team)) return 0;
		List<int> allies = AlliesOf(team);
		for(int i = 0; i < allies.Count; i++) {
			if (AsyncPath.ins.SharesBorder(allies[i], enemy)) return 2;
			for (int j = 0; j < allies.Count; j++)
			{
				if (AsyncPath.ins.SharesBorder(allies[i], enemy)) return 1;
			}
		}
		return 0;
    }

	public static List<int> AlliesOf(int team) {
		int al = AllianceOfTeam(team);
		if (al == -1) return null;
		return alliances[al];
	}

	public static bool HasAllies(int team)
	{
		return AllianceOfTeam(team) != -1;
	}

	public static bool IsMyAlly(int me, int them) {
		int al = AllianceOfTeam(me);
		if (al == -1) return false;
		bool cont = alliances[al].Contains(them);
		Debug.Log(me + " " + cont + " " + them);
		return cont;
	}

	public static int AllianceOfTeam(int team){ 
		for(int i = 0; i < alliances.Length; i++) {
			if (alliances[i].Contains(team)) {
				return i;
			}
		}
		return -1;
    }

	public static void Nuked(int perp, int victim, uint dead)
	{

	}

	public static string RandomName() {
		string name = "";
		int tries = 0;

		int index;
		do
		{
			tries++;
			index = UnityEngine.Random.Range(0, firsts.Length);
		} while (ftaken.Contains(index) && tries < 500);
		name += firsts[index];
		ftaken.Add(index);
		do
		{
			tries++;
			index = UnityEngine.Random.Range(0, seconds.Length);
		} while (staken.Contains(index) && tries < 500);
		name += seconds[index];
		staken.Add(index);
		do
		{
			tries++;
			index = UnityEngine.Random.Range(0, thirds.Length);
		} while (ttaken.Contains(index) && tries < 500);
		name += thirds[index];
		ttaken.Add(index);
		return name;
	}
	static List<int> ftaken = new List<int>();
	public static string[] firsts = new string[] {
		"p",
		"rus",
		"am",
		"fr",
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
