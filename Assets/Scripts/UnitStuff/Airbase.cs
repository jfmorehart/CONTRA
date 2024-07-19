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
			if(team == 0) {
				Debug.Log(dir);
			}

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

		UpdateIconDisplay(numPlanes);
		if (launched.Contains(plane)) {
			launched.Remove(plane);
		}
		StartCoroutine(Recover());
	}

	IEnumerator Recover() {
		yield return new WaitForSeconds(5);
		numPlanes++;
		yield break;
    }
	protected override void Reload()
	{
		numPlanes++;
	}
	protected override bool CanReload()
	{
		CleanLaunched();
		return (launched.Count + numPlanes) < maxPlanes;
	}
	public override void Direct(Order order)
	{
		base.Direct(order);
	}
	void CleanLaunched() {
		List<Plane> clean = new List<Plane>();
		foreach (Plane p in launched) { 
			if(p != null) {
				clean.Add(p);
			}
		}
		launched = clean;
    }
}
