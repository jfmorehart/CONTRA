using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Construction : Unit
{
    //public Unit toBuild;
    public ArmyManager.BuildingType btype;
    public float manHoursRemaining;
	public AudioClip clip;

	//Sites are registered on Awake as construction_sites in their team's State class
	// they are then Worked until completed, where they're replaced by the stored toBuild unit

	float gct;
	float gcd = 0.5f;

	public virtual void Update() { 
		if(Time.time - gct > gcd) {
			gct = Time.time;
			GroundCheck();
		}
    }

	public void PrepareBuild(ArmyManager.BuildingType bt) {
		btype = bt;
		Unit toBuild = ArmyManager.ins.buildPrefabs[(int)btype].GetComponent<Unit>();
		transform.localScale = toBuild.transform.localScale;
		GetComponent<SpriteRenderer>().sprite = toBuild.GetComponent<SpriteRenderer>().sprite;
		manHoursRemaining = toBuild.constructionCost;
		Debug.Log("preparing build");
		//if()
		//SFX.ins.NewSource(clip, 0.1f);
		if(team == Map.localTeam) {
			SFX.ins.VectorLockNewSound(clip, 0.02f, transform.position, 0.3f, 0.0004f);
		}
		else {
			SFX.ins.VectorLockNewSound(clip, 0.04f, transform.position, 0.3f, 0.002f);
		}

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
		if (Map.multi) {
			if (Map.host) {
				GameObject go = Instantiate(ArmyManager.ins.buildPrefabs[(int)btype], transform.position, transform.rotation, ArmyManager.ins.transform);
				go.GetComponent<NetworkObject>().SpawnWithOwnership(0);
			}
			else {
				MultiplayerVariables.ins.PlaceBuildingServerRPC(transform.position, (int)btype);
			}
		}
		else {
			Instantiate(ArmyManager.ins.buildPrefabs[(int)btype], transform.position, transform.rotation, ArmyManager.ins.transform);
		}

        Kill();
    }
}
