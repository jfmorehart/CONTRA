using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MapUtils;

public class Map : MonoBehaviour
{
	[HideInInspector]
	public static Map ins;

	[Header("General Settings")]
	public Vector2Int texelDimensions;
	public int numStates;
	public int numCities;
	public float armyInfluenceStrength;

	
	[HideInInspector]
	public Vector2Int[] stateCenters;
	public Color[] stateColors;

	//Buffers
	float[] stateInfluence;
	float[] population;
	int[] pixTeam;

	[Header("Compute Stuff")]
	public ComputeShader POP;
	ComputeBuffer popbuffer;
	ComputeBuffer citybuffer;
	ComputeBuffer colorBuffer;
	public ComputeShader NUKE;

	public ComputeShader Influences;
	ComputeBuffer stin;

	[Header("Rendering")]
	public ComputeShader Render;
	public Material mapMat;
	float lastDraw;
	public float reDrawDelay;

	private void Awake()
	{
		ins = this;
		ROE.SetUpRoe();
		Diplo.SetupDiplo();
	}

	public void Start()
	{
		//Create basic info
		NewMap();
	
		//Borders are initially set by state of war
		ROE.SetAllExceptIdentity(1);

		//Just city borders, used for army placement
		BuildInfluences();
		InfluenceMan.ins.RandomArmies(100);

		//Rebuild borders now with army info
		BuildInfluences();

		//Render
		ConvertToTexture();

		// Modern era peace :)
		// Lock borders at war-state, but now moving armies
		// wont override peacetime borders
		ROE.SetAllExceptIdentity(0);
	
	}

	public void Update()
	{
		if(Time.time - lastDraw > reDrawDelay) {
			lastDraw = Time.time;
			BuildInfluences();
			ConvertToTexture();
		}
	}

