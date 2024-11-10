using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Profiling;
using static MapUtils;
using System.Threading.Tasks;
using Unity.VisualScripting;
using Unity.Netcode;

public class Map : MonoBehaviour
{
	[HideInInspector]
	public static Map ins;
	public static bool multi;
	public static bool host;
	public static int localTeam = 0;

	public static Vector3 localScale;

	[Header("General Settings")]
	public Vector2Int texelDimensions;
	public int numStates;
	public int numCities;
	public float armyInfluenceStrength;

	
	//[HideInInspector]
	public Vector2Int[] state_centers;
	public Color[] state_colors;

	[HideInInspector]
	public Material[] state_mats;
	public Material state_mat_example;

	//Texel Buffers
	float[] stateInfluence;
	float[] pixelPop;
	int[] pixTeam;

	int[] originalMap;
	public int mapSeed;

	[Header("Compute Stuff")]
	public ComputeShader POP;
	ComputeBuffer popbuffer;
	ComputeBuffer teamOf;
	ComputeBuffer citybuffer;
	ComputeBuffer colorBuffer;

	public ComputeShader NUKE;
	public ComputeShader Influences;
	ComputeBuffer stin;

	public ComputeShader GROWTH;
	public float populationGrowthTickDelay = 0.25f;
	float lastGrowth;
	public bool growthDebugMode;
	public int[] state_growth_this_tick;
	//values used for managing growth in tutorial scene
	[HideInInspector]
	public bool growth_tutorialManualValues;
	[HideInInspector]
	public float growth_deltaOverride;
	[HideInInspector]
	public int growth_stateGrowthTickOverride;

	public ComputeShader OCEANS;

	[Header("Rendering")]

	public ComputeShader Render;
	RenderTexture mapRT;
	public Material mapMat;

	float lastDraw;
	public float reDrawDelay;

	[Header("Counting")]
	public ComputeShader COUNT;
	public ComputeShader CITYCOUNT;
	// Last count of populations
	public uint[] state_populations;

	public float rpoptocpop;

	public ComputeShader BORDERS;
	public ComputeBuffer brds;
	int[] borderLengths;
	int[] borderDataPoints;
	public List<Vector2Int>[][] borderPoints; 
	public ComputeBuffer brdPts;

	public int buildExclusionDistance;
	public int armyReadyDistance;

	SColor[] state_colors_bufferable;

	public static int texelLongLength;

	bool prepInfluences;
	float Hash(float fl)
	{
		uint value = (uint)((fl + 1) * 2147483647);
		value ^= value >> 16;  // Mix the high and low bits of value
		value *= 0x45d9f3b;    // A large prime multiplier for further mixing
		value ^= value >> 16;
		value *= 0x45d9f3b;
		value ^= value >> 16;
		float v = value / (float)4294967295;
		Debug.Log(v);
		return v;
	}
	private void Awake() {

		Hash(Hash(-0.3f) + Hash(0.311f));
		mapSeed = UnityEngine.Random.Range(-500, 500);
		if (MultiplayerVariables.ins != null)
		{
			multi = true;
			Debug.Log("live game");
			if (NetworkManager.Singleton.IsHost)
			{
				host = true;
				MultiplayerVariables.ins.MapSeed = mapSeed;
				int numPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
				MultiplayerVariables.ins.NumPlayers = numPlayers;
				MultiplayerVariables.ins.clientIDs = new ulong[numPlayers];
				numStates = numPlayers; //todo add bots back in

				Debug.Log("logging clients to teams.");
				for (int i = 0; i < numPlayers; i++)
				{
					MultiplayerVariables.ins.clientIDs[i] = NetworkManager.Singleton.ConnectedClientsIds[i];
					Debug.Log("team " + i + " is client " + MultiplayerVariables.ins.clientIDs[i]);
				}
				MultiplayerVariables.ins.ShareClientIDsClientRPC(MultiplayerVariables.ins.clientIDs);
			}
			else
			{
				mapSeed = MultiplayerVariables.ins.MapSeed;
				numStates = MultiplayerVariables.ins.NumPlayers;
			}
		}
		else
		{
			if (!Simulator.IsSetup) Simulator.Setup();
			numStates = Simulator.activeScenario.numTeams;
		}

		StartPrep();
	}

