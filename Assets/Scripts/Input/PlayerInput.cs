using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerInput : MonoBehaviour
{
	//Should handle more input centrally

	public static PlayerInput ins;
	public LayerMask regularMask;
	public LayerMask buildMask;

	public bool buildMode;

	private void Awake()
	{
		ins = this;
		Camera.main.cullingMask = buildMode ? buildMask : regularMask;
	}

	// Update is called once per frame
	void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) {
			UI.ins.BuildScreen();
	    }
	}

	public void ToggleBuildMode(bool enable) {
		buildMode = enable;
		Camera.main.cullingMask = buildMode ? buildMask : regularMask;
		Map.ins.ConvertToTexture();
	}

	public void BuildSilo() {

		Vector2 wp = transform.GetChild(0).transform.position;
		if (Map.ins.GetPixTeam(MapUtils.PointToCoords(wp)) != 0) {

			ConsolePanel.Log("unsuitable construction location");
			return;
		}

		ConsolePanel.Log("New ICBM Silo being constructed at: " + ((Vector2)(transform.position)).ToString());

		Transform t = Instantiate(InfluenceMan.ins.constructionPrefab,
	     wp, Quaternion.identity, InfluenceMan.ins.transform).transform;

		Construction co = t.GetComponent<Construction>();
		co.toBuild = InfluenceMan.ins.siloPrefab.GetComponent<Unit>();
		co.team = 0;
	}

	public void PlayerSendAid() {
		if (Diplomacy.states[0].manHourDebt > Diplomacy.states[0].assesment.buyingPower * 2)
		{
			ConsolePanel.Log("Insufficient funds to send aid");
		}
		else {

			Diplomacy.states[0].SendAid(UI.ins.targetNation);
		}
	
    }

	public void ConscriptTroops()
	{
		Diplomacy.states[0].SpawnTroops(5);
	}
	public void DisbandTroops()
	{
		ConsolePanel.Log("placing men on leave");
		Diplomacy.states[0].DisbandTroops(5);
	}
}
