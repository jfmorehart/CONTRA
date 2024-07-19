using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Construction : Unit
{
    public Unit toBuild;
    public float manHoursRemaining;

	//Sites are registered on Awake as construction_sites in their team's State class
	// they are then Worked until completed, where they're replaced by the stored toBuild unit

	public override void Start()
	{
		base.Start();
        GetComponent<SpriteRenderer>().sprite = toBuild.GetComponent<SpriteRenderer>().sprite;
        manHoursRemaining = toBuild.constructionCost;
	}
	public void Work(float workAmt)
    {
        manHoursRemaining -= workAmt;
	    if(manHoursRemaining < 0) {
            Complete();
	    }

        GroundCheck();
    }

    void Complete() {
        if (toBuild == null) return;
        Instantiate(toBuild.gameObject, transform.position, transform.rotation, ArmyManager.ins.transform);
        if(team == 0) {
            ConsolePanel.Log("ICBM Silo finished construction, awaiting orders");
	    }
        Kill();
    }
}
