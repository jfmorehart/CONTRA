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
	public Vector2 shake;
	public float shakestr, shakeFreq, shakeAmp, shakeDecay;
	public float exp, mult, mult2, sizeDecay;

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
	float resetTime;
	private void Start()
	{
		//Debug.Log("setting to max" + Time.timeSinceLevelLoad);
		GetComponent<Camera>().orthographicSize = 2000;
		resetTime = Time.timeSinceLevelLoad;
	}
	public void Update()
	{
		if (!canMove) return;
		Vector3 pos = transform.position - (Vector3)shake;

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

		if (Camera.main.orthographicSize > 600 && Time.timeSinceLevelLoad - resetTime> 0.1f)
		{
			float multval = Camera.main.orthographicSize * (1 - Time.deltaTime * sizeDecay * Camera.main.orthographicSize);
			Camera.main.orthographicSize = Mathf.Max(600, multval);
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

		shake = shakestr * new Vector3(shakeAmp * (Mathf.PerlinNoise1D(Time.time * shakeFreq) - 0.5f), shakeAmp * (Mathf.PerlinNoise1D(Time.time * shakeFreq + 1) - 0.5f));
		shake *= Camera.main.orthographicSize;
		transform.position = pos + (Vector3)shake;
		shakestr -= Time.deltaTime * shakeDecay;
		if (shakestr < 0.05f) shakestr = 0;

		sizeSpeed *= 1 - Time.deltaTime * drag;
		velo *= 1 - Time.deltaTime * drag;

	}

	public void Shake(Vector2 pos, float str) {
		Vector3 delta = pos - (Vector2)transform.position;
		delta.z += Camera.main.orthographicSize * 2;
		float amt = mult * Mathf.Pow(mult2/delta.magnitude, exp);
		shakestr += amt * str;
		if (shakestr > 2) shakestr = 2;
    }
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		crtmat.SetFloat("_t", Time.unscaledTime * timeScale);
		Graphics.Blit(source, destination, crtmat); 
	}
}
