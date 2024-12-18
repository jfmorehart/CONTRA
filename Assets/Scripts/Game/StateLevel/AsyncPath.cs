using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class AsyncPath : MonoBehaviour
{

	//Class for trying out async stuff
	public static AsyncPath ins;

	public static bool[,] borders;
	int storedSeed;

	private void Awake()
	{
		if (ins != this && ins != null) {
			Debug.Log("kill asyncPath");
			Destroy(gameObject);
			return;
		}
		ins = this;
		Diplomacy.StatesReady += Setup;
	}
	private void Start()
	{
		DisplayHandler.resetGame += Reset;
	}
	void Reset() {
		Diplomacy.StatesReady -= Setup;
    }

	public void Setup() {
		storedSeed = Map.ins.mapSeed;
		borders = new bool[Map.ins.numStates, Map.ins.numStates];
		//InvokeRepeating(nameof(CalcBorders), 0.1f, 5);
	}

	public bool SharesBorder(int a, int b) {
		int x = Mathf.Min(a, b);
		int y = Mathf.Max(a, b);
		return borders[x, y];
    }

	//public async Task CalcBorders() { 
	//	for(int i = 0; i < Map.ins.numStates; i++) {
	//		for (int j = i + 1; j < Map.ins.numStates; j++)
	//		{
	//			if ((i >= j)) continue;
	//			Vector2Int st = MapUtils.PointToCoords(Diplomacy.states[i].transform.position);
	//			Vector2Int en = MapUtils.PointToCoords(Diplomacy.states[j].transform.position);
	//			int[] pas = new int[] { i, j }; //strict interpretation
	//			Vector2Int[] path = await Task.Run(() => Path(st, en, pas.ToArray(), 1, 4000));
	//			borders[i, j] = (path != null);
	//			//Debug.Log(i + "does" + (borders[i, j] ? "" : "n't") + " border " + j);
	//		}
	//	}
	//   }


	public bool IsReachableCheck(Vector2Int start, Vector2Int end, int[] passableTeams, int downres, int maxTries = 800)
	{
		List<Node> open = new List<Node>();
		Dictionary<Vector2Int, Node> nlookup = new Dictionary<Vector2Int, Node>();

		//Task<Vector2Int> tempPath = new Task<Vector2Int>()
		Vector2Int st = start / downres;
		Vector2Int en = end / downres;
	
		Node n = CreateNode(en, 0, st, st);
		nlookup.Add(n.pos, n);
		open.Add(n);
		int lowf = int.MaxValue;
		int sf = Mathf.Max(n.fcost, maxTries);
		int tries = 0;

		while (lowf > 1 && tries < sf)
		{
			tries++;
			if (open.Count < 1)
			{
				//Debug.Log("returning null");
				return false;
			}

			Node toeval = NextNode(open, out int r);
			if (toeval.fcost < lowf) lowf = toeval.fcost;
			open.RemoveAt(r);

			//Update box around node
			Vector2Int off;
			Vector2Int pos;
			Node c;
			for (int i = 0; i < 9; i++)
			{
				off = new Vector2Int((i % 3) - 1, Mathf.FloorToInt(i / 3) - 1);
				pos = off + toeval.pos;

				//Dont create nodes for impassable terrain
				if (!ValidHalfPos(pos, downres)) { continue; }
				if (i == 4) continue;
				if (!IsPassable(pos * downres, passableTeams)) continue;
				if(nlookup.TryGetValue(pos, out Node item)) {
					if (!item.closed)
					{
						c = CreateNode(en, toeval.gcost, toeval.pos, pos);
						c.closed = true;
						if (c.gcost < nlookup[c.pos].gcost)
						{
							nlookup.Remove(c.pos);
							nlookup.Add(c.pos, c);
						}
					}
				}
				else
				{
					c = CreateNode(en, toeval.gcost, toeval.pos, pos);
					open.Add(c);
					nlookup.Add(c.pos, c);
				}

			}
		}

		//Debug.Log(tries + " tries, " + lowf + " lowf, " + sf + " start");

		if (nlookup.ContainsKey(en))
		{
			return true;
		}
		return false;
	}


	public Vector2Int[] Path(Vector2Int start, Vector2Int end, int[] passableTeams, int downres, int maxTries = 800)
	{
		List<Node> open = new List<Node>();
		Dictionary<Vector2Int, Node> nlookup = new Dictionary<Vector2Int, Node>();

		if (Map.ins == null)
		{
			Debug.LogError("messy async");
			//async operations are messy as fuck
			return null;
		}
		if (Map.ins.mapSeed != storedSeed)
		{
			Debug.LogError("messy async, wrong seed");
			return null;
		}

		//Task<Vector2Int> tempPath = new Task<Vector2Int>()
		Vector2Int st = start / downres;
		Vector2Int en = end / downres;


		Node n = CreateNode(en, 0, st, st);
		nlookup.Add(n.pos, n);
		open.Add(n);
		int lowf = int.MaxValue;
		int sf = Mathf.Max(n.fcost, maxTries);
		int tries = 0;
		while (lowf > 0 && tries < sf)
		{
			tries++;
			if (open.Count < 1)
			{
				//Debug.Log("returning null");
				return null;
			}

			Node toeval = NextNode(open, out int r);
			if (toeval.fcost < lowf) lowf = toeval.fcost;
			open.RemoveAt(r);

			//Update box around node
			for (int i = 0; i < 9; i++)
			{
				Vector2Int off = new Vector2Int((i % 3) - 1, Mathf.FloorToInt(i / 3) - 1);
				Vector2Int pos = off + toeval.pos;

				//Dont create nodes for impassable terrain
				if (!ValidHalfPos(pos, downres)) { continue; }
				if (i == 4) continue;
				if (!IsPassable(pos * downres, passableTeams)) continue;
				if (nlookup.TryGetValue(pos, out Node item))
				{
					if (!item.closed)
					{
						Node c = CreateNode(en, toeval.gcost, toeval.pos, pos);
						c.closed = true;
						if (c.gcost < nlookup[c.pos].gcost)
						{
							nlookup.Remove(c.pos);
							nlookup.Add(c.pos, c);
						}
					}
				}
				else
				{
					Node c = CreateNode(en, toeval.gcost, toeval.pos, pos);
					open.Add(c);
					nlookup.Add(c.pos, c);
				}
			}
		}
		//if (!passableTeams.Contains(0)) {
		//	Debug.Log(tries + " tries, " + lowf + " lowf, " + sf + " start");
		//}
	

		if (nlookup.ContainsKey(en))
		{
			List<Vector2Int> path = new List<Vector2Int>
			{
				nlookup[en].pos
			};
			Vector2Int added = en;
			int final = 0;
			while (added != st && final < 1000)
			{
				final++;
				added = nlookup[path[^1]].parent;
				path.Add(added);
			}
			path.Reverse();

			Vector2Int[] rwcds = new Vector2Int[path.Count];
			for (int i = 0; i < path.Count; i++)
			{
				rwcds[i] = path[i] * downres;
			}
			return rwcds;
		}
		else
		{
			return null;
		}
	}

	[IgnoredByDeepProfiler]
    public Vector2Int CheapestOpenNode(Vector2Int start, Vector2Int end, int[] passableTeams, int downres)
	{
		//Task<Vector2Int> tempPath = new Task<Vector2Int>()
		Vector2Int st = start / downres;
		Vector2Int en = end / downres;
		List<Node> open = new List<Node>();
		Dictionary<Vector2Int, Node> nlookup = new Dictionary<Vector2Int, Node>();

		Node n = CreateNode(en, 0, st, st);
		nlookup.Add(n.pos, n);
		open.Add(n);
		int lowC = int.MaxValue;
		int sf = n.fcost;
		Node lownode = n;
		int tries = 0;
		while (lownode.fcost > 1 && tries < 500)
		{
			tries++;
			if (open.Count < 1)
			{
				Debug.Log("closed all nodes: " + tries);
				return lownode.pos * downres;
			}

			Node toeval = NextNode(open, out int r);
			if (toeval.fcost + toeval.gcost * 0.8f < lowC)
			{
				lowC = Mathf.RoundToInt(toeval.fcost + toeval.gcost * 0.8f);
				lownode = toeval;
			}
			open.RemoveAt(r);

			//Update box around node
			for (int i = 0; i < 9; i++)
			{
				Vector2Int off = new Vector2Int((i % 3) - 1, Mathf.FloorToInt(i / 3) - 1);
				Vector2Int pos = off + toeval.pos;

				//Dont create nodes for impassable terrain
				if (!ValidHalfPos(pos, downres)) { continue; }
				if (i == 4) continue;
				if (!IsPassable(pos * downres, passableTeams)) continue;
				if (nlookup.ContainsKey(pos))
				{
					if (!nlookup[pos].closed)
					{
						Node c = CreateNode(en, toeval.gcost, toeval.pos, pos);
						c.closed = true;
						if (c.gcost < nlookup[c.pos].gcost)
						{
							nlookup.Remove(c.pos);
							nlookup.Add(c.pos, c);
						}
					}
				}
				else
				{
					Node c = CreateNode(en, toeval.gcost, toeval.pos, pos);
					open.Add(c);
					nlookup.Add(c.pos, c);
				}
			}
		}
		return lownode.pos * downres;
	}

    [IgnoredByDeepProfiler]
    public Node NextNode(List<Node> open, out int r)
	{
		int lowc = int.MaxValue;
		Node ln = open[0];
		r = 0;
		for (int i = 0; i < open.Count; i++)
		{
			int cnv = open[i].fcost + open[i].gcost;
			if (cnv < lowc)
			{
				lowc = cnv;
				ln = open[i];
				r = i;
			}
			else if (cnv == lowc)
			{
				if (open[i].fcost < ln.fcost)
				{
					ln = open[i];
					r = i;
				}
			}
		}
		return ln;
	}

    [IgnoredByDeepProfiler]
    public Node CreateNode(Vector2Int en, int pg, Vector2Int par, Vector2Int pos)
	{
		int dx = Mathf.Abs(en.x - pos.x);
		int dy = Mathf.Abs(en.y - pos.y);
		int comb = Mathf.Abs(dy - dx);
		int diag = Mathf.Min(dy, dx);
		int f = comb * 10 + diag * 14;
		int eg = Mathf.RoundToInt(Vector2Int.Distance(pos, par) * 10);
		int g = eg + pg;
		return new Node(pos, par, g, f);
	}
    [IgnoredByDeepProfiler]
    public bool IsPassable(Vector2Int pos, int[] passableTeams)
	{
		int pteam = Map.ins.GetPixTeam(pos);
		for(int i = 0; i < passableTeams.Length; i++) {
			if (passableTeams[i] == pteam) {
				return true;
			}
		}
		return false;
	}

    [IgnoredByDeepProfiler]
    public int CoordinateToIndex(Vector2Int coord)
	{
		int index = (coord.y * Map.ins.texelDimensions.x) + coord.x;
		index = Mathf.Clamp(index, 0, -1 + (Map.ins.texelDimensions.x * Map.ins.texelDimensions.y));
		return index;
	}
    [IgnoredByDeepProfiler]
    public bool ValidHalfPos(Vector2Int pos, int downres)
	{
		if (pos.x < 0 || pos.x > (Map.ins.texelDimensions.x / downres) - 1)
		{
			return false;
		}
		if (pos.y < 0 || pos.y > (Map.ins.texelDimensions.y / downres) - 1)
		{
			return false;
		}
		return true;
	}
	[IgnoredByDeepProfiler]
	public bool DictionaryContains(Dictionary<Vector2Int, Node> dict, Node n) {
		foreach (KeyValuePair<Vector2Int, Node> entry in dict)
		{
			if (entry.Value.pos == n.pos) return true;
		}
		return false;
	}
	[IgnoredByDeepProfiler]
	public bool DictionaryContains(Dictionary<Vector2Int, Node> dict, Vector2Int n)
	{
		foreach (KeyValuePair<Vector2Int, Node> entry in dict)
		{
			if (entry.Value.pos == n) return true;
		}
		return false;
	}
}
