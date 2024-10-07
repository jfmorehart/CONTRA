using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : Construction
{
	public GameObject iconPrefab;
	public List<GameObject> icons;
	public Vector2 offset;
	public float reloadTime;
	float lastReload;

	public Vector2Int mapPos;

	public override void Awake()
	{
		icons = new List<GameObject>();
		base.Awake();
		mapPos = MapUtils.PointToCoords(transform.position);
		manHoursRemaining = 0;
	}

	public override void Update()
	{
		if (CanReload() && Time.time - lastReload > reloadTime)
		{
			lastReload = Time.time;
			Reload();
		}
	}
	protected virtual void Reload() { 
    
    }
	protected virtual bool CanReload() {
		return false;
    }

	protected void UpdateIconDisplay(int numIcons)
	{
		for (int i = 0; i < icons.Count; i++)
		{
			Destroy(icons[i]);
		}
		icons.Clear();

		for (int i = 0; i < numIcons; i++)
		{
			float flop = ((i % 2) == 0) ? 1 : -1;
			Vector2 pos = (Vector2)transform.position + offset + Vector2.right * flop * (i + 0.5f) * 3;
			icons.Add(Instantiate(iconPrefab, pos, transform.rotation, transform));
		}
	}

	public override void Kill()
	{
		for (int i = 0; i < icons.Count; i++)
		{
			Destroy(icons[i]);
		}
		icons.Clear();
		base.Kill();
	}
}
