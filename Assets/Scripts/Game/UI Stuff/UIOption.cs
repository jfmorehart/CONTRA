using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UIOption : MonoBehaviour
{
	public UIMenu parentMenu;
	//this class goes on selectable menu options

	public bool locked;
	public bool highlighted;
	public bool highlight_recolors_sprite = true;
	public Kind kind;
	public float value; //0 to 1 for slider, 0 or 1 for switch

	[HideInInspector] public string plaintext;

	[HideInInspector] public TMP_Text text;
	[HideInInspector] public Color defaultColor;

	public UnityEvent onSelect;

	private void Awake()
	{
		if (text == null) {
			text = GetComponent<TMP_Text>();
		}
		if (parentMenu == null) {
			parentMenu = transform.parent.GetComponent<UIMenu>();
		}
		plaintext = text.text;
		defaultColor = text.color;

		//if (locked)
		//{
		//	text.color = Color.grey;
		//}
	}

	public void Highlight()
	{
		highlighted = true;
		text.text = ">" + plaintext + "<";

		if (locked) {
			text.color = Color.grey;
			return;
		}
		if (highlight_recolors_sprite) {
			text.color = parentMenu.highlight_recolor;
		}
		else {
			text.fontStyle = FontStyles.Underline | FontStyles.Bold;
		}

		if (kind == Kind.Switch) BoxTick();
	}

	public void UnHighlight()
	{
		highlighted = false;
		text.text = plaintext;

		if (locked)
		{
			text.color = Color.grey;
			return;
		}
		if (highlight_recolors_sprite)
		{
			text.color = defaultColor;
		}
		else
		{
			text.fontStyle = FontStyles.Bold;
		}

		if (kind == Kind.Switch) BoxTick();
	}

	public void BoxTick() {
		if (value != 1) return;
		//For use with tick boxes, ensure the tick is visible and not overwritten

		// this is only used to deviate from the plaintext. if the plaintext contains an x,
		// that will never be overwritten, as the inverse of this replace is never run.

		text.text = text.text.Replace(" ", "X");
	}

	public enum Kind { 
    
		Button,
		Slider, 
		Switch
	}

}

