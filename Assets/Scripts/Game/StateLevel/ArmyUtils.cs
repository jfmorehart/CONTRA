
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public static class ArmyUtils
{
	public static int[] nuclearCount;

	//Keeps a list of info on the armies that was collected
	// as a byproduct from a different operation to use for non-critical
	// calculations like city capture updates.

	public static List<Unit>[] armies;
	public static List<Unit>[] aircraft;
	public static List<Silo>[] silos;
	public static List<Airbase>[] airbases;
	public static List<AAA>[] batteries;


	//preallocation
	static int[] unitChunkIndices_prealloc;
	static int[] unitChunkValues_prealloc;

	public static void Init() {
		nuclearCount = new int[Map.ins.numStates];
		armies = new List<Unit>[Map.ins.numStates];
		aircraft = new List<Unit>[Map.ins.numStates];
		silos = new List<Silo>[Map.ins.numStates];
		airbases = new List<Airbase>[Map.ins.numStates];
		batteries = new List<AAA>[Map.ins.numStates];

		for (int i = 0; i < Map.ins.numStates; i++) {
			armies[i] = new List<Unit>();
			aircraft[i] = new List<Unit>();
			silos[i] = new List<Silo>();
			airbases[i] = new List<Airbase>();
			batteries[i] = new List<AAA>();
			GetArmies(i);
			GetSilos(i);
			GetAirbases(i);
			GetAAAs(i);
		}

		unitChunkIndices_prealloc = new int[UnitChunks.chunks.Length];
		unitChunkValues_prealloc = new int[UnitChunks.chunks.Length];
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

	static List<Target> tars = new List<Target>();
	public static Target[] GetTargets(int team) {

		tars.Clear();
		StrategicTargets(team, false);
		ConventionalTargets(team, 6, false);
		CivilianTargets(team, false);
		return TargetSort(tars.ToArray());
	}

	public static List<Target> GetTargets(int team, int saturation, bool nuclear, bool conventional, bool cities)
	{
		tars.Clear();
		if (nuclear)
		{
			StrategicTargets(team, false);
		}
		if (conventional)
		{
			ConventionalTargets(team, saturation, false);
		}
		if (cities)
		{
			CivilianTargets(team, false);
		}
		return tars;
	}

	static float[] keys;
	public static Target[] TargetSort(Target[] tr) {
		keys = new float[tr.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			keys[i] = -tr[i].value;
		}
		System.Array.Sort(keys, tr);
		return tr;
	}
	public static Target[] StrategicTargets(int team, bool clear = true) {
		if (clear) tars.Clear(); //allows for better list re-use;

		AirSupremacyTargets(team, false);
		NuclearTargets(team, false);

		return tars.ToArray();
	}
	public static Target[] AirSupremacyTargets(int team, bool clear = true) {

		if(clear)tars.Clear();

		foreach (Airbase sl in GetAirbases(team))
		{
			tars.Add(new Target(sl.transform.position, 45, Tar.Nuclear));
		}
		foreach (AAA sl in GetAAAs(team))
		{
			tars.Add(new Target(sl.transform.position, 15, Tar.Conventional));
		}
		return tars.ToArray();
	}
	public static Target[] NuclearTargets(int team, bool clear = true) {
		if(clear)tars.Clear(); //allows for better list re-use;

		int st = tars.Count;
		foreach (Silo sl in GetSilos(team))
		{
			tars.Add(new Target(sl.transform.position, 50, Tar.Nuclear));
		}
		nuclearCount[team] = tars.Count - st;
		return tars.ToArray();
	}

	public static Target[] ConventionalTargets(int team, int numTargets = 6, bool clear = true)
	{
		// no comments fio bud

		if (clear) tars.Clear(); //allows for better list re-use;

		//re-filling preallocated arrays
		for (int i = 0; i < UnitChunks.chunks.Length; i++) {
			unitChunkIndices_prealloc[i] = i; 
			unitChunkValues_prealloc[i] = UnitChunks.chunkValues[team][i];
		}
		System.Array.Sort(unitChunkValues_prealloc, unitChunkIndices_prealloc);
		System.Array.Reverse(unitChunkIndices_prealloc);

		int t = 0;
		while (tars.Count < numTargets) {
			t++;
			if(t > 25) {
				//no guarantees that there will be good targets, 
				// so we'll only look at the most promising chunks
				break;
			}
			Vector2 testPos = UnitChunks.ChunkIndexToMapPos(unitChunkIndices_prealloc[t]);
			Vector2Int gpos = MapUtils.PointToCoords(testPos);
			if(team != Map.ins.GetPixTeam(gpos)) {
				continue;
			}
			Target tar = new Target(
				testPos,
				UnitChunks.chunkValues[team][unitChunkIndices_prealloc[t]],
				Tar.Conventional
			);
			tars.Add(tar);
			if (tar.value < 3) break;
		}
		return tars.ToArray();
	}
	public static Target[] CivilianTargets(int team, bool clear = true)
	{
		if (clear) tars.Clear(); //allows for better list re-use;

		foreach (City sl in GetCities(team))
		{
			if (Map.ins.GetPixTeam(sl.mpos) != team) continue;
			tars.Add(new Target(sl.transform.position, sl.truepop * 0.5f, Tar.Civilian));
		}
		return tars.ToArray();
	}

	public static Silo[] GetSilos(int team)
	{
		return silos[team].ToArray();
	}
	public static Airbase[] GetAirbases(int team)
	{
		return airbases[team].ToArray();
	}

	public static AAA[] GetAAAs(int team) {
		return batteries[team].ToArray();
	}

	static List<Unit> uns = new List<Unit>();
	public static Unit[] AllUnitInventory(int team) {
		uns.Clear();
		uns.AddRange(GetArmies(team));
		uns.AddRange(GetAirbases(team));
		uns.AddRange(GetSilos(team));
		uns.AddRange(GetAircraft(team));

		return uns.ToArray();
	}

	static List<Building> team_buildings = new List<Building>();
	public static Building[] GetBuildings(int team) {
		team_buildings.Clear();

		//Pick the ones we want
		for (int i = 0; i < ArmyManager.ins.allbuildings.Count; i++)
		{
			if (ArmyManager.ins.allbuildings[i].team == team) {
				 team_buildings.Add(ArmyManager.ins.allbuildings[i]); 
			}
		}
		return team_buildings.ToArray();
	}

	public static Unit[] GetAircraft(int team) {
		return aircraft[team].ToArray();
	}

	public static Unit EnemyAircraftInRange(int team, Vector2 pos, float range, List<Unit> ignore = null) {
		int[] enemyTeams = ROE.GetEnemies(team).ToArray();
		bool ignoring = ignore != null;
		foreach (Unit u in ShuffleUnits(ArmyManager.ins.aircraft.ToArray()))
		{
			if (!enemyTeams.Contains(u.team)) continue;
			if (ignoring) {
				if (ignore.Contains(u)) continue;
			}

			Vector2 delta = (Vector2)u.transform.position - pos;
			if (delta.magnitude > range) continue;
			return u;
		}
		return null;
    }

	public static Unit[] GetArmies(int team)
	{
		return armies[team].ToArray();
	}

	static Unit[] prealloc_units;
	static float[] di;
	public static Unit[] GetArmies(int team, int number, Vector2 near, List<Unit> ignore)
	{
		//Get info from tracker
		uns.Clear();
		prealloc_units = ArmyManager.ins.armies.ToArray();
		//Pick the ones we want
		for (int i = 0; i < prealloc_units.Length; i++)
		{
			if (prealloc_units[i].team == team && !ignore.Contains(prealloc_units[i])) {
				uns.Add(prealloc_units[i]); 
			}
		}
		//Sort by distance
		di = new float[uns.Count];
		prealloc_units = uns.ToArray();
		for(int i = 0; i < prealloc_units.Length; i++) {
			di[i] = Vector2.Distance(prealloc_units[i].transform.position, near);
		}
		System.Array.Sort(di, prealloc_units);

		//Return if no trimming necessary
		if(number >= prealloc_units.Length) {
			return prealloc_units;
		}

		//Trim
		uns.Clear();
		for(int i = 0; i< number; i++) {
			uns.Add(prealloc_units[i]);
		}
		return uns.ToArray();
	}

	public static List<City> GetNearestCitiesOfTeams(List<City> cities,List<int> ofTeams, Vector2 pos) {
		int n = cities.Count;
		for (int i = 0; i < n; i++)
		{
			if (cities.Count <= i) break;
			if (!ofTeams.Contains(cities[i].team)) {
				cities.RemoveAt(i);
			}
		}

		float[] darr = new float[cities.Count];
		for(int i =0; i < darr.Length; i++) {
			darr[i] = Vector2.Distance(cities[i].mpos, pos);
		}
		City[] carr = cities.ToArray();
		System.Array.Sort(darr, carr);

		List<City> output = new List<City>();
		for (int i = 0; i < darr.Length; i++)
		{
			output.Add(carr[i]);
		}
		return output;
	}

	public static City NearestCity(Vector2 pos, int teamOf, List<City> ignore) {
		List<City> cities = ArmyManager.ins.cities;
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
		City[] cities = ArmyManager.ins.cities.ToArray();
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

	static List<City> cs = new List<City>();
	public static List<City> GetCities(int team) {
		cs.Clear();
		for(int i =0; i < ArmyManager.ins.cities.Count; i++) {
			if (ArmyManager.ins.cities[i].team == team) {
				cs.Add(ArmyManager.ins.cities[i]);
			}
		}
		return cs;
    }

	public static float AirAttackStrength(int team)
	{
		float str = 0;
		foreach (Airbase ab in airbases[team])
		{
			str += 5; //represents regenerative capability
			str += ab.numPlanes;
		}
		str += aircraft[team].Count;
		return str;
	}
	public static float AirDefenseStrength(int team) {

		float str = AirAttackStrength(team); //until bombers are a thing

		//todo improve assesment regarding battery placement?
		foreach (AAA ab in batteries[team])
		{
			str += 5; //represents regenerative capability
			str += ab.numMissiles;
		}

		return str;

    }

	static Vector2[] enc = new Vector2[5];
	public static Vector2[] Encircle(Vector2 pos, float radius, int numSamples) {
		enc = new Vector2[numSamples];
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
   
	public static bool Salvo(Silo sl, Order or, int repeat) {
		int n = Mathf.Min(repeat, sl.numMissiles);
		for (int i = 0; i < n; i++) {
			sl.Direct(or);
		}
		return n > 0; //return true if we fired
    }

	public static Unit[] ShuffleUnits(Unit[] toshuffle) {
		for(int i = 0; i < toshuffle.Length; i++) {
			int r = Random.Range(0, toshuffle.Length);
			Unit ui = toshuffle[i];
			toshuffle[i] = toshuffle[r];
			toshuffle[r] = ui;
		}
		return toshuffle;
    }

	public static float RatioToCOV(float ratio) { 
		return Mathf.Clamp(Mathf.Pow(0.08f, Mathf.Pow(ratio * 0.5f, 2)), 0.01f, 0.99f);
	}

}
