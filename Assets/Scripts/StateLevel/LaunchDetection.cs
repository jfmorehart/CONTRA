using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LaunchDetection
{
    public static Action<Vector2, Vector2, int, int, bool> launchDetectedAction;

	public static void Launched(Vector2 launchPos, Vector2 target)
	{
		int perp = Map.ins.GetPixTeam(MapUtils.PointToCoords(launchPos));
		int victim = Map.ins.GetPixTeam(MapUtils.PointToCoords(target));
		bool provoked = Diplomacy.relationships[victim, perp] == Diplomacy.Relationship.NuclearWar;
		launchDetectedAction.Invoke(launchPos, target, perp, victim, provoked);
	}
	public static void Launched(int perp, int victim)
	{
		Debug.Log("whoop de doo 2");

		bool provoked = Diplomacy.relationships[victim, perp] == Diplomacy.Relationship.NuclearWar;
		launchDetectedAction.Invoke(Vector2.zero, Vector2.zero, perp, victim, provoked);
	}
}
