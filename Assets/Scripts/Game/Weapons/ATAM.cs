using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ATAM : NetworkBehaviour
{
    bool flying;

    float boostfuel = 4;
    float lifeTime;
    float speed;
    public float accel, drag;

    public float turnRate;

    Renderer ren;
    TrailRenderer tren;

    Unit bogey;
	Missile fireball;
	bool ABMmode;

    float explosionDist = 15;
    int team;

    float swaySeed;
    public float swayFreq, swayAmp;

    float startSpeed;

	AudioSource src;

	bool airLaunched;

	NetworkObject no;

	private void Awake()
	{
        ren = GetComponent<Renderer>();
        tren = GetComponent<TrailRenderer>();
		if (Map.multi) {
			Toggle(true);
		}
		else {
			Toggle(false);
		}

        swaySeed = Random.Range(-5, 5f);
		no = GetComponent<NetworkObject>();
	}

	[ServerRpc(RequireOwnership = false)]
	public void ATAMLiveServerRPC(ulong obj_id) { 
		if(no.NetworkObjectId == obj_id) {
			src = SFX.ins.ATAMLaunch(transform).GetComponent<AudioSource>();
			Toggle(true);
			ATAMLiveClientRPC(obj_id);
		}
    }
	[ClientRpc]
	public void ATAMLiveClientRPC(ulong obj_id)
	{
		if (no.NetworkObjectId == obj_id)
		{
			src = SFX.ins.ATAMLaunch(transform).GetComponent<AudioSource>();
			Toggle(true);
		}
	}
	public void Launch(Vector2 ipos, Vector2 ivel, Unit target, int mteam, float boostlen = 5, bool airLaunch = true) {
		Debug.Log("atam launch");
		//ANTI AIRCRAFT MODE
        transform.position = ipos;
        transform.up = ivel;
        speed = ivel.magnitude;
        bogey = target;
		(bogey as Plane).SmokeInTheAir(this);
		boostfuel = boostlen;
		airLaunched = airLaunch;
        team = mteam;
        lifeTime = 5;
        startSpeed = ivel.magnitude;
        Toggle(true);
		src = SFX.ins.ATAMLaunch(transform).GetComponent<AudioSource>();
		ApplyUpgrades();
		//if (Map.multi)
		//{
		//	if (Map.host)
		//	{
		//		ATAMLiveClientRPC(no.NetworkObjectId);
		//	}
		//	else
		//	{
		//		ATAMLiveServerRPC(no.NetworkObjectId);
		//	}
		//}
	}
	public void Launch(Vector2 ipos, Vector2 ivel, Missile target, int mteam, float boostlen = 5, bool airLaunch = true)
	{
		//ANTI BALLISTIC MISSILE MODE
		ABMmode = true;
		transform.position = ipos;
		transform.up = ivel;
		speed = ivel.magnitude;
		fireball = target;

		boostfuel = boostlen;
		airLaunched = airLaunch;

		team = mteam;
		lifeTime = 5;
		startSpeed = ivel.magnitude;
		Toggle(true);
		src = SFX.ins.ATAMLaunch(transform).GetComponent<AudioSource>();
		ApplyUpgrades();
		//if (Map.multi)
		//{
		//	if (Map.host)
		//	{
		//		ATAMLiveClientRPC(no.NetworkObjectId);
		//	}
		//	else
		//	{
		//		ATAMLiveServerRPC(no.NetworkObjectId);
		//	}
		//}
	}
	void ApplyUpgrades()
	{
		if (airLaunched)
		{
			if (Research.unlockedUpgrades[team][2] > 2)
			{
				// "missiles i"
				//todo change missiles
				boostfuel = 4.5f;
				turnRate *= 1.5f;
			}
			if (Research.unlockedUpgrades[team][2] > 4)
			{
				//"missiles ii"
				boostfuel = 5;
				turnRate *= 1.5f;
			}
		}
		else {
			if (Research.unlockedUpgrades[team][1] > 2)
			{
				//"missile tech"
				boostfuel = 5f;
				turnRate *= 2;
			}
		}
	}
	private void Update()
	{
        if (!flying) return;
		if (Map.multi && !NetworkManager.Singleton.IsHost) { 
			if(!no.SynchronizeTransform) Toggle(false);
			return;
		}
        lifeTime -= Time.deltaTime;
        if (lifeTime < 0) Toggle(false);

        if(boostfuel > 1) {
            speed += accel * Time.deltaTime * boostfuel;
            boostfuel -= Time.deltaTime * 0.8f;
        }
        else {
			speed += accel * Time.deltaTime;
		}
        speed *= 1 - Time.deltaTime * drag;
		transform.Translate(speed * Time.deltaTime * transform.up, Space.World);

        if(speed < startSpeed) {
            //kill if slow
			Toggle(false);
		}

	

		if (fireball == null && bogey == null) {
			float dumbsway = (Mathf.PerlinNoise1D(Time.time * 0.1f * swayFreq + swaySeed) - 0.5f) * swayAmp * 0.1f;
			//gone dumb behavior
			if (dumbsway > 2)
			{
				transform.Rotate(Vector3.forward, turnRate * Time.deltaTime);
				speed -= turnRate * Time.deltaTime * 0.1f;
			}
			else if (dumbsway < -2)
			{
				transform.Rotate(-Vector3.forward, turnRate * Time.deltaTime);
				speed -= turnRate * Time.deltaTime * 0.1f;
			}
			return;
	    }

		Vector2 delta;
		float sway;

		if (ABMmode) {
			delta = fireball.transform.position - transform.position;
			sway = (Mathf.PerlinNoise1D((Time.time % 100) * swayFreq + swaySeed) - 0.5f) * swayAmp;
			if (delta.magnitude < explosionDist)
			{
				//kill both missiles
				Map.ins.Detonate(transform.position, 0.5f, team, true);
				if (Map.multi)
				{

					MultiplayerVariables.ins.ATAMDetonateClientRPC(transform.position, team);
					ulong networkteam = MultiplayerVariables.ins.clientIDs[team];
					MultiplayerVariables.ins.FireballDownClientRPC(networkteam, fireball.en);
				}
				fireball.Toggle(false);
				Toggle(false);
			}
		}
		else {
			delta = bogey.transform.position - transform.position;
			sway = (Mathf.PerlinNoise1D((Time.time % 100) * swayFreq + swaySeed) - 0.5f) * swayAmp;
			if (delta.magnitude < explosionDist)
			{
				Map.ins.Detonate(transform.position, 0.5f, team, true);
				if (Map.multi) MultiplayerVariables.ins.ATAMDetonateClientRPC(transform.position, team);
				Toggle(false);

			}
		}


		float dev = Vector2.SignedAngle((Vector2)transform.up, delta);
		if (dev + sway > 2)
		{
			transform.Rotate(Vector3.forward, turnRate * Time.deltaTime);
            speed -= turnRate * Time.deltaTime * 0.1f;
		}
		else if (dev + sway < -2)
		{
			transform.Rotate(-Vector3.forward, turnRate * Time.deltaTime);
			speed -= turnRate * Time.deltaTime * 0.1f;
		}

		if (dev > 45) {
			bogey = null; //lose the bogey
			fireball = null; //lose the bogey
		}
	}

	public void Toggle(bool on) {
        flying = on;
        ren.enabled = on;
		if (Map.multi) {
			if (no == null) no = GetComponent<NetworkObject>();
			no.SynchronizeTransform = on;
		}

		if(src != null) {
			Destroy(src.gameObject);
		}
        if (on) {
			tren.enabled = on;
			tren.Clear();
		}
    }
}
