using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerInput : MonoBehaviour
{
	//Should handle more input centrally

	public static PlayerInput ins;
	public LayerMask regularMask;
	public LayerMask buildMask;

	public bool buildMode;

	private void Awake()
	{
		ins = this;
	}

	// Update is called once per frame
	void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) {
			buildMode = !buildMode;
            Camera.main.cullingMask = buildMode? buildMask : regularMask;
			Map.ins.ConvertToTexture();
	    }
	}
}
