using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBuyMenu : UIMenu
{
    public UIOption[] all3;
    public UIOption empty;

    public GameObject emptyMessage;

    float lastUpdate;
    // Update is called once per frame
    void Update()
    {
        if(Time.time - lastUpdate > 0.1f) {
            Refresh();
	    }
    }

    public void Refresh() {
        List<UIOption> kiddos = new List<UIOption>();
        for(int i = 0; i < 3; i++) {
            if (Research.unlockedUpgrades[0][i + 1] > 0) {
                kiddos.Add(all3[i]);
				all3[i].gameObject.SetActive(true);
			}
            else {
                all3[i].gameObject.SetActive(false);
	        }
	    }
        if(kiddos.Count < 1) {
            kiddos.Add(empty);
            emptyMessage.SetActive(true);
        }
        else {
			emptyMessage.SetActive(false);
		}
        children = kiddos.ToArray();
        children[UI.ins.selected].Highlight();
    }
}
