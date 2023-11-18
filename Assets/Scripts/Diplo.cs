using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Diplo
{
	public static State[] states;


	public static void SetupDiplo() {
		states = new State[Map.ins.numStates];
    }

	public static void RegisterState(State st) {
		states[st.team] = st;
    }
}
