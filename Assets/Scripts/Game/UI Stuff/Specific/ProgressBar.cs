using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressBar : MonoBehaviour
{
	public bool displayActive;

    // Update is called once per frame
    void Update()
    {
		if (displayActive)
		{
			Vector3 left = -50 * Vector3.right;
			Vector3 pos = Vector3.Lerp(left, Vector3.zero, Research.unlockProgress[0]);
			Vector3 scale = Vector3.one;
			scale.x = Research.unlockProgress[0];// * transform.parent.localScale.x;

			transform.localPosition = pos;
			transform.localScale = scale;

		}
	}
}
