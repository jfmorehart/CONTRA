using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LaunchDetection
{
    public static Action<Vector2, Vector2, int, int> launchDetectedAction;
	public static Action<int, int, bool> strikeDetectedAction;

	public static void Launched(Vector2 launchPos, Vector2 target)
	{
		int perp = Map.ins.GetPixTeam(MapUtils.PointToCoords(launchPos));
		int victim = Map.ins.GetPixTeam(MapUtils.PointToCoords(target));
		launchDetectedAction.Invoke(launchPos, target, perp, victim);
	}
	public static void StrikeDetected(int perp, int victim)
	{
		bool provoked = Diplomacy.relationships[victim, perp] == Diplomacy.Relationship.NuclearWar;
		strikeDetectedAction.Invoke(perp, victim, provoked);
	}
}
