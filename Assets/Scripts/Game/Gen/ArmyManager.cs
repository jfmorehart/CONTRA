using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ArmyManager : MonoBehaviour
{
    public static ArmyManager ins;

    public GameObject armyPrefab;
	public GameObject cityPrefab;
	public GameObject statePrefab;
	public GameObject state_onlinePrefab;
	public GameObject playerPrefab;
	public GameObject siloPrefab;
	public GameObject airbasePrefab;
	public GameObject aaaPrefab;
	public GameObject constructionPrefab;

	public List<Building> allbuildings;

	public List<Army> armies;
	public List<Airbase> airbases;
	public List<Plane> aircraft;
	public List<Silo> silos;
	public List<AAA> batteries;
	public List<Unit> other;
	public List<City> cities;

	public enum BuildingType { 
		Silo, 
		Airbase,
		AAA
    }
	public GameObject[] buildPrefabs;

	Inf[] inf_prealloc;

	private void Awake()
	{
        ins = this;
        armies = new List<Army>();
		airbases = new List<Airbase>();
		silos = new List<Silo>();
		allbuildings = new List<Building>();
		buildPrefabs = new GameObject[] { siloPrefab, airbasePrefab, aaaPrefab};
	}
	public void Setup() {
		ins = this;
		cities = new List<City>();
	}

	public State NewState(int index, Vector2Int pos) {
		Debug.Log("placing state " + index);
		Vector3 p = MapUtils.CoordsToPoint(pos);
		GameObject go;

		if (Map.multi) {
			if (MultiplayerVariables.ins.clientIDs[index] == NetworkManager.Singleton.LocalClientId) {
				go = Instantiate(playerPrefab, p, Quaternion.identity, transform);
			}
			else {
				go = Instantiate(state_onlinePrefab, p, Quaternion.identity, transform);
			}
		}
		else {
			if (index == 0)
			{
				go = Instantiate(playerPrefab, p, Quaternion.identity, transform);
			}
			else
			{
				go = Instantiate(statePrefab, p, Quaternion.identity, transform);
			}
		}

		State s = go.GetComponent<State>();
		s.Setup(index, pos);
		return s;
	}


	public void RandomArmies(int numToSpawn) {

        for (int i = 0; i < numToSpawn; i++) {
            Vector2 wp = RandomPointOnMap();

			if(Map.ins.GetPixTeam(MapUtils.PointToCoords(wp)) < 0) continue;

			GameObject go = Instantiate(armyPrefab, wp, Quaternion.identity, transform);
			if (Map.multi) {
				go.GetComponent<NetworkObject>().Spawn();
			}
		}

		//for(int i = 0; i < 2; i++) {
		//	Vector2 wp = RandomPointOnMap();

		//	if (Map.ins.GetPixTeam(MapUtils.PointToCoords(wp)) < 0) continue;

		//	Transform t = Instantiate(siloPrefab, wp, Quaternion.identity, transform).transform;
		//}
		//for (int i = 0; i < 2; i++)
		//{
		//	Vector2 wp = RandomPointOnMap();

		//	if (Map.ins.GetPixTeam(MapUtils.PointToCoords(wp)) < 0) continue;

		//	Transform t = Instantiate(airbasePrefab, wp, Quaternion.identity, transform).transform;
		//}
		//for (int i = 0; i < 3; i++)
		//{
		//	Vector2 wp = RandomPointOnMap();

		//	if (Map.ins.GetPixTeam(MapUtils.PointToCoords(wp)) < 0) continue;

		//	Transform t = Instantiate(aaaPrefab, wp, Quaternion.identity, transform).transform;
		//}
	}
	public void PlaceArmy(Vector2 worldPos)
	{
		if (Map.ins.GetPixTeam(MapUtils.PointToCoords(worldPos)) < 0) return;

		if (Map.multi) {
			if (Map.host) {
				Army ar = Instantiate(armyPrefab, worldPos, Quaternion.identity, transform).GetComponent<Army>();
				if (Map.multi)
				{
					ar.GetComponent<NetworkObject>().Spawn();
				}
			}
			else {
				//Request spawn from server
				MultiplayerVariables.ins.SpawnArmyServerRPC(worldPos);
			}
		}
		else {
			Instantiate(armyPrefab, worldPos, Quaternion.identity, transform).GetComponent<Army>();
		}
	}

	public Unit NewConstruction(int team, Vector2Int mapPos, ArmyManager.BuildingType btype, bool grandfathered = false) {

		if (mapPos == Vector2Int.zero) return null; //no spot found
		if (!ValidMapPlacement(team , mapPos) && !grandfathered) return null;

		if (Map.multi && !Map.host)
		{
			MultiplayerVariables.ins.NewConstructionServerRPC(mapPos, (int)btype);
			return null;
		}

		Transform t = Instantiate(ArmyManager.ins.constructionPrefab,
			MapUtils.CoordsToPoint(mapPos), Quaternion.identity, ArmyManager.ins.transform).transform;

		Construction co = t.GetComponent<Construction>();
		co.team = team;
		co.PrepareBuild(btype);

		if (Map.multi) {
			//host
			NetworkObject no = co.GetComponent<NetworkObject>();
			no.SpawnWithOwnership(MultiplayerVariables.ins.clientIDs[team]);
			MultiplayerVariables.ins.NewConstructionClientRPC(no.NetworkObjectId, (int)btype);
		}
		return co as Unit;
	}
	public static bool ValidMapPlacement(int team, Vector2Int mapPos) {
		int teamOf = Map.ins.GetPixTeam(mapPos);
		if (team != teamOf) return false;

		foreach(Vector2Int v in MapUtils.BuildingPositions()) {
			if (Vector2Int.Distance(mapPos, v) < Map.ins.buildExclusionDistance) {
				return false;
			}
		}
		return true;
    }

	public City Spawn_CityLogic(int index, Inf city)
	{
		if (city.team == -1) return null;

		Vector3 p = MapUtils.CoordsToPoint(city.pos);
		City c = Instantiate(
			cityPrefab, p,
			Quaternion.identity,
			transform
			).GetComponent<City>();

		cities.Add(c);

		c.SetUpCity(city.team, city.pop);
		c.name = index.ToString();
		return c;
	}


	List<Unit> all = new List<Unit>();
	Unit[] all_arr;
	public Inf[] UpdateArmies() {

		all.Clear();
		all.AddRange(armies);
		all.AddRange(silos);
		all.AddRange(airbases);
		all.AddRange(batteries);
		all_arr = all.ToArray();
		Inf[] linfs = new Inf[all_arr.Length];
		for (int i = 0; i < all_arr.Length; i++)
		{
			linfs[i] = new Inf(
				MapUtils.PointToCoords(all_arr[i].transform.position),
				Map.ins.armyInfluenceStrength,
				all_arr[i].actingTeam,
				1
				);
		}
		return linfs;
	}

	Inf[] infcities;
	public Inf[] UpdateCities()
	{
		infcities = new Inf[cities.Count];
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
	List<Unit> clean = new List<Unit>();
	public List<Unit> CleanList(List<Unit> ls) {

		clean.Clear();
		for (int i = 0; i < ls.Count; i++)
		{
			if (ls[i] != null)
			{
				clean.Add(ls[i]);
			}
		}
		return clean;
	}

	Vector2 RandomPointOnMap() {
        Vector2 rn = new Vector2(Random.Range(0.01f, 0.99f), Random.Range(0.01f, 0.99f));
        rn *= Map.ins.transform.localScale;
        return rn;
    }

	List<Unit> sel = new List<Unit>();
	public Unit[] BoxSearch(Vector2 w1, Vector2 w2)
	{
		sel.Clear();
		float minX = Mathf.Min(w1.x, w2.x);
		float minY = Mathf.Min(w1.y, w2.y);
		float maxX = Mathf.Max(w1.x, w2.x);
		float maxY = Mathf.Max(w1.y, w2.y);

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

		//inclusive lists
		if(un is Building) {
			allbuildings.Add(un as Building);
		}

		//exclusive lists
		if(un is Army) {
			armies.Add(un as Army);
			ArmyUtils.armies[un.team].Add(un);
			return;
		}
		if (un is Plane)
		{
			aircraft.Add(un as Plane);
			ArmyUtils.aircraft[un.team].Add(un as Plane);
			return;
		}
		if (un is Airbase)
		{
			airbases.Add(un as Airbase);
			ArmyUtils.airbases[un.team].Add(un as Airbase);
			return;
		}
		if (un is AAA)
		{
			batteries.Add(un as AAA);
			ArmyUtils.batteries[un.team].Add(un as AAA);
			return;
		}
		if (un is Silo)
		{
			silos.Add(un as Silo);
			ArmyUtils.silos[un.team].Add(un as Silo);
		}
		else {
			other.Add(un);

			if (un is Construction)
			{
				if (((Construction)un).manHoursRemaining > 1)
				{
					Diplomacy.states[un.team].construction_sites.Add(un as Construction);
				}
				
			}
		}
	}

	public void DeregisterUnit(Unit un)
	{
		//inclusive lists
		if (un is Building)
		{
			allbuildings.Remove(un as Building);
		}

		//exclusive lists
		if (un is Army)
		{
			armies.Remove(un as Army);
			ArmyUtils.armies[un.team].Remove(un);
			return;
		}
		if (un is Plane)
		{
			aircraft.Remove(un as Plane);
			ArmyUtils.aircraft[un.team].Remove(un as Plane);
			return;
		}
		if (un is Silo)
		{
			silos.Remove(un as Silo);
			ArmyUtils.silos[un.team].Remove(un as Silo);
		}
		if (un is Airbase)
		{
			airbases.Remove(un as Airbase);
			ArmyUtils.airbases[un.team].Remove(un as Airbase);
		}
		if (un is AAA)
		{
			batteries.Remove(un as AAA);
			ArmyUtils.batteries[un.team].Remove(un as AAA);
			return;
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
