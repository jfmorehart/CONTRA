using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayHandler : MonoBehaviour
{
	public static DisplayHandler ins;
	public RenderTexture[] cameraOutputs;
	public Screen[] screens;

	private void Awake()
	{
		ins = this;
	}
	private void Start()
	{
		UI.ins.UIScreenToggle(true);
		MoveCam.ins.canMove = false;
	}
	private void Update()
	{
		//if (Input.GetKeyDown(KeyCode.Space)) {
		//	int c = screens[0].currentScreen + 1;
		//	if (c > cameraOutputs.Length - 1) c = 0;
		//	screens[0].Switch(c);
		//	if(c == 0) {
		//		MoveCam.ins.canMove = true;
		//	}
		//	else {
		//		MoveCam.ins.canMove = false;
		//	}
		//	if(c == 3) {
		//		UI.ins.UIScreenToggle(true);
		//	}
		//	else {
		//		UI.ins.UIScreenToggle(false);
		//	}
		//}
	}
}
