using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using static ArmyUtils;

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

	public List<Target> airTargets;
	float lastTargetRefresh;

	//these are used for ordering planes what to do
	public enum AirDoctrines
	{
		StrategicBombing,
		Groundforces,
		AirSuperiority,
		Civilian
	}
	public bool[] airdoctrine = { false, true, true, false };

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
		if (!Diplomacy.HasAllies(team)) return;
		int al = Diplomacy.AllianceOfTeam(team);
		Diplomacy.AllianceWarsUpdate(al);
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

		//todo find out a way to reduce the amount that troops get needlessly reordered
		if (discrepancy > 0.25f)
		{
			//expensive 
			ReAssignGarrisons(true);
		}
		else
		{
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
			tas[i] = eval.armyRatio * Diplomacy.CanIReachEnemyThroughAllies(team, i) * 0.3f;
			tas[i] *= (Diplomacy.IsMyAlly(team, i) ? 0.1f : 1);
			sharesBorder[i] = AsyncPath.ins.SharesBorder(team, i); //todo fix sharesborder
			if (ROE.AreWeAtWar(team, i) && AsyncPath.ins.SharesBorder(team, i)){
				// weight troop allocation by necessity
				tas[i] += 0.2f;
				tas[i] *= 10;
			}
			tasum += tas[i];
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] = tas[i] / (tasum + 0.001f);
		}
	}

	public void ICBMStrike(int warheads, List<Target> targets, int enemy)
	{
		int missilesAway = 0;

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
				if (targets.Count <= i) {
					InformLaunch(missilesAway, enemy);
					return;
				}

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
			while (silos[slcham].numMissiles < 1) {
				if (slcham >= silos.Length - 1) break;
				slcham++;
			}
			missilesAway += SiloFire(silos[slcham], target, 1)? 1 : 0;
			slcham++;
		}
		InformLaunch(missilesAway, enemy);
	}
	void InformLaunch(int missilesAway, int enemy) {
		if (missilesAway > 0)
		{
			//inform diplomacy
			LaunchDetection.StrikeDetected(team, enemy);
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

			//honestly stupid expensive city-oriented method
			//City c = await Task.Run(() => NearestCity(pos, borderwith, null));
			//if (c == null) return; // alexander wept

			Vector2Int dest;
			if (ROE.AreWeAtWar(team, borderwith) && Random.Range(0, 1f) > 0.25f)
			{
				//honestly stupid expensive city-oriented method
				//capture enemy cities during wartime
				City c = await Task.Run(() => NearestCity(pos, borderwith, null));
				if (c == null) return; // alexander wept
				dest = c.mpos;
			}
			else
			{
				//newer border-oriented method
				//spread out troops defensively across the border in peacetime
				int borderSize = Map.ins.borderPoints[team][borderwith].Count;
				if (borderSize < 1) continue;
				dest = Map.ins.borderPoints[team][borderwith][troops[i].id % borderSize];
			}

			int[] pas = ROE.Passables(team); //which states we can pass over

			Vector2 moveto = MapUtils.CoordsToPoint(dest);
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

	public void ScrambleAircraft()
	{
		Airbase[] bases = GetAirbases(team);
		foreach (Airbase b in bases)
		{
			b.LaunchAircraft();
		}
	}
	public Plane.Mission RequestBombingTargets(Plane plane) {
		if (!ROE.AreWeAtWar(team)) return Plane.NULLMission;

		if (Time.time - lastTargetRefresh > 1)
		{
			lastTargetRefresh = Time.time;
			airTargets = RefreshAirTargets();
		}
		if (airTargets.Count > 0)
		{
			if (!targetHashList.Contains(airTargets[0].hash))
			{
				//targetHashList.Add(airTargets[0].hash);
				Plane.Mission mission;
				if (airTargets[0].type == Tar.Conventional) {
					mission = new Plane.Mission(airTargets[0].wpos, Plane.AcceptableDistance.Bombtarget, null, airTargets[0].value);
				}
				else {
					mission = new Plane.Mission(airTargets[0].wpos, Plane.AcceptableDistance.Bombtarget);
				}
				
				airTargets.RemoveAt(0);
				return mission;
			}
		}
		else {
			Debug.Log("team " + team + "can find no targets");
			int[] enemies = ROE.GetEnemies(team).ToArray();
			int pick = enemies[Random.Range(0, enemies.Length)];
			City c = ArmyUtils.NearestCity(plane.transform.position, pick, null);
			if (c == null) return Plane.NULLMission;
			return new Plane.Mission(c.wpos, Plane.AcceptableDistance.Waypoint);
		}
		return Plane.NULLMission;
	}
	public void ReportTargetBombed(Vector2 pos) {
		Target t = new Target(pos, 0, Tar.Conventional);
		//targetHashList.Remove(t.hash);
    }
	List<Target> RefreshAirTargets() {

		int[] enemies = ROE.GetEnemies(team).ToArray();
		List<Target> ts = new List<Target>();
		if (airdoctrine[(int)AirDoctrines.Groundforces])
		{
			foreach (int i in enemies)
			{
				ts.AddRange(ConventionalTargets(i));
			}
		}
		if (airdoctrine[(int)AirDoctrines.Civilian])
		{
			foreach (int i in enemies)
			{
				ts.AddRange(CivilianTargets(i));
			}
		}
		if (airdoctrine[(int)AirDoctrines.StrategicBombing])
		{
			foreach (int i in enemies)
			{
				ts.AddRange(NuclearTargets(i));
			}
		}
		return TargetSort(ts.ToArray()).ToList();
	}

	public override void LaunchDetect(Vector2 launcher, Vector2 target, int perp, int victim)
	{
		base.LaunchDetect(launcher, target, perp, victim);
	}

	public override void StrikeDetect(int perp, int victim, bool provoked)
	{
		base.StrikeDetect(perp, victim, provoked);
		if (victim == team && Diplomacy.relationships[team, perp] != Diplomacy.Relationship.NuclearWar)
		{
			ROE.DeclareWar(team, perp);
			Diplomacy.relationships[team, perp] = Diplomacy.Relationship.NuclearWar;
		}
	}

	protected void ReAssignGarrisons(bool overwriteRecentOrders) {
		//used for cleaning up garrison system

		ClearGarrisons();
		Unit[] uns = ArmyUtils.GetArmies(team);
		List<Unit> assigned = new List<Unit>(); 
		if (overwriteRecentOrders) {
			recentlyOrdered.Clear();
		}
		else {
			assigned = new List<Unit>(recentlyOrdered);
		}
		
		int[] allotment = new int[Map.ins.numStates];

		for (int i = 0; i < Map.ins.numStates; i++) {
			if (troopAllocations[i] < 0.01f) continue;
			allotment[i] = Mathf.FloorToInt(uns.Length * troopAllocations[i]);
			if (allotment[i] < 1) continue;
			Vector2 ep = Diplomacy.states[i].transform.position;
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
		//this is used for marking a unit ready to reorder, 
		//to ideally not order the same unit a million times in a row
		base.ReadyForOrders(un);
		recentlyOrdered.Remove(un);
	}

	protected bool SiloFire(Silo sl, Target t, int warheads)
	{
		if (sl.numMissiles < 1) return false;
		Order or = new Order(Order.Type.Attack, t.wpos);
		if(ArmyUtils.Salvo(sl, or, warheads)) {
			//todo only add to hashlist if a missile really was fired
			targetHashList.Add(t.hash);
			StartCoroutine(RemoveFromHash(t.hash, MapUtils.Tau(sl.transform.position, t.wpos)));
			return true;
		}
		return false;
	}

	protected void ClearGarrisons() { 
    
		foreach(List<Unit> gr in garrisons) {
			gr.Clear();
		} 
	}

	//these hash functions serve as a memory of what targets have been recently fired at
	//to avoid wasting nukes on the same targets
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


