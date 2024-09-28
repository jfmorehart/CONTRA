using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Missile : MonoBehaviour
{
	TrailRenderer tren;
	Renderer ren;
	bool flying;
	Vector2 st;
	Vector2 en;
	float yield;
	float speed = 50;
	float hmult = 1.5f;

	public int team;
	SFX_OneShot src;

	bool mirvCapable;
	bool isTerminal;

	private void Awake()
	{
		tren = GetComponent<TrailRenderer>();
		ren = GetComponent<Renderer>();
		Toggle(false);
	}
	
	public void MIRVSeparate(Vector2 iLaunchPos, Vector2 seperatePos, Vector2 end, float myield, int mteam) {
		mirvCapable = false;
		transform.position = seperatePos;
		st = iLaunchPos;
		en = end;
		Toggle(true);
		yield = myield;
		team = mteam;
		src = SFX.ins.MissileLaunch(transform);

		//will register itself as terminal
		isTerminal = false;
	}
	public void Launch(Vector2 start, Vector2 end, float myield, int mteam) {
		isTerminal = false;
		LaunchDetection.Launched(start, end);
		transform.position = start;
		st = start;
		en = end;
		Toggle(true);
		flying = true;
		yield = myield;
		team = mteam;
		src = SFX.ins.MissileLaunch(transform);

		if (Research.unlockedUpgrades[team][(int)Research.Branch.silo] > 4) {
			mirvCapable = true;
			yield *= 0.25f; //smaller warheads
		}
	}
	private void Update()
	{
		if (flying) {
			Vector2 delta = en - st;
			float per = PercentOfPath();
			Vector2 adj = delta.normalized + (0.5f - per) * hmult * Vector2.up;
			transform.LookAt((Vector2)transform.position + adj);
			transform.Translate(speed * Time.deltaTime * adj, Space.World);

			if (per > 1) {
				flying = false;
				Toggle(false);
				Map.ins.Detonate(en, yield, team);
			}

			if(per > 0.5f && !isTerminal) {
				isTerminal = true;
				TerminalMissileRegistry.Register(this, Map.ins.GetPixTeam(MapUtils.PointToCoords(en)));
				if (mirvCapable) {
					Vector2 ran;
					for (int i = 0; i < 5; i++)
					{
						ran = 20 * Mathf.Sqrt(yield) * Random.insideUnitCircle;
						Pool.ins.GetMissile().MIRVSeparate(st, transform.position, en + ran, yield, team);
					}
				}
			}
		}
	}
	public void Toggle(bool swi) {
		flying = swi;
		ren.enabled = swi;
		tren.enabled = swi;
		if(src != null) {
			Destroy(src.gameObject);
		}
		if (swi) {
			tren.Clear();
		}

		if (!swi && isTerminal) {
			TerminalMissileRegistry.DeRegister(this, Map.ins.GetPixTeam(MapUtils.PointToCoords(en)));
			isTerminal = false;
		}
    }

	public float PercentOfPath() { 
		Vector2 delta = en - st;
		if (delta.x == 0) return 1;
		float per = transform.position.x - st.x;
		per /= delta.x;
		return per;
    }
}
