using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UISlider : MonoBehaviour
{
    public UIOption boss;
    public Slider sl;

	public bool troopAllocSlider;

	private void Awake()
	{
		if(sl == null) {
			sl = GetComponent<Slider>();
		}
		boss.value = 0.5f;
	}
	// Update is called once per frame
	void Update()
    {
		sl.value = boss.value;

		if (troopAllocSlider) {
			State_Player pl = Diplomacy.states[Map.localTeam] as State_Player;
			if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.H) || Input.GetKey(KeyCode.L)) {
				//this is the troop slider
				pl.troopAllocPlayerInput[UI.ins.targetNation] = boss.value - 0.5f;
			}
			else {
				boss.value = pl.troopAllocPlayerInput[UI.ins.targetNation] + 0.5f;
			}
		}

	
	}
}
