using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screen : MonoBehaviour
{
	// Mesh Renderer.
	public Renderer renderer;
	public int currentScreen;
	int defaultScreen;
	public bool wideFormat;

	private void Start()
	{
		Switch(currentScreen);
		defaultScreen = currentScreen;
	}
	// An example to update the emission color & intensity (and albedo) every frame.
	void Update()
	{
		RendererExtensions.UpdateGIMaterials(renderer);
	}

	public void Switch(int i) {
		if(i == -1) {
			currentScreen = defaultScreen;
			renderer.material.SetTexture("_EmissionMap", DisplayHandler.ins.cameraOutputs[defaultScreen]);
			renderer.material.SetTexture("_MainTex", DisplayHandler.ins.cameraOutputs[defaultScreen]);
		}
		else {
			currentScreen = i;
			renderer.material.SetTexture("_EmissionMap", DisplayHandler.ins.cameraOutputs[i]);
			renderer.material.SetTexture("_MainTex", DisplayHandler.ins.cameraOutputs[i]);
		}

	}
}
