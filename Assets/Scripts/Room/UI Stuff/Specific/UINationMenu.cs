using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINationMenu : UIMenu
{
    bool warMode;
    public UIOption[] peaceChildren;
    public UIOption[] warChildren; //its just a shot away

	private void Awake()
	{
		children = peaceChildren;
	}
	private void Start()
	{
		//HideWarMenu();
		ToggleWarMode(false);
	}
	// Update is called once per frame
	void Update()
    {
        if(ROE.AreWeAtWar(0, UI.ins.targetNation)) {
            if (!warMode) {
                ToggleWarMode(true);
	        }
        }
        else {
			if (warMode)
			{
				ToggleWarMode(false);
			}
		}
    }

    void ToggleWarMode(bool enable) {
        warMode = enable;

		if(children.Length > UI.ins.selected) {
			children[UI.ins.selected].UnHighlight();
		}

		children = warMode ? warChildren : peaceChildren;

		//this looks stupid, but the order of the functions is important
		//to ensure that we dont disable the mutual children
		if (warMode) {
			TogglePeaceMenu(!warMode);
			ToggleWarMenu(warMode);
		}
		else {
			ToggleWarMenu(warMode);
			TogglePeaceMenu(!warMode);
		}

		UI.ins.selected = Mathf.Min(UI.ins.selected, children.Length - 1);
		children[UI.ins.selected].Highlight();
	}

    void ToggleWarMenu(bool on) { 
        foreach(UIOption op in warChildren) {
			op.gameObject.SetActive(on);
		}
    }
	void TogglePeaceMenu(bool on)
	{
		foreach (UIOption op in peaceChildren)
		{
			op.gameObject.SetActive(on);
		}
	}
}
