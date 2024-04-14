using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArmyUtils;

public class UIStrikeMenu : UIMenu
{
    public float tickRate = 0.1f;
    float lastTick;

    public UIOption saturationSlider;

    // Update is called once per frame
    void Update()
    {
        if(Time.time - lastTick > tickRate) {
            lastTick = Time.time;
            UpdateStrikePlanScreen();
	    }
    }

    void UpdateStrikePlanScreen() {
		float sat = saturationSlider.value * 20;
		int sati = Mathf.CeilToInt(Mathf.Max(1, sat));

		List<Target> tars = GetTargets(UI.ins.targetNation, sati, children[0].value == 1, children[1].value == 1, children[2].value == 1);
		StrikePlan.ins.DrawPlan(sati, tars);
	}
}
