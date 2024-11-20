using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using static ArmyUtils;
using Unity.Netcode;

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
	List<int> bombedHashList;

	//some kind of scariness evaluation used for determining how many
	//troops to send to the border with given enemy
	public float[] troopAllocations;
	[SerializeField] int[] border_allotment;
	public bool[] sharesBorder;

	//list of units assigned to each border organized by enemyborder team#
	public List<Unit>[] garrisons;

	public List<Target> airTargets;
	float lastTargetRefresh;

	//test hack
	//public bool testREGARRSION;

	//these are used for ordering planes what to do
	public enum AirDoctrines
	{
		StrategicBombing,
		Groundforces,
		AirSuperiority,
		Civilian
	}
	public bool[][] airdoctrine;

	protected override void Awake()
	{
		base.Awake();
		recentlyOrdered = new List<Unit>();
		attacked = new List<City>();
		targetHashList = new List<int>();
		bombedHashList = new List<int>();
		border_allotment = new int[Map.ins.numStates];

		airdoctrine = new bool[Map.ins.numStates][];
		for(int i = 0; i < Map.ins.numStates; i++) {
			airdoctrine[i] = new bool[]{ true, true, true, true };
		}
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
		//Called a few ms after awake
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
		if (!alive) return;
		base.StateUpdate();

		//this call updates info for nuclear threat assesment
		GetTargets(team);
			
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
			discrepancy += Mathf.Abs((garrisons[r].Count / total) - troopAllocations[r]);
		}

		//todo find out a way to reduce the amount that troops get needlessly reordered
		ReAssignGarrisons(true);

		//if (discrepancy > 0.5f)
		//{
		//	//expensive 
		//	ReAssignGarrisons(true);
		//}
		//else
		//{
		//	//bit cheaper
		//	ReAssignGarrisons(false);
		//}

		for (int r = 0; r < Map.ins.numStates; r++)
		{
			//Expensive
			DistributedPositions(r, garrisons[r]);
		}
	}


	protected virtual void ConductWar_Update(int enemy, War war)
	{
		//if (Map.ins.state_populations[enemy] < 1) ROE.MakePeace(team, enemy);
	}
	public virtual void GenerateTroopAllocations()
	{
		float[] tas = new float[Map.ins.numStates];
		float tasum = 0;

		//this function should generate fresh data for troopAllocations
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			if (team == i) continue;
			StateDynamic eval = new StateDynamic(team, i); //not that expensive really
			sharesBorder[i] = Diplomacy.CanIReachEnemyThroughAllies(team, i) > 0;
			tas[i] = eval.armyRatio * (sharesBorder[i] ? 1 : 0);

			if (ROE.AreWeAtWar(team, i) && sharesBorder[i]){
				// weight troop allocation by necessity
				tas[i] += 0.2f;
				tas[i] *= 10;
			}
			tasum += tas[i];
		}
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			troopAllocations[i] = (tas[i]) / (tasum + 0.001f);
			//Debug.Log("team " + team + " al" + troopAllocations[i] + " to " + i);
		}
	}

	public int ICBMStrike(int warheads, List<Target> targets, int enemy)
	{
		int missilesAway = 0;

		//Nice little self contained function for obliterating civilization
		Silo[] silos = ArmyUtils.GetSilos(team);
		if (silos.Length < 1) return 0;
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
					if(missilesAway > 0) {
						StartCoroutine(nameof(InformLaunch), enemy);
					}
					return missilesAway;
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
		if(missilesAway > 0) {
			StartCoroutine(nameof(InformLaunch), enemy);
		}
		return missilesAway;
		
	}
	IEnumerator InformLaunch(int enemy) {
		yield return new WaitForSeconds(3); //todo find launch delay
		if(!ROE.AreWeAtWar(team, enemy)) ROE.DeclareWar(team, enemy);
		LaunchDetection.StrikeDetected(team, enemy);

	}

	protected async void DistributedPositions(int borderwith, List<Unit> troops, bool teleport = false)
	{
		//fucky and overcomplex function designed to spread out troops along the border
		//with an enemy state, for defensive posturing

		int[] pas = ROE.Passables(team); //which states we can pass over
		for (int i = 0; i < troops.Count; i++)
		{
			Vector2 pos = (troops[i] as Army).wpos;
			Vector2Int mpos = MapUtils.PointToCoords(pos);

			//honestly stupid expensive city-oriented method
			//City c = await Task.Run(() => NearestCity(pos, borderwith, null));
			//if (c == null) return; // alexander wept

			Vector2Int dest;
			if (ROE.AreWeAtWar(team, borderwith) && Random.Range(0, 1f) > 0.5f)
			{
				//honestly stupid expensive city-oriented method
				//capture enemy cities during wartime
				City c = await Task.Run(() => NearestCity(pos, borderwith, null));
				if (c == null)
				{
					Debug.LogError("alexander wept");
					return; // alexander wept
				}
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
				//if(team != 0)Debug.Log("dir");
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
		bombedHashList.Add(t.hash);
		StartCoroutine(RemoveBombedMarking(t.hash));
    }
	IEnumerator RemoveBombedMarking(int hash) {
		yield return new WaitForSeconds(5);
		bombedHashList.Remove(hash);
    }
	public bool BombTargetOK(Vector2 pos) {
		Target t = new Target(pos, 0, Tar.Conventional);
		return !bombedHashList.Contains(t.hash);
    }
	List<Target> RefreshAirTargets() {

		int[] enemies = ROE.GetEnemies(team).ToArray();
		List<Target> ts = new List<Target>();

		foreach (int i in enemies)
		{
			if (airdoctrine[i][(int)AirDoctrines.Groundforces])
			{
				ts.AddRange(ConventionalTargets(i));
			}
		}
		foreach (int i in enemies)
		{
			if (airdoctrine[i][(int)AirDoctrines.Civilian])
			{
				ts.AddRange(CivilianTargets(i));
			}
		}
		foreach (int i in enemies)
		{
			if (airdoctrine[i][(int)AirDoctrines.StrategicBombing])
			{
				ts.AddRange(NuclearTargets(i));
			}
		}
		foreach (int i in enemies)
		{
			if (airdoctrine[i][(int)AirDoctrines.AirSuperiority])
			{
				ts.AddRange(AirSupremacyTargets(i));
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

	Unit[] uns;
	List<Unit> assigned = new List<Unit>();
	protected void ReAssignGarrisons(bool overwriteRecentOrders) {
		//used for cleaning up garrison system

		ClearGarrisons();
		uns = ArmyUtils.GetArmies(team);
		assigned.Clear();

		if (overwriteRecentOrders)
		{
			recentlyOrdered.Clear();
			assigned = new List<Unit>();
		}
		else
		{
			assigned = new List<Unit>(recentlyOrdered);
		}

		//sorting this by allocation size, so that the biggest threat gets the best troops
		//int[] teamArray = new int[Map.ins.numStates]; 
		//float[] troopAllocations = new float[Map.ins.numStates];
		//for(int i = 0; i < Map.ins.numStates; i++) {
		//	teamArray[i] = i;
		//	troopAllocations[i] = troopAllocations[i];
		//}
		//System.Array.Sort(troopAllocations, teamArray);

		for (int i = 0; i < Map.ins.numStates; i++) {
			int enemyTeam = i;// teamArray[i];
			if (enemyTeam == team) continue;
			if (troopAllocations[enemyTeam] < 0.01f) continue;
			border_allotment[enemyTeam] = Mathf.FloorToInt(uns.Length * troopAllocations[enemyTeam]);
			if (border_allotment[enemyTeam] < 1) continue; //no troops assigned

			//grab units closest to arbitrary enemy city
			City c = NearestCity(transform.position, enemyTeam, null);
			if(c == null) continue;
			Unit[] alo = GetArmies(team, border_allotment[enemyTeam], c.wpos, assigned);
			for (int u = 0; u < alo.Length; u++)
			{
				Unit un = alo[u];
				garrisons[enemyTeam].Add(un);
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
			StartCoroutine(RemoveFromHash(t.hash, 3 + MapUtils.Tau(sl.transform.position, t.wpos)));
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
		Total,
		Ranged,
	}
}


