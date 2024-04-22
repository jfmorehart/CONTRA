using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIMenu : MonoBehaviour
{
	//this class goes on togglable menu gameobjects

	public UIMenu parent;
	public UIOption[] children;
	public TMP_Text stateColor; //set to state color on load
	public int lastSelected; //for preserving input between screens

	public bool preserveLastSelected = true; // disable for confirmation screens
	public int defaultSelected;
}
