using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopulationScreen : MonoBehaviour
{
	public Camera popCam;

	public Transform[] popChart;
	public Transform[] armyChart;
	public GameObject chartPrefab;
	public Transform center;
	public float spacer, armySpacer, armyWidth;

	public float pop2Scale;

	public int maxChartValue;
	public float scaleFactor;

	private void Start()
	{
		popChart = new RectTransform[Map.ins.numStates];
		armyChart = new RectTransform[Map.ins.numStates];

		for (int i = 0; i < Map.ins.numStates; i++) {
			popChart[i] = Instantiate(chartPrefab, transform).transform;
			Vector2 spos = (Vector2)center.transform.position + Vector2.right * spacer * i;
			//center 
			spos -= Vector2.right * spacer * Map.ins.numStates * 0.5f;
			popChart[i].transform.position = spos;
			popChart[i].GetComponent<Image>().color = Map.ins.state_colors[i];

			armyChart[i] = Instantiate(chartPrefab, transform).transform;
			spos += Vector2.right * spacer * armySpacer;
			armyChart[i].transform.position = spos;
			armyChart[i].GetComponent<Image>().color = Color.white;
		}
	}

	private void Update()
	{
		if (UI.ins == null) return;
		if (UI.ins.currentMenu == UI.ins.menu_strike) {
			popCam.enabled = false;
			return;
		}
		else {
			popCam.enabled = true;
		}

		//Shrink all the bars to fit the largest on screen
		scaleFactor = 1;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (Map.ins.state_populations[i] > maxChartValue)
			{
				float newScaleFactor = maxChartValue / (float)Map.ins.state_populations[i];
				if (newScaleFactor < scaleFactor)
				{
					scaleFactor = newScaleFactor;
				}
			}
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			Vector3 scale = new Vector3(1, scaleFactor * pop2Scale * Map.ins.state_populations[i], 0);
			popChart[i].transform.localScale = scale;
			Vector2 pos = popChart[i].transform.localPosition;
			popChart[i].transform.localPosition = new Vector3(pos.x, center.transform.localPosition.y - scale.y / 2, 0);

			Vector3 ascale = new Vector3(armyWidth, scaleFactor * pop2Scale * ArmyUtils.armies[i].Count, 0);
			armyChart[i].transform.localScale = ascale;
			Vector2 apos = armyChart[i].transform.localPosition;
			armyChart[i].transform.localPosition = new Vector3(apos.x, center.transform.localPosition.y - ascale.y / 2, 0);
		}
	}
}
