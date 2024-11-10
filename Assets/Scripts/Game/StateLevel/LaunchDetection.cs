using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class LaunchDetection
{
    public static Action<Vector2, Vector2, int, int> launchDetectedAction;
	public static Action<int, int, bool> strikeDetectedAction;

	public static void Launched(Vector2 launchPos, Vector2 target)
	{
		int perp = Map.ins.GetPixTeam(MapUtils.PointToCoords(launchPos));
		int victim = Map.ins.GetPixTeam(MapUtils.PointToCoords(target));
		if (perp == -1 || victim == -1) return;
		if(!ROE.AreWeAtWar(perp, victim)) {
			ROE.DeclareWar(perp, victim);
		}
		launchDetectedAction.Invoke(launchPos, target, perp, victim);
	}
	public static void StrikeDetected(int perp, int victim)
	{
		bool provoked = Diplomacy.relationships[victim, perp] == Diplomacy.Relationship.NuclearWar;
		strikeDetectedAction.Invoke(perp, victim, provoked);

		if (Map.multi)
		{
			if (Map.host)
			{
				ulong team1 = MultiplayerVariables.ins.clientIDs[perp];
				ulong team2 = MultiplayerVariables.ins.clientIDs[victim];
				MultiplayerVariables.ins.StrikeDetectClientRPC(team1, team2, NetworkManager.Singleton.LocalClientId);
			}
			else
			{
				ulong team1 = MultiplayerVariables.ins.clientIDs[perp];
				ulong team2 = MultiplayerVariables.ins.clientIDs[victim];
				MultiplayerVariables.ins.StrikeDetectServerRPC(team1, team2, NetworkManager.Singleton.LocalClientId);
			}
		}
	}
}
