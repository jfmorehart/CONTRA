using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerInput : MonoBehaviour
{
	//Should handle more input centrally

	public static PlayerInput ins;
	public LayerMask regularMask;
	public LayerMask buildMask;
	public LayerMask airMask;

	public bool buildMode;
	public bool airMode;

	public static Action<bool> minimize;

	private void Awake()
	{
		ins = this;
		if (buildMode) {
			Camera.main.cullingMask = buildMask;
		}
		else if (airMode) {
			Camera.main.cullingMask = airMask;
		}
		else {
			Camera.main.cullingMask = regularMask;
		}

	}

	// Update is called once per frame
	void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) {
			UI.ins.BuildScreen();
	    }
	}

	public void ToggleAirMode(bool enable) {
		if (airMode == enable) return;
		airMode = enable;
		Camera.main.cullingMask = enable ? airMask : regularMask;
		minimize?.Invoke(enable);
		Map.ins.ConvertToTexture();
	}

	public void ToggleBuildMode(bool enable) {
		buildMode = enable;
		Camera.main.cullingMask = buildMode ? buildMask : regularMask;
		Map.ins.ConvertToTexture();
	}


	public void BuildBase(int kind)
	{

		Vector2 wp = transform.GetChild(0).transform.position;
		if (Map.ins.GetPixTeam(MapUtils.PointToCoords(wp)) != 0)
		{

			ConsolePanel.Log("unsuitable construction location", 5);
			return;
		}

		ConsolePanel.Log("New Base being constructed at: " + wp.ToString());

		Transform t = Instantiate(ArmyManager.ins.constructionPrefab,
		 wp, Quaternion.identity, ArmyManager.ins.transform).transform;

		Construction co = t.GetComponent<Construction>();
		if(kind == 0) {
			co.toBuild = ArmyManager.ins.siloPrefab.GetComponent<Unit>();
		}
		if (kind == 1)
		{
			co.toBuild = ArmyManager.ins.airbasePrefab.GetComponent<Unit>();
		}
		if (kind == 2)
		{
			//co.toBuild = InfluenceMan.ins.siloPrefab.GetComponent<Unit>();
		}
		co.team = 0;
	}

	public void PlayerSendAid() {
		if (Diplomacy.states[0].manHourDebt > Diplomacy.states[0].assesment.buyingPower * 2)
		{
			ConsolePanel.Log("Insufficient funds to send aid", 5);
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
		ConsolePanel.Log("placing men on leave", 5);
		Diplomacy.states[0].DisbandTroops(5);
	}
}
