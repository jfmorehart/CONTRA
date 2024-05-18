using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppearEffect : MonoBehaviour
{
    bool live;
    float length = 1;
    float startTime;
    public float speed, fade;
    SpriteRenderer ren;


	private void Awake()
	{
        ren = GetComponent<SpriteRenderer>();
        ren.enabled = false;
        ren.material = new Material(ren.material);
	}

    public void Spawn(Vector3 pos) {
        transform.position = pos - Vector3.forward;
        transform.localScale = Vector3.one;// * 0.1f;
        ren.material.color = Color.white;
        ren.enabled = true;
        live = true;
        startTime = Time.time;
    }

	void Update()
    {
        if (!live) return;

        if (Time.time - startTime > length) {
            live = false;
            ren.enabled = false;
			return;
        }

        float inv = 1 - (Time.time - startTime) / length;
        transform.localScale += speed * Time.deltaTime * Vector3.one * inv;// * transform.localScale.x * Vector3.one;
		Color c = ren.material.color;
		c.a *= 1 - Time.deltaTime * fade * inv;
		ren.material.color = c;
	}
}
