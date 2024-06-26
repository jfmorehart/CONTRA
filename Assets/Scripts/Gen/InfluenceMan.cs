using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InfluenceMan : MonoBehaviour
{
    public static InfluenceMan ins;

    public GameObject armyPrefab;
	public GameObject cityPrefab;
	public GameObject statePrefab;
	public GameObject playerPrefab;
	public GameObject siloPrefab;
	public GameObject constructionPrefab;

	public List<Army> armies;
	public List<Silo> silos;
	public List<Unit> other;
	public List<City> cities;

	private void Awake()
	{
        ins = this;
        armies = new List<Army>();
        silos = new List<Silo>();
	}
	public void Setup() {
		ins = this;
		cities = new List<City>();
	}

	public State NewState(int index, Vector2Int pos) {
		Vector3 p = MapUtils.CoordsToPoint(pos);
		GameObject go;
		
		//go = Instantiate(statePrefab, p, Quaternion.identity, transform);
		if (index == 0)
		{
			go = Instantiate(playerPrefab, p, Quaternion.identity, transform);
		}
		else
		{
			go = Instantiate(statePrefab, p, Quaternion.identity, transform);
		}
		State s = go.GetComponent<State>();
		s.Setup(index, pos);
		return s;
	}


	public void RandomArmies(int numToSpawn) {

        for (int i = 0; i < numToSpawn; i++) {
            Vector2 wp = RandomPointOnMap();

			if(Map.ins.GetPixTeam(MapUtils.PointToCoords(wp)) < 0) continue;

            Transform t = Instantiate(armyPrefab, wp, Quaternion.identity, transform).transform;
            Army rm = t.GetComponent<Army>();
		}

		for(int i = 0; i < 10; i++) {
			Vector2 wp = RandomPointOnMap();

			if (Map.ins.GetPixTeam(MapUtils.PointToCoords(wp)) < 0) continue;

			Transform t = Instantiate(siloPrefab, wp, Quaternion.identity, transform).transform;
		}

	}
	public Army PlaceArmy(Vector2 worldPos)
	{
		if (Map.ins.GetPixTeam(MapUtils.PointToCoords(worldPos)) < 0) return null;

		Army ar = Instantiate(armyPrefab, worldPos, Quaternion.identity, transform).GetComponent<Army>();
		return ar;
	}
	public void Spawn_CityLogic(int index, Inf city)
	{
		if (city.team == -1) return;

		Vector3 p = MapUtils.CoordsToPoint(city.pos);
		City c = Instantiate(
			cityPrefab, p,
			Quaternion.identity,
			transform
			).GetComponent<City>();

		cities.Add(c);

		c.SetUpCity(city.team, city.pop);
		c.name = index.ToString();
	}


    public Inf[] UpdateArmies() {

		CleanArmies();
		Unit[] tarmies = armies.ToArray();
		Silo[] tsilos = silos.ToArray();
        Inf[] linfs = new Inf[tarmies.Length + tsilos.Length];
        for(int i = 0; i < tarmies.Length; i++) {
            linfs[i] = new Inf(
                MapUtils.PointToCoords(tarmies[i].transform.position),
                Map.ins.armyInfluenceStrength,
                tarmies[i].team,
				1
                );
	    }
		for (int i = tarmies.Length; i < tarmies.Length + tsilos.Length; i++)
		{
			linfs[i] = new Inf(
				MapUtils.PointToCoords(tsilos[i - tarmies.Length].transform.position),
				Map.ins.armyInfluenceStrength,
				tsilos[i - tarmies.Length].team,
				1
				);
		}

		return linfs;
    }
	public Inf[] UpdateCities()
	{
		Inf[] infcities = new Inf[cities.Count];
		for (int i = 0; i < infcities.Length; i++)
		{
			infcities[i] = new Inf(
				cities[i].mpos,
                cities[i].pop,
				cities[i].team,
				0
				);
		}

		return infcities;
	}
	public void SwapTeamsCities(int team1, int team2) { 
		for(int i = 0; i < cities.Count; i++) {
			if (cities[i].team == team1) {
				cities[i].team = team2;
			}else if (cities[i].team == team2)
			{
				cities[i].team = team1;
			}
		}
    }
	public List<Unit> CleanList(List<Unit> ls) {
		List<Unit> clean = new List<Unit>();
		for (int i = 0; i < ls.Count; i++)
		{
			if (ls[i] != null)
			{
				clean.Add(ls[i]);
			}
		}
		return clean;
	}
	public void CleanArmies() {
		//List<Army> clean = new List<Army>();
		//for(int i = 0; i < armies.Count; i++) {
		//	if (armies[i] != null) {
		//		clean.Add(armies[i]);
		//	}
		//}
		//armies = clean;
    }

	Vector2 RandomPointOnMap() {
        Vector2 rn = new Vector2(Random.Range(0.01f, 0.99f), Random.Range(0.01f, 0.99f));
        rn *= Map.ins.transform.localScale;
        return rn;
    }

	public Unit[] BoxSearch(Vector2 w1, Vector2 w2)
	{
		List<Unit> sel = new List<Unit>();
		float minX = Mathf.Min(w1.x, w2.x);
		float minY = Mathf.Min(w1.y, w2.y);
		float maxX = Mathf.Max(w1.x, w2.x);
		float maxY = Mathf.Max(w1.y, w2.y);

		CleanArmies();
		foreach (Unit r in armies)
		{
			Vector2 pos = r.transform.position;
			if (pos.x < minX) continue;
			if (pos.x > maxX) continue;
			if (pos.y < minY) continue;
			if (pos.y > maxY) continue;
			sel.Add(r);
		}
		return sel.ToArray();
	}
	public void RemoveCity(City c) {
		cities.Remove(c);
		Map.ins.numCities = cities.Count;
    }

	public void RegisterUnit(Unit un) {
		if(un is Army) {
			armies.Add(un as Army);
		}
		if (un is Silo)
		{
			silos.Add(un as Silo);
		}
		else {
			other.Add(un);

			if (un is Construction)
			{
				Diplomacy.states[un.team].construction_sites.Add(un as Construction);
			}
		}
	}

	public void DeregisterUnit(Unit un)
	{
		if (un is Army)
		{
			armies.Remove(un as Army);
		}
		if (un is Silo)
		{
			silos.Remove(un as Silo);
		}
		else
		{
			other.Remove(un);

			if(un is Construction) {
				Diplomacy.states[un.team].construction_sites.Remove(un as Construction);
			}
		}
	}

}
