using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
	public static InfoPanel instance;

	public Image background;
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
		int numWars = texts.Count;
		ClearTexts();
		for(int s = 1; s < Map.ins.numStates; s++) {
			if (ROE.AreWeAtWar(0, s)){
				NewText(s);
			}
		}
		if(numWars < texts.Count) {
			FlashColor(Color.red);
		}else if (numWars > texts.Count) {
			FlashColor(Color.green);
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
		Vector2 offset = spacer * texts.Count * Vector2.down;
		g.transform.position = (Vector2)textOrigin.transform.position + offset;
		tex.text = Diplo.state_names[nation]; //todo replace with nation names
		tex.color = Map.ins.state_colors[nation];
	}

	void ClearTexts() { 
		for(int i = 0; i < texts.Count; i++) {
			Destroy(texts[i].gameObject);
		}
		texts.Clear();
    }
}
