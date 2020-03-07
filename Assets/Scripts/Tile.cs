using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile {
    public Tile next { get; set; }
    public GameObject gameObject { get; }
    public Vector3 position { 
        get {
            return gameObject.transform.position;
        }
    }
    public bool discovered = false;

    private List<Tile> adjacentTiles = new List<Tile>();

    public Tile(GameObject go) {
        gameObject = go;
    }

    public List<Tile> GetAdjacentTiles() {
        if (adjacentTiles.Count > 0)
            return adjacentTiles;

        List<Vector3> adjacentPositions = new List<Vector3>();
        adjacentPositions.Add(position + Vector3.up);
        adjacentPositions.Add(position + Vector3.right);
        adjacentPositions.Add(position + Vector3.down);
        adjacentPositions.Add(position + Vector3.left);

        foreach (Tile t in GameManager.instance.GetBoardTiles())
            if (adjacentPositions.Contains(t.position))
                adjacentTiles.Add(t);

        return adjacentTiles;
    }

    public bool IsObjectInLayer(LayerMask mask) {
        return mask.value == (mask.value | (1 << gameObject.layer));
    }
}
