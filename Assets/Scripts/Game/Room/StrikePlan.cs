using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static ArmyUtils;

public class StrikePlan : MonoBehaviour
{
	public static StrikePlan ins;

	public Camera strikeCam;
	public RectTransform backGround;
	public GameObject iconPrefab; //blank image
	public GameObject worldTargetPrefab;
	public GameObject worldLinePrefab;

	[SerializeField] Sprite siloSprite;
	[SerializeField] Sprite citySprite;
	[SerializeField] Sprite armySprite;

	public TMP_Text warning;
	public Color lineColor;
	public Color siloColor;
	public Color validTargetColor;
	public Color invalidTargetColor;

	public List<Transform> currentPlanIcons;

	float lastUpdate;
	public float refreshDelay;

	State_AI player;
	private void Awake()
	{
		ins = this;
		currentPlanIcons = new List<Transform>();
	}
	private void Start()
	{
		player = (Diplomacy.states[Map.localTeam] as State_AI);
	}
	private void Update()
	{
		if (UI.ins == null) return;
		if (UI.ins.currentMenu == UI.ins.menu_strike || UI.ins.currentMenu == UI.ins.menu_airdoctrine) {
			strikeCam.enabled = true;
		}
		else {
			strikeCam.enabled = false;
			return;
		}

	}
	List<Fighter> missionPlanes = new();
	public void DrawAirPlan() {
		Debug.Log("drawing air plan");
		ErasePlan();
		missionPlanes.Clear();

		Unit[] planes = ArmyUtils.GetAircraft(Map.localTeam);
		Debug.Log(planes.Length + " planes");
		if (planes.Length < 1)
		{
			warning.text = "No Bombers Remaining";
			return;
		}
		int numBombs = 0;

		for (int s = 0; s < planes.Length; s++)
		{
			Fighter f = (planes[s] as Fighter);
			int bombs = f.hasBombs ? 1 : 0;
			numBombs += bombs;
			if(bombs > 0 && f.target != Plane.NULLMission) {
				//tars.Add(f.target);
				missionPlanes.Add(f);
			}
		}
		if (missionPlanes.Count < 1)
		{
			warning.text = "All Bombers Re-Arming";
			return;
		}

		for(int i = 0; i < missionPlanes.Count; i++) {

			Debug.Log("drawing line");
			Vector2 local = MapPositionToLocalPosition(missionPlanes[i].target.wpos);
			Vector2 st = MapPositionToLocalPosition(missionPlanes[i].transform.position);
			DrawLine(st, local);
			Color c = Color.white;
			switch (missionPlanes[i].target.distance) {
				case Plane.AcceptableDistance.Bombtarget:
					c = Color.red;
					break;
				case Plane.AcceptableDistance.Waypoint:
					c = Color.green;
					break;
				case Plane.AcceptableDistance.Landing:
					c = Color.blue;
					break;
				case Plane.AcceptableDistance.Bogey:
					c = Color.red;
					break;

			}
			DrawWorldLine(missionPlanes[i].transform.position, missionPlanes[i].target.wpos, c);
		}
	}
	public void DrawPlan(int warheads, List<Target> targets) {
		ErasePlan();
		
		Silo[] silos = ArmyUtils.GetSilos(Map.localTeam);
		//putting this before the target count check is much slower, 
		// but its important to explain to the player
		if (silos.Length < 1)
		{
			warning.text = "No Silos Remaining";
			return;
		}
		int numMissiles = 0;
		for(int s = 0; s < silos.Length; s++) {
			numMissiles += silos[s].numMissiles;
		}
		if(numMissiles < 1) {
			warning.text = "All Silos Empty";
			return;
		}
		
		int n = Mathf.Min(targets.Count, warheads);
		n = Mathf.Min(n, numMissiles);
		Debug.Log("targets: " + targets.Count + " n: " + n);
		if (n == 0) {
			warning.text = "No Valid Targets Selected";
			return;
		}

		warning.text = "";
		//spawn icon of friendly silo
		Transform[] silo_icons = new Transform[silos.Length];
		for(int s = 0; s < silos.Length; s++) {
			GameObject go = Spawn(MapPositionToLocalPosition(silos[s].transform.position), siloSprite);
			silo_icons[s] = go.transform;
			go.GetComponent<Image>().color = siloColor;
		}

		int slcham = 0;
		int validTargetsDrawn = 0;

		int targetindex = -1;
		for (int i = 0; i < n; i++) {
			bool target_invalid;

			do
			{
				targetindex++; // weirdly makes sense here as long as its -1 to start
				if (targets.Count <= targetindex) {
					if(validTargetsDrawn == 0) {
						warning.text = "All selected targets have been fired at";
					}
					return;
				}
				ArmyUtils.Target trytarget = targets[targetindex];
				target_invalid = player.TargetInHash(trytarget.hash);
				if (target_invalid) {
					GameObject inv = Spawn(MapPositionToLocalPosition(trytarget.wpos), Tar2Sprite(trytarget.type)); ;
					inv.GetComponent<Image>().color = invalidTargetColor;
				}

			} while (target_invalid);
			ArmyUtils.Target target = targets[targetindex];
			Sprite toSpawn = Tar2Sprite(target.type);
			Vector2 local = MapPositionToLocalPosition(target.wpos);
			GameObject tOb = Spawn(local, toSpawn);
			GameObject ObReal = SpawnWorldTarget(target.wpos);
			tOb.GetComponent<Image>().color = validTargetColor;

			Vector2 st = MapPositionToLocalPosition(silos[slcham].transform.position);
			DrawLine(st, local);
			DrawWorldLine(silos[slcham].transform.position, target.wpos, lineColor);
			validTargetsDrawn++;
			slcham++;
			if (slcham >= silos.Length) slcham = 0;
		}
	}

