using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Silo : Unit
{
	public int numMissiles = 20;

	float gCheck_rate = 2f;
	float gCheck_last;
	public void Update()
	{
		if(Time.time - gCheck_last > gCheck_rate) {
			gCheck_last = Time.time;
			GroundCheck();
		}
	}
	void GroundCheck() {
		if(Map.ins.GetPixTeam(MapUtils.PointToCoords(transform.position)) != team) {
			Kill();
		}
    }
	public override void Direct(Order order)
	{
		base.Direct(order);

		if (numMissiles < 1) return;

		Vector2 pos = order.pos;
		//todo Circular Error Probable
		Vector2 ran = Random.insideUnitCircle; //* Random.Range(0f, 100);
		Pool.ins.GetMissile().Launch(transform.position, pos + ran, 10f, team);
		numMissiles--;
	}
}
