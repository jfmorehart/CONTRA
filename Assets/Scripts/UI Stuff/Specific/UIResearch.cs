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
    public UIOption cancel;

	private void Start()
	{
        Research.currentlyResearching[0] = new Vector2Int(-1, -1);
        HideBar();
	}

	// Update is called once per frame
	void Update()
    {
        if (displayActive && Research.currentlyResearching[0].x < 0)
        {
            HideBar();
        }
        if (!displayActive && Research.currentlyResearching[0].x > -1)
        {
            ShowBar();
        }

        if (displayActive) {
            Vector3 pos = Vector3.Lerp(p1, p2, Research.unlockProgress[0]);
            Vector3 scale = Vector3.one * 0.5f;
            scale.x = Research.unlockProgress[0] * 6.9f;

            bar.anchoredPosition = pos;
            bar.transform.localScale = scale;
			subtitle.text = Research.headers[Research.currentlyResearching[0].x] + " :" + Research.names[Research.currentlyResearching[0].x][Research.currentlyResearching[0].y];

		}
        //Research.unlockProgress[0] += Time.deltaTime * 0.3f;
	}

    void HideBar() {
        displayActive = false;
        bar.gameObject.SetActive(false);
        back.SetActive(false);
        cancel.gameObject.SetActive(false);
		foreach (UIOption ui in kiddos)
		{
			ui.gameObject.SetActive(true);
		}
		children = kiddos.ToArray();
	}


    void ShowBar() {
		UI.ins.selected = 0;
		displayActive = true;
		bar.gameObject.SetActive(true);
		back.SetActive(true);
        foreach(UIOption ui in kiddos) {
            ui.gameObject.SetActive(false);
	    }
        children = new UIOption[1];
        children[0] = cancel;
		cancel.gameObject.SetActive(true);
        cancel.Highlight();
	}

    public void CancelResearch() {
        Research.currentlyResearching[0] = new Vector2Int(-1, -1);
        HideBar();
    }
}
