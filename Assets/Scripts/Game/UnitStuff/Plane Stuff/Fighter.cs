using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Netcode;
using UnityEngine;

public class Fighter : Plane
{
	[HideInInspector]
	public Unit bogey;

	public float trackingRange;
	public float firingRange;
	float maxAngleOfFire = 20;

	protected float lastTargetAcquire;
	protected float targetAcquireCooldown = 0.1f;

	float lastValidityCheck;
	readonly float validityCheckCooldown = 0.5f;

	readonly float patrolDistance = 200;

	float lastShot = -10;
	public float reloadTime = 5;

	float lastRadarCheck;

	int patrolcham = 0;
	int patroldir;

	public bool hasBombs = true;

	State_AI state;

	SFX_OneShot speaker;

	float yield = 0.3f;
	int bombCapacity = 4;

	public override void Start()
	{
		base.Start();
		patrolcham = Random.Range(0, homeBase.patrolPoints.Length);
		patroldir = (Random.value > 0.5f) ? -1 : 1;
		firingRange += Random.Range(-30, 30);
		turnRate += Random.Range(-10, 10);
		speed += Random.Range(-10, 10);
		state = Diplomacy.states[team] as State_AI;
	}
	public override void ApplyUpgrades()
	{
		if (Research.unlockedUpgrades[team][2] > 2)
		{
			// "missiles i"
			reloadTime = 4f;
			firingRange = 430;
			firingRange += Random.Range(-30, 30);

			//bombs
			yield = 1.5f;
			bombCapacity = 4;
		}
		if (Research.unlockedUpgrades[team][2] > 3)
		{
			// "range"
			fuel = 60;
			speed = 90;
			turnRate = 90;
			turnRate += Random.Range(-10, 10);
			speed += Random.Range(-10, 10);
			firingRange = 500;
			firingRange += Random.Range(-30, 30);
			trackingRange = 600;
		}
		if (Research.unlockedUpgrades[team][2] > 4)
		{
			//"missiles ii"
			reloadTime = 3f;

			//bombs
			yield = 3f;
			bombCapacity = 3;
		}
	}
	public override void Update()
	{
		if (Map.multi) {
			if (!IsOwner) return;
		}

		CheckRadar();

		if (bogey != null)
		{
			rateFight = true;
			target = new Mission(bogey.transform.position, AcceptableDistance.Bogey);
			Vector2 delta = bogey.transform.position - transform.position;
			
			if (delta.magnitude < firingRange && Vector2.Angle(transform.up, delta) < maxAngleOfFire)
			{
				if (Time.time - lastShot > reloadTime)
				{
					FireMissile();
				}
			}
		}
		else if (target.distance != AcceptableDistance.None)
		{
			if (target.distance == AcceptableDistance.Bombtarget) {
				ValidTargetCheck();
			}

			rateFight = false;
			if(target.distance == AcceptableDistance.Bombtarget && target.value != -1 && hasBombs && !bingo) {
				if (Time.time - lastTargetAcquire > targetAcquireCooldown)
				{
					SearchForHigherValue();
					lastTargetAcquire = Time.time;
				}
			}
		}

		base.Update();

	}
	public void FireMissile() {
		Vector2 vel = speed * Time.deltaTime * transform.up;
		//ATAM missile = Pool.ins.GetATAM();
		//missile.Launch(transform.position, vel, bogey, team);
		lastShot = Time.time;

		if (Map.multi) { //multiplayer cases
			if (Map.host) {
				GameObject m = Instantiate(Pool.ins.atamPrefab, Pool.ins.transform);
				m.GetComponent<ATAM>().Launch(transform.position, vel, bogey, team);
				NetworkObject no_atam = m.GetComponent<NetworkObject>();
				no_atam.SpawnWithOwnership(0);
				Fox3ClientRPC(no_atam.NetworkObjectId);
			}
			else {
				Fox3ServerRPC(no.NetworkObjectId, bogey.no.NetworkObjectId);
			}
		}
		else {
			//normal singleplayer firing
			ATAM missile = Pool.ins.GetATAM();
			missile.Launch(transform.position, vel, bogey, team);
			lastShot = Time.time;
			bogey = null;
		}

		bogey = null;
	}
	[ServerRpc(RequireOwnership = false)]
	public void Fox3ServerRPC(ulong fighter, ulong mbogey) {
		Debug.Log("recieved fox3 serverrpc");
		if(fighter == no.NetworkObjectId) {
			Debug.Log(" id match");
			if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(mbogey, out NetworkObject no_bogey))
			{
				Unit bogey = no_bogey.GetComponent<Unit>();
				Vector2 vel = speed * Time.deltaTime * transform.up;
				GameObject m = Instantiate(Pool.ins.atamPrefab, Pool.ins.transform);
				m.GetComponent<ATAM>().Launch(transform.position, vel, bogey, team);
				NetworkObject no_atam = m.GetComponent<NetworkObject>();
				no_atam.SpawnWithOwnership(0); //host owner anyhow
				Fox3ClientRPC(no_atam.NetworkObjectId);
				Debug.Log("spawned atam, called clientrpc");
			}
		}
	}
	[ClientRpc]
	public void Fox3ClientRPC(ulong atam)
	{
		Debug.Log("recieved atamlaunch clientrpc");
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(atam, out NetworkObject no_atam))
		{
			Debug.Log("toggled live");
			no_atam.GetComponent<ATAM>().Toggle(true);
		}
	}
	protected override void ArrivedOverTarget() {
		if(target.distance == AcceptableDistance.Bombtarget && hasBombs) {
			StartCoroutine(BombingRun(bombCapacity));
		}
		else {
			base.ArrivedOverTarget();
		}
		if (speaker == null)
		{
			speaker = SFX.ins.PilotChatter(true, this);
		}
	}
	IEnumerator BombingRun(int count) {
		bingo = true;
		hasBombs = false;
		yield return new WaitForSeconds(0.1f * (3 - count));

		if (Map.multi && !IsOwner)
		{
			yield break;
		}

		for (int i = 0; i < count; i++)
		{
			BombManager.ins.Drop(team, transform.position, yield);
			yield return new WaitForSeconds(0.1f + Random.Range(-0.05f, 0.1f));
		}
    }
	protected override void Idle()
	{
		target = Plane.NULLMission;
		if (Time.time - lastTargetAcquire > targetAcquireCooldown)
		{
			AcquireNewTarget();
			if (homeBase == null)
			{
				Airbase[] airbases = ArmyUtils.GetAirbases(team);
				if (airbases.Length > 0)
				{
					homeBase = airbases[Random.Range(0, airbases.Length)];
				}
			}
		}
		else
		{
			if (homeBase != null)
			{
				//PatrolPoint(homeBase.transform.position);
				target = new Mission(homeBase.patrolPoints[patrolcham], AcceptableDistance.Waypoint);
				patrolcham += patroldir;
				if (patrolcham >= homeBase.patrolPoints.Length)
				{
					patrolcham = 0;
				}
				if (patrolcham < 0) patrolcham = homeBase.patrolPoints.Length - 1;
			}
		}
	}
	void SearchForHigherValue() {
		int targetChunk = UnitChunks.ChunkLookup(target.wpos);
		int end = UnitChunks.HighestValueBoxSearch(targetChunk, MapUtils.WorldPosToTeam(target.wpos));
		int value = UnitChunks.targetables[end].Count;
		Vector2 tpos = UnitChunks.ChunkIndexToMapPos(end);
		target = new Mission(tpos, AcceptableDistance.Bombtarget, null, value);
    }

	protected virtual void AcquireNewTarget()
	{
		lastTargetAcquire = Time.time;

		if (true) {
			lastTargetAcquire = Time.time;
			bogey = ArmyUtils.EnemyAircraftInRange(team, transform.position, trackingRange);
			if (Random.value < 0.1 && speaker == null)
			{
				speaker = SFX.ins.PilotChatter(true, this);
			}
		}

		if (bogey != null) return;

		if (hasBombs) {
			if(state == null) state = Diplomacy.states[team] as State_AI;
			target = state.RequestBombingTargets(this);
			if (speaker == null)
			{
				speaker = SFX.ins.PilotChatter(true, this);
			}
		}
		return;
	}
	void CheckRadar() {
		if (Time.time - lastRadarCheck < 1) return;
		lastRadarCheck = Time.time;
		if (bogey != null) return;

		bogey = ArmyUtils.EnemyAircraftInRange(team, transform.position, trackingRange);
	}
	void PatrolPoint(Vector2 patrolPoint) {
		Vector2 radial = patrolPoint - (Vector2)transform.position;
		if(radial.magnitude > patrolDistance + 10) {
			float dev = Vector2.SignedAngle((Vector2)transform.up, radial);
			if (dev > 0)
			{
				transform.Rotate(Vector3.forward, turnRate * Time.deltaTime);
			}
			else
			{
				transform.Rotate(-Vector3.forward, turnRate * Time.deltaTime);
			}
		}else if (radial.magnitude < patrolDistance - 10) {
			float dev = Vector2.SignedAngle((Vector2)transform.up, -radial);
			if (dev > 0)
			{
				transform.Rotate(Vector3.forward, turnRate * Time.deltaTime);
			}
			else
			{
				transform.Rotate(-Vector3.forward, turnRate * Time.deltaTime);
			}
		}
		else {
			Spin(patrolPoint);
		}
	}

	void Spin(Vector2 point) {

		//patrol airbase
		Vector2 radial = point - (Vector2)transform.position;
		float dev = Vector2.Dot((Vector2)transform.up, radial);

		//tells the aircraft to just orbit at the current radius
		if (dev > 0)
		{
			transform.Rotate(Vector3.forward, turnRate * Time.deltaTime);
		}
		else
		{
			transform.Rotate(-Vector3.forward, turnRate * Time.deltaTime);
		}
	}

	void ValidTargetCheck() { 
		if(Time.time - lastValidityCheck < validityCheckCooldown) {
			return;
		}
		lastValidityCheck = Time.time;

		if (target.distance != AcceptableDistance.Bombtarget) return;
		if (!state.BombTargetOK(target.wpos)) {
			ScrapTarget();
			return;
		}
		int tarteam = MapUtils.WorldPosToTeam(target.wpos);
		if(tarteam == -1) {
			ScrapTarget();
			return;
		}
		if (!ROE.AreWeAtWar(team, tarteam) || team == tarteam) {
			ScrapTarget();
			return;
		}
	}
	void ScrapTarget() {
		target = NULLMission;
	}

	public override void SmokeInTheAir(ATAM atam)
	{
		base.SmokeInTheAir(atam);
		speaker = SFX.ins.PilotChatter(false, this);
	}
}


