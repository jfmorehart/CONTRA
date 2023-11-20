using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
	public int team;

	//[HideInInspector]
    public float pop; //todo some sort of popcount
	public float truepop;

	public float capRate;
	public int maxCapDist;

	public float sample_radius;
	int numSamples = 5;

    public string debug;


	public void SetUpCity(int te, float popu)
	{
		team = te;
		pop = popu;
		truepop = pop;
		InvokeRepeating(nameof(IncrementalCapture), Random.Range(0, 1f), 0.25f);
	}

	public void IncrementalCapture() {
		// This function finds the nearest army lads to check to see if 
		// this city is being captured, so it can adjust its influence 
		// on the map territory accordingly
		debug = "";
		float[] teaminfs = new float[Map.ins.numStates];
		int[] keys = new int[Map.ins.numStates];
		for (int i = 0; i < Map.ins.numStates; i++) {
	
			keys[i] = i;
			if (!ROE.AreWeAtWar(team, i)) continue;
			debug += "atwar" + team + " " + i + " /n";
			Unit[] units = ArmyUtils.GetUnits(i);
			debug += units.Length;
			foreach (Unit un in units) {
				debug += "/";
				float d = Vector2.Distance(transform.position, un.transform.position);
				d = Mathf.Max(d, 10);
				if (d > maxCapDist) continue;
				teaminfs[un.team] += capRate / (d * d);
			}
		}
		//Determine whether or not to cap
		teaminfs[team] += 0.001f;
		System.Array.Sort(teaminfs, keys);
		if (keys[^1] != team) {
			//Shrink effective pop, and check if captured
			pop -= teaminfs[^1];
			if (pop < 0)
			{
				team = keys[^1];
			}
			debug += "d = " + teaminfs[^1];
		}
		else {
			//Grow effective pop to match realpop
			teaminfs[team] += 0.01f;
			pop += teaminfs[^1];
			if (pop > truepop) pop = truepop;
			debug += "s = " + teaminfs[^1];
		}
	}

	public void SurroundCheck() {

		//Only check in wartime
		if (!ROE.AreWeAtWar(team)) return;
		
        int[] sample_teams = new int[Map.ins.numStates];
		Vector2[] sample_pos = ArmyUtils.Encircle(transform.position, sample_radius, numSamples);

        for(int i = 0; i < numSamples; i++) {
			sample_teams[MapUtils.PointToTeam(sample_pos[i])] += 1;
		}

		int mteam = 0;
		int mamt = -1;

		for(int i = 0; i < Map.ins.numStates; i++) {
			if (sample_teams[i] > mamt) {
				mamt = sample_teams[i];
				mteam = i;
			}
		}
		
		//Only convert to team we are at war with
		if(ROE.AreWeAtWar(team, mteam)){
			if(team != mteam) {
				Debug.Log("captured");
			}

			team = mteam;
		}
    }
}
