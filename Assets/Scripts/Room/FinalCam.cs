using System.Collections;
using UnityEngine;

public class FinalCam : MonoBehaviour
{
	public RenderTexture tempRt;
	public RenderTexture tempRt2;

	public ComputeShader ExtractBloomLayer;
	public ComputeShader BlurAndAdd;

	public Shader blurShader;
	public int blurIterations;
	public Material blurMat;
	public Material isolateBloomMat;
	private void Start()
	{
		tempRt = new RenderTexture(UnityEngine.Screen.width, UnityEngine.Screen.height, 1);
		tempRt.enableRandomWrite = true;
		tempRt.Create();
		tempRt2 = new RenderTexture(UnityEngine.Screen.width, UnityEngine.Screen.height, 1);
		tempRt2.enableRandomWrite = true;
		tempRt2.Create();
	}
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, tempRt, isolateBloomMat);

		//Compute Shader to get bloomy parts of image
		//ExtractBloomLayer.SetTexture(0, "LayerOut", tempRt);
		//ExtractBloomLayer.Dispatch(0, tempRt.width / 32, tempRt.height / 32, 1);

		//tempRt now contains the information that I'll blur to make the bloom

		////apply the blur (only one iteration for testing)
		//Graphics.Blit(tempRt, tempRt2, blurMat);

		////sending raw blur to screen (for testing)
		//Graphics.Blit(tempRt2, destination);

		//blur process
		//blurMat.shader = blurShader;
		if (blurIterations > 50) blurIterations = 50;
		for (int i = 0; i < blurIterations; i++)
		{
			Graphics.Blit(tempRt, tempRt2, blurMat);
			Graphics.Blit(tempRt2, tempRt, blurMat);
		}



		Graphics.Blit(source, tempRt);
		BlurAndAdd.SetTexture(0, "CamIn", tempRt);
		BlurAndAdd.SetTexture(0, "BLayer", tempRt2);
		BlurAndAdd.Dispatch(0, tempRt.width / 32, tempRt.height / 32, 1);
		Graphics.Blit(tempRt, destination);
	}
}