	private void StartPrep()
	{ 
		ins = this;
		texelLongLength = texelDimensions.x * texelDimensions.y;
		localScale = transform.localScale; //used for non-main threads

		Debug.Log(mapSeed);

		Research.Setup();
		UnitChunks.Init();
		ArmyUtils.Init();
		Diplomacy.SetupDiplo();
		ROE.SetUpRoe();
		Economics.SetupEconomics();
		TerminalMissileRegistry.Setup();

		//Prefabulate that amulite
		state_colors_bufferable = new SColor[numStates];
		state_mats = new Material[numStates];
		for (int i = 0; i < numStates; i++)
		{
			state_colors_bufferable[i] = new SColor(state_colors[i].r, state_colors[i].g, state_colors[i].b);
			state_mats[i] = new Material(state_mat_example);
			state_mats[i].color = state_colors[i];
		}


		popbuffer = new ComputeBuffer(texelLongLength, 4);
		borderLengths = new int[numStates * numStates];
		borderDataPoints = new int[texelLongLength];
		brds = new ComputeBuffer(borderLengths.Length, 4);
		brdPts = new ComputeBuffer(borderDataPoints.Length, 4);
		brds.SetData(borderLengths);
		brdPts.SetData(borderDataPoints);


		//Create basic info
		NewMap();
		//Borders are initially set by state of war
		ROE.SetAllExceptIdentity(1);
		Influences.SetInt("defenseBias", 0);

		//Just city borders, used for army placement
		StartCoroutine(nameof(BuildInfluences), false); //BuildInfluences();

		//if we're a lil nation, swap us with a bigger one
		//removed 
		//if (CheckSwapColors()) {
		//	//complete the swap
		//	StartCoroutine(nameof(BuildInfluences), false); //BuildInfluences();
		//}
		originalMap = new int[pixTeam.Length];
		Array.Copy(pixTeam, originalMap, pixTeam.Length);
		state_populations = new uint[numStates];

		//this array is used for state border detection
		borderLengths = new int[numStates * numStates];
		//this one is used in an intermediary step
		borderDataPoints = new int[texelLongLength];

		//and this one stores a ton of Vector2s that are points along specific borders
		//array + list hell
		borderPoints = new List<Vector2Int>[numStates][];
		for(int i= 0; i < numStates; i++) {
			borderPoints[i] = new List<Vector2Int>[numStates];
			for (int j = 0; j< numStates; j++)
			{
				borderPoints[i][j] = new List<Vector2Int>();
			}
		}

	}

	public void Start()
	{
		//Prep the screen for rendering the map
		mapRT = new RenderTexture(texelDimensions.x, texelDimensions.y, 0);
		mapRT.enableRandomWrite = true;
		mapRT.filterMode = FilterMode.Point;
		mapRT.Create();

		if (!multi) {
			//spawn in some starting armies and silos randomly
			ArmyManager.ins.RandomArmies(80);

			//Rebuild borders now with army info
			StartCoroutine(nameof(BuildInfluences), false);//BuildInfluences();
		}

		//Render
		ConvertToTexture();

		// Modern era peace :)
		// Lock borders at war-state, but now moving armies
		// wont override peacetime borders.
		ROE.SetAllExceptIdentity(0);

		// bias the influence system towards holding territory
		Influences.SetInt("defenseBias", 1);

		CountPop();

		
		//fucking pass by reference
		float[] a = Diplomacy.CalculateStatePowerRankings();
		for(int i = 0; i < a.Length; i++) {
			Diplomacy.startingPowerPercentages[i] = a[i];
			Diplomacy.score[i] = 0;
		}

		InvokeRepeating(nameof(UpdatePops), 1, 1f);
		InvokeRepeating(nameof(SlowUpdate), 5, 1f);

		if (Simulator.tutorialOverride) {
			gameObject.AddComponent<TutorialOverride>();
		}

	}

