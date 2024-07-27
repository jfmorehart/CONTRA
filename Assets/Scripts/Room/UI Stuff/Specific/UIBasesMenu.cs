using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIBasesMenu : UIMenu
{
    int bsel;
    List<Construction> binfo;
    float lu;

    public TMP_Text sitenumber;
    public TMP_Text sitename;
    public TMP_Text btype;
    public Image icon;
    public TMP_Text ammo;
    public TMP_Text tech1;
    public TMP_Text tech2;

	public UIOption[] normal;
	public UIOption[] construction;
	public UIOption[] empty;

	private void Awake()
	{
        binfo = new List<Construction>();
	}
    void Refresh() {
        Clear();
        binfo.Clear();
		binfo.AddRange(ArmyUtils.GetBuildings(0));
		binfo.AddRange(Diplomacy.states[0].construction_sites);
		if (bsel < 0) bsel = binfo.Count - 1;
		if (bsel > binfo.Count - 1) bsel = 0;
	}

	// Update is called once per frame
	void Update()
    {
		if (Time.time - lu > 0.1f)
		{
			DisplayInfo();
			lu = Time.time;
		}



        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            SwitchBase(-1);
	    }
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			SwitchBase(1);
		}

		if (binfo.Count > bsel && bsel > -1)
		{
			if (binfo[bsel] == null) return;
			Vector2 pos = binfo[bsel].transform.position;
			MoveCam.ins.transform.position = new Vector3(pos.x, pos.y, -20);
		}
	}

    void SwitchBase(int dir) {
        bsel += dir;
        if (bsel < 0) bsel = binfo.Count - 1;
        if (bsel > binfo.Count - 1) bsel = 0;
        DisplayInfo();
	}
	void ChildrenSwap(UIOption[] newset) {
		foreach (UIOption child in normal)
		{
			child.gameObject.SetActive(false);
		}

		if (children.Length > 0) {
			children[UI.ins.selected].UnHighlight();
		}
		children = newset;
		if (UI.ins.selected > children.Length - 1)
		{
			UI.ins.selected = children.Length - 1;
		}
		if (children.Length < 1) return;
		children[UI.ins.selected].Highlight();
		foreach (UIOption child in newset)
		{
			child.gameObject.SetActive(true);
		}
	}
    void DisplayInfo() {

        Refresh();
        if (binfo.Count < 1)
        {
			ChildrenSwap(empty);
            return;
		}

		sitenumber.text = "< " + (bsel + 1).ToString() + " of " + binfo.Count.ToString() + " >";
		sitename.text = binfo[bsel].name;
		ArmyManager.BuildingType mbtype = binfo[bsel].btype;
		btype.text = System.Enum.GetName(typeof(ArmyManager.BuildingType), (int)mbtype);
		icon.enabled = true;
		icon.overrideSprite = binfo[bsel].GetComponent<SpriteRenderer>().sprite;
		ammo.text = "ammo";
		tech1.text = "bababa";
		tech2.text = "bababaran";

		if (binfo[bsel].manHoursRemaining > 0) {
			ChildrenSwap(construction);
			ammo.text = "under construction";
			tech1.text = "";
			tech2.text = "";
		}
		else {
			ChildrenSwap(normal);
		}

	}
    void Clear() {
		sitenumber.text = "";
        sitename.text = "no bases found";
		btype.text = "";
        icon.enabled = false;
        ammo.text = "";
		tech1.text = "";
		tech2.text = "";
	}

    public void Shutter() {
		GameObject go = Instantiate(ArmyManager.ins.constructionPrefab,
            binfo[bsel].transform.position, binfo[bsel].transform.rotation,
			ArmyManager.ins.transform);
		Construction co = go.GetComponent<Construction>();
        co.PrepareBuild(binfo[bsel].btype);
		co.team = 0;
        co.manHoursRemaining = Economics.cost_siloUpkeep * 10;
		binfo[bsel].Kill();
        StartCoroutine(ReDrawAfterFrame());
	}
    IEnumerator ReDrawAfterFrame() {
        yield return null;
		SwitchBase(0);
	}

    public void Demolish() {
		binfo[bsel].Kill();
		StartCoroutine(ReDrawAfterFrame());
	}
}
