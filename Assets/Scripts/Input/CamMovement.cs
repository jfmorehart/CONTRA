using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCam : MonoBehaviour
{
	public static MoveCam ins;
	public KeyCode zoomIN;
	public KeyCode zoomOut;

	public Vector2 velo;
	public float sizeSpeed;
	public Vector3 accel;

	public float drag;

	public int preDown;
	public int downRes;
	public int iterations;


	public Material blurmat;
	public Material crtmat;

	public float timeScale;

	public bool canMove = true;

	private void Awake()
	{
		ins = this;
	}
	public void Update()
	{
		if (!canMove) return;
		Vector3 pos = transform.position;

		int horiz = 0;
		if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D)) {
			horiz = -1;
		}
		if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))
		{
			horiz = 1;
		}
		int verti = 0;
		if (Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))
		{
			verti = -1;
		}
		if (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
		{
			verti = 1;
		}
		velo.x += horiz * accel.x * Time.deltaTime;
		velo.y += verti * accel.y * Time.deltaTime;
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

		if (Camera.main.orthographicSize > 600)
		{
			Camera.main.orthographicSize = 600;
		}
		float xM = Map.ins.transform.localScale.x * 0.5f;
		float yM = Map.ins.transform.localScale.y * 0.5f;
		if(pos.x < Map.ins.transform.position.x - xM)
		{
			pos.x = Map.ins.transform.position.x - xM;
		}
		if (pos.x > Map.ins.transform.position.x + xM)
		{
			pos.x = Map.ins.transform.position.x + xM;
		}
		if (pos.y < Map.ins.transform.position.y - yM)
		{
			pos.y = Map.ins.transform.position.y - yM;
		}
		if (pos.y > Map.ins.transform.position.y + yM)
		{
			pos.y = Map.ins.transform.position.y + yM;
		}


		transform.position = pos;

		sizeSpeed *= 1 - Time.deltaTime * drag;
		velo *= 1 - Time.deltaTime * drag;
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		crtmat.SetFloat("_t", Time.unscaledTime * timeScale);
		Graphics.Blit(source, destination, crtmat); 
	}
}
