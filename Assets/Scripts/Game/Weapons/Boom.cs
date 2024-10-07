using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boom : MonoBehaviour
{
	Renderer ren;
	Vector3 ozo = new Vector3(1, 0.001f, 1);
	bool fading;
	float radius;
	float sf;
	private void Awake()
	{
		ren = GetComponent<Renderer>();
		ren.material = new Material(ren.material);
		Hide();
	}

	public void Nuke(Vector2 pos, float rad) {
		Vector2 point = MapUtils.CoordsToPoint(MapUtils.PointToCoords(pos));
		radius = rad;
		transform.localScale = Mathf.Pow(radius, 0.4f) * 35 * ozo;
		transform.position = point;
		ren.material.color = Color.white;
		Show();

		Invoke(nameof(StartFade), 1.5f);
    }

	void StartFade()
	{
		fading = true;
		sf = Time.time;
    }
	private void Update()
	{
		if (fading) {
			float fadeSpeed = 3 - (Time.time - sf);
			float fsq = fadeSpeed * fadeSpeed;
			Color c = ren.material.color;
			c.a *= 1 - Time.deltaTime * fsq * c.a;
			ren.material.color = c;
			Vector3 ls = transform.localScale;
			ls *= 1 - Time.deltaTime * fsq;
			ls -= fsq * fadeSpeed * Time.deltaTime * 0.0001f * ls ;
			transform.localScale = ls;

			if (fadeSpeed < 0){
				fading = false;
				Hide();
			}
		}
	}

	void Hide() {
		ren.enabled = false;
    }
	void Show()
	{
		ren.enabled = true;
	}
}
