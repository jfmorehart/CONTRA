using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamMovement : MonoBehaviour
{
	public KeyCode zoomIN;
	public KeyCode zoomOut;

	public Vector2 velo;
	public float sizeSpeed;
	public Vector3 accel;

	public float drag;

	public int preDown;
	public int downRes;
	public int iterations;


	public Material mat;

	public void Update()
	{
		Vector3 pos = transform.position;

		velo.x += Input.GetAxisRaw("Horizontal") * accel.x * Time.deltaTime;
		velo.y += Input.GetAxisRaw("Vertical") * accel.y * Time.deltaTime;
		float zax = 0;
		if (Input.GetKey(zoomIN)){
			zax = 1;
		} else if (Input.GetKey(zoomOut)) {
			zax = -1;
		}
		sizeSpeed += zax * accel.z * Time.deltaTime;

		pos += 0.01f * Camera.main.orthographicSize * Time.deltaTime * (Vector3)velo;
		Camera.main.orthographicSize += sizeSpeed * Time.deltaTime;

		if(Camera.main.orthographicSize < 15) {
			Camera.main.orthographicSize = 15;
		}

		if (Camera.main.orthographicSize > 540)
		{
			Camera.main.orthographicSize = 540;
		}

		transform.position = pos;

		sizeSpeed *= 1 - Time.deltaTime * drag;
		velo *= 1 - Time.deltaTime * drag;
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		int width = source.width >> preDown;
		int height = source.height >> preDown;

		if (iterations > 50)
		{
			iterations = 50;
		}
		RenderTexture temp = RenderTexture.GetTemporary(width, height);
		RenderTexture temp2 = RenderTexture.GetTemporary(width, height);

		Graphics.Blit(source, temp);

		for (int i = 0; i < iterations; i++)
		{
			Graphics.Blit(temp, temp2, mat);
			Graphics.Blit(temp2, temp, mat);
		}



		int dwidth = width >> downRes;
		int dheight = height >> downRes;
		RenderTexture temp3 = RenderTexture.GetTemporary(dwidth, dheight);
		Graphics.Blit(temp, temp3);
		Graphics.Blit(temp3, destination);
		RenderTexture.ReleaseTemporary(temp);
		RenderTexture.ReleaseTemporary(temp2);
		RenderTexture.ReleaseTemporary(temp3);

	}
}