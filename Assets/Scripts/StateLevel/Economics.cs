using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Economics
{
	public static float buyingPowerPerPopulation = 1f;

	public static float cost_siloUpkeep = 20f;
	public static float cost_armyUpkeep = 2;
	public static float cost_armySpawn = 5;

	public static float maxPowerPerSite = 30;

	public static int[][] state_recent_growth;
	//x array is time, y array is state

	const int numStoredTicks = 16; // size of x array
	public static int overwriteIndex; //last overwritten timeslot

	public static Assesment[] state_assesments;

	public const float tickDiff = 1 / (float)numStoredTicks;

	public static void SetupEconomics() {
		state_assesments = new Assesment[Map.ins.numStates];
		state_recent_growth = new int[numStoredTicks][];  //store 16 sets of data
		for (int i = 0; i < numStoredTicks; i++) {
			//store one int for each state per set
			state_recent_growth[i] = new int[Map.ins.numStates];
		}
    }
	public static int[] NewGrowthTick() {
		int[] newgrowth = new int[Map.ins.numStates];
		for (int i = 0; i < Map.ins.numStates; i++)
		{
			float pctg = Mathf.Pow(Mathf.Abs(state_assesments[i].percentGrowth), 0.5f);
			pctg *= Mathf.Sign(state_assesments[i].percentGrowth);
			float rta = RecentTickAvg(i);

			//only grow if growing would not exceed the desired avg
			if (pctg > rta + tickDiff) { 
				newgrowth[i] = 1;
			}
			else if(pctg < rta - tickDiff){
				//only shrink if growing would not subceed the desired avg
				newgrowth[i] = -1;
			}
			else {
				//so pretty much just trend towards zero growth 
				newgrowth[i] = 0;
			}
		}

		//overwrite state_growth_recent data
		state_recent_growth[overwriteIndex] = newgrowth; //weirdly elegant
		overwriteIndex++;
		if (overwriteIndex >= numStoredTicks) overwriteIndex = 0;

		/*
		string debug = "";
		for (int i = 0; i < numStoredTicks; i++)
		{
			debug += "\n";
			for (int j = 0; j < Map.ins.numStates; j++)
			{
				debug += state_recent_growth[i][j].ToString();
			}
		}
		Debug.Log(debug);
		*/

		return newgrowth;
	}

	public static float RecentTickAvg(int team) {
		float avg = 0;
		for(int i = 0; i < numStoredTicks; i++) {
			avg += state_recent_growth[i][team];
		}
		return avg / (float)numStoredTicks;
	}

	public static Assesment RunAssesment(int team) {
		State state = Diplomacy.states[team];

		//Where we get our money
		float gross = buyingPowerPerPopulation * Map.ins.state_populations[team];
		//todo add in trade

		//Debt is used to hamstring an economy after over-conscription
		//spread out debt payment
		float debtPayment = Mathf.Min(Diplomacy.states[team].manHourDebt * 0.3f, gross * 0.2f);
		float buyingPower = gross - debtPayment;

		Diplomacy.states[team].manHourDebt -= debtPayment;


		//despite this split, unused military budget will return to net;
		float militaryBuyingPower = state.econ_military_max * buyingPower;

		//calculate unit upkeep costs
		float upkeep = 0;
		Unit[] allunit = ArmyUtils.AllUnitInventory(team);
		for(int i = 0; i < allunit.Length; i++) {
			upkeep += allunit[i].upkeepCost;
		}

		//negative is military surplus, used for construction and unit aquisition
		float overrun = upkeep - militaryBuyingPower;


		//Find remaining power for new construction
		float conPower = Mathf.Max(0, militaryBuyingPower - upkeep);

		//Construction — allocated last
		float demand = state.construction_sites.Count * maxPowerPerSite;
		float usage;
		if(demand > 0) {
			usage = 1;//  Mathf.Min(conPower / demand, 1); //construction no longer limited by surplus
		}
		else {
			usage = 0;
		}
		float totalConstructionCosts = usage * demand;

		overrun += totalConstructionCosts; //subtract construction from surplus

		float manHoursPerSite = totalConstructionCosts / (state.construction_sites.Count + 0.01f);

		float net = (buyingPower - upkeep) - totalConstructionCosts;
		float percentGrowth = net / gross; //used for growing the country

		if(buyingPower > gross) {
			//re-credit surplus money
			Diplomacy.states[team].manHourDebt -= (buyingPower - gross) * 0.8f;
		}

		return new Assesment(buyingPower, upkeep, overrun, totalConstructionCosts, net, usage,manHoursPerSite, percentGrowth);
    }

	public struct Assesment {

		public float buyingPower; //total money per tick

		public float upkeepCosts; // total money spent on upkeep (necessary)
		public float costOverrun; // if upkeep > buying power, this will be the difference

		public float constructionCosts; // total money spent on new buildings (scalable)
		public float net; // amount left over for funding domestic growth

		public float constructionPercentSpeed; // how quickly buildings will build (0 to 1)
		public float manHoursPerSite; // capped at 20 per tick

		public float percentGrowth; // (net / gross= used for growth metrics

		public Assesment(float buying, float upkeep, float over, float constr, float ne, 
	    float conperspeed, float manHoursPS, float pctG) {

			buyingPower = buying;
			upkeepCosts = upkeep;
			costOverrun = over;
			constructionCosts = constr;
			net = ne;

			constructionPercentSpeed = conperspeed;
			manHoursPerSite = manHoursPS;
			percentGrowth = pctG;
		}
    }
}
