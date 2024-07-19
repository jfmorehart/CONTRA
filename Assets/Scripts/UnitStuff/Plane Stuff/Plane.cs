using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Plane : Unit
{

	public float speed;
	public float turnRate;
	public float fuel;
	public bool bingo;

	public Airbase homeBase;
	public Mission target;

	public ATAM incoming;

	bool defensive;
	float defensiveTime = 3;

	//setting this to true keeps trying to turn into the target
	//even if they're within the minimum turning radius of the plane
	protected bool rateFight = false; 

	float turnCircleRadius = 60;

	////for turn circle tests
	//public GameObject testPrefab;
	//public GameObject turnDude;
	//public GameObject turnDude2;

	public static readonly Mission NULLMission = new Mission(Vector2.zero, AcceptableDistance.None);
	public struct Mission {
		public Vector2 wpos;
		public AcceptableDistance distance;
		public Plane bogey;
		public float value; //may reaquire if they find a better target
		public Mission(Vector2 pos, AcceptableDistance acd, Plane bg = null, float val = -1) {
			wpos = pos;
			distance = acd;
			bogey = bg;
			value = val;
		}
    }
	public enum AcceptableDistance { 
		//the int values are the acceptable distances
		None = 0,
		Landing = 25, 
		Waypoint = 80, 
		Bogey = 5,
		Bombtarget = 15
    }

	public override void Awake()
	{
		useChunkSystem = false;
		base.Awake();
	}
	public override void Start()
	{
		base.Start();
		DisplayHandler.resetGame += Reset;

		TrailRenderer tren = GetComponent<TrailRenderer>();
		Material tmat = new Material(tren.material);
		tmat.color = Map.ins.state_colors[team];
		tren.material = tmat;
		GetComponent<SpriteRenderer>().color = Map.ins.state_colors[team] + new Color(0.3f, 0.3f, 0.3f);

		fuel *= Random.Range(0.7f, 1.3f);

		//turnDude = Instantiate(testPrefab);
		//turnDude2 = Instantiate(testPrefab);
	}

	// Update is called once per frame
	public virtual void Update()
	{

		////turn circle test
		//turnDude.transform.position = transform.position - transform.right * turnCircleRadius;
		//turnDude2.transform.position = transform.position + transform.right * turnCircleRadius;
		//transform.Rotate(Vector3.forward, turnRate * Time.deltaTime);
		//transform.Translate(speed * Time.deltaTime * transform.up, Space.World);
		//return;

		if (fuel < 0) Kill();

		if (target.distance == AcceptableDistance.Landing)
		{
			if (homeBase == null)
			{
				bingo = false;
			}
		}

		transform.Translate(speed * Time.deltaTime * transform.up, Space.World);
		fuel -= Time.deltaTime;

		if ((bingo || !FuelCheck()) && homeBase != null)
		{
			target = new Mission(homeBase.transform.position, AcceptableDistance.Landing);
		}

		if (incoming != null) {
			if(Vector2.Angle(incoming.transform.position - transform.position, transform.up) < 180) {
				
				if (defensive) {
					if (defensiveTime < 0)
					{
						//time to go aggressive
						defensive = false;
						defensiveTime += Time.deltaTime;
					}
					else
					{
						//stay defensive
						TurnAwayFrom(incoming.transform.position);
						defensiveTime -= Time.deltaTime;
						defensive = true;
						return;
					}
				}
				else { 
					if(defensiveTime > 2) {
						//go defensive
						TurnAwayFrom(incoming.transform.position);
						defensiveTime -= Time.deltaTime;
						defensive = true;
						return;
					}
					else {
						//stay aggressive
						defensiveTime += Time.deltaTime;
					}
				}
			
			}
		}
		else {
			defensiveTime = 3;
			defensive = false;
		}
		if (target.distance == AcceptableDistance.None || Vector2.Distance(target.wpos, Vector2.zero) < 0.1f) {
			//these are indicators of no target
			Idle();
		}
		else {
			//fly to target
			DirectFlight();
		}
	}
	protected virtual void Idle(){
		target = new Mission(homeBase.transform.position, AcceptableDistance.Landing);
	}

	public virtual void SmokeInTheAir(ATAM atam) {
		incoming = atam;
    }

	protected void DirectFlight() {
		if((Vector2.Distance(target.wpos, Vector2.zero) < 0.1f)){
			Debug.Log("zeroed out");
		}
		Vector2 dv = target.wpos - (Vector2)transform.position;
		if (dv.magnitude < (int)target.distance)
		{
			if (target.distance == AcceptableDistance.Landing)
			{
				Land();
			}
			else
			{
				ArrivedOverTarget();
			}
		}
		if (TargetInsideMyTurn(target.wpos) && !rateFight) {
			TurnAwayFrom(target.wpos);
		}
		else { 
			TurnTowards(target.wpos);
		}
	}
	protected virtual void ArrivedOverTarget() {
		target.wpos = Vector2.zero;
	}
	protected void TurnTowards(Vector2 point) {

		Vector2 dv = point - (Vector2)transform.position;
		float dev = Vector2.SignedAngle((Vector2)transform.up, dv);
		if (dev > 0)
		{
			transform.Rotate(Vector3.forward, turnRate * Time.deltaTime);
		}
		else
		{
			transform.Rotate(-Vector3.forward, turnRate * Time.deltaTime);
		}
	}
	protected void TurnAwayFrom(Vector2 point)
	{

		Vector2 dv = point - (Vector2)transform.position;
		float dev = Vector2.SignedAngle((Vector2)transform.up, dv);
		if (dev < 0)
		{
			transform.Rotate(Vector3.forward, turnRate * Time.deltaTime);
		}
		else
		{
			transform.Rotate(-Vector3.forward, turnRate * Time.deltaTime);
		}
	}
	bool TargetInsideMyTurn(Vector2 point) {
		//left circle
		Vector2 leftPoint = transform.position - transform.right * turnCircleRadius;
		if (Vector2.Distance(point, leftPoint) < turnCircleRadius) return true;

		//right circle
		Vector2 rightPoint = transform.position + transform.right * turnCircleRadius;
		if (Vector2.Distance(point, rightPoint) < turnCircleRadius) return true;

		return false;
	}

	public bool FuelCheck() {
		if(homeBase == null)
		{
			bingo = true;
			return false;
		}
		Vector2 dh = homeBase.transform.position - transform.position;
		//2πr that π character probablys gonna brick the game
		if (dh.magnitude + 400 < fuel * speed) {
			bingo = false;
			return true;
		}
		bingo = true;
		return false;
	}
	public override void Kill()
	{
		base.Kill();
	}

	public virtual void Land() {
		if(homeBase != null) {
			homeBase.LandAircraft(this);
			Kill();
		}
		else {
			bingo = false;
			target = Plane.NULLMission;
		}
    }

	void Reset()
	{
		DisplayHandler.resetGame -= Reset;
	}
}