	public void Update()
	{
		Research.PerFrameResearch();
		if(Time.time - lastDraw > reDrawDelay) {
			lastDraw = Time.time;
			if (prepInfluences) {
				StartCoroutine(nameof(BuildInfluences), true);
			}
			else {
				ConvertToTexture();
			}
			prepInfluences = !prepInfluences;
		}

		if(Time.time - lastGrowth > populationGrowthTickDelay) {
			if (!growthDebugMode)
			{
				state_growth_this_tick = Economics.NewGrowthTick();
			}
			lastGrowth = Time.time;
			GrowPopulation();
		}
	}

	void UpdatePops() {
		//Called every 0.25 seconds. Sorta heavy.

		CountPop();
		CountBorders(); //todo reduce GC allocation

    }
	void SlowUpdate() {
		Diplomacy.CalculateStatePowerRankings();
	}


	public void Detonate(Vector2 wpos, float radius, int dteam, bool hitsAir = false) {
		Pool.ins.Explode().Nuke(wpos, radius);
		NukePop(wpos, radius);
		MapUtils.NukeObjs(wpos, radius, hitsAir);
		SFX.ins.NukePosition(wpos, radius);
		MoveCam.ins.Shake(wpos, radius);
	}

	public uint NukePop(Vector2 wpos, float radius) {
		Vector2Int pos = MapUtils.PointToCoords(wpos);

		//popbuffer = new ComputeBuffer(texelLongLength, 4);
		popbuffer.SetData(pixelPop);
		NUKE.SetBuffer(0, "pop", popbuffer);
		NUKE.SetInts("dime", texelDimensions.x, texelDimensions.y);
		NUKE.SetInts("pos", pos.x, pos.y);
		NUKE.SetFloat("radius", radius);

		uint[] de = new uint[1] { 0 };
		ComputeBuffer deadbuffer = new ComputeBuffer(1, 4);
		deadbuffer.SetData(de);
		NUKE.SetBuffer(0, "dead", deadbuffer);

		NUKE.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);

		popbuffer.GetData(pixelPop);
		deadbuffer.GetData(de);

