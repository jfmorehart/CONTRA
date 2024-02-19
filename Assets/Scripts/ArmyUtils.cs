using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ArmyUtils
{
	public static int[] conventionalCount;
	public static int[] nuclearCount;

	//Keeps a list of info on the armies that was collected
	// as a byproduct from a different operation to use for non-critical
	// calculations like city capture updates.

	public static List<Unit>[] armies;
	//public static int[] lastWrite;

	public static void Init() {
		conventionalCount = new int[Map.ins.numStates];
		nuclearCount = new int[Map.ins.numStates];
		armies = new List<Unit>[Map.ins.numStates];
		for(int i = 0; i < Map.ins.numStates; i++) {
			armies[i] = new List<Unit>();
			GetArmies(i);
		}
	}

	public enum Tar { 
		Nuclear, //icbms, runways, submarine pens
		Conventional, //armies
		Civilian, // cities
		Support, // radars, etc
    }
	public struct Target {
		public Target(Vector2 p, float v, Tar t) {
			wpos = p;
			value = v;
			type = t;
			hash = 139841 % Mathf.RoundToInt(p.x + 2000) % (1 + (139843 % Mathf.RoundToInt(p.y + 2000)));
		}
		public Vector2 wpos;
		public float value;
		public Tar type;
		public int hash;
    }

	public static Target[] GetTargets(int team) {
		List<Target> tars = new List<Target>();
		tars.AddRange(NuclearTargets(team));
		tars.AddRange(ConventionalTargets(team));
		tars.AddRange(CivilianTargets(team));
		return TargetSort(tars.ToArray());
	}

	public static Target[] TargetSort(Target[] tr) {
		float[] keys = new float[tr.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			keys[i] = -tr[i].value;
		}
		System.Array.Sort(keys, tr);
		return tr;
	}

	public static Target[] NuclearTargets(int team) {
		List<Target> tars = new List<Target>();
		Silo[] sls = GetSilos(team);
		foreach (Silo sl in sls)
		{
			tars.Add(new Target(sl.transform.position, 10, Tar.Nuclear));
		}
		nuclearCount[team] = tars.Count;
		return tars.ToArray();
	}

	const int CONVENTIONAL_TARGETLIST_SIZE = 6;
	public static Target[] ConventionalTargets(int team)
	{
		// no comments fio bud
		List<Target> tars = new List<Target>();
		int[] indexValues = new int[UnitChunks.chunks.Length];
		int[] indexes = new int[UnitChunks.chunks.Length];
		for(int i = 0; i < UnitChunks.chunks.Length; i++) {
			indexes[i] = i;
			indexValues[i] = UnitChunks.chunkValues[team][i];
		}
		System.Array.Sort(indexValues, indexes);
		Debug.Log(UnitChunks.chunkValues[team][indexes[^1]]);
		for(int i = 0; i < CONVENTIONAL_TARGETLIST_SIZE; i++) {
			Debug.Log("targeting + "+ indexes[^(i+1)]);
			Target t = new Target(
				UnitChunks.ChunkIndexToMapPos(indexes[^(i+1)]),
				UnitChunks.chunkValues[team][indexes[^(i+1)]],
				Tar.Conventional
				);
			tars.Add(t);
		}
		return tars.ToArray();
	}
	public static Target[] CivilianTargets(int team)
	{
		List<Target> tars = new List<Target>();
		foreach (City sl in GetCities(team))
		{
			tars.Add(new Target(sl.transform.position, sl.truepop, Tar.Civilian));
		}
		return tars.ToArray();
	}

	public static Silo[] GetSilos(int team)
	{
		//Get info from tracker
		List<Silo> uns = new List<Silo>();
		InfluenceMan.ins.CleanArmies();
		Silo[] units = InfluenceMan.ins.silos.ToArray();

		//Pick the ones we want
		for (int i = 0; i < units.Length; i++)
		{
			if (units[i].team == team) { uns.Add(units[i]); }
		}
		return uns.ToArray();
	}

	public static Unit[] GetArmies(int team)
	{
		//Get info from tracker
		List<Unit> uns = new List<Unit>();
		InfluenceMan.ins.CleanArmies();
		Unit[] units = InfluenceMan.ins.armies.ToArray();

		//Pick the ones we want
		for(int i = 0; i < units.Length; i++){
			if (units[i].team == team) { uns.Add(units[i]); }
		}
		conventionalCount[team] = uns.Count;
		armies[team] = uns;
		return uns.ToArray();
	}
	public static Unit[] GetArmies(int team, int number, Vector2 near, List<Unit> ignore)
	{
		//Get info from tracker
		List<Unit> uns = new List<Unit>();
		InfluenceMan.ins.CleanArmies();
		Unit[] units = InfluenceMan.ins.armies.ToArray();
		//Pick the ones we want
		for (int i = 0; i < units.Length; i++)
		{
			if (units[i].team == team && !ignore.Contains(units[i])) {
				uns.Add(units[i]); 
			}
		}
		//Sort by distance
		float[] di = new float[uns.Count];
		Unit[] unar = uns.ToArray();
		for(int i = 0; i < unar.Length; i++) {
			di[i] = Vector2.Distance(unar[i].transform.position, near);
		}
		System.Array.Sort(di, unar);

		//Return if no trimming necessary
		if(number >= unar.Length) {
			return unar;
		}

		//Trim
		List<Unit> trim = new List<Unit>();
		for(int i = 0; i< number; i++) {
			trim.Add(unar[i]);
		}
		return trim.ToArray();
	}
	public static City NearestCity(Vector2 pos, int teamOf, List<City> ignore) {
		List<City> cities = InfluenceMan.ins.cities;
		float cdist = float.MaxValue;
		City near = null;
		if(ignore == null) {
			for (int i = 0; i < cities.Count; i++)
			{
				if (cities[i].team != teamOf) continue;
				float ndist = Vector2.Distance(pos, cities[i].wpos);
				if (ndist < cdist)
				{
					cdist = ndist;
					near = cities[i];
				}
			}
		}
		else {
			for (int i = 0; i < cities.Count; i++)
			{
				if (cities[i].team != teamOf || ignore.Contains(cities[i])) continue;
				float ndist = Vector2.Distance(pos, cities[i].transform.position);
				if (ndist < cdist)
				{
					cdist = ndist;
					near = cities[i];
				}
			}
		}

		return near;
    }
	public static City BiggestCity(int teamOf, List<City> ignore)
	{
		City[] cities = InfluenceMan.ins.cities.ToArray();
		float bpop = float.MinValue;
		City big = null;
		for (int i = 0; i < cities.Length; i++)
		{
			if (cities[i].team != teamOf || ignore.Contains(cities[i])) continue;
			if (cities[i].truepop > bpop) {
				bpop = cities[i].truepop;
				big = cities[i];
			}
		}
		return big;
	}

	public static List<City> GetCities(int team) {
		List<City> cs = new List<City>();
		for(int i =0; i < InfluenceMan.ins.cities.Count; i++) {
			if (InfluenceMan.ins.cities[i].team == team) {
				cs.Add(InfluenceMan.ins.cities[i]);
			}
		}
		return cs;
    }

	public static Vector2[] Encircle(Vector2 pos, float radius, int numSamples) {
		Vector2[] enc = new Vector2[numSamples];
		float invS = 1 / (float)numSamples;
		for (int i = 0; i < numSamples; i++)
		{
			float theta = 360 * invS * i * Mathf.Deg2Rad;

			Vector2 off = Vector2.zero;
			off.x = radius * Mathf.Cos(theta);
			off.y = radius * Mathf.Sin(theta);
			enc[i] = pos + off;
		}
		return enc;
	}

	public static void Salvo(Silo sl, Order or, int repeat) {
		int n = Mathf.Min(repeat, sl.numMissiles);
		for (int i = 0; i < n; i++) {
			sl.Direct(or);
		}
    }

	//public static List<Army> FetchStoredArmyData(int team) { 
		
 //   }
}
