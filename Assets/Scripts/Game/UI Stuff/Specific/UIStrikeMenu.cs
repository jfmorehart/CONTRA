using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using static ArmyUtils;

public class UIStrikeMenu : UIMenu
{
    public float tickRate = 0.1f;
    float lastTick;

    public TMP_Text nation;
    public UIOption saturationSlider;

    // Update is called once per frame
    void Update()
    {
        nation.text = "Target: " + ConsolePanel.ColoredName(UI.ins.targetNation);
        if(Time.time - lastTick > tickRate) {
            lastTick = Time.time;
            UpdateStrikePlanScreen();
	    }
    }

    public void UpdateStrikePlanScreen() {
		float sat = saturationSlider.value * 20;
		int sati = Mathf.CeilToInt(Mathf.Max(1, sat));

		List<Target> tars = GetTargets(UI.ins.targetNation, sati, children[0].value == 1, children[1].value == 1, children[2].value == 1);
		StrikePlan.ins.DrawPlan(sati, TargetSort(tars.ToArray()).ToList());
	}
}
