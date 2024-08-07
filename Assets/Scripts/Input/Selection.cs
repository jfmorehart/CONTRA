using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Selection : MonoBehaviour
{
	public List<Unit> selected;

	public float worldDragMin;
	Vector2 click1;
	Vector2 rclick1;

	public void Update()
	{
		if (Input.GetMouseButtonDown(0)) {
			LMBDown();
		}
		if (Input.GetMouseButtonUp(0)) {
			LMBUp();
		}
		if (Input.GetMouseButtonDown(1))
		{
			RMBDown();
		}
		if (Input.GetMouseButtonUp(1))
		{
			RMBUp();
		}

	}

	void LMBDown() {
		click1 = Input.mousePosition;

		if (!Input.GetKey(KeyCode.LeftShift)) {
			ClearSelected();
		}
	}
	void LMBUp()
	{
		Vector2 w1 = ScreenToWorld(click1);
		Vector2 w2 = ScreenToWorld(Input.mousePosition);

		if (Vector3.Distance(w1, w2) < worldDragMin) return;

		//BoxSearch
		AddToSelected(ArmyManager.ins.BoxSearch(w1, w2));

    }

	void RMBDown() {
		rclick1 = Input.mousePosition;
		int t1 = TeamAtScreenPoint(click1);
		int t2 = TeamAtScreenPoint(Input.mousePosition);

		Vector2Int co1 = MapUtils.PointToCoords(ScreenToWorld(click1));
		Vector2Int co2 = MapUtils.PointToCoords(ScreenToWorld(Input.mousePosition));

		int[] passableTeams = new int[1] { 0 };

		Vector2Int co3 = AsyncPath.ins.CheapestOpenNode(co1, co2, passableTeams, 2);

		//Debug.Log(co1 + " ")

		//PathFind.Path(co1, co2, passableTeams);
		//if (ROE.AreWeAtWar(t1, t2))
		//{
		//	ROE.MakePeace(t1, t2);
		//}
		//else
		//{
		//	ROE.DeclareWar(t1, t2);
		//}
	}
	void RMBUp() {
		Vector2 c1 = ScreenToWorld(rclick1);
		Vector2 c2 = ScreenToWorld(Input.mousePosition);

		if(Vector2.Distance(c1, c2) < worldDragMin || selected.Count < 2) { 
			DirectSelected(new Order(Order.Type.MoveTo, ScreenToWorld(Input.mousePosition)));
			return;
		}
		Vector2 axis = c1 - c2;
		float[] axisSort = new float[selected.Count];
		for (int i = 0; i < selected.Count; i++)
		{
			if (selected[i] == null) continue;
			axisSort[i] = Vector2.Distance(selected[i].transform.position, axis * 1000);
		}
		Unit[] sar = selected.ToArray();
		System.Array.Sort(axisSort, sar);
		selected = sar.ToList();
		for (int i = 0; i < selected.Count; i++) {
			float lp = i / ((float)selected.Count - 1);
			Vector2 pos = Vector2.Lerp(c1, c2, lp);
			Order o = new Order(Order.Type.MoveTo, pos);
			selected[i].Direct(o);
		}

	}

	void DirectSelected(Order order) { 
		foreach(Unit un in selected) {
			un.Direct(order);
		}
	}

	void ClearSelected() {
		for (int i =0; i < selected.Count; i++) {

			if (selected[i] != null) {
				selected[i].Deselect();
			}
		}
		selected.Clear();
    }
	void Deselect(Unit un) {
		un.Deselect();
    }

	void AddToSelected(Unit[] units)
	{
		for (int i = 0; i < units.Length; i++) {
			AddToSelected(units[i]);
		}
	}
	void AddToSelected(Unit un) {
		selected.Add(un);
		un.Select();
    }

	Vector3 ScreenToWorld(Vector3 mpos) {
		mpos.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
		return Camera.main.ScreenToWorldPoint(mpos);
	}

	int TeamAtScreenPoint(Vector3 mpos) {
		Vector3 wpos = ScreenToWorld(mpos);
		Vector2Int coords = MapUtils.PointToCoords(wpos);
		return (Map.ins.GetPixTeam(coords));
	}
}
