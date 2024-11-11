using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThinPanel : MonoBehaviour
{
    public GameObject idleScreen;
    public GameObject threatScreen;

    public Image background;
    public Color sineColor;
    public float sineFreq, sineAmp;

    public TMP_Text threat;
    public string plainText;
    int PT_index;
    public float lastLetterTime, letterDelay, letterSpeed;

	private void Awake()
	{
		if (Simulator.tutorialOverride)
		{
            GetComponent<TimePanel>().timer = float.MaxValue;
			idleScreen.SetActive(false);
			threatScreen.SetActive(false);
			//Destroy(gameObject);
		}
	}
	// Update is called once per frame
	void Update()
    {
		if (Simulator.tutorialOverride) return;

		if(Time.timeScale == 0) {
			idleScreen.SetActive(false);
			threatScreen.SetActive(false);
			background.color = Color.black;
			return;
		}
		if (UI.ins.incomingMissiles > 0) {
        sineColor = Color.red;
            if (!threatScreen.activeInHierarchy) {
				idleScreen.SetActive(false);
				threatScreen.SetActive(true);
			}
	    }
        else if (!Research.currentlyResearching[0].Equals(-Vector2Int.one)) {
            sineColor = Color.green * 0.3f;
			if (!idleScreen.activeInHierarchy)
			{
				idleScreen.SetActive(true);
				threatScreen.SetActive(false);
			}
		}
        else {
            sineColor = Color.black;
			if (!idleScreen.activeInHierarchy)
			{
				idleScreen.SetActive(true);
				threatScreen.SetActive(false);
			}
		}

        float sine = (Mathf.Sin(Time.time * sineFreq) * 0.5f + 1) * sineAmp;
        background.color = Color.Lerp(Color.black, sineColor, sine);

        if(Time.time - lastLetterTime > letterDelay) {
            lastLetterTime = Time.time;
            AddLetter();
        }
    }

    void AddLetter() {
        if (PT_index >= plainText.Length) PT_index = 0;
        threat.text += plainText[PT_index];
        PT_index++;
        if(threat.text.Length >= plainText.Length) {
			threat.text = threat.text.Remove(0, 1);
		}


    }
}
