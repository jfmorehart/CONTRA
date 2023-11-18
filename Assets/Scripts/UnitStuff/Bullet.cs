using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
	public float speed;

	bool isFlying;
	Renderer ren;
	TrailRenderer tren;
	Vector2 df;
	float st;
	
	private void Awake()
	{
		ren = GetComponent<Renderer>();
		tren = GetComponent<TrailRenderer>();
		Hide();
	}

	public float Fire(Vector2 pos, Vector2 dir) {

		isFlying = true;
		st = Time.time;
		transform.SetPositionAndRotation(pos, Quaternion.Euler(dir));
		df = speed * Time.deltaTime * dir.normalized;
		Show();
		float hTime = dir.magnitude / speed;
		Invoke(nameof(Hide), hTime);

		return hTime;
    }

	private void Update()
	{
		if (!isFlying) return;
		transform.Translate(df, Space.World);
	}

	void Hide() {
		isFlying = false;
		ren.enabled = false;
		tren.enabled = false;
    }
	void Show() {
		ren.enabled = true;
		tren.Clear();
		tren.enabled = true;
    }
}
