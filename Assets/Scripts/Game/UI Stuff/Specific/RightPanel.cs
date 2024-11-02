using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RightPanel : MonoBehaviour
{
	public static RightPanel ins;
	public UIMenu menu_econ;
	public UIMenu menu_atwar;

	public UIMenu current_menu;

	public Renderer ecoren;
	public Material econmat;

	public int arraySizes;
	public float time;
	public float[] popOverTime;
	int target;

	public float minScale, maxScale = 1;
	private void Awake()
	{
		ins = this;
	}
	private void Start()
	{
		//SwitchMenus(menu_econ, menu_econ);
		//for(popOverTime)
		popOverTime = new float[arraySizes];
		InvokeRepeating(nameof(RecordEconomy), 0.1f, 1f);
	}
	private void Update()
	{
		//ecoren.material.SetFloat("time", time);
		if(UI.ins.targetNation != target) {
			target = UI.ins.targetNation;
			RecordEconomy();
		}
		int cham = Diplomacy.states[target].graphCham;
		if (cham < popOverTime.Length) {
			ecoren.material.SetInt("popLen", cham);
		}
		else {
			ecoren.material.SetInt("popLen", popOverTime.Length);
		}

		if (popOverTime.Length > 0) {
			ecoren.material.SetFloatArray("popOverTime", popOverTime);
		}

	}
	public void RecordEconomy() {
		float max;
		float min;
		target = UI.ins.targetNation;
		int cham = Diplomacy.states[target].graphCham;
		float[] realPops = Diplomacy.states[target].recentPops;
		
		if(cham >= popOverTime.Length) { 
			//Graph is at max size
			min = realPops.Min();
			max = realPops.Max();
			UpdateDisplayArray(realPops, min, max);
			return;
		}

		min = float.MaxValue;
		max = float.MinValue;
		for(int i = 0; i < cham; i++) {
			if (realPops[i] > max) {
				max = realPops[i];
			}
			if (realPops[i] < min) {
				min = realPops[i];
			}
		}
		UpdateDisplayArray(realPops, min, max);
    }
	void UpdateDisplayArray(float[] inputArray, float min, float max) {
		for (int i = 0; i < inputArray.Length; i++)
		{
			popOverTime[i] = ValueRemap(min, max, inputArray[i]);
			if (i > 1)
			{
				float diff = popOverTime[i] - popOverTime[i - 1];
				if (Mathf.Abs(diff) < 0.01f)
				{
					float sign = Mathf.Sign(Diplomacy.states[target].recentGrowths[i]);
					popOverTime[i] += 0.01f * sign;
				}
			}
		}
	}
	float ValueRemap(float min, float max, float value) { 
		return (value - min * minScale) / (maxScale * max - min);
	}

	void SwitchMenus(UIMenu start, UIMenu end)
	{
		start.gameObject.SetActive(false);
		current_menu = end;
		current_menu.gameObject.SetActive(true);

		if (current_menu.stateColor != null)
		{
			current_menu.stateColor.color = Map.ins.state_colors[UI.ins.targetNation];
			current_menu.stateColor.text = Diplomacy.state_names[UI.ins.targetNation];
		}
	}
}
