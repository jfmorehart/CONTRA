using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Construction : Unit
{
    public Unit toBuild;
    public float manHoursRemaining;

    
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
        Instantiate(toBuild.gameObject, transform.position, transform.rotation, InfluenceMan.ins.transform);
        if(team == 0) {
            ConsolePanel.Log("ICBM Silo finished construction, awaiting orders");
	    }
        Kill();
    }
}
