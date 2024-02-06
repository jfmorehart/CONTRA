using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class IdleCam : MonoBehaviour
{
    Vector2 focus;
    public float zoom;

    public float idleTime;
    float lmv = -9;
	public float wiggleroom;

	Vector2 v;
	public float accel;
	public float drag;
	Camera cam;

	public Material crtmat;
	public float timeScale;
	public int downres;

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	private void Update()
	{
		if(Time.time - lmv > idleTime) {
			NextFocus();
			lmv = Time.time;
		}

		Vector2 delta = focus - (Vector2)transform.position;
		if(delta.magnitude > wiggleroom) {
			v += accel * Time.deltaTime * delta.normalized;
		}
		v *= 1 - Time.deltaTime * drag;
		cam.orthographicSize = zoom + delta.magnitude * 0.15f;
		transform.Translate(v * Time.deltaTime, Space.World);
	}

	void NextFocus() {
		List<Silo> sl = InfluenceMan.ins.silos;
		Debug.Log(" focus " + sl.Count);
		if (sl.Count < 1) return;
		focus = sl[Random.Range(0, sl.Count)].transform.position;
    }

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		crtmat.SetFloat("_t", Time.time * timeScale);
		Graphics.Blit(source, destination, crtmat); 


		//int width = source.width >> downres;
		//int height = source.height >> downres;

		//RenderTexture temp3 = RenderTexture.GetTemporary(width, height);
		//Graphics.Blit(source, temp3); // overwrites all previous work
		//crtmat.SetFloat("_t", Time.time * timeScale);
		//Graphics.Blit(temp3, destination, crtmat);
		//RenderTexture.ReleaseTemporary(temp3);

	}

}
