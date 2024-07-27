using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using static UnityEngine.UI.GridLayoutGroup;

public class AAA : Building
{
	public int maxMissiles = 5;

	public int numMissiles;

	public float rechamberSpeed;
	public float radarCheckDelay;
	float lastRadarCheck;
	float lastShotTime;

	public float fireRange;

	List<Unit> firedAt;

	public override void Start()
	{
		numMissiles = maxMissiles;
		base.Start();
		UpdateIconDisplay(numMissiles);
		firedAt = new List<Unit>();
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

	public override void Update()
	{
		base.Update();

		if (numMissiles < 1) return;

		if (Time.time - lastRadarCheck > radarCheckDelay)
		{
			RadarCheck();
		}

	}

	Unit RadarCheck() {
		lastRadarCheck = Time.time;
		Unit bogey = ArmyUtils.EnemyAircraftInRange(team, transform.position, fireRange, firedAt);
		if (bogey != null && Time.time - lastShotTime > rechamberSpeed)
		{
			Launch(bogey);
		}
		return bogey;
	}

	void Launch(Unit bogey)
	{
		lastShotTime = Time.time;
		Vector2 ivel = Vector2.zero;
		ivel = (bogey.transform.position - transform.position).normalized * 40f;
		ATAM mis = Pool.ins.GetATAM();
		mis.Launch(transform.position, ivel, bogey, team, 4.2f);
		numMissiles--;

		StartCoroutine(ScrubBogey(bogey));
		UpdateIconDisplay(numMissiles);
		SFX.ins.MissileLaunch(mis.transform, 0.3f);
	}

	IEnumerator ScrubBogey(Unit bogey) {
		firedAt.Add(bogey);
		yield return new WaitForSeconds(3);
		if (bogey == null) yield break;
		firedAt.Remove(bogey);
	}
}
