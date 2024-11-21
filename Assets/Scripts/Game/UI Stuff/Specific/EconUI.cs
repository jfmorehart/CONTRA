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


	// Update is called once per frame
	void Update()
    {
		State state = Diplomacy.states[UI.ins.targetNation];
		string rating = "";
		growth.text = "Growth: " + Rounded(state.assesment.percentGrowth, 2);

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
		condition.text = "Economy: " + rating;
		population.text = "Population: " + Map.ins.state_populations[UI.ins.targetNation] + "k";
		army.text = "Army: " + ArmyUtils.armies[UI.ins.targetNation].Count.ToString() + "k";
		header.text = ColoredName(UI.ins.targetNation);
    }

	string Rounded(float input, int decimals) {
		float scale = Mathf.Pow(10, decimals);
		float output = Mathf.Round(input * scale) / scale;
		return output.ToString();
	}

	public static string Colorize(string input, Color col)
	{
		return "<color=#" + ConsolePanel.ToHex(col) + ">" + input + "</color >"; 
	}
}
