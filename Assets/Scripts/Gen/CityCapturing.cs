using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class CityCapturing : MonoBehaviour
{
	public static CityCapturing ins;
	public Unit[][] icprep;
	private void Start()
	{
		ins = this;
		InvokeRepeating(nameof(PrepForIC), 0.9f, 0.25f);
		InvokeRepeating(nameof(DistributedIncrementCapture), 1, 0.25f);
	}

	public async void DistributedIncrementCapture() {
		for (int i = 0; i < Map.ins.numCities; i++)
		{
			City c = ArmyManager.ins.cities[i];
			if (c == null) continue;
			await Task.Run(() => c.IncrementalCapture()); //asynchronous
		}
	}

	public void PrepForIC()
	{
		//this function is synchronous, and just creates copies of the data that
		// IncrementalCapture needs to stay unchanged while the thread chugs along
		icprep = new Unit[Map.ins.numStates][];
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			icprep[i] = ArmyUtils.armies[i].ToArray();
		}
	}
}
