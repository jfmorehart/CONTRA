using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ArmyUtils;

public class State_AI : State
{
	int warScope = 6;
	List<City> attacked;

	int armySize;
	List<Unit> recentlyOrdered;

	List<int> targetHashList;

	readonly float ICBMspeed = 50;

	protected override void Awake()
	{
		base.Awake();
		recentlyOrdered = new List<Unit>();
		attacked = new List<City>();
		targetHashList = new List<int>();
	}

	public override void Setup(int i, Vector2Int pos)
	{
		//Called a few ms after start
		base.Setup(i, pos);
	}

	protected override void StateUpdate()
	{

		base.StateUpdate();
		//if (!ROE.AreWeAtWar(team)) return;
		armySize = GetArmies(team).Length;
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			StateEval eval = new StateEval(team, i);
			if (ROE.AreWeAtWar(team, i))
			{
				War war = War.Peer;
				if (eval.popRatio < 0.5f)
				{
					war = War.Colonial;
				}
				if (eval.armyRatio > 1.5)
				{
					war = War.Defensive;
				}
				if (eval.pVictory < 0.1 || eval.isHotWar)
				{
					war = War.Total;
				}
				ConductWar_Update(i, war);
			}
			else
			{
				if (AsyncPath.ins == null) return;
				if (AsyncPath.ins.SharesBorder(team, i))
				{
					if (eval.pVictory > 0.7f && !ROE.AreWeAtWar(team))
					{
						Debug.Log(team + " starting a war with " + i);
						ROE.DeclareWar(team, i);
						ConductWar_Update(i, War.Colonial);
					}
				}

			}
		}
	}


	void ConductWar_Update(int enemy, War war)
	{
		List<Target> targets = new List<Target>();

		CaptureACity(enemy); //slow
		 
		switch (war)
		{
			case War.Peer:
				// Conventional Invasion
				// Maintain countervalue threat
				targets.AddRange(NuclearTargets(enemy));
				ICBMStrike(20, targets);
				break;
			case War.Colonial:
				// Conventional Invasion
				// Prevent escalation with countervalue deterrence (offer way out)
				// Counterforce to preserve capturable civilian centers
				//targets.AddRange(NuclearTargets(enemy));
				//ICBMStrike(20, targets);
				break;
			case War.Defensive:
				// Repel invasion 
				// Diplomatic Pressure from allies
				// Maintain limited countervalue threat
				targets.AddRange(CivilianTargets(enemy));
				targets = TargetSort(targets.ToArray()).ToList();
				ICBMStrike(30, targets);
				break;
			case War.Total:

				// Short Term: Eliminate Nuclear Assets
				// Long Term: Eliminate Cities
				targets.AddRange(NuclearTargets(enemy));
				targets.AddRange(CivilianTargets(enemy));
				targets.AddRange(ConventionalTargets(enemy));
				targets = TargetSort(targets.ToArray()).ToList();
				ICBMStrike(20, targets);
				break;
		}
	}

	void ICBMStrike(int warheads, List<Target> targets)
	{
		Silo[] silos = ArmyUtils.GetSilos(team);
		if (silos.Length < 1) return;
		int slcham = 0;
		int n = Mathf.Min(targets.Count, warheads);
		for (int i = 0; i < n; i++)
		{
			if (slcham >= silos.Length) slcham = 0;

			Target target;
			bool target_aquired = false;
			do
			{
				if (targets.Count <= i) return;

				target = targets[i];
				if (targetHashList.Contains(target.hash))
				{
					targets.Remove(target);
				}
				else
				{
					target_aquired = true;
				}

			}
			while (!target_aquired);

			SiloFire(silos[slcham], target);
			slcham++;
		}
	}


	void ArmyEncircle(City toAttack, Unit[] units)
	{
		Vector2[] pos = ArmyUtils.Encircle(toAttack.transform.position, 20, units.Length);
		for (int i = 0; i < pos.Length; i++)
		{
			units[i].Direct(new Order(Order.Type.MoveTo, pos[i]));
		}
	}

	//Test
	void CaptureACity(int ofteam)
	{
		City toAttack = ArmyUtils.NearestCity(transform.position, ofteam, attacked);
		if (toAttack == null) return; // war over lmao

		//Get units closest to nearest city
		Unit[] units = ArmyUtils.GetArmies(team, 5, toAttack.transform.position, recentlyOrdered);
		foreach (Unit un in units)
		{
			recentlyOrdered.Add(un);
		}
		// Reassign city toAttack to target the city closest to them
		// since it may differ from above target
		toAttack = ArmyUtils.NearestCity(transform.position, ofteam, attacked);
		attacked.Add(toAttack);
		if (attacked.Count >= warScope)
		{
			attacked.RemoveAt(0);
		}

		Vector2[] pos = ArmyUtils.Encircle(toAttack.transform.position, 20, units.Length);
		for (int i = 0; i < pos.Length; i++)
		{
			units[i].Direct(new Order(Order.Type.MoveTo, pos[i]));
		}
	}

	public override void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim)
	{
		base.LaunchDetect(launcher, target, perp, victim);

		if (victim == team)
		{
			Debug.Log(team + " we're getting nuked!!");
			//We're about to get nuked
			ROE.DeclareWar(team, perp);
			Diplo.relationships[team, perp] = Diplo.Relationship.NuclearWar;
		}
	}

	public override void ReadyForOrders(Unit un)
	{
		base.ReadyForOrders(un);
		recentlyOrdered.Remove(un);
	}

	void SiloFire(Silo sl, Target t)
	{
		Order or = new Order(Order.Type.Attack, t.wpos);
		sl.Direct(or);
		targetHashList.Add(t.hash);
		float time = Vector2.Distance(sl.transform.position, t.wpos) / ICBMspeed;
		StartCoroutine(RemoveFromHash(t.hash, time));
	}
	bool CanWalkTo(int other)
	{
		Vector2Int mp = MapUtils.PointToCoords(transform.position);
		Vector2Int ip = MapUtils.PointToCoords(Diplo.states[other].transform.position);
		List<int> pas = ROE.Passables(team).ToList();
		pas.Add(other);
		Vector2Int[] path = PathFind.Path(mp, ip, pas.ToArray(), 10);
		if (path != null)
		{
			// We can walk there;
			return true;
		}
		return false;
	}

	IEnumerator RemoveFromHash(int i, float t)
	{
		yield return new WaitForSeconds(t);
		targetHashList.Remove(i);
		yield break;
	}
	public enum War
	{
		Peer,
		Colonial,
		Defensive,
		Total
	}
}


