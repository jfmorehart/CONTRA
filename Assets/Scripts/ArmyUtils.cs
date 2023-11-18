using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ArmyUtils
{
	public static Unit[] GetUnits(int team)
	{
		//Get info from tracker
		List<Unit> uns = new List<Unit>();
		InfluenceMan.ins.CleanArmies();
		Unit[] units = InfluenceMan.ins.tracker.ToArray();

		//Pick the ones we want
		for(int i = 0; i < units.Length; i++){
			if (units[i].team == team) { uns.Add(units[i]); }
		}
		return uns.ToArray();
	}
	public static Unit[] GetUnits(int team, int number, Vector2 near, List<Unit> ignore)
	{
		//Get info from tracker
		List<Unit> uns = new List<Unit>();
		InfluenceMan.ins.CleanArmies();
		Unit[] units = InfluenceMan.ins.tracker.ToArray();
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
		City[] cities = InfluenceMan.ins.cities;
		float cdist = float.MaxValue;
		City near = null;
		for(int i = 0; i < cities.Length; i++) {
			if (cities[i].team != teamOf || ignore.Contains(cities[i])) continue;
			float ndist = Vector2.Distance(pos, cities[i].transform.position);
			if(ndist < cdist) {
				cdist = ndist;
				near = cities[i];
			}
		}
		return near;
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
}