		//popbuffer.Release();
		deadbuffer.Release();
		return TexelPopToWorldPop(de[0]);

    }

	public void NewMap() {

		//Prep buffers

		// The stin array is just room for the GPU to make its calculations
		// We never need to read this, and the GPU can reset it,
		// so we dont actually need to update this with every Influences Dispatch.
		stateInfluence = new float[texelLongLength * numStates];
		stin = new ComputeBuffer(texelLongLength * numStates, 4);
		stin.SetData(stateInfluence); //3.2 MB of 0's :)  // god bless preallocation
		Influences.SetBuffer(0, "stin", stin); //fire and forget

		pixTeam = new int[texelLongLength];
		teamOf = new ComputeBuffer(texelLongLength, 4);
		pixelPop = new float[texelLongLength];

		//Fill Oceans
		CreateOcean();

		//Create States, the different colored nations
		state_centers = new Vector2Int[numStates];
		for (int i = 0; i < numStates; i++)
		{
			state_centers[i] = PlaceState(i);
		}

		ArmyManager.ins.Setup();

		PopulateCities();
		//Populate cityBuffer
		citybuffer = new ComputeBuffer(numCities, 20);
		citybuffer.SetData(ArmyManager.ins.UpdateCities());

		//POPBUFFER
		// POP is a compute shader that generates popbuffer, a per-texel float value 
		// that represents the number of people that live in that area.
		//popbuffer = new ComputeBuffer(texelLongLength, 4);
		popbuffer.SetData(pixelPop);

		//Prep it
		POP.SetInts("dime", texelDimensions.x, texelDimensions.y);
		POP.SetInt("numCities", numCities);
		POP.SetBuffer(0, "cityBuffer", citybuffer);
		POP.SetBuffer(0, "popBuffer", popbuffer);

		//Send it
		POP.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);

		//Clean up
		citybuffer.Release();
		popbuffer.GetData(pixelPop);
		//popbuffer.Release();
	}
	void PopulateCities() {

		//Put cities everywhere

		List<City>[] citiesPerTeam = new List<City>[Map.ins.numStates];
		for (int i = 0; i < numStates; i++)
		{
			citiesPerTeam[i] = new List<City>();
		}

		List<City> cities = new List<City>();
		List<float> distancesFromCorner = new List<float>();
		for (int i = 0; i < numCities; i++)
		{
			City c = ArmyManager.ins.Spawn_CityLogic(i, PlaceCity(i));
			if (c != null)
			{
				cities.Add(c);
				citiesPerTeam[c.team].Add(c);
				distancesFromCorner.Add(c.transform.position.magnitude);
			}
		}
		numCities = cities.Count();

		//Re-center state_centers
		Vector2Int[] sumCenter = new Vector2Int[numStates];
		int[] sumCounter = new int[numStates];
		for (int i = 0; i < numCities; i++)
		{
			City c = cities[i];
			sumCounter[c.team]++;
			sumCenter[c.team] += c.mpos;
		}
		for (int i = 0; i < numStates; i++)
		{
			if (sumCounter[i] == 0) {
				Debug.LogError("no cities on team " + i + " state center = " + state_centers[i]);
				state_centers[i] = Vector2Int.zero;
				continue;
			}
			Vector2Int avgCenter = sumCenter[i] / sumCounter[i];
			state_centers[i] = avgCenter;
		}

		if (Simulator.activeScenario.percentOfCities == null) return;
		//reassign cities protocol is active

		//calculate city deficits
		int[] calloc = new int[numStates];
		int[] deficit = new int[numStates];
		List<int> surplusTeams = new List<int>();
		List<int> unlockedTeams = new List<int>();

		for (int i = 0; i < numStates; i++)
		{
			unlockedTeams.Add(i);
			calloc[i] = Mathf.FloorToInt((float)Simulator.activeScenario.percentOfCities[i] * numCities);
			deficit[i] = calloc[i] - citiesPerTeam[i].Count();
			if (deficit[i] < 0) surplusTeams.Add(i);
		}

		List<int> orderbyDistance = new List<int>();

		//sort teams by distance from map corner
		City[] carr = cities.ToArray();
		float[] darr = distancesFromCorner.ToArray();
		Array.Sort(darr, carr);
		for(int i = 0; i < numCities; i++) {
			if (!orderbyDistance.Contains(carr[i].team)) {
				orderbyDistance.Add(carr[i].team);
			}
			if(orderbyDistance.Count == numStates) {
				break;
			}
		}

		//steal cities from other teams
		for(int i =0;i < numStates; i++) {
			int team = orderbyDistance[i];
			if (citiesPerTeam[team].Count > calloc[team]) continue;
			unlockedTeams.Remove(team);


			//generate a list of nearby cities from unlocked teams
			//its as long as the deficit is
			List<City> stolen = ArmyUtils.GetNearestCitiesOfTeams(cities, unlockedTeams, state_centers[team]);
			foreach(City stole in stolen) {

				//we may have locked this team during this loop, so check anyhow
				if (!unlockedTeams.Contains(stole.team)) continue;

				//mark it stolen
				citiesPerTeam[stole.team].Remove(stole);

				//they're at a good spot, lock them
				if (citiesPerTeam[stole.team].Count == calloc[stole.team]){
					
					//if they've already gone, lock them
					if(stole.team < team) {
						unlockedTeams.Remove(stole.team);
					}
				}

				//steal it
				stole.team = team;
				citiesPerTeam[team].Add(stole);
				if (citiesPerTeam[team].Count >= calloc[team]) {
					break;
				}
			}
		}

		for (int i = 0; i < numStates; i++)
		{
			calloc[i] = Mathf.FloorToInt((float)Simulator.activeScenario.percentOfCities[i] * numCities);
			deficit[i] = calloc[i] - citiesPerTeam[i].Count();
		}

		//Re-center state_centers
		sumCenter = new Vector2Int[numStates];
		sumCounter = new int[numStates];
		for (int x = 0; x < cities.Count; x++)
		{
			City c = cities[x];
			sumCounter[c.team]++;
			sumCenter[c.team] += c.mpos;
		}
		for (int x = 0; x < numStates; x++)
		{
			Vector2Int avgCenter = sumCenter[x] / sumCounter[x];
			state_centers[x] = avgCenter;
		}
	}

	public void CountPop()
	{
		teamOf.SetData(pixTeam);
		COUNT.SetBuffer(0, "teamOf", teamOf);

		//popbuffer = new ComputeBuffer(popSize, 4);
		popbuffer.SetData(pixelPop);
		COUNT.SetBuffer(0, "popBuffer", popbuffer);

		COUNT.SetInts("dime", texelDimensions.x, texelDimensions.y);

		COUNT.SetInt("numStates", numStates);

		ComputeBuffer popCount = new ComputeBuffer(numStates, 4);

		state_populations = new uint[numStates];
		popCount.SetData(state_populations);
		COUNT.SetBuffer(0, "popcount", popCount);

		COUNT.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);

		//popbuffer.Release();
		//teamOf.Release();

		popCount.GetData(state_populations);
		popCount.Release();
		for (int i = 0; i < state_populations.Length; i++) {
			state_populations[i] = TexelPopToWorldPop(state_populations[i]);
		}

	}

	public uint CountPop_City(Vector2Int mpos) {
		//popbuffer = new ComputeBuffer(texelLongLength, 4);
		popbuffer.SetData(pixelPop);
		CITYCOUNT.SetBuffer(0, "popBuffer", popbuffer);
		CITYCOUNT.SetBuffer(0, "teamOf", teamOf);
		CITYCOUNT.SetInt("teamToCount", GetPixTeam(mpos));
		CITYCOUNT.SetInts("dime", texelDimensions.x, texelDimensions.y);
		CITYCOUNT.SetInts("cpos", mpos.x, mpos.y);

		CITYCOUNT.SetInt("numStates", numStates);

		ComputeBuffer popCount = new ComputeBuffer(numStates, 4);

		uint[] cpop = new uint[1] { 0 };
		popCount.SetData(cpop);
		CITYCOUNT.SetBuffer(0, "popcount", popCount);

		CITYCOUNT.Dispatch(0, 1, 1, 1);
		popCount.GetData(cpop);

		//popbuffer.Release();
		popCount.Release();

		return TexelPopToWorldPop(cpop[0]);
    }

	IEnumerator BuildInfluences(bool waitframe = false)
	{

		teamOf.SetData(pixTeam);
		Influences.SetBuffer(0, "teamOf", teamOf);

		ComputeBuffer atWar = new ComputeBuffer(numStates * numStates, 4);
		atWar.SetData(ROE.atWar);
		Influences.SetBuffer(0, "atWar", atWar);

		ComputeBuffer liveteams = new ComputeBuffer(numStates, 4);
		liveteams.SetData(MapUtils.LiveTeamsBuffer());
		Influences.SetBuffer(0, "liveTeams", liveteams);

		if (ArmyManager.ins == null)
		{
			ArmyManager.ins = FindObjectOfType<ArmyManager>();
		}
		//Probably unnecessary to update cities so frequently, but what the hell
		Inf[] cities = ArmyManager.ins.UpdateCities();
		numCities = cities.Length;

		Inf[] armies = ArmyManager.ins.UpdateArmies();
		Influences.SetInt("numInfs", numCities + armies.Length);
		ComputeBuffer infs = new ComputeBuffer(numCities + armies.Length, 20);

		Inf[] combined = new Inf[numCities + armies.Length];
		for (int i = 0; i < numCities; i++)
		{
			combined[i] = cities[i];
		}
		if (armies.Length > 0)
		{
			for (int i = numCities; i < numCities + armies.Length; i++)
			{
				combined[i] = armies[i - numCities];
			}

		}

		infs.SetData(combined);
		Influences.SetBuffer(0, "infs", infs);


		Influences.SetInts("dime", texelDimensions.x, texelDimensions.y);
		Influences.SetInt("numStates", numStates);


		Influences.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);



		teamOf.GetData(pixTeam);

		if (waitframe) {
			yield return new WaitForEndOfFrame();
		}


		//teamOf.Release();
		atWar.Release();
		//stin.Release(); we're gonna leave this allocated
		infs.Release();
		liveteams.Release();

		yield break;
	}

	public  void ConvertToTexture() {

		teamOf.SetData(pixTeam);
		Render.SetBuffer(0, "teamOf", teamOf);
		Render.SetFloat("seed", mapSeed);
		Render.SetInt("buildMode", PlayerInput.ins.buildMode ? 1 : 0);
		Render.SetInt("playerTeam", Map.localTeam);
		Render.SetInt("airmode", PlayerInput.ins.airMode ? 1 : 0);
		Render.SetFloat("time", Time.time);

		colorBuffer = new ComputeBuffer(numStates, 12);
		colorBuffer.SetData(state_colors_bufferable);

		Render.SetBuffer(0, "stateColors", colorBuffer);
		Render.SetInts("dime", texelDimensions.x, texelDimensions.y);

		//ComputeBuffer popbuffer = new ComputeBuffer(pixelPop.Length, 4);
		popbuffer.SetData(pixelPop);
		Render.SetBuffer(0, "popBuffer", popbuffer);
		Render.SetTexture(0, "Result", mapRT);

		Vector2Int[] bpos = BuildingPositions();

		Render.SetInt("exclusionDistance", buildExclusionDistance);

		ComputeBuffer exclude;
		if (bpos.Length > 0 && PlayerInput.ins.buildMode) {
			exclude = new ComputeBuffer(bpos.Length, 8);
			exclude.SetData(BuildingPositions());
			Render.SetInt("exclusionLength", bpos.Length);
		}
		else {
			exclude = new ComputeBuffer(1, 4);
			Render.SetInt("exclusionLength", 0);
		}

		Render.SetBuffer(0, "exclude", exclude);


		Render.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);


		//popbuffer.Release();
		colorBuffer.Release();
		//teamOf.Release(); keep it allocated

		exclude.Release();
		mapMat.SetTexture("_MainTex", mapRT);
	}

	void GrowPopulation() {

		if (growth_tutorialManualValues) {
			state_growth_this_tick[0] = growth_stateGrowthTickOverride;
			GROWTH.SetFloat("deltaOverride", growth_deltaOverride);
		}

		teamOf.SetData(pixTeam);
		GROWTH.SetBuffer(0, "teamOf", teamOf);

		ComputeBuffer growths = new ComputeBuffer(state_growth_this_tick.Length, 4);
		growths.SetData(state_growth_this_tick);
		GROWTH.SetBuffer(0, "growths", growths);

		GROWTH.SetInts("dime", texelDimensions.x, texelDimensions.y);

		//ComputeBuffer popbuffer = new ComputeBuffer(pixelPop.Length, 4);
		popbuffer.SetData(pixelPop);
		GROWTH.SetBuffer(0, "popcount", popbuffer);

		//write city distances
		Inf[] cities = ArmyManager.ins.UpdateCities();
		GROWTH.SetInt("numInfs", cities.Length);
		ComputeBuffer infs = new ComputeBuffer(numCities, 20);
		infs.SetData(cities);
		GROWTH.SetBuffer(0, "infs", infs);

		GROWTH.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);
		//teamOf.Release(); keep it allocated
		growths.Release();
		popbuffer.GetData(pixelPop);
		//popbuffer.Release();
		infs.Release();
	}
	[IgnoredByDeepProfiler]
	void CountBorders() {
		//this function counts the length of the border between each pair of states

		BORDERS.SetBuffer(0, "borderLengths", brds);
		BORDERS.SetBuffer(0, "otherTeam", brdPts);
		BORDERS.SetBuffer(0, "teamOf", teamOf);
		BORDERS.SetInt("numStates", numStates);
		BORDERS.SetInts("dime", texelDimensions.x, texelDimensions.y);
		BORDERS.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);
		brds.GetData(borderLengths);
		brdPts.GetData(borderDataPoints);

		int[] count = new int[numStates];
		for (int i = 0; i < numStates; i++)
		{
			for (int j = 0; j < numStates; j++)
			{
				//int index = numStates * j + i;
				borderPoints[i][j].Clear();
			}
		}
		//int scale = 1;
		for (int x = 0; x < (texelLongLength); x++)
		{
			int team = pixTeam[x];
			int other = borderDataPoints[x];

			if (team < 0) continue;
			if (other < 0) continue;

			borderPoints[team][other].Add(IndexToCoords(x));
			//Debug.Log(team + " borders " + other + " at " + IndexToCoords(x));
			count[team]++;
		}


		//if the border is longer than one unit, we'll consider the states adjacent
		for (int i = 0; i < numStates; i++) {
			for (int j = 0; j < numStates; j++)
			{
				int index = numStates * j + i;
				//Debug.Log(i + " " + j + " " + borderLengths[index]);
				AsyncPath.borders[i, j] = (borderLengths[index] > 5);
			}
		}

		//brds.Release();
		//brdPts.Release();
	}
	void CreateOcean() {
		teamOf.SetData(pixTeam);
		OCEANS.SetBuffer(0, "teamOf", teamOf);
		OCEANS.SetFloat("seed", mapSeed);
		OCEANS.SetInts("dime", texelDimensions.x, texelDimensions.y);
		OCEANS.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);
		teamOf.GetData(pixTeam);
	}

	bool CheckSwapColors()
	{
		/*
			This function swaps the player team (zero) with a larger power.
			This is just to hopefully avoid guaranteed losses without having to
			regenerate the map to find a more favorable position for our player.
		*/
		CountPop();

		int[] ranks = new int[state_populations.Length];
		for (int i = 1; i < state_populations.Length; i++)
		{
			ranks[i] = i;
		}
		System.Array.Sort(state_populations, ranks);
		System.Array.Reverse(ranks);
		int myrank = 0;
		for (int i = 1; i < state_populations.Length; i++)
		{
			if (ranks[i] == 0)
			{
				myrank = i;
				break;
			}
		}
		Debug.Log("Your nation is ranked " + (myrank + 1) + "st in population");
		if (myrank < 3) return false;
		Debug.Log("Thats bad, so we're switching you with team " + ranks[1] + ".");

		ArmyManager.ins.SwapTeamsCities(0, ranks[2]);
		(state_centers[ranks[1]], state_centers[0]) = (state_centers[0], state_centers[ranks[1]]);
		return true;
	}

	public struct SColor {
		//struct color used for cleanly passing info to shaders
		//dont ask why i didn't just use Vector4s.

		public float r;
		public float g;
		public float b;
		public SColor(float x, float y, float z) {
			r = x;
			g = y;
			b = z;
		}
    }
	public int GetOriginalMap(Vector2Int coordinate)
	{
		int index = (coordinate.y * texelDimensions.x) + coordinate.x;
		index = Mathf.Clamp(index, 0, originalMap.Length - 1);
		return originalMap[index];
	}
	public int GetPixTeam(Vector2Int coordinate) {
		if (pixTeam == null) return 0;
		int index = (coordinate.y * texelDimensions.x) + coordinate.x;
		index = Mathf.Clamp(index, 0, pixTeam.Length - 1);
		return pixTeam[index];
    }
}
