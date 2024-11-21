using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class UIResearch : UIMenu
{
    public bool displayActive;

    public TMP_Text subtitle;
    public RectTransform bar;
	public GameObject back;

    public Vector3 p1;
    public Vector3 p2;

    public UIOption[] kiddos;
	public UIOption[] k2;
    public TMP_Text budgetText;

    public Research.Branch prevBranch;

	private void Start()
	{
        Research.currentlyResearching[Map.localTeam] = new Vector2Int(-1, -1);
        HideBar();
	}

	// Update is called once per frame
	void Update()
    {
        if (displayActive && Research.currentlyResearching[Map.localTeam].x < 0)
        {
            HideBar();
        }
        if (!displayActive && Research.currentlyResearching[Map.localTeam].x > -1)
        {
            ShowBar();
        }

        if (displayActive) {
			subtitle.text =
Research.headers[Research.currentlyResearching[Map.localTeam].x] + " :" +
Research.names[Research.currentlyResearching[Map.localTeam].x][Research.currentlyResearching[Map.localTeam].y];

            Research.budget[Map.localTeam] = k2[0].value;
            budgetText.text = "budget: " + Mathf.RoundToInt(k2[0].value * 25) + "% of gdp";
		}

	}

    void HideBar() {
        displayActive = false;
        bar.gameObject.SetActive(false);
        back.SetActive(false);

		foreach (UIOption ui in kiddos)
		{
			ui.gameObject.SetActive(true);
            ui.UnHighlight();
		}
		foreach (UIOption ui in k2)
		{
			ui.gameObject.SetActive(false);
		}
		children = kiddos.ToArray();
        UI.ins.selected = (int)prevBranch;
        children[UI.ins.selected].Highlight();
	}


    void ShowBar() {
		UI.ins.selected = 0;
		displayActive = true;
		bar.gameObject.SetActive(true);
		back.SetActive(true);
        foreach(UIOption ui in kiddos) {
            ui.gameObject.SetActive(false);
	    }
		foreach (UIOption ui in k2)
		{
			ui.gameObject.SetActive(true);
			ui.UnHighlight();
		}
        children = k2;
        k2[0].Highlight();

        prevBranch = (Research.Branch)Research.currentlyResearching[Map.localTeam].x;
	}

    public void CancelResearch() {
        UI.ins.selected = Research.currentlyResearching[Map.localTeam].x;
        Research.unlockProgress[Map.localTeam] = 0;
        Research.currentlyResearching[Map.localTeam] = new Vector2Int(-1, -1);
        HideBar();
    }
}
