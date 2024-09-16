using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightPanel : MonoBehaviour
{
	public UIMenu menu_econ;
	public UIMenu menu_atwar;

	public UIMenu current_menu;

	private void Start()
	{
		//SwitchMenus(menu_econ, menu_econ);
	}
	private void Update()
	{
		//if (UI.ins.currentMenu == UI.ins.menu_nation || (UI.ins.currentMenu == UI.ins.menu_diplo))
		//{
		//	if (current_menu != menu_atwar)
		//	{
		//		SwitchMenus(current_menu, menu_atwar);
		//	}
		//}
		//else
		//{
		//	if (current_menu == menu_atwar)
		//	{
		//		SwitchMenus(current_menu, menu_econ);
		//	}
		//}

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
