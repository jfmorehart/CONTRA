using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TallScreenCam : MonoBehaviour
{
	public static TallScreenCam ins;
	private void Awake()
	{
		ins = this;
	}

	public void End()
	{
		Debug.Log("moving cam");
		GetComponent<Camera>().orthographicSize = 120;
		Vector2 cpos = EndPanel.ins.transform.position;
		transform.position = new Vector3(cpos.x, cpos.y, transform.position.z);
	}
}
