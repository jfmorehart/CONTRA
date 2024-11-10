using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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
		if (Map.multi) {
			if (!NetworkManager.Singleton.IsHost) return;
		}
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

	[ServerRpc(RequireOwnership = false)]
	public void LaunchAAAServerRPC(ulong bogey_id)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bogey_id, out NetworkObject no_bogey))
		{
			lastShotTime = Time.time;
			Vector2 ivel;
			Unit bogey = no_bogey.GetComponent<Unit>();
			ivel = (bogey.transform.position - transform.position).normalized * 40f;
			GameObject m = Instantiate(Pool.ins.atamPrefab, Pool.ins.transform);
			ATAM mis = m.GetComponent<ATAM>();
			mis.Launch(transform.position, ivel, bogey, team, 4.2f);
			numMissiles--;

			StartCoroutine(ScrubBogey(bogey));
			UpdateIconDisplay(numMissiles);
			//SFX.ins.MissileLaunch(mis.transform, 0.3f);

			NetworkObject no_atam = mis.GetComponent<NetworkObject>();
			no_atam.SpawnWithOwnership(0);
			InformLaunchClientRPC(no_atam.NetworkObjectId, no_bogey.NetworkObjectId);
			//Fox3ClientRPC(no_atam.NetworkObjectId);
		}
	}
	[ClientRpc]
	public void InformLaunchClientRPC(ulong atam, ulong bogey = 999) {
		Debug.Log("recieved AAA clientrpc");
		if(!no.IsOwner)numMissiles--;
		UpdateIconDisplay(numMissiles);
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(atam, out NetworkObject no_atam))
		{
			Debug.Log("toggled aaa live");
			no_atam.GetComponent<ATAM>().Toggle(true);
			SFX.ins.ATAMLaunch(no_atam.transform).GetComponent<AudioSource>();
		}
		if (bogey == 999) {
			//fireball
 
			return;
		}
		else { 
			
		}
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bogey, out NetworkObject no_bogey))
		{
			no_bogey.GetComponent<Plane>().SmokeInTheAir(no_atam.GetComponent<ATAM>());
		}
	}

	void Launch(Unit bogey)
	{
		if (Map.multi) {
			if (Map.host) {
				//
			}
			else {
				numMissiles--;
				LaunchAAAServerRPC(bogey.no.NetworkObjectId);
				StartCoroutine(ScrubBogey(bogey));
				UpdateIconDisplay(numMissiles);
				return;
			}
		}
		// ANTI AIRCRAFT MODE

		lastShotTime = Time.time;
		Vector2 ivel = Vector2.zero;
		ivel = (bogey.transform.position - transform.position).normalized * 40f;
		GameObject m = Instantiate(Pool.ins.atamPrefab, Pool.ins.transform);
		ATAM mis = m.GetComponent<ATAM>();
		mis.Launch(transform.position, ivel, bogey, team, 4.2f);
		numMissiles--;

		StartCoroutine(ScrubBogey(bogey));
		UpdateIconDisplay(numMissiles);
		//SFX.ins.MissileLaunch(mis.transform, 0.3f);
		if (Map.multi) {
			NetworkObject no_atam = mis.GetComponent<NetworkObject>();
			no_atam.SpawnWithOwnership(0);
			InformLaunchClientRPC(no_atam.NetworkObjectId, bogey.no.NetworkObjectId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void LaunchABMServerRPC(Vector2 dest)
	{
		Debug.Log("ABM rpc recieved, checking validity...");
		Missile fireball = null;
		foreach (Missile m in TerminalMissileRegistry.registry[team]) {
			if (Vector3.Distance(m.en, dest) < 10) {
				fireball = m;
				break;
			}
		}
		if (fireball == null) {
			Debug.LogError("no fireball found");
			return;
		}
		Debug.Log("valid ABM rpc recieved, firing");
		lastShotTime = Time.time;
		Vector2 ivel = Vector2.zero;
		ivel = (fireball.transform.position - transform.position).normalized * 40f;
		GameObject mob = Instantiate(Pool.ins.atamPrefab, Pool.ins.transform);
		ATAM mis = mob.GetComponent<ATAM>();
		mis.Launch(transform.position, ivel, fireball, team, 4.2f);
		numMissiles--;

		StartCoroutine(ScrubFireball(fireball));
		UpdateIconDisplay(numMissiles);
		//SFX.ins.MissileLaunch(mis.transform, 0.3f);

		NetworkObject no_atam = mis.GetComponent<NetworkObject>();
		no_atam.SpawnWithOwnership(0);
		InformLaunchClientRPC(no_atam.NetworkObjectId);
		//Fox3ClientRPC(no_atam.NetworkObjectId);
	}
	void Launch(Missile fireball)
	{
		if (Map.multi)
		{
			if (!Map.host)
			{
				Debug.Log("client sending abm rpc");
				LaunchABMServerRPC(fireball.en);
				StartCoroutine(ScrubFireball(fireball));
				numMissiles--;
				UpdateIconDisplay(numMissiles);
				return;
			}
		}
		// ANTI BALLISTIC MISSLE MODE

		lastShotTime = Time.time;
		Vector2 ivel = Vector2.zero;
		ivel = (fireball.transform.position - transform.position).normalized * 40f;

		ATAM mis = null;
		if (Map.multi) {
			GameObject mob = Instantiate(Pool.ins.atamPrefab, Pool.ins.transform);
			mis = mob.GetComponent<ATAM>();
		}
		else {
			mis = Pool.ins.GetATAM();
		}

		mis.Launch(transform.position, ivel, fireball, team, 4.2f);
		numMissiles--;

		StartCoroutine(ScrubFireball(fireball));
		UpdateIconDisplay(numMissiles);
		//SFX.ins.MissileLaunch(mis.transform, 0.3f);

		if (Map.multi) {
			NetworkObject no_abm = mis.GetComponent<NetworkObject>();
			no_abm.SpawnWithOwnership(0);
			InformLaunchClientRPC(no_abm.NetworkObjectId);
		}
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
