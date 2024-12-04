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
		renderer.material = new Material(renderer.material);
		Switch(currentScreen);
		defaultScreen = currentScreen;
	}
	// An example to update the emission color & intensity (and albedo) every frame.
	float clock;

	void Update()
	{
		clock += Time.deltaTime;
		if(clock > 0.5f) {
			RendererExtensions.UpdateGIMaterials(renderer);
		}
	}

	public void Switch(int i) {
		if(i == -2) {
			//renderer.material = new Material(renderer.material);
			renderer.material.SetColor("_EmissionColor", Color.black);
			renderer.material.SetColor("_Color", Color.black);
			//renderer.material.SetTexture("_EmissionMap", null);
			RendererExtensions.UpdateGIMaterials(renderer);
		}
		else if(i == -1) {
			renderer.material.SetColor("_Color", Color.white);
			renderer.material.SetColor("_EmissionColor", Color.white);
			currentScreen = defaultScreen;
			renderer.material.SetTexture("_EmissionMap", DisplayHandler.ins.cameraOutputs[defaultScreen]);
			renderer.material.SetTexture("_MainTex", DisplayHandler.ins.cameraOutputs[defaultScreen]);
			RendererExtensions.UpdateGIMaterials(renderer);
		}
		else {
			renderer.material.SetColor("_Color", Color.white);
			renderer.material.SetColor("_EmissionColor", Color.white);
			currentScreen = i;
			renderer.material.SetTexture("_EmissionMap", DisplayHandler.ins.cameraOutputs[i]);
			renderer.material.SetTexture("_MainTex", DisplayHandler.ins.cameraOutputs[i]);
			RendererExtensions.UpdateGIMaterials(renderer);
		}

	}
}
