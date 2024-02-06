using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Silo : Unit
{
	public int numMissiles = 20;

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
