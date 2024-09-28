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
	List<Missile> mfiredAt;

	bool ABMCapable;

	public override void Start()
	{
		base.Start();
		UpdateIconDisplay(numMissiles);
		firedAt = new List<Unit>();
		mfiredAt = new List<Missile>();
		ApplyUpgrades();
	}
	protected override void Reload()
	{
		base.Reload();
		numMissiles++;
		UpdateIconDisplay(numMissiles);
	}
	public override void ApplyUpgrades() {
		if (Research.unlockedUpgrades[team][1] > 0)
		{
			//unlock
			//does nothing
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.1f);
		}
		if (Research.unlockedUpgrades[team][1] > 1)
		{
			//"production i"
			reloadTime = 7;
			maxMissiles = 4;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.3f);
		}
		if (Research.unlockedUpgrades[team][1] > 2)
		{
			//"missile tech"
			//todo change missiles
			rechamberSpeed = 0.25f;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.5f);
		}
		if (Research.unlockedUpgrades[team][1] > 3)
		{
			//production ii
			maxMissiles = 5;
			reloadTime = 5;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 2f);
		}
		if (Research.unlockedUpgrades[team][1] > 4)
		{
			//"abm capable"
			//todo ABM systems
			ABMCapable = true;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 4f);
		}
	}
	protected override bool CanReload()
	{
		ApplyUpgrades();
		return numMissiles < maxMissiles;
	}

	public override void Update()
	{
		base.Update();

		if (numMissiles < 1) return;

		if (Time.time - lastRadarCheck > radarCheckDelay)
		{
			RadarCheck();
			if (ABMCapable) {
				ABMCheck();
			}

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

	Missile ABMCheck() { 
		if(Time.time - lastShotTime > rechamberSpeed) {
			foreach (Missile m in TerminalMissileRegistry.registry[team])
			{
				if (Vector2.Distance(m.transform.position, transform.position) < fireRange)
				{
					if (m.PercentOfPath() > 0.9f) continue; //we dont have time to intercept
					if (mfiredAt.Contains(m)) continue;
					Launch(m);
					return m;
				}
			}
		}

		return null;
    }

	void Launch(Unit bogey)
	{
		// ANTI AIRCRAFT MODE

		lastShotTime = Time.time;
		Vector2 ivel = Vector2.zero;
		ivel = (bogey.transform.position - transform.position).normalized * 40f;
		ATAM mis = Pool.ins.GetATAM();
		mis.Launch(transform.position, ivel, bogey, team, 4.2f);
		numMissiles--;

		StartCoroutine(ScrubBogey(bogey));
		UpdateIconDisplay(numMissiles);
		//SFX.ins.MissileLaunch(mis.transform, 0.3f);
	}

	void Launch(Missile fireball)
	{
		// ANTI BALLISTIC MISSLE MODE

		lastShotTime = Time.time;
		Vector2 ivel = Vector2.zero;
		ivel = (fireball.transform.position - transform.position).normalized * 40f;
		ATAM mis = Pool.ins.GetATAM();
		mis.Launch(transform.position, ivel, fireball, team, 4.2f);
		numMissiles--;

		StartCoroutine(ScrubFireball(fireball));
		UpdateIconDisplay(numMissiles);
		//SFX.ins.MissileLaunch(mis.transform, 0.3f);
	}

	IEnumerator ScrubBogey(Unit bogey) {
		firedAt.Add(bogey);
		yield return new WaitForSeconds(3);
		if (bogey == null) yield break;
		firedAt.Remove(bogey);
	}
	IEnumerator ScrubFireball(Missile fireball)
	{
		mfiredAt.Add(fireball);
		yield return new WaitForSeconds(3);
		if (fireball == null) yield break;
		mfiredAt.Remove(fireball);
	}
}
