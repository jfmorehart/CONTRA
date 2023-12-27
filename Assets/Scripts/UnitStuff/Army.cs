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
	Vector3 dest;
	bool enroute;
	float stopDist = 1f;

	List<Unit> inView;

	public float reload;
	float lastShot;

	public Vector2Int[] path;
	int current;
	public Order pathOrder;

	[HideInInspector]
	public Vector2 wpos;

	public override void Awake()
	{
		base.Awake();
		inView = new List<Unit>();
		lastShot = Random.Range(0, 1f);
		ROE.roeChange += StaggerTargetCheck;
	}

	public virtual void Update()
	{

		wpos = transform.position; //For use in multithreaded city shenanigans

		if (enroute) {
			Vector2 delta = dest - transform.position;
			float pspeed = speed * (hP / (float)maxHP);
			transform.Translate(pspeed * Time.deltaTime * delta.normalized);

			if(delta.magnitude < stopDist) {
				enroute = false;
				if(path != null) { 
					if(current < path.Length - 1) {
						current++;
						enroute = true;
						dest = MapUtils.CoordsToPoint(path[current]);
					}
					else {
						Diplo.states[team].ReadyForOrders(this);
					}
				}
				else {
					Diplo.states[team].ReadyForOrders(this);
				}

			}
		}

		if(Time.time - lastShot > reload) {
			lastShot = Time.time;
			ShootUpdate();
		}
	}

	public virtual void ShootUpdate() {
		List<Unit> uns = ValidTargets();
		if (uns.Count < 1) return;
		int i = Random.Range(0, uns.Count);
		Vector2 delta = uns[i].transform.position - transform.position;
		float hTime = Pool.ins.GetBullet().Fire(transform.position, delta, team);
		uns[i].Hit(hTime);
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
			current = 1;
			dest = MapUtils.CoordsToPoint(path[current]);
		}
	}

	//Used so that not every army dude does this check on the same frame
	void StaggerTargetCheck()
	{
		if (!enroute) return;
		Invoke(nameof(CheckTargetValidity), Random.Range(0f, 0.5f));
	}

	void CheckTargetValidity() { 
		if (!enroute) return;
		if(path != null) {
			int et = Map.ins.GetPixTeam(MapUtils.PointToCoords(path[^1]));
			if (!ROE.Passables(team).Contains(et)) {
				enroute = false;
			}
		}
    }

	List<Unit> ValidTargets() 
    {
		CleanViewList();
		List<Unit> uns = new List<Unit>();
		for(int i = 0; i < inView.Count; i++) { 
			if(ROE.AreWeAtWar(team, inView[i].team)) {
				uns.Add(inView[i]);
			}
		}
		return uns;
    }

	void CleanViewList() {
		List<Unit> clear = new List<Unit>();
		for(int i = 0; i< inView.Count; i++) {
			if (inView[i] == null) {
				clear.Add(inView[i]);
				continue;
			}
		}

		for(int i = 0; i< clear.Count; i++) {
			inView.Remove(clear[i]);
		}
    }

	public override void Kill()
	{
		base.Kill();
		ROE.roeChange -= StaggerTargetCheck;
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (!collision.CompareTag("Unit")) return;
		if (collision.gameObject.TryGetComponent(out Unit un)){
			if (un.team == team) return;
			if (inView.Contains(un)) return;
			inView.Add(un);
		}
	}
	private void OnTriggerExit2D(Collider2D collision)
	{
		if (!collision.CompareTag("Unit")) return;

		if (collision.gameObject.TryGetComponent(out Unit un))
		{
			if (un.team == team) return;
			inView.Remove(un);
		}
	}
}
