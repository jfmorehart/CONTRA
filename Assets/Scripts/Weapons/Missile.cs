using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour
{
	TrailRenderer tren;
	Renderer ren;
	bool flying;
	Vector2 st;
	Vector2 en;
	float yield;
	float speed = 50;
	float hmult = 1.5f;

	public int team;

	private void Awake()
	{
		tren = GetComponent<TrailRenderer>();
		ren = GetComponent<Renderer>();
		Toggle(false);
	}
	public void Launch(Vector2 start, Vector2 end, float myield, int mteam) {
		//LaunchDetection.Launched(start, end);
		transform.position = start;
		st = start;
		en = end;
		Toggle(true);
		flying = true;
		yield = myield;
		team = mteam;
	}
	private void Update()
	{
		if (flying) {
			Vector2 delta = en - st;
			float per = PercentOfPath();
			Vector2 adj = delta.normalized + (0.5f - per) * hmult * Vector2.up;
			transform.LookAt((Vector2)transform.position + adj);
			transform.Translate(speed * Time.deltaTime * adj, Space.World);

			if (per > 1) {
				flying = false;
				Toggle(false);
				Map.ins.Detonate(en, yield, team);
			}
		}
	}
	void Toggle(bool swi) {
		ren.enabled = swi;
		tren.enabled = swi;
		if (swi) {
			tren.Clear();
		}
	
    }

	float PercentOfPath() {
		Vector2 delta = en - st;
		if (delta.x == 0) return 1;
		float per = transform.position.x - st.x;
		per /= delta.x;
		return per;
    }
}
