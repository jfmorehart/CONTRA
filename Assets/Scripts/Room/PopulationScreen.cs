using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopulationScreen : MonoBehaviour
{
	public Transform[] popChart;
	public GameObject chartPrefab;
	public Transform center;
	public float spacer;

	public float pop2Scale;

	private void Start()
	{
		popChart = new RectTransform[Map.ins.numStates];
	
		for(int i = 0; i < Map.ins.numStates; i++) {
			popChart[i] = Instantiate(chartPrefab, UI.ins.transform).transform;
			Vector2 spos = (Vector2)center.transform.position + Vector2.right * spacer * i;
			//center 
			spos -= Vector2.right * spacer * Map.ins.numStates * 0.5f;
			popChart[i].transform.position = spos;
			popChart[i].GetComponent<Image>().color = Map.ins.state_colors[i];
		}
	}

	private void Update()
	{
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			Vector3 scale = new Vector3(1, pop2Scale * Map.ins.state_populations[i], 0);
			popChart[i].transform.localScale = scale;
			Vector2 pos = popChart[i].transform.position;
			popChart[i].transform.position = new Vector3(pos.x, center.transform.position.y - scale.y / 2, 0);
		}
	}
}
