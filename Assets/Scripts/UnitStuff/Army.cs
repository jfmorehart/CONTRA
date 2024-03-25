using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Army : Unit
 {
	[Header("Army")]
	public float strength;

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
	}
	public override void Start()
	{
		base.Start();
		lastShot = Random.Range(0, 1f);
		DisplayHandler.resetGame += Reset;
	}

	public virtual void Update()
	{
		secondsSinceSaidReady += Time.deltaTime;
		wpos = transform.position; //For use in multithreaded city shenanigans

		if (enroute) {
			Vector2 delta = dest - transform.position;
			float pspeed = (speed * (hP / (float)maxHP)) * speedMod;
			transform.Translate(pspeed * Time.deltaTime * delta.normalized);

			if(delta.magnitude < stopDist) {
				if(path != null) { 
					if(currentPathNodeIndex < path.Length - 1) {
						currentPathNodeIndex++;
						enroute = true;
						dest = MapUtils.CoordsToPoint(path[currentPathNodeIndex]);
						speedMod = ((Map.ins.GetOriginalMap(path[currentPathNodeIndex]) == team) ? 1.5f : 0.8f);
						int et = Map.ins.GetPixTeam(path[^1]);
						if(Map.ins.GetPixTeam(path[currentPathNodeIndex]) != team) {
							ren.material.color = Map.ins.state_colors[team] + Color.white * 0.2f;
						}
						else {
							ren.material.color = Color.white;
						}
						int[] pas = ROE.Passables(team);
						
						if (!pas.Contains(et))
						{
							Idle();
						}
					}
					else {
						Idle();
					}
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
	void Idle() {
		enroute = false;
		Diplo.states[team].ReadyForOrders(this);
		secondsSinceSaidReady = 0;
	}

	public virtual void ShootUpdate() {
		Unit un = GetValidTarget();
		if (un == null) return;
		Vector2 delta = un.transform.position - transform.position;
		float hTime = Pool.ins.GetBullet().Fire(transform.position, delta, team);
		un.Hit(hTime);
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
		path = await Task.Run(() => AsyncPath.ins.Path(cpos, opos, pas.ToArray(), 2));
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
	}

	void Reset() {
		ROE.roeChange -= StaggerPathTargetCheck;
		DisplayHandler.resetGame -= Reset;
	}

	//private void OnTriggerEnter2D(Collider2D collision)
	//{
	//	if (!collision.CompareTag("Unit")) return;
	//	if (collision.gameObject.TryGetComponent(out Unit un)){
	//		if (un.team == team) return;
	//		if (inView.Contains(un)) return;
	//		inView.Add(un);
	//	}
	//}
	//private void OnTriggerExit2D(Collider2D collision)
	//{
	//	if (!collision.CompareTag("Unit")) return;

	//	if (collision.gameObject.TryGetComponent(out Unit un))
	//	{
	//		if (un.team == team) return;
	//		inView.Remove(un);
	//	}
	//}
}
