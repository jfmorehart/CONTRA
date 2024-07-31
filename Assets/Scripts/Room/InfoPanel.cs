using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
	public static InfoPanel instance;

	public Image background;
	public TMP_Text header;
	public List<TMP_Text> texts;
	public RectTransform textOrigin;
	public GameObject textPrefab;
	public float spacer;

	private void Awake()
	{
		instance = this;
		ROE.roeChange += ROEUpdate;
		texts = new List<TMP_Text>();
	}
	private void Start()
	{
		ROEUpdate();
		DisplayHandler.resetGame += Reset;
	}
	void Reset() {
		ROE.roeChange -= ROEUpdate;
		DisplayHandler.resetGame -= Reset;

    }
	public void ROEUpdate() {
		if (UI.ins == null) return;
		if (Time.timeSinceLevelLoad < 1) return;

		int numWars = texts.Count;
		RefreshAtWarScreen();
		if (numWars < texts.Count)
		{
			FlashColor(Color.red);
		}
		else if (numWars > texts.Count)
		{
			FlashColor(Color.green);
		}
	}

	public void RefreshAtWarScreen() {
		int target = UI.ins.targetNation;
		
		ClearTexts();
		for (int s = 0; s < Map.ins.numStates; s++)
		{
			if (s == target) continue;
			if (ROE.AreWeAtWar(target, s))
			{
				NewText(s);
			}
		}

		if(texts.Count > 0){
			if (target == 0)
			{
				header.text = ConsolePanel.ColoredName(target) + " are at war with:";
			}
			else
			{
				header.text = ConsolePanel.ColoredName(target) + " is at war with:";
			}
		}
		else
		{
			if (target == 0)
			{
				header.text = ConsolePanel.ColoredName(target) + " are not at war";
			}
			else
			{
				header.text = ConsolePanel.ColoredName(target) + " is not at war";
			}
		}
    }
	void FlashColor(Color c) {
		background.color = c;
		Invoke(nameof(EndFlash), 0.5f);
    }

	void EndFlash() {
		background.color = Color.black;
    }
	void NewText(int nation) {
		GameObject g = Instantiate(textPrefab, transform);
		
		TMP_Text tex = g.GetComponent<TMP_Text>();
		texts.Add(tex);
		Vector2 offset = spacer * (1 + texts.Count) * Vector2.down;
		g.transform.position = (Vector2)textOrigin.transform.position + offset;
		tex.text = ConsolePanel.ColoredName(nation);
	}

	void ClearTexts() { 
		for(int i = 0; i < texts.Count; i++) {
			Destroy(texts[i].gameObject);
		}
		texts.Clear();
    }
}
