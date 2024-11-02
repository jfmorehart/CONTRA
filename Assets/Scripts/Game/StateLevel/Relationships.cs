using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Relationships : MonoBehaviour
{
	public static Relationships ins;
	public GameObject linePrefab;
	public GameObject dotPrefab;
	
	public List<GameObject> drawing;
	float drawDelay = 0.15f;
    float lastDraw = 0;
	public int focalNode = -1;

	public float sc1, sc2;
	private void Awake()
	{
		ins = this;
	}
	void Start(){
		drawing = new ();
	}
	// Update is called once per frame
	void Update()
	{
		if(UI.ins.currentMenu == UI.ins.menu_feelings || (UI.ins.currentMenu == UI.ins.menu_diplo)) {
			if (Time.time - lastDraw > drawDelay)
			{
				focalNode = UI.ins.targetNation;
				Draw();
				lastDraw = Time.time;
			}
		}

	}
	public void Clear() {
		foreach (GameObject go in drawing)
		{
			Destroy(go);
		}
		drawing.Clear();
	}
	public void Draw(){
		Clear();

		for(int i = 0; i < Map.ins.numStates; i++) {
			//Draw State Centers

			if (!Diplomacy.states[i].alive) continue;
			GameObject go = Instantiate(dotPrefab, transform);
			go.transform.position = (Vector3)MapUtils.CoordsToPoint(Map.ins.state_centers[i]) - Vector3.forward * 2;
			go.GetComponent<Renderer>().material = Map.ins.state_mats[i];
			go.transform.localScale *= sc1;
			drawing.Add(go);

			if (focalNode != 0 && focalNode != i) continue;
			for (int j = 0; j < Map.ins.numStates; j++)
			{
				//Draw RelationLines
				if (focalNode == 0 && j > 0) break;
				if (i == j) continue;
				if (!Diplomacy.states[j].alive) continue;
				GameObject line = Instantiate(linePrefab, transform);
				Vector3 otherpos = (Vector3)MapUtils.CoordsToPoint(Map.ins.state_centers[j]) - Vector3.forward;
				line.transform.position = Vector3.Lerp(otherpos, go.transform.position, 0.5f);
				Vector3 delta = otherpos - go.transform.position;

				line.transform.eulerAngles = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x));
				line.transform.localScale = new Vector3(delta.magnitude, sc2, 1);

				line.GetComponent<Renderer>().material.SetColor("_col1", Diplomacy.OpinionColor(j, i));
				line.GetComponent<Renderer>().material.SetColor("_col2", Diplomacy.OpinionColor(i, j));
				drawing.Add(line);
			}
		}
	}
}
