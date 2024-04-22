using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using static ArmyUtils;
using Unity.VisualScripting;

public class State_AI : State
{
	// State class inherited by Player and NPCs.
	// Has some useful tools, namely ICBMStrike(and related target hashing),
	// troop movement, so the defensive posturing stuff and attacking stuff
		// the garisson system, DistributedPositions, etc
		// CaptureACity, which i thought was gonna be temp but oh well, etc


	//number of guaranteed different cities to simulntaneously attack
	//we avoid duplicates within this number of attack commands
	const int recentlyAttackedCities_ListSize = 6;
	//list of said cities
	List<City> attacked;

	//list of units not to boss around just yet
	List<Unit> recentlyOrdered;

	//list of targets not to fire at since theres already a nuke enroute
	List<int> targetHashList;

	//some kind of scariness evaluation used for determining how many
	//troops to send to the border with given enemy
	public float[] troopAllocations;
	public bool[] sharesBorder;

	//list of units assigned to each border organized by enemyborder team#
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
		sharesBorder = new bool[Map.ins.numStates];
		garrisons = new List<Unit>[Map.ins.numStates];
		for (int i = 0; i < Map.ins.numStates; i++) {
			garrisons[i] = new List<Unit>();
		}
	}
	public override void Setup(int i, Vector2Int pos)
	{
		//Called a few ms after start
		base.Setup(i, pos);
		NuclearTargets(team);
	}
	public override void WarStarted(int by) {
		if (!Diplo.HasAllies(team)) return;
		int al = Diplo.AllianceOfTeam(team);
		Diplo.AllianceWarsUpdate(al);
    }
	protected override void StateUpdate()
	{
		//this call updates info for nuclear threat assesment
		ArmyUtils.NuclearTargets(team);

		base.StateUpdate();
		GenerateTroopAllocations();

		//evaluate the difference between desired troop allocations and 
		// garrison sizes. Only overwrite previous assignments if there are significant discrepancies
		// in proportional size
		float total = 0;
		float discrepancy = 0;
		for (int r = 0; r < Map.ins.numStates; r++)
		{
			total += garrisons[r].Count;
		}
		for (int r = 0; r < Map.ins.numStates; r++)
		{
			discrepancy += Mathf.Abs((garrisons[r].Count / total)  - troopAllocations[r]);
		}
		if(discrepancy > 0.25f) {
			//expensive 
			ReAssignGarrisons(true);
		}
		else {
			//bit cheaper
			ReAssignGarrisons(false);
		}


		for (int r = 0; r < Map.ins.numStates; r++)
		{
			//Expensive
			DistributedPositions(r, garrisons[r]);
		}
	}


	protected virtual void ConductWar_Update(int enemy, War war)
	{
		if (Map.ins.state_populations[enemy] < 1) ROE.MakePeace(team, enemy);
		CaptureACity(enemy); //slow
	}
	public virtual void GenerateTroopAllocations()
	{
		float[] tas = new float[Map.ins.numStates];
		float tasum = 0;


		//this function should generate fresh data for troopAllocations
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
		
			StateEval eval = new StateEval(team, i); //not that expensive really
			tas[i] = eval.armyRatio * Diplo.CanIReachEnemyThroughAllies(team, i) * 0.3f;
			tas[i] *= (Diplo.IsMyAlly(team, i) ? 0.1f : 1);
			sharesBorder[i] = AsyncPath.ins.SharesBorder(team, i); //todo fix sharesborder
			if (ROE.AreWeAtWar(team, i) && AsyncPath.ins.SharesBorder(team, i)){
				// weight troop allocation by necessity
				tas[i] += 0.2f;
				tas[i] *= 5;
			}
			tasum += tas[i];
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] = tas[i] / (tasum + 0.001f);
		}
	}

	public void ICBMStrike(int warheads, List<Target> targets)
	{
		//Nice little self contained function for obliterating civilization

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

	protected async void DistributedPositions(int borderwith, List<Unit> troops, bool teleport = false)
	{
		//fucky and overcomplex function designed to spread out troops along the border
		//with an enemy state, for defensive posturing

		for (int i = 0; i < troops.Count; i++)
		{
			Vector2 pos = (troops[i] as Army).wpos;
			Vector2Int mpos = MapUtils.PointToCoords(pos);

			//honestly stupid expensive
			City c = await Task.Run(() => NearestCity(pos, borderwith, null));
			if (c == null) return; // alexander wept
			int[] pas = ROE.Passables(team); //which states we can pass over

			//find us the closest legal square to the desired one
			Vector2Int con = await Task.Run(() => AsyncPath.ins.CheapestOpenNode(mpos, c.mpos, pas, 4));
			Vector2 moveto = MapUtils.CoordsToPoint(con);
			Vector2 edit = moveto;
			int tries = 0;
			bool goodToGo; 
			do
			{
				tries++;
				Vector2 ran = Random.insideUnitCircle * 80;
				edit = moveto + ran;
				goodToGo = pas.Contains(Map.ins.GetPixTeam(MapUtils.PointToCoords(edit)));
			}
			while (!goodToGo && tries < 50);
			Order o = new Order(Order.Type.MoveTo, edit);
			if (i >= troops.Count) break;
			if (troops[i] == null) continue;
			if (teleport) {
				troops[i].transform.position = o.pos;
			}
			else {
				troops[i].Direct(o);
				recentlyOrdered.Add(troops[i]);
			}
		}
	}
	//async void CityGarrisons(int borderWith, List<Unit> troops) {

	//	Vector2 ep = Diplo.states[borderWith].transform.position;
	//	Vector2Int mep = MapUtils.PointToCoords(ep);
	//	int[] pas = ROE.Passables(team);
	//	List<City> needsProtection = new List<City>();
	//	List<Vector2> borders = new List<Vector2>();
	//	for (int i = 0; i < 10; i++)
	//	{
		
	//		City c = NearestCity(ep, team, needsProtection);
	//		if (c == null) break;
	//		needsProtection.Add(c);
	//		Vector2Int node = await Task.Run(() => AsyncPath.ins.CheapestOpenNode(needsProtection[i].mpos, mep, pas, 2));
	//		if (node != Vector2Int.zero)
	//		{

	//			borders.Add(MapUtils.CoordsToPoint(node));
	//		}
	//	}
	//	int l = borders.Count;
	//	if (l < 1) return;

	//	Vector2[] spawns = new Vector2[troops.Count];
	//	for (int i = 0; i < spawns.Length; i++)
	//	{
	//		bool gtg = false;
	//		int tries = 0;
	//		do
	//		{
	//			tries++;
	//			Vector2 ran = Random.insideUnitCircle * 80;
	//			spawns[i] = borders[i % l] + ran;
	//			gtg = pas.Contains(Map.ins.GetPixTeam(MapUtils.PointToCoords(spawns[i])));
	//		}
	//		while (!gtg && tries < 50);
	//		Order o = new Order(Order.Type.MoveTo, spawns[i]);
	//		troops[i].Direct(o);
	//		recentlyOrdered.Add(troops[i]);
	//	}
	//}

	//Test, slow
	void CaptureACity(int ofteam)
	{
		City toAttack = ArmyUtils.NearestCity(transform.position, ofteam, attacked);
		if (toAttack == null) return; // war over lmao

		//Get units closest to nearest city
		int unitsToSend = Mathf.CeilToInt(garrisons[ofteam].Count * 0.15f);
		Unit[] units = ArmyUtils.GetArmies(team, unitsToSend, toAttack.transform.position, recentlyOrdered);
		// Reassign city toAttack to target the city closest to them
		// since it may differ from above target
		toAttack = ArmyUtils.NearestCity(transform.position, ofteam, attacked);
		attacked.Add(toAttack);
		if (attacked.Count >= recentlyAttackedCities_ListSize)
		{
			attacked.RemoveAt(0);
		}

		//code for surrounding enemy cities. was implemented when i used to
		//calculate city capturing using actual encirclement which was meh
		//but visually this is still coolish
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

		if (victim == team && Diplo.relationships[team, perp] != Diplo.Relationship.NuclearWar)
		{
			ROE.DeclareWar(team, perp);
			Diplo.relationships[team, perp] = Diplo.Relationship.NuclearWar;
		}
	}

	protected void ReAssignGarrisons(bool overwriteRecentOrders) {
		//used for cleaning up garrison system

		ClearGarrisons();
		Unit[] uns = ArmyUtils.GetArmies(team);
		List<Unit> assigned = new List<Unit>(); 
		if (!overwriteRecentOrders) {
			//assigned = recentlyOrdered;
		}
		
		int[] allotment = new int[Map.ins.numStates];

		for (int i = 0; i < Map.ins.numStates; i++) {
			if (troopAllocations[i] < 0.01f) continue;
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
		if (sl.numMissiles < 1) return;
		Order or = new Order(Order.Type.Attack, t.wpos);
		ArmyUtils.Salvo(sl, or, warheads);
		//todo only add to hashlist if a missile really was fired
		targetHashList.Add(t.hash);
		StartCoroutine(RemoveFromHash(t.hash, MapUtils.Tau(sl.transform.position, t.wpos)));
	}

	protected void ClearGarrisons() { 
    
		foreach(List<Unit> gr in garrisons) {
			gr.Clear();
		} 
	}
	public bool TargetInHash(int i) {
		return targetHashList.Contains(i);
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


