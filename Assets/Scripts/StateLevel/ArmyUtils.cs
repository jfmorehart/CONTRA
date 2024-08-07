
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public static class ArmyUtils
{
	public static int[] conventionalCount;
	public static int[] nuclearCount;

	//Keeps a list of info on the armies that was collected
	// as a byproduct from a different operation to use for non-critical
	// calculations like city capture updates.

	public static List<Unit>[] armies;
	public static List<Unit>[] aircraft;
	public static List<Silo>[] silos;
	public static List<Airbase>[] airbases;
	public static List<AAA>[] batteries;
 

	public static void Init() {
		conventionalCount = new int[Map.ins.numStates];
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
			GetArmies(i);
			GetSilos(i);
			GetAirbases(i);
			GetAAAs(i);
		}
	}
	/*		if (armies[team].Count > 0) {
			uns.AddRange(armies[team]);
		}
		if (airbases[team].Count > 0)
		{
			uns.AddRange(airbases[team]);
		}
		if (silos[team].Count > 0)
		{
			uns.AddRange(silos[team]);
		}
		if (aircraft[team].Count > 0)
		{
			uns.AddRange(aircraft[team]);
		}*/

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
	public static List<Target> GetTargets(int team, int saturation, bool nuclear, bool conventional, bool cities)
	{
		List<Target> tars = new List<Target>();
		if (nuclear)
		{
			tars.AddRange(StrategicTargets(team));
		}
		if (conventional)
		{
			tars.AddRange(ConventionalTargets(team, saturation));
		}
		if (cities)
		{
			tars.AddRange(CivilianTargets(team));
		}
		return tars;
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
	public static Target[] StrategicTargets(int team) {
		List<Target> tars = new List<Target>();
		tars.AddRange(AirSupremacyTargets(team));
		tars.AddRange(NuclearTargets(team));
		return tars.ToArray();
	}
	public static Target[] AirSupremacyTargets(int team) {
		List<Target> tars = new List<Target>();
		Airbase[] air = GetAirbases(team);
		foreach (Airbase sl in air)
		{
			tars.Add(new Target(sl.transform.position, 45, Tar.Nuclear));
		}
		AAA[] bat = GetAAAs(team);
		foreach (AAA sl in bat)
		{
			tars.Add(new Target(sl.transform.position, 15, Tar.Conventional));
		}
		return tars.ToArray();
	}
	public static Target[] NuclearTargets(int team) {
		List<Target> tars = new List<Target>();
		Silo[] sls = GetSilos(team);
		foreach (Silo sl in sls)
		{
			tars.Add(new Target(sl.transform.position, 50, Tar.Nuclear));
		}
		nuclearCount[team] = tars.Count;
		return tars.ToArray();
	}

	public static Target[] ConventionalTargets(int team, int numTargets = 6)
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
		System.Array.Reverse(indexes);

		int t = 0;
		while (tars.Count < numTargets) {
			t++;
			if(t > 25) {
				//no guarantees that there will be good targets, 
				// so we'll only look at the most promising chunks
				break;
			}
			Vector2 testPos = UnitChunks.ChunkIndexToMapPos(indexes[t]);
			Vector2Int gpos = MapUtils.PointToCoords(testPos);
			if(team != Map.ins.GetPixTeam(gpos)) {
				continue;
			}
			Target tar = new Target(
				testPos,
				UnitChunks.chunkValues[team][indexes[t]],
				Tar.Conventional
			);
			tars.Add(tar);
			if (tar.value < 3) break;
		}
		return tars.ToArray();
	}
	public static Target[] CivilianTargets(int team)
	{
		List<Target> tars = new List<Target>();
		foreach (City sl in GetCities(team))
		{
			if (Map.ins.GetPixTeam(sl.mpos) != team) continue;
			tars.Add(new Target(sl.transform.position, sl.truepop * 0.5f, Tar.Civilian));
		}
		return tars.ToArray();
	}

	public static Silo[] GetSilos(int team)
	{
		//Get info from tracker
		List<Silo> uns = new List<Silo>();
		ArmyManager.ins.CleanArmies();
		Silo[] units = ArmyManager.ins.silos.ToArray();

		//Pick the ones we want
		for (int i = 0; i < units.Length; i++)
		{
			if (units[i].team == team) { uns.Add(units[i]); }
		}
		silos[team] = uns;
		return uns.ToArray();
	}
	public static Airbase[] GetAirbases(int team)
	{
		//Get info from tracker
		List<Airbase> uns = new List<Airbase>();
		ArmyManager.ins.CleanArmies();
		Airbase[] units = ArmyManager.ins.airbases.ToArray();

		//Pick the ones we want
		for (int i = 0; i < units.Length; i++)
		{
			if (units[i].team == team) { uns.Add(units[i]); }
		}
		airbases[team] = uns;
		return uns.ToArray();
	}

	public static AAA[] GetAAAs(int team) {
		//Get info from tracker
		List<AAA> uns = new List<AAA>();
		AAA[] units = ArmyManager.ins.batteries.ToArray();

		//Pick the ones we want
		for (int i = 0; i < units.Length; i++)
		{
			if (units[i].team == team) { uns.Add(units[i]); }
		}
		batteries[team] = uns;
		return uns.ToArray();
	}

	public static Unit[] AllUnitInventory(int team) {
		List<Unit> uns = new List<Unit>();


		uns.AddRange(GetArmies(team));
		uns.AddRange(GetAirbases(team));
		uns.AddRange(GetSilos(team));
		uns.AddRange(GetAircraft(team));

		return uns.ToArray();
	}

	public static Building[] GetBuildings(int team) {

		List<Building> uns = new List<Building>();
		Building[] units = ArmyManager.ins.allbuildings.ToArray();

		//Pick the ones we want
		for (int i = 0; i < units.Length; i++)
		{
			if (units[i].team == team) { uns.Add(units[i]); }
		}
		
		return uns.ToArray();
	}

	public static Unit[] GetAircraft(int team) {
		List<Unit> uns = new List<Unit>();
		Unit[] units = ArmyManager.ins.aircraft.ToArray();

		//Pick the ones we want
		for (int i = 0; i < units.Length; i++)
		{
			if (units[i].team == team) { uns.Add(units[i]); }
		}

		aircraft[team] = uns;
		return uns.ToArray();
	}
	public static Unit EnemyAircraftInRange(int team, Vector2 pos, float range, List<Unit> ignore = null) {
		int[] enemyTeams = ROE.GetEnemies(team).ToArray();
		Unit[] units = ShuffleUnits(ArmyManager.ins.aircraft.ToArray());
		bool ignoring = ignore != null;
		foreach (Unit u in units) {
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
		//Get info from tracker
		List<Unit> uns = new List<Unit>();
		ArmyManager.ins.CleanArmies();
		Unit[] units = ArmyManager.ins.armies.ToArray();

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
		ArmyManager.ins.CleanArmies();
		Unit[] units = ArmyManager.ins.armies.ToArray();
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

	public static List<City> GetCities(int team) {
		List<City> cs = new List<City>();
		for(int i =0; i < ArmyManager.ins.cities.Count; i++) {
			if (ArmyManager.ins.cities[i].team == team) {
				cs.Add(ArmyManager.ins.cities[i]);
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

}
