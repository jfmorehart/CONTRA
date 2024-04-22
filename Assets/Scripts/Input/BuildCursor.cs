using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildCursor : MonoBehaviour
{
    Vector2 velo;
    Vector3 pos;
    public float springFactor = 1;
    public float springLength = 1;
    public float drag = 2;
    float springForce;
	// Update is called once per frame

	private void Awake()
	{
        pos = transform.position;
	}
	void Update()
    {
		springForce = springFactor / springLength;
		Vector3 delta = transform.position - Camera.main.transform.position;
		if (delta.magnitude >= springLength)
		{
			pos = Camera.main.transform.position + (springLength * 1) * delta.normalized;// Vector3.ClampMagnitude(delta, delta.magnitude - springLength);
		}
		velo += -springForce * Time.deltaTime * (Vector2)delta;
		velo *= 1 - Time.deltaTime * drag;
		pos += new Vector3(velo.x, velo.y, 0);
        //delta = transform.position - Camera.main.transform.position;

		transform.position = pos;
    }
}
