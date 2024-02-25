using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static Diplo;

public class AlliancePanel : MonoBehaviour
{
	public static AlliancePanel ins;

	public GameObject textPrefab;
	public List<TMP_Text> live;
	public Transform center;
	public float fontSize;

	public float spacer_c, spacer_r;

	private void Awake()
	{
		ins = this;
	}
	public void AlliancePanelUpdate()
	{
		ClearPanel();
		for(int i = 0; i < alliances.Length; i++) { 
			for(int j = 0; j < alliances[i].Count; j++) {
				GameObject go = Instantiate(textPrefab, center);
				go.transform.localPosition = new Vector3(
					spacer_c * i,
					-spacer_r * j,
					0);
				TMP_Text tex = go.GetComponent<TMP_Text>();
				tex.text = Diplo.state_names[alliances[i][j]];
				tex.color = Map.ins.state_colors[alliances[i][j]];
				tex.fontSize = fontSize;
				tex.alignment = TextAlignmentOptions.TopLeft;
				live.Add(tex);
			}
		}
	}

	void ClearPanel() { 
		foreach(TMP_Text g in live) {
			Destroy(g.gameObject);
		}
		live.Clear();
    }
}
