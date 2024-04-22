using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Economics
{
	public static float buyingPowerPerPopulation = 1f;
	public static float cost_siloUpkeep = 10f;
	public static float cost_armyUpkeep = 1;
	public static float maxPowerPerSite = 20;

	public static Assesment RunAssesment(int team) {
		State state = Diplo.states[team];
		float buyingPower = buyingPowerPerPopulation * Map.ins.state_populations[team];

		//despite this split, unused military budget will return to net;
		float militaryBuyingPower = state.econ_military_max * buyingPower;

		//calculate unit and silo upkeep
		float siloUpkeep = cost_siloUpkeep * ArmyUtils.GetSilos(team).Length;
		float upkeep = siloUpkeep + cost_armyUpkeep * ArmyUtils.GetArmies(team).Length;

		//negative is military surplus, used for construction and unit aquisition
		float overrun = upkeep - militaryBuyingPower;

		//Find remaining power for new construction
		float conPower = Mathf.Max(0, militaryBuyingPower - upkeep);

		//Construction — allocated last
		float demand = state.construction_sites.Count * maxPowerPerSite;
		float usage;
		if(demand > 0) {
			usage = Mathf.Min(conPower / demand, 1);
		}
		else {
			usage = 0;
		}
		float totalConstructionCosts = usage * demand;

		overrun += totalConstructionCosts; //subtract construction from surplus

		float manHoursPerSite = totalConstructionCosts / (state.construction_sites.Count + 0.01f);

		float net = (buyingPower - upkeep) - totalConstructionCosts;
		return new Assesment(buyingPower, upkeep, overrun, totalConstructionCosts, net, usage,manHoursPerSite );
    }

	public struct Assesment {

		public float buyingPower; //total money per tick

		public float upkeepCosts; // total money spent on upkeep (necessary)
		public float costOverrun; // if upkeep > buying power, this will be the difference

		public float constructionCosts; // total money spent on new buildings (scalable)
		public float net; // amount left over for funding domestic growth

		public float constructionPercentSpeed; // how quickly buildings will build (0 to 1)
		public float manHoursPerSite; // capped at 20 per tick

		public Assesment(float buying, float upkeep, float over, float constr, float ne, 
	    float conperspeed, float manHoursPS) {

			buyingPower = buying;
			upkeepCosts = upkeep;
			costOverrun = over;
			constructionCosts = constr;
			net = ne;

			constructionPercentSpeed = conperspeed;
			manHoursPerSite = manHoursPS;
		}
    }
}
