using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TypingInterface : MonoBehaviour
{
    public TMP_Text dialogue;

    string pstring = "";
	public string doneText = "";
    public string outString;

	public float cTimer;
	float cflop;
	bool hascursor;

	private void Awake()
	{
		if (!Simulator.IsSetup) Simulator.Setup();
		Time.timeScale = 1;
	}

	private void Start()
	{
        outString = dialogue.text;
	}
	// Update is called once per frame
	void Update()
    {

        foreach(char c in Input.inputString) {
            if (!pstring.Contains(c)) {
				if(c == '\b') {
					if (outString.Length > 0)
					{
						outString = outString.Remove(outString.Length - 1);
					}
				}
				else {
					outString += c;
				}

	        }
	    }

		if (Input.GetKeyDown(KeyCode.Return))
		{
			Debug.Log("ret");
			if(outString.Contains("joshua", System.StringComparison.CurrentCultureIgnoreCase)) {
				outString += " \n greetings professor falken \n";
			}

			if (outString.Contains("scenario a", System.StringComparison.CurrentCultureIgnoreCase))
			{
				outString += " \n very well... \n";
				Simulator.activeScenario = Simulator.scenarios[0];
				Invoke(nameof(LoadGame), 3);
				Debug.Log("invoked");
			}
			if (outString.Contains("scenario b", System.StringComparison.CurrentCultureIgnoreCase))
			{
				outString += " \n very well... \n";
				Simulator.activeScenario = Simulator.scenarios[1];
				Invoke(nameof(LoadGame), 3);
			}
			if (outString.Contains("scenario c", System.StringComparison.CurrentCultureIgnoreCase))
			{
				outString += " \n very well... \n";
				Simulator.activeScenario = Simulator.scenarios[2];
				Invoke(nameof(LoadGame), 3);
			}


			doneText = outString.ToLower();
			outString = '\n' + ">";

		}


		pstring = Input.inputString;

        dialogue.text = doneText + outString.ToLower();

		if (hascursor)
		{
			dialogue.text += "|";
		}

		if (Time.time - cflop > cTimer)
		{
			hascursor = !hascursor;
			cflop = Time.time;
		}
	}

	void LoadGame() {
		Debug.Log("loading scene");
		SceneManager.LoadScene("Game");
	}
}
