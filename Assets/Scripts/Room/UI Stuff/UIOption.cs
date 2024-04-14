using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UIOption : MonoBehaviour
{
	public UIMenu parentMenu;
	//this class goes on selectable menu options

	public bool selected;
	public bool highlight_recolors_sprite = true;
	public Kind kind;
	public float value; //0 to 1 for slider, 0 or 1 for switch

	[HideInInspector] public string plaintext;

	[HideInInspector] public TMP_Text text;
	[HideInInspector] public Color defaultColor;
	public UnityEvent onSelect;

	private void Awake()
	{
		if(text == null) {
			text = GetComponent<TMP_Text>();
		}

		if(parentMenu == null) {
			parentMenu = transform.parent.GetComponent<UIMenu>();
		}
		plaintext = text.text;
		defaultColor = text.color;
	}

	public void Highlight()
	{
		if (highlight_recolors_sprite) {
			text.color = Color.yellow;
		}
		else {
			text.fontStyle = FontStyles.Underline | FontStyles.Bold;
		}

		text.text = ">" + plaintext + "<";

		if (kind == Kind.Switch) BoxTick();
	}

	public void UnHighlight()
	{
		if (highlight_recolors_sprite)
		{
			text.color = defaultColor;
		}
		else
		{
			text.fontStyle = FontStyles.Bold;
		}
		text.text = plaintext;
		if (kind == Kind.Switch) BoxTick();
	}

	void BoxTick() {
		if (value != 1) return;
		//For use with tick boxes, ensure the tick is visible and not overwritten
		text.text = text.text.Replace(" ", "X");
	}

	public enum Kind { 
    
		Button,
		Slider, 
		Switch
	}

}

