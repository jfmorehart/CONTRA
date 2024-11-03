using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class State_Online : State
{
	//this class controls a the local version of a foreign player-state
	//it essentially does nothing

	protected override void Awake()
	{
	}

	protected override void StateUpdate()
	{
		assesment = Economics.RunAssesment(team);
		Economics.state_assesments[team] = assesment;
		RecordEconomyData();
	}
}
