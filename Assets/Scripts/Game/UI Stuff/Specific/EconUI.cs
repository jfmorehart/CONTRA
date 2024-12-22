using System;
using TMPro;
using UnityEngine;
using static ConsolePanel;
public class EconUIPanel : MonoBehaviour
{
	public TMP_Text condition;
	public TMP_Text growth;
	public TMP_Text population;
	public TMP_Text army;
	public TMP_Text header;

	public TMP_Text debt;
	public TMP_Text payment; 
    public TMP_Text upkeep;
	public TMP_Text research;
	public TMP_Text budget;
	public TMP_Text total;

	public TMP_Text[] strs;

	public int numchars;
	// Update is called once per frame
	void Update()
    {
		State state = Diplomacy.states[UI.ins.targetNation];
		string rating = "";

		

		if (state.assesment.percentGrowth > 0) {

			rating = Colorize("fair", Color.white);

			if (state.assesment.percentGrowth > 0.3f)
			{
				rating =  Colorize("good", Color.green);
			}
			if (state.assesment.percentGrowth > 0.6f)
			{
				rating = Colorize("excellent", Color.blue);
			}
		}
		else {
			rating = Colorize("stagnant", Color.grey);

			if (state.assesment.percentGrowth < -0.3f)
			{
				rating = Colorize("poor", Color.red);
			}
			if (state.assesment.percentGrowth < -0.6f)
			{
				rating = Colorize("dire", Color.magenta);
			}
		}
		Economics.Assesment econ = Economics.RunAssesment(UI.ins.targetNation);
		condition.text = "Economy: " + rating;
		population.text = "Population: " + Map.ins.state_populations[UI.ins.targetNation] + "k";
		army.text = "Army: " + ArmyUtils.armies[UI.ins.targetNation].Count.ToString() + "k";
		header.text = ColoredName(UI.ins.targetNation);
		if(Diplomacy.states[UI.ins.targetNation].manHourDebt > 2) { 
			debt.text = "debt: " + Rounded(Diplomacy.states[UI.ins.targetNation].manHourDebt, 2) ;
			payment.text = "payment: <color=\"red\">" + Rounded(econ.debtPayment, 2) + "</color>";
		}
		else if(Diplomacy.states[UI.ins.targetNation].manHourDebt < -2) {
			debt.text = "surplus: " + Rounded(-1 * Diplomacy.states[UI.ins.targetNation].manHourDebt, 2);
			payment.text = "payment: " + "<color=\"green\"> " + Rounded(-1 * econ.debtPayment, 2) + "</color>";
		}
		else {
			debt.text = "";
			payment.text = "";
		}
	

		upkeep.text = "upkeep: <color=\"red\">" + Rounded(-1 * econ.upkeepCosts, 2) + "</color>";
		//constr.text = "construction: " + Rounded(econ.constructionCosts, 2);
		if(econ.researchBudget + econ.constructionCosts > 0) { 
			research.text = "r&d: " + "<color=\"red\">"+ Rounded(-1 *econ.researchBudget + -1 * econ.constructionCosts, 2) + "</color>";
		}
		else {
			research.text = "";
		}
		budget.text = "budget: " + "<color=\"green\">" + Rounded(econ.buyingPower + econ.debtPayment, 2) +  "</color>";
		if(econ.costOverrun > 0) {
			growth.text = "growth: " + "<color=\"red\">" + Rounded(state.assesment.percentGrowth * 100, 2) + "%" + "</color>";
			total.text = "total: " + "<color=\"red\">" + Rounded(-1 *econ.costOverrun, 2) + "</color>";
		}
		else {
			growth.text = "Growth: " + "<color=\"green\"> " + Rounded(state.assesment.percentGrowth * 100, 2) + "% " + "</color>";
			total.text = "total: " + "<color=\"green\">" + Rounded(-1 * econ.costOverrun, 2) + "</color>";
		}

		for(int i = 0; i < strs.Length; i++) {
			int tries = 0;
			while(LengthWithoutTags(strs[i].text) < numchars) {
				tries++;
				int ind = strs[i].text.IndexOf(' ');
				if (ind == -1) break;
				strs[i].text = strs[i].text.Insert(ind, " ");
				if (tries > 20) break;
			}

			Debug.Log(tries);
		}
	}

	string Rounded(float input, int decimals) {
		float scale = Mathf.Pow(10, decimals);
		float output = Mathf.Round(input * scale) / scale;
		return String.Format("{0:0.00}", output);// output.ToString();
	}

	public static string Colorize(string input, Color col)
	{
		return "<color=#" + ConsolePanel.ToHex(col) + ">" + input + "</color >"; 
	}
	int LengthWithoutTags(string str)
	{
		int add = 0;
		int l = 0;
		while (add < str.Length)
		{
			if (str[add] != '<')
			{
				l++;
				add++;
			}
			else
			{
				while (str[add] != '>')
				{
					add++;
					if (add >= str.Length) break;
				}
			}
		}
		if (l == add)
		{
			return l;
		}
		else
		{
			return l - 2;
		}

	}
}
