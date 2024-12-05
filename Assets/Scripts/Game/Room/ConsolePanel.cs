using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;

public class ConsolePanel : TypingInterface
{
	public static ConsolePanel ins;
	public Transform center;
	public TMP_Text textPrefab;

	public struct Logline{
		public string text;
		public int mult;
		public float startTime;
		public float lifeTime;
		public Logline(string l, int m, float start, float life) {
			text = l;
			mult = m;
			startTime = start;
			lifeTime = life;
		}
	}
	//public static List<Logline> lines;
	//public TMP_Text[] spaces;
	//public static int lines.Length = 5;

	public Vector2 offset;

	const string carat = "<color=\"yellow\"> > </color>";
	const string greencarat = "<color=\"green\"> > </color>";
	public const string you = "<color=\"red\"> you </color>";

	public TMP_Text toolhead;
	public TMP_Text tooltext;

	public bool toolTipLockout;

	public override void Awake()
	{
		lockout = true;
		base.Awake();
		ins = this;
	}
	private void Start()
	{
		//ins.WriteOut(ColoredText(0, "123456789012345678901234567890123456"));
		//ins.WriteOut(ColoredText(0, "1234567890123456789012345678901234567"));
		//ins.WriteOut(ColoredText(0, "12345678901234567890123456789012345678"));
		//ins.WriteOut(ColoredText(0, "123456789012345678901234567890123456789"));
	}
	public override void Update()
	{
		RefreshTooltip();
		base.Update();
	}
	void RefreshTooltip()
	{
		if (toolTipLockout) return;
		if (UI.ins.selected > UI.ins.currentMenu.children.Length) return;
		if (UI.ins.currentMenu.children[UI.ins.selected] == null) return;
		toolhead.text = UI.ins.currentMenu.children[UI.ins.selected].tooltip_headerText;
		tooltext.text = UI.ins.currentMenu.children[UI.ins.selected].tooltip_bodyText;
	}

	public static void Log(string str, float lifeTime = 10) {
		//too tired to go replace all of the references to lifetime

		if (ins.lines.Length > 0)
		{
			if (str.Contains('\n'))
			{
				str = str.Remove('\n');
			}
			if (str[str.Length - 1] == ' ') {
				str = str.Remove(str.Length - 1);
			}

			int repeatCheck = LineThatContains(str);
			if (repeatCheck == -1)
			{	if (str.Length < 1) return;
				if(lifeTime == Mathf.Infinity) {
					ins.WriteOut(str, true, true, true); //green, instant, permanent
				}
				else {
					ins.WriteOut(str);
				}
			}
			else
			{
				//increment counter
				if (ins.lines[repeatCheck].Contains(" X"))
				{
					int ind = ins.lines[repeatCheck].IndexOf(" X");
					string sub = ins.lines[repeatCheck].Substring(ind);
					string num = sub.Replace(" X", "");
					Int32.TryParse(num, out int repetitions);
					//Debug.Log(repetitions);
					repetitions++;
					ins.lines[repeatCheck] = ins.lines[repeatCheck].Replace(sub, " X" + repetitions);
					ins.lines[repeatCheck] += '\n';
				}
				else
				{
					ins.lines[repeatCheck] += " X2";
					if (ins.lines[repeatCheck].Contains('\n'))
					{

						ins.lines[repeatCheck] = ins.lines[repeatCheck].Replace('\n', ' ');
						//Debug.Log("found and removed, remaining: " + ins.lines[repeatCheck]);
						ins.lines[repeatCheck] += '\n';
					}

					//Debug.Log("x2");
				}
			}
		}

		//lines.Add(new Logline(str, 1, Time.time, lifeTime));
		//if(lines.Count > lines.Length) {
		//	for(int i = 0; i < lines.Count; i++) {
		//		if (lines[i].lifeTime == Mathf.Infinity) {
		//			//Do not remove this line, its permanent
		//		}
		//		else {
		//			lines.RemoveAt(i);
		//			break;
		//		}
		//		if(i == lines.Count - 1) lines.RemoveAt(i); //dont spawn the new one after all
		//	}

		//}
	}
	public static string ColoredText(int team, string message) {
		
		return "<color=#" + ToHex(Map.ins.state_colors[team]) + ">" + message + "</color >";

	}
	public static string ColoredName(int team) {
		if(team == Map.localTeam) {
			return "<color=#" + ToHex(Map.ins.state_colors[team]) + ">" + "you" + "</color >";
		}
		return "<color=#" + ToHex(Map.ins.state_colors[team]) + ">" + Diplomacy.state_names[team] + "</color >";
	}

	public static int LineThatContains(string str) { 
		for(int i =0; i < ins.lines.Length; i++) {
			if (ins.lines[i].Contains(str)) return i;
		}
		return -1;
	}

	public static void Clear() {
		ins.ClearConsole();
		//ins.lines.Clear();
		//for (int i = 0; i < lines.Count; i++)
		//{
		//	lines.RemoveAt(0);
		//}
	}

	public static string ToHex(Color c) {
		return ColorUtility.ToHtmlStringRGB(c);
    }
	//=> $"#{c.r:X2}{c.g:X2}{c.b:X2}";
}
