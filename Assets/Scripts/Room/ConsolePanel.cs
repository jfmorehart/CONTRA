using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class ConsolePanel : MonoBehaviour
{
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
	public static List<Logline> lines;
	public TMP_Text[] spaces;
	public static int linecount = 8;

	public Vector2 offset;

	const string carat = "<color=\"yellow\"> > </color>";
	const string greencarat = "<color=\"green\"> > </color>";
	public const string you = "<color=\"red\"> you </color>";

	private void Start()
	{
		spaces = new TMP_Text[linecount];
		lines = new List<Logline>();

		for (int i = 0; i < linecount; i++) {
			spaces[i] = Instantiate(textPrefab, center.parent);
			if(i % 2 > 0) {
				spaces[i].text = carat + " Deterrence is the art of producing in the mind of the enemy the <color=\"red\"> FEAR </color> to attack";
			}
			else {
				spaces[i].text = carat + "The international communist plot to...";
			}
			
			spaces[i].transform.position = center.transform.position;
			spaces[i].transform.Translate(i * offset, Space.World);
		}
	}

	void Update()
	{
		List<Logline> expired = new List<Logline>();
		for(int i = 0; i < lines.Count; i++) { 
			if(Time.time - lines[i].startTime > lines[i].lifeTime) {
				expired.Add(lines[i]);
			}
		}
		foreach(Logline log in expired) {
			lines.Remove(log);
		}


		for (int i = 0; i < linecount; i++)
		{
			string line = "";
			if (lines.Count > i)
			{
				if (i == lines.Count - 1)
				{
					line = greencarat + lines[i].text;
				}
				else
				{
					line = carat + lines[i].text;
				}
				if (lines[i].mult > 1)
				{
					line += " x" + lines[i].mult;
				}
			}

			spaces[i].text = line;

		}
	}

	public static void Log(string str, float lifeTime = 10) {
		if(lines.Count > 0) {
			int repeatCheck = LineThatContains(str);
			if (repeatCheck != -1)
			{
				lines[repeatCheck] = new Logline(lines[repeatCheck].text, lines[repeatCheck].mult + 1, Time.time, lines[repeatCheck].lifeTime);
				return;
			}
		}

		lines.Add(new Logline(str, 1, Time.time, lifeTime));
		if(lines.Count > linecount) {
			lines.RemoveAt(0);
		}
	}

	public static string ColoredName(int team) {
		if(team == 0) {
			return "<color=#" + Map.ins.state_colors[team].ToHexString() + ">" + "you" + "</color >";
		}
		return "<color=#" + Map.ins.state_colors[team].ToHexString() + ">" + Diplomacy.state_names[team] + "</color >";
	}

	public static int LineThatContains(string str) { 
		for(int i =0; i < lines.Count; i++) {
			if (lines[i].text.Contains(str)) return i;
		}
		return -1;
	}
}
