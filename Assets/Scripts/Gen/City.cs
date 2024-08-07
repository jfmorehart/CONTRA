using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

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

	
	float lastPop;
	float popdelay = 5;

	public Vector2Int mpos;
	public Vector2 wpos;

	public void SetUpCity(int te, float popu)
	{
		team = te;
		pop = popu;
		//truepop = pop;

		lastPop = Random.Range(0f, 5f);
		mpos = MapUtils.PointToCoords(transform.position);
		wpos = transform.position;

		truepop = Map.ins.CountPop_City(MapUtils.PointToCoords(transform.position));
	}

	private void Update()
	{
		if (!Diplomacy.states[team].alive)
		{
			team = TeamSurround();
		}

		if (Time.unscaledTime - lastPop > popdelay)
		{
			lastPop = Time.unscaledTime;
			PopCount();
		}
	}

	void PopCount() {
		float f = truepop;
		truepop = Map.ins.CountPop_City(MapUtils.PointToCoords(transform.position));
		if(truepop < 1) {
			ArmyManager.ins.RemoveCity(this);
			Destroy(gameObject);
		}
    }

	public void IncrementalCapture() {
		// This function finds the nearest army lads to check to see if 
		// this city is being captured, so it can adjust its influence 
		// on the map territory accordingly

		if (team == -1) return;

		float[] teaminfs = new float[Map.ins.numStates];
		int[] keys = new int[Map.ins.numStates];
		for (int i = 0; i < Map.ins.numStates; i++) {
	
			keys[i] = i;
			if (!ROE.AreWeAtWar(team, i)) continue;

			//this info may be old!
			foreach (Unit un in ArmyUtils.armies[i]) {
				if (un == null) continue;
				float d = Vector2.Distance(wpos, (un as Army).wpos);
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
				team = TeamSurround();
				//if (SurroundCheck(keys[^1])) {
				//	team = keys[^1];
				//}
				//else {
				//	team = TeamSurround();
				//}
			}
		}
		else {
			//Grow effective pop to match realpop
			teaminfs[team] += 0.01f;
			pop += teaminfs[^1];
			if (pop > truepop) pop = truepop;
		}
	}

	public bool SurroundCheck(int capteam) {
		
        int[] sample_teams = new int[Map.ins.numStates];
		Vector2[] sample_pos = ArmyUtils.Encircle(wpos, sample_radius, numSamples);

        for(int i = 0; i < numSamples; i++) {
			int t = MapUtils.WorldPosToTeam(sample_pos[i]);
			if (t < 0) continue;
			if (capteam == t) {
				return true;
			}
			//sample_teams[MapUtils.WorldPosToTeam(sample_pos[i])] += 1;
		}
		return false;
		//int mteam = 0;
		//int mamt = -1;

		//for(int i = 0; i < Map.ins.numStates; i++) {
		//	if (sample_teams[i] > mamt) {
		//		mamt = sample_teams[i];
		//		mteam = i;
		//	}
		//}
		
		////Only convert to team we are at war with
		//if(ROE.AreWeAtWar(team, mteam)){
		//	if(team != mteam) {
		//		Debug.Log("captured");
		//	}

		//	team = mteam;
		//}
    }


	public int TeamSurround()
	{
		//returns the team with the most surrounding points
		int[] sample_teams = new int[Map.ins.numStates];
		Vector2[] sample_pos = ArmyUtils.Encircle(wpos, sample_radius, numSamples);

		for (int i = 0; i < numSamples; i++)
		{
			int t = MapUtils.WorldPosToTeam(sample_pos[i]);
			if (t < 0) continue;
			sample_teams[t] += 1;
		}
		int mteam = 0;
		int mamt = -1;

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (sample_teams[i] > mamt)
			{
				mamt = sample_teams[i];
				mteam = i;
			}
		}

		return mteam;

	}
}
