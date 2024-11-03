using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapUtils
{
	static readonly int ICBMspeed = 50;

	public static Inf PlaceCity(int index) {

		Vector2Int posI;

		if (Map.multi) {
			posI = MultiplayerCityPosition(index);
		}
		else {
			posI = new Vector2Int(
				Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.x)),
				Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.y)));
		}

		int mteam = 0;
		float cdist = 9000;

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			float dist = Vector2.Distance(Map.ins.state_centers[i], posI);
			if (dist < cdist)
			{
				cdist = dist;
				mteam = i;
			}
		}
		//dont spawn a city in the ocean
		if (Map.ins.GetPixTeam(posI) == -1) mteam = -1;

		float popU = Random.Range(1, 10); //todo replace with popcount
		return new Inf(posI, popU, mteam, 0);
    }

	public static Vector2Int MultiplayerCityPosition(int i)
	{
		float x = MultiplayerVariables.Ran(new Vector2(i + Map.ins.mapSeed, i - Map.ins.mapSeed));
		float y = MultiplayerVariables.Ran(new Vector2(i - Map.ins.mapSeed, i + Map.ins.mapSeed));

		Vector2Int posI = new Vector2Int(

			Mathf.RoundToInt(Mathf.Lerp(1, Map.ins.texelDimensions.x, x)),
			Mathf.RoundToInt(Mathf.Lerp(1, Map.ins.texelDimensions.y, y)));

		return posI;
	}

	public static void NukeObjs(Vector2 wpos, float radius, bool hitsAir = false) {
		float dist = Mathf.Pow(radius, 0.4f) * 25;
		if (hitsAir)
		{
			for (int i = 0; i < ArmyManager.ins.aircraft.Count; i++)
			{
				float nd = Vector2.Distance(ArmyManager.ins.aircraft[i].transform.position, wpos);
				if (nd < dist)
				{
					ArmyManager.ins.aircraft[i].Kill();
				}
			}
			return;
		}
		for(int i =0; i < ArmyManager.ins.armies.Count; i++) {
			float nd = Vector2.Distance(ArmyManager.ins.armies[i].transform.position, wpos);
			if(nd < dist) {
				ArmyManager.ins.armies[i].Kill();
			}
		}

		for (int i = 0; i < ArmyManager.ins.silos.Count; i++)
		{
			float nd = Vector2.Distance(ArmyManager.ins.silos[i].transform.position, wpos);
			if (nd < dist)
			{
				ArmyManager.ins.silos[i].Kill();
			}
		}
		for (int i = 0; i < ArmyManager.ins.airbases.Count; i++)
		{
			float nd = Vector2.Distance(ArmyManager.ins.airbases[i].transform.position, wpos);
			if (nd < dist)
			{
				ArmyManager.ins.airbases[i].Kill();
			}
		}
		for (int i = 0; i < ArmyManager.ins.batteries.Count; i++)
		{
			float nd = Vector2.Distance(ArmyManager.ins.batteries[i].transform.position, wpos);
			if (nd < dist)
			{
				ArmyManager.ins.batteries[i].Kill();
			}
		}
		for (int i = 0; i < ArmyManager.ins.other.Count; i++)
		{
			float nd = Vector2.Distance(ArmyManager.ins.other[i].transform.position, wpos);
			if (nd < dist)
			{
				ArmyManager.ins.other[i].Kill();
			}
		}
	}
	public static Vector2Int PlaceState(int index)
	{
		bool valid = false;
		Vector2Int posI = Vector2Int.zero;

		int it = 0;
		while (!valid) {
			it++;
			if (it > 500) {
				Debug.LogError("no valid point found to place state");
				return Vector2Int.zero;
			}
			if (Map.multi) {
				posI = MultiplayerCityPosition((index * 11003) + 1 * it);
				Debug.Log("input " + ((index * 11003) + 1 * it) + " output = " + posI);
			}
			else {
				posI = new Vector2Int(
				Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.x)),
				Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.y)));
			}

			//state may not be centered in water
			valid = Map.ins.GetPixTeam(posI) != -1;
		}
		
		ArmyManager.ins.NewState(index, posI);
		return posI;
	}

	public static Vector2Int RandomPointInState(int state) {
		Vector2Int posI = new Vector2Int(
		Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.x)),
		Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.y)));
		int tries = 0;
		while (Map.ins.GetPixTeam(posI) != state) {
			tries++;
			if(tries > 2000) {
				Debug.Log("No suitable location found " + tries);
				return Vector2Int.zero;
			}
			posI = new Vector2Int(
				Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.x)),
				Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.y)));
		}
		return posI;
	}
	public static int[] LiveTeamsBuffer() {
		int[] liveteams = new int[Map.ins.numStates];
		for(int i = 0; i < liveteams.Length; i++) {
			liveteams[i] = Diplomacy.states[i].alive ? 1 : 0;
		}
		return liveteams;
    }
	public static (bool hit, Vector2Int hitpos) TexelRayCast(Vector2Int start, Vector2 direction, int maxDistance = 500, bool ignoreOcean = true) {
		int team = Map.ins.GetPixTeam(start);
		for(int i = 0; i < maxDistance; i++) {
			Vector2 realpt = start + direction * i;
			Vector2Int newPoint = new Vector2Int(Mathf.RoundToInt(realpt.x), Mathf.RoundToInt(realpt.y));
			if (!InBounds(newPoint)) {
				realpt -= direction;
				return (true, new Vector2Int(Mathf.RoundToInt(realpt.x), Mathf.RoundToInt(realpt.y)));
			}
			int pointTeam = Map.ins.GetPixTeam(newPoint);
			if (pointTeam != team && (pointTeam != -1 || !ignoreOcean)) {
				return (true, newPoint);
			}
		}
		return (false, Vector2Int.zero);
    }
	public static bool InBounds(Vector2Int point) {
		if (point.x < 0 || point.x > Map.ins.texelDimensions.x - 1) {
			return false;
		}
		if (point.y < 0 || point.y > Map.ins.texelDimensions.y - 1)
		{
			return false;
		}
		return true;
	}

	public static Vector3Int STIN_FromIndex(int index) {
		// presently unused, but don't want to delete in case i ever need it
		// since I don't really remember how the STIN array works

		if (!Map.ins) return Vector3Int.zero;
		int rowSize = Map.ins.numStates * Map.ins.texelDimensions.x;
		float fy = index / (float)(rowSize);
		int y = Mathf.FloorToInt(fy);

		int amalg = index % rowSize;
		float fx = amalg / (float)(Map.ins.numStates);
		int x = Mathf.FloorToInt(fx);
		int state = amalg % Map.ins.numStates;

		return new Vector3Int(x, y, state);
    }

	public static int STIN_ToIndex(Vector3Int seperate) {
		// presently unused, but don't want to delete in case i ever need it
		// since I don't really remember how the STIN array works

		if (!Map.ins) return 0;
		int state = seperate.z;
		int x = seperate.x;
		int y = seperate.y;
		int index = y * (Map.ins.texelDimensions.x * Map.ins.numStates);
		index += x * Map.ins.numStates;
		index += state;
		return index;
		// st = 1, x  = 2, y = 0
    }
	public static Vector2Int[] BuildingPositions() {
		List<Vector2Int> points = new List<Vector2Int>();
		foreach(Building b in ArmyManager.ins.allbuildings) {
			points.Add(b.mapPos);
		}
		foreach(Unit u in ArmyManager.ins.other) {
			points.Add(PointToCoords(u.transform.position));
		}
		return points.ToArray();
    }

	public static Vector2Int PointToCoords(Vector2 point) {
		Vector2 co = new Vector2(point.x / Map.localScale.x,
	    point.y / Map.localScale.y);
		co.x *= Map.ins.texelDimensions.x;
		co.y *= Map.ins.texelDimensions.y;
		Vector2Int cor = new Vector2Int(Mathf.RoundToInt(co.x), Mathf.RoundToInt(co.y));
		return cor;
	}

	public static Vector2 CoordsToPoint(Vector2Int coords) {
		Vector2 point = Vector2.zero;
		point.x = coords.x / (float)Map.ins.texelDimensions.x;
		point.y = coords.y / (float)Map.ins.texelDimensions.y;

		point *= Map.ins.transform.localScale;

		return point;
    }

	public static int WorldPosToTeam(Vector2 point) {
		return Map.ins.GetPixTeam(PointToCoords(point));
    }

	public static uint TexelPopToWorldPop(float rout) {
		//arbitrary?
		rout = Mathf.Pow(rout, 0.7f) * 0.3f;
		return (uint)Mathf.RoundToInt(rout);
	}

	public static float Tau(Vector2 p1, Vector2 p2) {
		return Vector2.Distance(p1, p2) / ICBMspeed;
	}
	public static Vector2Int IndexToCoords(int index) {
		int x = index % Map.ins.texelDimensions.x;
		int y = Mathf.FloorToInt(index / Map.ins.texelDimensions.x);
		return new Vector2Int(x, y);
    }

	public static int[] BitwiseTeams(int parse) {
		List<int> teams = new List<int>();
		for(int i = 0; i < Map.ins.numStates; i++) {
			if ((parse & Mathf.RoundToInt(Mathf.Pow(2, i))) > 0) {
				teams.Add(i);
			}
		}
		return teams.ToArray();
    }
}
