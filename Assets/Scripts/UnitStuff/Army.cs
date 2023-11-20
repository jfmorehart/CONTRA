using System.Collections;
using System.Collections.Generic;
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

	public override void Awake()
	{
		base.Awake();
		inView = new List<Unit>();
		lastShot = Random.Range(0, 1f);
	}

	public virtual void Update()
	{
		if (enroute) {
			Vector2 delta = dest - transform.position;
			float pspeed = speed * (hP / (float)maxHP);
			transform.Translate(pspeed * Time.deltaTime * delta.normalized);

			if(delta.magnitude < stopDist) {
				enroute = false;
				Diplo.states[team].ReadyForOrders(this);
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

	public void Setup(float st, int te) {
		strength = st;
		team = te;   
    }

	public override void Direct(Order order)
	{
		base.Direct(order);

		switch (order.type) {
			case Order.Type.MoveTo:
				enroute = true;
				dest = order.pos;
				break;
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
