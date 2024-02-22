using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class LockedCam : MonoBehaviour
{
	int downres = 1;
	float timeScale = 3;

	public Material crtmat;
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		crtmat.SetFloat("_t", Time.unscaledTime * timeScale);
		Graphics.Blit(source, destination, crtmat);

	}

}
