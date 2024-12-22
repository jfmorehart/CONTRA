using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThinPanel : MonoBehaviour
{
    public GameObject idleScreen;
    public GameObject threatScreen;

    public Image background;
    public Color sineColor;
    public float sineFreq, sineAmp;

    public TMP_Text threat;
    public string plainText;
    int PT_index;
	int maxChars = 30;
    public float lastLetterTime, letterDelay, threatLetterDelay, warningLetterDelay;
	public Color threatCol;
	public Color warningCol;

	float lastStatusUpdate;
	float statusUpdateDelay = 12;
	float statusLength = 8;
	bool showingstatus;

	private void Start()
	{
		if (Simulator.activeScenario.tutorial == 1)
		{
            GetComponent<TimePanel>().timer = float.MaxValue;
			idleScreen.SetActive(false);
			threatScreen.SetActive(false);
			//Destroy(gameObject);
		}
	}
	// Update is called once per frame
	void Update()
    {
		if (Simulator.activeScenario.tutorial == 1) return;

		if(Time.timeScale == 0) {
			idleScreen.SetActive(false);
			threatScreen.SetActive(false);
			background.color = Color.black;
			return;
		}
		if (UI.ins.incomingMissiles > 0) {
			sineColor = Color.red;
            if (!threatScreen.activeInHierarchy || showingstatus) {
				threat.text = "             ";
				PT_index = 0;
				maxChars = 30;
				plainText = "/incoming nuclear threat/";
				showingstatus = false;
				letterDelay = warningLetterDelay;
				threat.color = threatCol;
				idleScreen.SetActive(false);
				threatScreen.SetActive(true);
			}
		}
		else {

			if (Time.time - lastStatusUpdate > statusUpdateDelay)
			{
				Debug.Log("status check!");
				plainText = CreateStatusMessage();
				threat.color = warningCol;
				letterDelay = warningLetterDelay;
				lastStatusUpdate = Time.time;
				PT_index = 0;
				threat.text = "             ";
				Debug.Log("status recieved = " + plainText);
				if (plainText != " ")
				{
					Debug.Log("now showing status!");
					showingstatus = true;
				}
			}

			if (!Research.currentlyResearching[0].Equals(-Vector2Int.one))
			{
				maxChars = 10;
				sineColor = Color.green * 0.3f;
				if (!idleScreen.activeInHierarchy)
				{
					idleScreen.SetActive(true);
					//threatScreen.SetActive(false);
				}
			}
			else {
				maxChars = 30;
				sineColor = Color.black;
				if (idleScreen.activeInHierarchy)
				{
					idleScreen.SetActive(false);
				}
			}

			if (showingstatus)
			{
				if (!threatScreen.activeInHierarchy)
				{
					Debug.Log("enabling threat screen");
					threatScreen.SetActive(true);
				}
				if (Time.time - lastStatusUpdate > statusLength)
				{
					Debug.Log("disabling status message");
					showingstatus = false;
				}
			}
			else if (threatScreen.activeInHierarchy)
			{
				Debug.Log("status disabled.");
				//idleScreen.SetActive(true);
				threatScreen.SetActive(false);
			}

		}


        float sine = (Mathf.Sin(Time.time * sineFreq) * 0.5f + 1) * sineAmp;
        background.color = Color.Lerp(Color.black, sineColor, sine);

        if(Time.time - lastLetterTime > letterDelay) {
            lastLetterTime = Time.time;
            AddLetter();
        }
    }

    void AddLetter() {
		if (!showingstatus) {
			if (PT_index >= plainText.Length) PT_index = 0;
			threat.text += plainText[PT_index];
		}
		else { 
			//only play them once
			if(PT_index >= plainText.Length) {
				threat.text += ' ';
			}
			else {
				threat.text += plainText[PT_index];
			}
		}
        PT_index++;
        if(threat.text.Length >= maxChars) {
			threat.text = threat.text.Remove(0, (threat.text.Length - maxChars) + 1);
		}
    }

	string CreateStatusMessage() {
	
		float gthreat = 0;
		float nukethreat = 0;
		float airthreat = 0;
		for(int i = 0; i < Map.ins.numStates; i++) {
			if (i == Map.localTeam) continue;
			if (!IsBot(i)) continue;
			State_Enemy sten = Diplomacy.states[i] as State_Enemy;
			float pwar = ProbabilityOfWar(Map.localTeam, i);
			gthreat += sten.ArmiesReadyOnFront(Map.localTeam) * pwar;
			nukethreat += ArmyUtils.silos[i].Count * pwar;
			airthreat += ArmyUtils.airbases[i].Count * pwar;
		}
		//most dire threat is invasion, so invasion first
		float localarmy = ArmyUtils.armies[Map.localTeam].Count;
		if (gthreat > localarmy * 0.5f) {
			if (gthreat > localarmy)
			{
				return "/enemies surround us, mobilize now!/";
			}
			return "/mobilization recommended/";
		}
		//then nukes
		float localsilos = ArmyUtils.silos[Map.localTeam].Count;
		if(nukethreat > localsilos) {
			return "/intelligence identifies missile gap/";
		}

		//then bombers
		float bombers = ArmyUtils.airbases[Map.localTeam].Count;
		float sams = ArmyUtils.batteries[Map.localTeam].Count;
		if (airthreat > bombers + sams) {
			return "/intelligence identifies bomber gap/";
		}
		return " ";
	}
	bool IsBot(int team) {
		if (Diplomacy.states[team] is State_Enemy) {
			return true;
		}
		return false;
	}
	float ProbabilityOfWar(StateDynamic dynamic)
	{
		//the likelihood of war is their ability to invade us, 
		//mitigated by our opinion of them
		
		State_Enemy sten = Diplomacy.states[dynamic.team2] as State_Enemy;
		float pLoss = 1 - dynamic.pVictory;
		float invOpinion = 1 - sten.opinion[dynamic.team1];
		return pLoss * invOpinion;
	}
	float ProbabilityOfWar(int team, int enemy)
	{
		StateDynamic dynamic = new StateDynamic(team, enemy);
		return ProbabilityOfWar(dynamic);
	}
}
