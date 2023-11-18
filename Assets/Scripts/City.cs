using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
	public int team;

	[HideInInspector]
    public float pop; //todo some sort of popcount

	public float sample_radius;
	int numSamples = 5;

	public void SetUpCity(int te, float popu)
	{
		team = te;
		pop = popu;
		InvokeRepeating(nameof(SurroundCheck), Random.Range(0, 1f), 2);
	}

	public void SurroundCheck() {

		//Only check in wartime
		if (!ROE.AreWeAtWar(team)) return;
		
        int[] sample_teams = new int[Map.ins.numStates];
		Vector2[] sample_pos = ArmyUtils.Encircle(transform.position, sample_radius, numSamples);

        for(int i = 0; i < numSamples; i++) {
			sample_teams[MapUtils.PointToTeam(sample_pos[i])] += 1;
		}

		int mteam = 0;
		int mamt = -1;

		for(int i = 0; i < Map.ins.numStates; i++) {
			if (sample_teams[i] > mamt) {
				mamt = sample_teams[i];
				mteam = i;
			}
		}
		
		//Only convert to team we are at war with
		if(ROE.AreWeAtWar(team, mteam)){
			if(team != mteam) {
				Debug.Log("captured");
			}

			team = mteam;
		}
    }
}