	public void ErasePlan() { 
		for(int i = 0; i < currentPlanIcons.Count; i++) {
			Destroy(currentPlanIcons[i].gameObject);
		}
		currentPlanIcons.Clear();
    }
	void DrawLine(Vector2 start, Vector2 end) {
		GameObject go = Instantiate(iconPrefab, backGround);
		currentPlanIcons.Add(go.transform);
		go.transform.localPosition = (start + end) * 0.5f;

		Vector2 delta = end - start;
		float theta = Mathf.Atan2(delta.y, delta.x);
		go.transform.eulerAngles = new Vector3(0, 0, theta * Mathf.Rad2Deg);
		go.transform.localScale = new Vector3(delta.magnitude * 0.01f, 0.1f, 1);

		go.GetComponent<Image>().color = lineColor;
    }
	void DrawWorldLine(Vector2 start, Vector2 end, Color col) {
		GameObject go = Instantiate(worldLinePrefab);
		currentPlanIcons.Add(go.transform);
		go.transform.position = (start + end) * 0.5f;

		Vector2 delta = end - start;
		float theta = Mathf.Atan2(delta.y, delta.x);
		go.transform.eulerAngles = new Vector3(0, 0, theta * Mathf.Rad2Deg);
		go.transform.localScale = new Vector3(delta.magnitude, 2f, 1);

		Material renmat = go.GetComponent<Renderer>().material;
		renmat.SetColor("_Color", col);
		//renmat.color = col;
	}


	GameObject Spawn(Vector2 localPosition, Sprite sprite) {
		GameObject go = Instantiate(iconPrefab, backGround);
		go.transform.localPosition = localPosition;

		currentPlanIcons.Add(go.transform);
		go.GetComponent<Image>().overrideSprite = sprite;
		return go;
	}

	GameObject SpawnWorldTarget(Vector2 worldPos) {
		GameObject go = Instantiate(worldTargetPrefab);
		go.transform.position = new Vector3(worldPos.x, worldPos.y, -1);
		currentPlanIcons.Add(go.transform);
		return go;
	}

	Vector2 MapPositionToLocalPosition(Vector2 mapPos) {
		Vector2Int coords = MapUtils.PointToCoords(mapPos);
		Vector2 point = Vector2.zero;
		point.x = coords.x / (float)Map.ins.texelDimensions.x;
		point.y = coords.y / (float)Map.ins.texelDimensions.y;
		point -= Vector2.one * 0.5f;
		point *= backGround.sizeDelta * 0.8f;

		return point;
	}

	Sprite Tar2Sprite(Tar tar) {
		switch (tar) {
			case Tar.Civilian:
				return citySprite;
			case Tar.Conventional:
				return armySprite;
			case Tar.Nuclear:
				return siloSprite;
			case Tar.Support:
				Debug.LogError("support?");
				return siloSprite;
		}
		return null;
    }
}
