using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINationMenu : UIMenu
{
    bool warMode;

	public UIOption relationships;
	public UIOption declarewar;
	public UIOption sendAid;
	public UIOption airdoctrine;
	public UIOption nuclearstrike;
	public UIOption troopSlider;

	bool usingSatura;

	private void Awake()
	{
		usingSatura = troopSlider != null;
	}
	private void Start()
	{
		//HideWarMenu();
		//ToggleWarMode(false);
	}
	// Update is called once per frame
	void Update()
    {
		RefreshOptions();

		if (usingSatura) {
			if ((Diplomacy.states[UI.ins.targetNation] as State_Enemy).sharesBorder[0])
			{
				if (!troopSlider.gameObject.activeInHierarchy)
				{
					troopSlider.gameObject.SetActive(true);
				}
			}
			else
			{
				if (troopSlider.gameObject.activeInHierarchy)
				{
					troopSlider.gameObject.SetActive(false);
				}
			}
		}


		if(highlight_recolor != Map.ins.state_colors[UI.ins.targetNation]) {
			highlight_recolor = Map.ins.state_colors[UI.ins.targetNation];
			children[UI.ins.selected].Highlight();
		}
    }

	void RefreshOptions() {

		//Adapt UI options to the specific scenario

		if(children.Length > UI.ins.selected) {
			children[UI.ins.selected].UnHighlight();
		}

		List<UIOption> kiddos = new List<UIOption>();
		bool atWar = ROE.AreWeAtWar(0, UI.ins.targetNation);

		if(relationships != null) kiddos.Add(relationships);
		if (declarewar != null) kiddos.Add(declarewar);

		sendAid?.gameObject.SetActive(false);
		nuclearstrike?.gameObject.SetActive(false);
		airdoctrine?.gameObject.SetActive(false);
		StateEval eval = new StateEval(0);
		if (atWar) {

			if (eval.str_air > 0)
			{
				airdoctrine.gameObject.SetActive(true);
				kiddos.Add(airdoctrine);
			}

			if(eval.str_nuke > 0) {
				nuclearstrike.gameObject.SetActive(true);
				kiddos.Add(nuclearstrike);
				nuclearstrike.plaintext = "[nuclear strike]";
				nuclearstrike.text.text = "[nuclear strike]";
			}

		}
		else {
			sendAid.gameObject?.SetActive(true);
			kiddos.Add(sendAid);

			if (eval.str_nuke > 0)
			{
				nuclearstrike.gameObject.SetActive(true);
				kiddos.Add(nuclearstrike);
				nuclearstrike.plaintext = "[pre-emptive strike]";
				nuclearstrike.text.text = "[pre-emptive strike]";
			}
		}

		if (usingSatura) {
			if ((Diplomacy.states[Map.localTeam] as State_AI).sharesBorder[UI.ins.targetNation]) {
				troopSlider.gameObject.SetActive(true);
				kiddos.Add(troopSlider);
			}
		}
		children = kiddos.ToArray();

		if (UI.ins.selected >= children.Length) {
			UI.ins.selected = Mathf.Max(0, children.Length - 1);
		}
		children[UI.ins.selected].Highlight();
	}

 //   void ToggleWarMode(bool enable) {
 //       warMode = enable;

	//	if(children.Length > UI.ins.selected) {
	//		children[UI.ins.selected].UnHighlight();
	//	}

	//	children = warMode ? warChildren : peaceChildren;

	//	//this looks stupid, but the order of the functions is important
	//	//to ensure that we dont disable the mutual children
	//	if (warMode) {
	//		TogglePeaceMenu(!warMode);
	//		ToggleWarMenu(warMode);
	//	}
	//	else {
	//		ToggleWarMenu(warMode);
	//		TogglePeaceMenu(!warMode);
	//	}

	//	UI.ins.selected = Mathf.Min(UI.ins.selected, children.Length - 1);
	//	children[UI.ins.selected].Highlight();
	//}

 //   void ToggleWarMenu(bool on) { 
 //       foreach(UIOption op in warChildren) {
	//		op.gameObject.SetActive(on);
	//	}
 //   }
	//void TogglePeaceMenu(bool on)
	//{
	//	foreach (UIOption op in peaceChildren)
	//	{
	//		op.gameObject.SetActive(on);
	//	}
	//}
}
