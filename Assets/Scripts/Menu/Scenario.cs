using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Scenario
{
	public string name;
	public string description;

	public int numTeams;

	public double[] percentOfCities; //roughly determines starting popsizes

	public int[][] affiliations; //determines starting opinions
								 //if a nation is listed here, they like everyone else in their array
								 //and hate everyone on a different array
								 //and are neutral to unlisted parties
	public ScenarioConditions conditions;

	public Scenario(string sname, string sdesc, int tn, double[] pctc, int[][] afil, ScenarioConditions cond)
	{
		name = sname;
		description = sdesc;

		numTeams = tn;
		percentOfCities = pctc;
		affiliations = afil;

		if (afil == null)
		{
			int[][] teams = new int[2][];
			teams[0] = new int[] { }; //team A is the player
			teams[1] = new int[] { }; //team B is the enemy}
			afil = teams;
		}
		conditions = cond;
	}

	public class ScenarioConditions
	{
		public int randomArmies;
		public int[][] unlockedupgrades;
		public int[] silos;
		public int[] airbases;
		public int[] batteries;
		public int[] troops;

	}
}
