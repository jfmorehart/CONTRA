using System.Collections;
using UnityEngine;

public class FinalCam : MonoBehaviour
{
	public static FinalCam ins;

	public RenderTexture tempRt;
	public RenderTexture tempRt2;

	public ComputeShader ExtractBloomLayer;
	public ComputeShader BlurAndAdd;

	public int blurIterations;
	public Material blurMat;
	public Material isolateBloomMat;

	public float shakestr, shakeAmp, shakeFreq, shakeDecay, launchShakeMagnitude;
	public Vector2 shake;
	private void Start()
	{
		ins = this;
		tempRt = new RenderTexture(UnityEngine.Screen.width, UnityEngine.Screen.height, 1);
		tempRt.enableRandomWrite = true;
		tempRt.Create();
		tempRt2 = new RenderTexture(UnityEngine.Screen.width, UnityEngine.Screen.height, 1);
		tempRt2.enableRandomWrite = true;
		tempRt2.Create();
	}
	private void Update()
	{
		Vector3 pos = transform.position - (Vector3)shake;
		shake = shakestr * new Vector3(shakeAmp * (Mathf.PerlinNoise1D(Time.time * shakeFreq) - 0.5f), shakeAmp * (Mathf.PerlinNoise1D(Time.time * shakeFreq + 1) - 0.5f));
		transform.position = pos + (Vector3)shake;
		shakestr -= Time.deltaTime * shakeDecay;
		if (shakestr < 0.05f) shakestr = 0;
	}
	public void MissileLaunch()
	{
		StartCoroutine(nameof(LaunchShake));
	}
	IEnumerator LaunchShake()
	{
		float maxcountdown = 3;
		float countdown = 3;
		while (countdown > 0)
		{
			float d = maxcountdown / (countdown + 0.01f);
			d = Mathf.Pow(d, 0.2f);
			shakestr += Time.deltaTime * d * launchShakeMagnitude;
			countdown -= Time.deltaTime;
			yield return null;
		}
	}
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		//Graphics.Blit(source, destination);
		//return;
		if (Time.timeSinceLevelLoad < 0.15f) return;
		if(blurIterations < 1) {
			Graphics.Blit(source, destination);
			return;
		}
		//Graphics.Blit(source, destination, isolateBloomMat);
		Graphics.Blit(source, tempRt, isolateBloomMat);
		Graphics.Blit(source, tempRt2);
		if (blurIterations > 20) blurIterations = 20;
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
