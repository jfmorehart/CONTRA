using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class UnitChunks
{
    public static List<Unit>[] chunks;
    public static int[][] chunkValues;
    public static int chunkSize = 60;
    public static Vector2Int dime;
	public static void Init() {
        //this is going to be called during map's awake function
        dime.x = Mathf.CeilToInt(Map.ins.transform.localScale.x / chunkSize);
		dime.y = Mathf.CeilToInt(Map.ins.transform.localScale.y / chunkSize);
		chunks = new List<Unit>[dime.x * dime.y];// ends up with 576 chunks, which seems reasonable;
        chunkValues = new int[Map.ins.numStates][];
		for (int x = 0; x < Map.ins.numStates; x++)
		{
			chunkValues[x] = new int[dime.x * dime.y];
		}
		for (int i = 0; i < chunks.Length; i++) {
            chunks[i] = new List<Unit>();

            for(int x = 0; x < Map.ins.numStates; x++) {
                chunkValues[x][i] = 0;
	        }
	    }
	}
    public static void RemoveFromChunk(int index, Unit me) {
        chunks[index].Remove(me);
        chunkValues[me.team][index]--;
    }
    public static void AddToChunk(int index, Unit me) {
        chunks[index].Add(me);
		chunkValues[me.team][index]++;
	}

    public static int ChunkLookup(Vector2 position) {
        int x = Mathf.FloorToInt(position.x / chunkSize);
		int y = Mathf.FloorToInt(position.y / chunkSize);
		return x + (y * dime.x);
    }

    public static List<Unit> GetSurroundingChunkData(int chunk) {
        List<Unit> sur = new List<Unit>();
        Vector2Int cv2 = IndexToV2(chunk);
        for(int i = -1; i < 2; i++) {
			for (int j = -1; j < 2; j++)
			{
                int index = (chunk + j) + (i * dime.x); // index from v2

                if (!IndexIsValid(index)) continue; //invalid sqrs
                Vector2Int iv2 = IndexToV2(index);
                if (Vector2.Distance(cv2, iv2) > 1.5f) continue; //avoid wrapping
                sur.AddRange(chunks[index]);
			}
		}
        return sur;
    }   
    static bool IndexIsValid(int index) {
        if ((index < 0) || (index > (dime.x * dime.y) - 1)) return false;
        return true;
    }

    static Vector2Int IndexToV2(int index) {
        int y = Mathf.FloorToInt(index / dime.x);
        int x = index % dime.x;
        return new Vector2Int(x, y);
	}

    public static Vector2 ChunkIndexToMapPos(int index) {
        Vector2Int v2 = IndexToV2(index);
        Vector2 wpos = v2 * chunkSize + 0.5f * chunkSize * Vector2.one; //add half chunk
        return wpos;
    }

}
