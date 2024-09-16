using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RTopicMenu : UIMenu
{
	public Research.Branch branch;
	float lastUpdate;
	public TMP_Text header;
	public TMP_Text sub;


	private void Update()
	{
		if(Time.time - lastUpdate > 0.1f) {
			Refresh();
		}
		lastUpdate = Time.time;
	}
	void Refresh() {
		header.text = Research.headers[(int)branch];
		for (int i = 0; i < children.Length; i++)
		{
			children[i].plaintext = Research.names[(int)branch][i];
			children[i].text.text = Research.names[(int)branch][i];
			if (Research.unlockedUpgrades[0][(int)branch] == i) {
				//unlocked
				children[i].locked = false;
				children[i].text.fontStyle = FontStyles.Bold | FontStyles.Italic;
				children[i].text.fontSize = 25;
			}
			else {
				//locked
				children[i].locked = true;
				if(Research.unlockedUpgrades[0][(int)branch] > i) {
					children[i].text.fontStyle = FontStyles.Italic;
					//children[i].text.color = Color.green;
					//children[i].defaultColor = Color.green;
					children[i].text.fontSize = 20;
				}
				else {
					children[i].text.fontStyle = FontStyles.Normal;
					children[i].text.color = Color.grey;
					children[i].text.fontSize = 20;
					children[i].defaultColor = Color.grey;
				}

			}	
		}
		children[UI.ins.selected].Highlight();
	}

	public void BeginNewResearch()
	{
		if (children[UI.ins.selected].locked) return;
		Research.unlockProgress[0] = 0;
		Research.currentlyResearching[0] = new Vector2Int((int)branch, UI.ins.selected);
		UI.ins.Cancel();
	}
}
