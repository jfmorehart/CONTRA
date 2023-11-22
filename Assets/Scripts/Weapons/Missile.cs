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
	float hmult = 3;

	private void Awake()
	{
		tren = GetComponent<TrailRenderer>();
		ren = GetComponent<Renderer>();
	}
	public void Launch(Vector2 start, Vector2 end, float myield) {
		transform.position = start;
		st = start;
		en = end;
		Toggle(true);
		flying = true;
		yield = myield;
	}
	private void Update()
	{
		if (flying) {
			Vector2 delta = en - st;
			float per = PercentOfPath();
			Debug.Log(per);

			Vector2 adj = delta.normalized + (0.5f - per) * hmult * Vector2.up;
			transform.Translate(speed * Time.deltaTime * adj);

			if (per > 1) {
				flying = false;
				Toggle(false);
				Map.ins.Detonate(en, yield);
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
