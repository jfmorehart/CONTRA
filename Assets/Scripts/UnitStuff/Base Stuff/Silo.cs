using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Silo : Building
{
	public int maxMissiles = 3;
	public int numMissiles;

	public float yield;

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
		Pool.ins.GetMissile().Launch(transform.position, pos + ran, yield, team);
		numMissiles--;

		UpdateIconDisplay(numMissiles);
	}
	public override void ApplyUpgrades()
	{

		if (Research.unlockedUpgrades[team][3] > 1)
		{
			//"warhead i", 
			yield = 8;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.3f);
		}
		if (Research.unlockedUpgrades[team][3] > 2)
		{
			//"production",
			maxMissiles = 5;
			reloadTime = 10;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.5f);

		}
		if (Research.unlockedUpgrades[team][3] > 3)
		{
			// "warhead ii", 
			yield = 15;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 2f);
		}
		if (Research.unlockedUpgrades[team][3] > 4)
		{
			//"mirv"
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 3f);
		}
	}
}
