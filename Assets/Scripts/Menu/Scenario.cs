using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Scenario
{
	public int number;

	public int numTeams;

	public double[] percentOfCities; //roughly determines starting popsizes

	public int[][] affiliations; //determines starting opinions
	//if a nation is listed here, they like everyone else in their array
	//and hate everyone on a different array
	//and are neutral to unlisted parties

	public Scenario(int n, int tn, double[] pctc, int[][] afil) {
		number = n;
		numTeams = tn;
		percentOfCities = pctc;
		affiliations = afil;
    }
}
