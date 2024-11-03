using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.VisualScripting;
using System.Linq;

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

	public List<int> reachableCountries;
	public string debugInfo;

	float neighborCheck_last = 0.1f;
	float neighborCheck_delay = 1.5f;

	public void SetUpCity(int te, float popu)
	{
		team = te;
		pop = popu;
		//truepop = pop;

		lastPop = Random.Range(Time.time, Time.time + 5);
		neighborCheck_last = Random.Range(Time.time, Time.time + 1);
		mpos = MapUtils.PointToCoords(transform.position);
		wpos = transform.position;

		truepop = Map.ins.CountPop_City(MapUtils.PointToCoords(transform.position));
		reachableCountries = new List<int>();
	}

	private void Update()
	{
		if (!Diplomacy.states[team].alive)
		{
			team = TeamSurround();
		}

		if (Time.time - lastPop > popdelay)
		{
			lastPop = Time.time + Random.Range(0, 0.3f);
			PopCount();
		}

		if(Time.time - neighborCheck_last > neighborCheck_delay) {
			CheckReachableCountries();
			neighborCheck_last = Time.time;
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

	//maybe this cant be a member if its referenced by different workers?
	List<int> neighborlist = new List<int>();

	 async void CheckReachableCountries() {
		//return;
		neighborlist.Clear();
		debugInfo = ""; 
		for (int i= 0; i< Map.ins.numStates; i++) {
			if (i == team) continue;
			if (AsyncPath.ins.SharesBorder(team, i)) {
				//possibly a neighbor
				debugInfo += i.ToString() + "mb, ";
				//loop thru cities until we can make it to one
				City checkC = ArmyUtils.NearestCity(wpos, i, null);
				if (checkC == null) continue;
				List<int> pas = ROE.Passables(team).ToList();
				pas.Add(i);
				bool reach = await Task.Run(() => AsyncPath.ins.IsReachableCheck(mpos, checkC.mpos, pas.ToArray(), 1, 3200));
				//neighborPath_prealloc = await Task.Run(() => AsyncPath.ins.Path(mpos, checkC.mpos, pas.ToArray(), 1, 3200));
				if (reach) {
					neighborlist.Add(i);
				}
			}
		}
		//debugInfo += " reached end, updated + " + Time.time.ToString();
		reachableCountries = neighborlist;
    }

	public void IncrementalCapture() {
		//Async!!!

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
			if (CityCapturing.ins.icprep[i] == null) return;
			foreach (Unit un in CityCapturing.ins.icprep[i]) {
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


	Vector2[] sample_pos = new Vector2[5];
	int[] sample_teams = new int[10];
	public int TeamSurround()
	{
		//returns the team with the most surrounding points
		sample_teams = new int[Map.ins.numStates];
		sample_pos = ArmyUtils.Encircle(wpos, sample_radius, numSamples);

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
