using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ATAM : MonoBehaviour
{
    bool flying;

    float boostfuel = 5;
    float lifeTime;
    float speed;
    public float accel, drag;

    public float turnRate;

    Renderer ren;
    TrailRenderer tren;

    Unit bogey;
    float explosionDist = 15;
    int team;

    float swaySeed;
    public float swayFreq, swayAmp;

    float startSpeed;

	private void Awake()
	{
        ren = GetComponent<Renderer>();
        tren = GetComponent<TrailRenderer>();
        Toggle(false);
        swaySeed = Random.Range(-5, 5f);
	}

	public void Launch(Vector2 ipos, Vector2 ivel, Unit target, int mteam, float boostlen = 5) {
        transform.position = ipos;
        transform.up = ivel;
        speed = ivel.magnitude;
        bogey = target;
		(bogey as Plane).SmokeInTheAir(this);
		boostfuel = boostlen;
        team = mteam;
        lifeTime = 5;
        startSpeed = ivel.magnitude;
        Toggle(true);
    }

	private void Update()
	{
        if (!flying) return;
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

	

		if (bogey == null) {
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

        Vector2 delta = bogey.transform.position - transform.position;
		float sway = (Mathf.PerlinNoise1D((Time.time % 100) * swayFreq + swaySeed) - 0.5f) * swayAmp;
		if (delta.magnitude < explosionDist)
		{
            //explode todo
            Map.ins.Detonate(transform.position, 0.5f, team, true);
            Toggle(false);
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

        if (dev > 45) bogey = null; //lose the bogey
	}

	void Toggle(bool on) {
        flying = on;
        ren.enabled = on;

        if (on) {
			tren.enabled = on;
			tren.Clear();
		}

    }
}
