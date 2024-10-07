using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Load : MonoBehaviour
{
	float start_load;

	private void Awake()
	{
		start_load = Time.time;
		Time.timeScale = 1;
		Debug.Log("loading");
	}

	private void Update()
	{
		if(Time.time - start_load > 0.1f) {
			Debug.Log("load back!");
			SceneManager.LoadScene("Game");
		}
	}
}
