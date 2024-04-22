using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapUtils
{
	static readonly int ICBMspeed = 50;
	public static Inf PlaceCity(int index) {
		Vector2Int posI = new Vector2Int(
			Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.x)),
			Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.y)));

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

	public static void NukeObjs(Vector2 wpos, float radius) {
		float dist = Mathf.Pow(radius, 0.4f) * 25;
		for(int i =0; i < InfluenceMan.ins.armies.Count; i++) {
			float nd = Vector2.Distance(InfluenceMan.ins.armies[i].transform.position, wpos);
			if(nd < dist) {
				InfluenceMan.ins.armies[i].Kill();
			}
		}
		for (int i = 0; i < InfluenceMan.ins.silos.Count; i++)
		{
			float nd = Vector2.Distance(InfluenceMan.ins.silos[i].transform.position, wpos);
			if (nd < dist)
			{
				InfluenceMan.ins.silos[i].Kill();
			}
		}
		for (int i = 0; i < InfluenceMan.ins.other.Count; i++)
		{
			float nd = Vector2.Distance(InfluenceMan.ins.other[i].transform.position, wpos);
			if (nd < dist)
			{
				InfluenceMan.ins.other[i].Kill();
			}
		}
	}
	public static Vector2Int PlaceState(int index)
	{
		Vector2Int posI = new Vector2Int(
			Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.x)),
			Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.y)));

		InfluenceMan.ins.NewState(index, posI);
		return posI;
	}

	public static Vector3Int STIN_FromIndex(int index) {
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

	public static Vector2Int PointToCoords(Vector2 point) {
		if (!Map.ins) return Vector2Int.zero;

		Vector2 co = new Vector2(point.x / Map.ins.transform.localScale.x,
	     point.y / Map.ins.transform.localScale.y);
		co.x *= Map.ins.texelDimensions.x;
		co.y *= Map.ins.texelDimensions.y;
		Vector2Int cor = new Vector2Int(Mathf.RoundToInt(co.x), Mathf.RoundToInt(co.y));
		return cor;
	}

	public static Vector2 CoordsToPoint(Vector2Int coords) {
		if (!Map.ins) return Vector2.zero;
		Vector2 point = Vector2.zero;
		point.x = coords.x / (float)Map.ins.texelDimensions.x;
		point.y = coords.y / (float)Map.ins.texelDimensions.y;

		point *= Map.ins.transform.localScale;

		return point;
    }

	public static int PointToTeam(Vector2 point) {
		Vector2Int coords = PointToCoords(point);
		return Map.ins.GetPixTeam(coords);
    }

	public static uint TexelPopToWorldPop(float rout) {
		rout = Mathf.Pow(rout, 0.7f) * 0.3f;
		return (uint)Mathf.RoundToInt(rout);
	}

	public static float Tau(Vector2 p1, Vector2 p2) {
		return Vector2.Distance(p1, p2) / ICBMspeed;
	}
}
