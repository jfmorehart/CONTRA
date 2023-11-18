using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Order
{
	public Type type;
	public Vector2 pos;

	public Order(Type ntype, Vector2 npos) {
		type = ntype;
		pos = npos;
    }
	public enum Type {
		MoveTo,
		Attack,
		Stop,
		Capture
	}

}
