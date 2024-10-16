using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Airbase : Building
{
	public int maxPlanes;
	public int numPlanes;
	public GameObject planePrefab;

	[HideInInspector]
	public Vector2[] patrolPoints;
	float lastLaunch;
	float launchDelay = 1.5f;

	List<Plane> launched;

	public override void Start()
	{
		base.Start();
		launched = new List<Plane>();
		UpdateIconDisplay(numPlanes);
		Vector2Int pos = MapUtils.PointToCoords(transform.position);
		int points = 16;
		patrolPoints = new Vector2[points];
		float angle = 0;
		for(int i = 0; i < points; i++) {
			Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

			(bool hit, Vector2Int hitpos) = MapUtils.TexelRayCast(pos, dir, 200, false);
			if (hit)
			{
				Vector2 pt = MapUtils.CoordsToPoint(hitpos);
				patrolPoints[i] = pt - dir * 60;
			}
			else
			{
				Vector2 end = (pos + dir * 200);
				patrolPoints[i] = MapUtils.CoordsToPoint(new Vector2Int(Mathf.RoundToInt(end.x), Mathf.RoundToInt(end.y)));
			}

			angle += Mathf.Deg2Rad * (360 / (float)points);
		}

		LaunchAircraft();
		ApplyUpgrades();
	}

	public override void ApplyUpgrades()
	{
		if (Research.unlockedUpgrades[team][1] > 0)
		{
			//unlock
			//does nothing
	}
		if (Research.unlockedUpgrades[team][1] > 1)
		{
			//"production i"
			reloadTime = 20;
			maxPlanes = 8;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.1f);
		}
		if (Research.unlockedUpgrades[team][1] > 2)
		{
			// "missiles i"
			//todo change missiles
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.5f);
		}
		if (Research.unlockedUpgrades[team][1] > 3)
		{
			// "range"
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 2f);
		}
		if (Research.unlockedUpgrades[team][1] > 4)
		{
			//"missiles ii"
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 3f);
		}
	}

	public override void Update()
	{
		base.Update();
		if (ROE.AreWeAtWar(team)) {
			if(numPlanes > 0 && Time.time - lastLaunch > launchDelay) {
				LaunchAircraft();
			}
		}
		else if(numPlanes > 0){ 
			if(launched.Count < 1) {
				LaunchAircraft();
			}
		}
	}

	public void LaunchAircraft() {
		if (numPlanes < 1) return;
		lastLaunch = Time.time;
		Vector3 pos = transform.position - 3 * Vector3.forward;
		GameObject p = Instantiate(planePrefab, pos, transform.rotation, Pool.ins.transform);
		Plane pl = p.GetComponent<Plane>();
		pl.homeBase = this;

		launched.Add(pl);
		numPlanes--;
		UpdateIconDisplay(numPlanes);
	}
	public void LandAircraft(Plane plane) {

		if (launched.Contains(plane)) {
			launched.Remove(plane);
		}
		StartCoroutine(Recover());
	}

	IEnumerator Recover() {
		yield return new WaitForSeconds(5);
		if(launched.Count + numPlanes < maxPlanes) {
			numPlanes++;
			UpdateIconDisplay(numPlanes);
		}
    }
	protected override void Reload()
	{
		numPlanes++;
	}
	protected override bool CanReload()
	{
		ApplyUpgrades();
		CleanLaunched();
		return (launched.Count + numPlanes) < maxPlanes;
	}
	public override void Direct(Order order)
	{
		base.Direct(order);
	}

	List<Plane> clean = new List<Plane>();
	void CleanLaunched() {
		clean.Clear();
		foreach (Plane p in launched) { 
			if(p != null) {
				clean.Add(p);
			}
		}
		launched = clean;
    }
}
