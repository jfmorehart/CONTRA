using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    TMP_Text text;
	int frames;
	float lastSecond;

	private void Awake()
	{
		text = GetComponent<TMP_Text>();
	}

	// Update is called once per frame
	void Update()
    {
		frames++;
		if(Time.realtimeSinceStartup - lastSecond > 1) {
			text.text = frames.ToString();
			lastSecond = Time.realtimeSinceStartup;
			frames = 0;
		}
    }
}
