using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Inf
{
    public Vector2Int pos;
    public float pop;
    public int team;
    public int isArmy;

	public Inf(Vector2Int mpos, float popu, int mteam, int isarm) {
        pos = mpos;
        pop = popu;
        team = mteam;
        isArmy = isarm;
    }
}
