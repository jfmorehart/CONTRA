using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WideScreenCam : MonoBehaviour
{
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
		Debug.Log("moving cam");
		rpause = pause;
		//move the console camera over to the Pause menu to display the widescreen format
		if (pause)
		{
			Vector2 cpos = PausePanel.ins.transform.position;
			transform.position = new Vector3(cpos.x, cpos.y, transform.position.z);
		}
		else
		{
			transform.localPosition = new Vector3(0, 0, transform.localPosition.z);
		}
	}
}
