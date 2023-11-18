using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfluenceMan : MonoBehaviour
{
    public static InfluenceMan ins;

    public GameObject armyPrefab;
	public GameObject cityPrefab;
	public GameObject statePrefab;

   
    public List<Unit> tracker;
	public City[] cities;

	private void Awake()
	{
        ins = this;
        tracker = new List<Unit>();
	}
	public void Setup() {
		cities = new City[Map.ins.numCities];
	}

	public State NewState(int index, Vector2Int pos) {
		Vector3 p = MapUtils.CoordsToPoint(pos);
		GameObject go = Instantiate(statePrefab, p, Quaternion.identity, transform);
		State s = go.GetComponent<State>();
		s.Setup(index, pos);
		return s;
	}

	public void RandomArmies(int numToSpawn) {

        for (int i = 0; i < numToSpawn; i++) {
            Vector2 wp = RandomPointOnMap();
			Vector2Int pt = MapUtils.PointToCoords(wp);
            int te = Map.ins.GetPixTeam(pt);
            Transform t = Instantiate(armyPrefab, wp, Quaternion.identity, transform).transform;
            Army rm = t.GetComponent<Army>();
            rm.Setup(50, te); 
            tracker.Add(rm);
		}
    }
	public void Spawn_CityLogic(int index, Inf city)
	{
		Vector3 p = MapUtils.CoordsToPoint(city.pos);
		cities[index] = Instantiate(
			cityPrefab, p,
			Quaternion.identity,
			transform
			).GetComponent<City>();
		cities[index].SetUpCity(city.team, city.pop);
	}


    public Inf[] UpdateArmies() {

		CleanArmies();
        Inf[] armies = new Inf[tracker.Count];
        for(int i = 0; i < armies.Length; i++) {
            armies[i] = new Inf(
                MapUtils.PointToCoords(tracker[i].transform.position),
                Map.ins.armyInfluenceStrength,
                tracker[i].team,
				1
                );
	    }

        return armies;
    }
	public Inf[] UpdateCities()
	{
		Inf[] infcities = new Inf[cities.Length];
		for (int i = 0; i < infcities.Length; i++)
		{
			infcities[i] = new Inf(
				MapUtils.PointToCoords(cities[i].transform.position),
                cities[i].pop,
				cities[i].team,
				0
				);
		}

		return infcities;
	}

	public void CleanArmies() {
		List<Unit> clean = new List<Unit>();
		for(int i = 0; i < tracker.Count; i++) {
			if (tracker[i] != null) {
				clean.Add(tracker[i]);
			}
		}
		tracker = clean;
    }

	Vector2 RandomPointOnMap() {
        Vector2 rn = new Vector2(Random.Range(0.05f, 0.95f), Random.Range(0.05f, 0.95f));
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
		foreach (Unit r in tracker)
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

}
