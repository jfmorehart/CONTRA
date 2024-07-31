using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Silo : Building
{
	public int maxMissiles = 10;
	public int numMissiles;


	public override void Start()
	{

		base.Start();
		UpdateIconDisplay(numMissiles);
	}
	protected override void Reload()
	{
		base.Reload();
		numMissiles++;
		UpdateIconDisplay(numMissiles);
	}
	protected override bool CanReload()
	{
		return numMissiles < maxMissiles;
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

		UpdateIconDisplay(numMissiles);
	}
}
