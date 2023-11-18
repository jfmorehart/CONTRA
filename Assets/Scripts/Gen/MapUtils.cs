using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapUtils
{
	public static Inf PlaceCity(int index) {
		Vector2Int posI = new Vector2Int(
			Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.x)),
			Mathf.RoundToInt(Random.Range(1, Map.ins.texelDimensions.y)));

		int mteam = 0;
		float cdist = 9000;

		for (int i = 0; i < Map.ins.numStates; i++)
		{
			float dist = Vector2.Distance(Map.ins.stateCenters[i], posI);
			if (dist < cdist)
			{
				cdist = dist;
				mteam = i;
			}
		}
		float popU = Random.Range(1, 10); //todo replace with popcount
		return new Inf(posI, popU, mteam, 0);
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
		Vector2 co = new Vector2(point.x / Map.ins.transform.localScale.x,
	     point.y / Map.ins.transform.localScale.y);
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

	public static int PointToTeam(Vector2 point) {
		Vector2Int coords = PointToCoords(point);
		return Map.ins.GetPixTeam(coords);
    }
}
