using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class CityCapturing : MonoBehaviour
{
	private void Start()
	{
		InvokeRepeating(nameof(DistributedIncrementCapture), 1, 0.25f);
	}

	public async void DistributedIncrementCapture() {
		for (int i = 0; i < Map.ins.numCities; i++)
		{
			City c = ArmyManager.ins.cities[i];
			if (c == null) continue;
			await Task.Run(() => c.IncrementalCapture());
		}
	}
}
