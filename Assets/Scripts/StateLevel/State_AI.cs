using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using static ArmyUtils;
using Unity.VisualScripting;

public class State_AI : State
{
	int warScope = 6;
	List<City> attacked;

	List<Unit> recentlyOrdered;

	List<int> targetHashList;

	public float[] troopAllocations;

	public List<Unit>[] garrisons;

	protected override void Awake()
	{
		base.Awake();
		recentlyOrdered = new List<Unit>();
		attacked = new List<City>();
		targetHashList = new List<int>();

	}
	public virtual void Start()
	{
		troopAllocations = new float[Map.ins.numStates];
		garrisons = new List<Unit>[Map.ins.numStates];
		for (int i = 0; i < Map.ins.numStates; i++) {
			garrisons[i] = new List<Unit>();
		}
	}
	public override void Setup(int i, Vector2Int pos)
	{
		//Called a few ms after start
		base.Setup(i, pos);
	}


	protected void ConductWar_Update(int enemy, War war)
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
	public void ICBMStrike(int warheads, List<Target> targets)
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

			SiloFire(silos[slcham], target, 1);
			slcham++;
		}
	}

	protected async void DistributedPositions(int borderwith, List<Unit> troops)
	{
		for (int i = 0; i < troops.Count; i++)
		{
			Vector2 pos = (troops[i] as Army).wpos;
			Vector2Int mpos = MapUtils.PointToCoords(pos);
			City c = await Task.Run(() => NearestCity(pos, borderwith, null));
			if (c == null) return; // alexander wept
			int[] pas = ROE.Passables(team);
			Vector2Int con = await Task.Run(() => AsyncPath.ins.CheapestOpenNode(mpos, c.mpos, pas, 4));
			Vector2 moveto = MapUtils.CoordsToPoint(con);
			Vector2 edit = moveto;
			int tries = 0;
			bool gtg;
			do
			{
				tries++;
				Vector2 ran = Random.insideUnitCircle * 80;
				edit = moveto + ran;
				gtg = pas.Contains(Map.ins.GetPixTeam(MapUtils.PointToCoords(edit)));
			}
			while (!gtg && tries < 50);
			Order o = new Order(Order.Type.MoveTo, edit);
			if (i >= troops.Count) break;
			if (troops[i] == null) continue;
			troops[i].Direct(o);
			recentlyOrdered.Add(troops[i]);
		}
	}
	async void CityGarrisons(int borderWith, List<Unit> troops) {

		Vector2 ep = Diplo.states[borderWith].transform.position;
		Vector2Int mep = MapUtils.PointToCoords(ep);
		int[] pas = ROE.Passables(team);
		List<City> needsProtection = new List<City>();
		List<Vector2> borders = new List<Vector2>();
		for (int i = 0; i < 10; i++)
		{
		
			City c = NearestCity(ep, team, needsProtection);
			if (c == null) break;
			needsProtection.Add(c);
			Vector2Int node = await Task.Run(() => AsyncPath.ins.CheapestOpenNode(needsProtection[i].mpos, mep, pas, 2));
			if (node != Vector2Int.zero)
			{

				borders.Add(MapUtils.CoordsToPoint(node));
			}
		}
		int l = borders.Count;
		if (l < 1) return;

		Vector2[] spawns = new Vector2[troops.Count];
		for (int i = 0; i < spawns.Length; i++)
		{
			bool gtg = false;
			int tries = 0;
			do
			{
				tries++;
				Vector2 ran = Random.insideUnitCircle * 80;
				spawns[i] = borders[i % l] + ran;
				gtg = pas.Contains(Map.ins.GetPixTeam(MapUtils.PointToCoords(spawns[i])));
			}
			while (!gtg && tries < 50);
			Order o = new Order(Order.Type.MoveTo, spawns[i]);
			troops[i].Direct(o);
			recentlyOrdered.Add(troops[i]);
		}
	}

	//Test, slow
	void CaptureACity(int ofteam)
	{
		City toAttack = ArmyUtils.NearestCity(transform.position, ofteam, attacked);
		if (toAttack == null) return; // war over lmao

		//Get units closest to nearest city
		Unit[] units = ArmyUtils.GetArmies(team, 5, toAttack.transform.position, recentlyOrdered);
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
			recentlyOrdered.Add(units[i]);
		}
	}

	public override void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim)
	{
		base.LaunchDetect(launcher, target, perp, victim);

		if (victim == team)
		{
			//Debug.Log(team + " we're getting nuked!!");
			//We're about to get nuked
			ROE.DeclareWar(team, perp);
			Diplo.relationships[team, perp] = Diplo.Relationship.NuclearWar;
		}
	}

	protected void ReAssignGarrisons(bool overwriteRecentOrders) {
		ClearGarrisons();
		Unit[] uns = ArmyUtils.GetArmies(team);
		List<Unit> assigned = new List<Unit>(); 
		if (!overwriteRecentOrders) {
			assigned =  recentlyOrdered.ToList();
		}
		
		int[] allotment = new int[Map.ins.numStates];

		for (int i = 0; i < Map.ins.numStates; i++) {
			if (troopAllocations[i] < 0.05f) continue;
			allotment[i] = Mathf.FloorToInt(uns.Length * troopAllocations[i]);
			if (allotment[i] < 1) continue;
			Vector2 ep = Diplo.states[i].transform.position;
			Unit[] alo = GetArmies(team, allotment[i], ep, assigned);
			for (int u = 0; u < alo.Length; u++)
			{
				Unit un = alo[u];
				garrisons[i].Add(un);
				assigned.Add(un);
			}
		}
	}

	public override void ReadyForOrders(Unit un)
	{
		base.ReadyForOrders(un);
		recentlyOrdered.Remove(un);
	}

	protected void SiloFire(Silo sl, Target t, int warheads)
	{
		Order or = new Order(Order.Type.Attack, t.wpos);
		ArmyUtils.Salvo(sl, or, warheads);
		targetHashList.Add(t.hash);

		StartCoroutine(RemoveFromHash(t.hash, MapUtils.Tau(sl.transform.position, t.wpos)));
	}

	protected void ClearGarrisons() { 
    
		foreach(List<Unit> gr in garrisons) {
			gr.Clear();
		} 
	}

	public IEnumerator RemoveFromHash(int i, float t)
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


