using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
	public float speed;

	bool isFlying;
	Renderer ren;
	TrailRenderer tren;
	LineRenderer lren;
	Vector2 df;
	float st;

	private void Awake()
	{
		ren = GetComponent<Renderer>();
		//tren = GetComponent<TrailRenderer>();
		//lren = GetComponent<LineRenderer>();
		Hide();
	}

	public float Fire(Vector2 pos, Vector2 dir, int team) {

		isFlying = true;
		st = Time.time;
		transform.SetPositionAndRotation(pos, Quaternion.Euler(dir));
		df = speed * dir.normalized;
		Show();
		float hTime = dir.magnitude / speed;
		Invoke(nameof(Hide), hTime);

		//Vector3[] posAr = new Vector3[2];
		//posAr[0] = pos;
		//posAr[1] = pos + dir;
		//lren.SetPositions(posAr);
		//lren.startColor = Map.ins.state_colors[team];
		//lren.endColor = Map.ins.state_colors[team];

		SFX.ins.Shoot(transform.position);
		return hTime;
    }

	public void FUpdate()
	{
		if (!isFlying) return;
		transform.Translate(df * Time.deltaTime, Space.World);
	}

	void Hide() {
		isFlying = false;
		ren.enabled = false;
		//tren.enabled = false;
		//lren.enabled = false;
    }
	void Show() {
		ren.enabled = true;
		//tren.Clear();
		//tren.enabled = true;
		//lren.enabled = true;
    }
}
