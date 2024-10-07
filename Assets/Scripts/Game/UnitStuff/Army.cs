using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Army : Unit
 {
	[Header("Army")]

	//Movement
	public float speed;
	float speedMod = 1;
	Vector3 dest;
	[SerializeField] bool enroute;
	[SerializeField] float secondsSinceSaidReady;
	float stopDist = 4f;

	//List<Unit> inView;
	public int range;
	public float reload;
	int damage = 1;

	float lastShot;

	public Vector2Int[] path;
	int currentPathNodeIndex;
	public Order pathOrder;
	

	[HideInInspector]
	public Vector2 wpos;

	public override void Awake()
	{
		base.Awake();

		ROE.roeChange += StaggerPathTargetCheck;
		PlayerInput.minimize += ToggleMinimize;

		//so that they dont spawn big
		if (PlayerInput.ins.airMode) {
			ToggleMinimize(true);
		}
	}
	public override void ApplyUpgrades()
	{
		if (Research.unlockedUpgrades[team][0] > 0)
		{
			//"training"
			//does nothing
		}
		if (Research.unlockedUpgrades[team][0] > 1)
		{
			//"armor support"
			if(maxHP < 5) {
				int shp = maxHP;
				maxHP = 5;
				hP = Mathf.CeilToInt(hP * (maxHP / (float)shp));
			}
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.1f);
		}
		if (Research.unlockedUpgrades[team][0] > 2)
		{
			//"mechanization"
			speed = 12;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.2f);
		}
		if (Research.unlockedUpgrades[team][0] > 3)
		{
			//"combined arms"
			damage = 2;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.3f);
		}
		if (Research.unlockedUpgrades[team][0] > 4)
		{
			//"modernization"
			if (maxHP < 7)
			{
				int shp = maxHP;
				maxHP = 7;
				hP = Mathf.CeilToInt(hP * (maxHP / (float)shp));
			}
			speed = 15f;
			upkeepCost = Mathf.CeilToInt(baseUpkeepCost * 1.6f);
		}
	}
	public override void Start()
	{
		base.Start();
		lastShot = Random.Range(Time.time, Time.time + 1);
		DisplayHandler.resetGame += Reset;
	}

	public virtual void Update()
	{
		if (!Diplomacy.states[team].alive) {
			Kill();
			return;
		}

		secondsSinceSaidReady += Time.deltaTime;
		if(secondsSinceSaidReady > 10 && !enroute) {
			//remind them
			Idle();
		}
		wpos = transform.position; //For use in multithreaded city shenanigans

		if (enroute) {
			Vector2 delta = dest - transform.position;
			float pspeed = (speed * (hP / (float)maxHP)) * speedMod;
			transform.Translate(pspeed * Time.deltaTime * delta.normalized);

			//avoid bunching
			//SpaceOut();

			if(delta.magnitude < stopDist) {
				if(path != null) {
					ContinueOnPath();
				}
				else {
					Idle();
				}
			}
		}

		if(Time.time - lastShot > reload) {
			lastShot = Time.time;
			Check4ChunkUpdate();
			ShootUpdate();
		}
	}
	//void SpaceOut() {
	////failed attempt at spacing out units

	//	if (Time.time - lastSpaceOut < spaceDelay) return;
	//	lastSpaceOut = Time.time;

	//	Check4ChunkUpdate();

	//	List<Unit> buddies = UnitChunks.chunks[positionChunk];
	//	foreach(Unit un in buddies) {
	//		if (un.team != team) continue;
	//		Vector2 delta = transform.position - un.transform.position;
	//		float pushForce = 10 * speed * speedMod * Mathf.Min(10, (1 / Mathf.Pow(delta.magnitude, 0.5f)));
	//		transform.Translate(Time.deltaTime * pushForce * delta.normalized);
	//	}
 //   }

	void ContinueOnPath() {
		if (currentPathNodeIndex < path.Length - 1)
		{
			currentPathNodeIndex++;
			enroute = true;
			dest = MapUtils.CoordsToPoint(path[currentPathNodeIndex]);

			//Code to slow armies when traveling through enemy territory
			//todo update original map image;
			int teamOfCurrentDest = Map.ins.GetOriginalMap(path[currentPathNodeIndex]);
			bool slowdown = (teamOfCurrentDest != team) && Map.ins.state_populations[teamOfCurrentDest] > 0;
			speedMod = (slowdown ? 1.4f : 1.5f);//hack removed significant slowdown

			int et = Map.ins.GetPixTeam(path[^1]);
			int onLandOf = Map.ins.GetPixTeam(path[currentPathNodeIndex]);
			if (Diplomacy.IsMyAlly(team, onLandOf)) {
				actingTeam = onLandOf;
			}
			if (onLandOf != team)
			{
				//recolor army when on ally's territory
				ren.material.color = Map.ins.state_colors[team] + Color.white * 0.2f;
			}
			else
			{
				ren.material.color = Color.grey;
			}
			int[] pas = ROE.Passables(team);

			if (!pas.Contains(et))
			{
				Idle();
			}
		}
		else
		{
			Idle();
		}
	}
	void Idle() {
		enroute = false;
		Diplomacy.states[team].ReadyForOrders(this);
		secondsSinceSaidReady = 0;
	}

	public virtual void ShootUpdate() {
		Unit un = GetValidTarget();
		if (un == null) return;
		Vector2 delta = un.transform.position - transform.position;
		float hTime = Pool.ins.GetBullet().Fire(transform.position, delta, team);

		//this is stupid, but i dont want to fuck with the basic functions at this point
		for(int i = 0; i < damage; i++) {
			un.Hit(hTime);
		}

		return;
	}

	public override void Direct(Order order)
	{
		base.Direct(order);

		switch (order.type) {
			case Order.Type.MoveTo:
				pathOrder = order;
				//Async
				Invoke(nameof(PathFind), 0);


				//Synchronous solution
				//Vector2Int[] npath = PathFind.Path(cpos, opos, ROE.Passables(team), 4);
				//if(npath != null) {
				//	enroute = true;
				//	path = npath;
				//	pathOrder = order;
				//	current = 1;
				//	dest = MapUtils.CoordsToPoint(path[current]);
				//}
				break;
		}
	}
	public async void PathFind() {
		Vector2Int cpos = MapUtils.PointToCoords(transform.position);
		Vector2Int opos = MapUtils.PointToCoords(pathOrder.pos);
		int[] pas = ROE.Passables(team);
		path = await Task.Run(() => AsyncPath.ins.Path(cpos, opos, pas.ToArray(), 2, 3200));
		PathIsSet();
	}

	public void PathIsSet() {
		if (path != null)
		{
			if (path.Length < 2) return;
			enroute = true;
			currentPathNodeIndex = 1;
			dest = MapUtils.CoordsToPoint(path[currentPathNodeIndex]);
		}
	}

	//Used so that not every army dude does this check on the same frame
	void StaggerPathTargetCheck()
	{
		if (!enroute) return;
		Invoke(nameof(CheckPathTargetValidity), Random.Range(0f, 0.5f));
	}

	void CheckPathTargetValidity() {
		if (!enroute) return;
		if(path != null) {
			int et = Map.ins.GetPixTeam(path[^1]);
			if (!ROE.Passables(team).Contains(et)) {
				Idle();
			}
		}
    }

	Unit GetValidTarget() 
    {
		List<Unit> uns = UnitChunks.GetSurroundingChunkData(positionChunk);
		for (int i = 0; i < uns.Count; i++) {
			if (uns[i] == null) continue;
			if (team == uns[i].team) continue;
			if (!ROE.AreWeAtWar(team, uns[i].team)) {
				continue;
			}
			Vector2 delta = transform.position - uns[i].transform.position;
			if(delta.magnitude > range) {
				continue;
			}
			return uns[i];
		}
		return null;
    }

	
	public override void Kill()
	{
		base.Kill();
		ROE.roeChange -= StaggerPathTargetCheck;
		PlayerInput.minimize -= ToggleMinimize;
	}

	void Reset() {
		ROE.roeChange -= StaggerPathTargetCheck;
		PlayerInput.minimize -= ToggleMinimize;
		DisplayHandler.resetGame -= Reset;
	}

	void ToggleMinimize(bool on) {
		transform.localScale *= on ? 0.5f : 2;
    }
}
