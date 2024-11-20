using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WideScreenCam : MonoBehaviour
{
	public int normalScale = 55;
	public int pauseScale = 30;
	public Vector2 pausePos;
	public static WideScreenCam ins;
	bool rpause;

	private void Awake()
	{
		ins = this;
	}
   
	public void Refresh()
	{
		if (rpause)
		{
			if (!DisplayHandler.ins.paused)
			{
				Pause(false);
			}
		}
		else
		{
			if (DisplayHandler.ins.paused)
			{
				Pause(true);
			}
		}
	}
	void Pause(bool pause)
	{
		rpause = pause;
		//move the console camera over to the Pause menu to display the widescreen format
		if (pause)
		{
			Debug.Log("moving to paused pos");
			transform.localPosition = new Vector3(pausePos.x, pausePos.y, -10);
			GetComponent<Camera>().orthographicSize = pauseScale;
		}
		else
		{
			Debug.Log("moving to normal pos");
			transform.localPosition = new Vector3(0, 0, -10);
			GetComponent<Camera>().orthographicSize = normalScale;
		}
	}
}
