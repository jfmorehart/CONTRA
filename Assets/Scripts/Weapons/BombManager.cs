using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombManager : MonoBehaviour
{
	public static BombManager ins;

	private void Awake()
	{
		ins = this;
	}

	public void Drop(int mteam, Vector2 wpos, float myield, float CEP = 20, float dyield = 1) {
		Vector2 apos = wpos + CEP * Random.insideUnitCircle;
		float ryield = Mathf.Max(0.3f, myield + dyield * Random.Range(-1, 1));
		Debug.Log("myield = " + myield + " ryield " + ryield);
		StartCoroutine(Fall(new Bomb(mteam, apos, ryield)));
    }

    static IEnumerator Fall(Bomb bomb) {
        yield return new WaitForSeconds(1);
        Map.ins.Detonate(bomb.pos, bomb.yield, bomb.team);
    }

	public struct Bomb
	{
		public int team;
		public Vector2 pos;
		public float yield;

		public Bomb(int mteam, Vector2 wpos, float myield) {
			team = mteam;
			pos = wpos;
			yield = myield;
		}

	}
}