	public void Detonate(Vector2 wpos, float radius) {
		Pool.ins.Explode().Nuke(wpos, radius);
	}
	public void NukePop(Vector2 wpos, float radius) {
		Vector2Int pos = MapUtils.PointToCoords(wpos);
		int popSize = texelDimensions.x * texelDimensions.y;
		popbuffer = new ComputeBuffer(popSize, 4);
		popbuffer.SetData(population);
		NUKE.SetBuffer(0, "pop", popbuffer);
		NUKE.SetInts("dime", texelDimensions.x, texelDimensions.y);
		NUKE.SetInts("pos", pos.x, pos.y);
		NUKE.SetFloat("radius", radius);
		NUKE.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);
		popbuffer.GetData(population);
		popbuffer.Release();
    }

	public void NewMap() { 
    
		//Prep buffers
		stateInfluence = new float[texelDimensions.x * texelDimensions.y * numStates];
		pixTeam = new int[texelDimensions.x * texelDimensions.y];
		population = new float[texelDimensions.x * texelDimensions.y];

		//Create States, the different colored nations
		stateCenters = new Vector2Int[numStates];
		for (int i = 0; i < numStates; i++)
		{
			stateCenters[i] = PlaceState(i);
		}

		InfluenceMan.ins.Setup();
		//Put cities everywhere
		Inf[] cities = new Inf[numCities];
		for(int i = 0; i < numCities - numStates; i++) {
			InfluenceMan.ins.Spawn_CityLogic(i, PlaceCity(i));
		}

		//Put at least one in each state
		for (int i = numCities - numStates; i < numCities; i++)
		{
			int tries = 0;
			Inf testCity = PlaceCity(i);
			while(testCity.team != i % numStates && tries < 500) {
				testCity = PlaceCity(i);
				tries++;
			}
			InfluenceMan.ins.Spawn_CityLogic(i, testCity);

		}

		//Populate cityBuffer
		citybuffer = new ComputeBuffer(numCities, 20);
		citybuffer.SetData(InfluenceMan.ins.UpdateCities());

		//POPBUFFER
		// POP is a compute shader that generates popbuffer, a per-texel float value 
		// that represents the number of people that live in that area.
		int popSize = texelDimensions.x * texelDimensions.y;
		popbuffer = new ComputeBuffer(popSize, 4);
		popbuffer.SetData(population);

		//Prep it
		POP.SetInts("dime", texelDimensions.x, texelDimensions.y);
		POP.SetInt("numCities", numCities);
		POP.SetBuffer(0, "cityBuffer", citybuffer);
		POP.SetBuffer(0, "popBuffer", popbuffer);

		//Send it
		POP.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);

		//Clean up
		citybuffer.Release();
		popbuffer.GetData(population);
		popbuffer.Release();
	}

	void BuildInfluences() {

		stin = new ComputeBuffer(texelDimensions.x * texelDimensions.y * numStates, 4);
		stin.SetData(stateInfluence);
		Influences.SetBuffer(0, "stin", stin);
		ComputeBuffer teamOf = new ComputeBuffer(texelDimensions.x * texelDimensions.y, 4);
		teamOf.SetData(pixTeam);
		Influences.SetBuffer(0, "teamOf", teamOf);

		ComputeBuffer atWar = new ComputeBuffer(numStates * numStates, 4);
		atWar.SetData(ROE.atWar);
		Influences.SetBuffer(0, "atWar", atWar);

		//Probably unnecessary to update cities so frequently, but what the hell
		Inf[] cities = InfluenceMan.ins.UpdateCities();
		Inf[] armies = InfluenceMan.ins.UpdateArmies();
		Influences.SetInt("numInfs", numCities + armies.Length);
		ComputeBuffer infs = new ComputeBuffer(numCities + armies.Length, 20);



		Inf[] combined = new Inf[numCities + armies.Length];
		for (int i = 0; i < numCities; i++) {
			combined[i] = cities[i];
		}
		if(armies.Length > 0) {
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
		//disp

		teamOf.GetData(pixTeam);
		teamOf.Release();
		atWar.Release();
		stin.Release();
		infs.Release();

	}

	void ConvertToTexture() {

		RenderTexture mapRT = new RenderTexture(texelDimensions.x, texelDimensions.y, 0);
		mapRT.enableRandomWrite = true;
		mapRT.filterMode = FilterMode.Point;
		mapRT.Create();

		ComputeBuffer teamOf = new ComputeBuffer(texelDimensions.x * texelDimensions.y, 4);
		teamOf.SetData(pixTeam);
		Render.SetBuffer(0, "teamOf", teamOf);


		SColor[] scolors = new SColor[numStates];
		for(int i = 0; i < numStates; i++) {
			scolors[i] = new SColor(stateColors[i].r, stateColors[i].g, stateColors[i].b);
		}
		colorBuffer = new ComputeBuffer(numStates, 12);
		colorBuffer.SetData(scolors);

		Render.SetBuffer(0, "stateColors", colorBuffer);
		Render.SetInts("dime", texelDimensions.x, texelDimensions.y);

		int popSize = texelDimensions.x * texelDimensions.y;
		popbuffer = new ComputeBuffer(popSize, 4);
		popbuffer.SetData(population);
		Render.SetBuffer(0, "popBuffer", popbuffer);
		Render.SetTexture(0, "Result", mapRT);

		Render.Dispatch(0, texelDimensions.x / 32, texelDimensions.y / 32, 1);
		popbuffer.Release();
		colorBuffer.Release();
		teamOf.Release();
		mapMat.SetTexture("_MainTex", mapRT);
	}

	public struct SColor {
		public float r;
		public float g;
		public float b;
		public SColor(float x, float y, float z) {
			r = x;
			g = y;
			b = z;
		}
    }

	public int GetPixTeam(Vector2Int coordinate) {
		int index = (coordinate.y * texelDimensions.x) + coordinate.x;
		index = Mathf.Clamp(index, 0, pixTeam.Length - 1);
		return pixTeam[index];
    }
}
