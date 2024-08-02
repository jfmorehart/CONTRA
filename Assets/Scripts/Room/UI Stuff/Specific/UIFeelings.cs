using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIFeelings : UIMenu
{
	public GameObject nationprefab;

	private void Update()
	{
		Render();
	}
	void Render()
	{
		foreach(UIOption child in children) {
			Destroy(child.gameObject);
		}
		List<UIOption> kids = new List<UIOption>();
		for(int i = 0; i < Map.ins.numStates; i++) {
			if (i == UI.ins.targetNation) continue;
			GameObject go = Instantiate(nationprefab, transform);
			kids.Add(go.GetComponent<UIOption>());
			go.transform.localPosition = new Vector3(0, -UI.ins.infoSpacer * kids.Count);
			kids[^1].onSelect = null;
			go.GetComponent<TMP_Text>().text = Diplomacy.OpinionText(UI.ins.targetNation, i) + ": " + ConsolePanel.ColoredName(i);
		}
		children = kids.ToArray();
	}
}
