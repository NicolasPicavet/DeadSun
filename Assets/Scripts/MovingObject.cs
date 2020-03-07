using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovingObject : MonoBehaviour {
    public float speed;
    public LayerMask blockingLayer;
    public LayerMask indestructibleLayer;

    private BoxCollider2D boxCollider;
    private Rigidbody2D rb2D;
    private float inverseMoveTime;
    private bool isMoving;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    protected bool Move(int xDir, int yDir, out RaycastHit2D hit, out RaycastHit2D indestructible) {
        Vector2 start = transform.position;
        Vector2 end = start + new Vector2(xDir, yDir);

        boxCollider.enabled = false;
        hit = Physics2D.Linecast(start, end, blockingLayer);
        indestructible = Physics2D.Linecast(start, end, indestructibleLayer);
        boxCollider.enabled = true;

        if (hit.transform == null && indestructible.transform == null && !isMoving) {
            StartCoroutine(SmoothMovement(end, speed));
            return true;
        }

        StartCoroutine(OnMoveDone(false));
        return false;
    }

    protected virtual IEnumerator SmoothMovement(Vector3 end, float speed) {
        isMoving = true;
        float sqrRemainingDistance = (transform.position - end).sqrMagnitude;

        while(sqrRemainingDistance > float.Epsilon) {
            Vector3 newPosition = Vector3.MoveTowards(rb2D.position, end, speed * Time.deltaTime);
            rb2D.MovePosition(newPosition);
            sqrRemainingDistance = (transform.position - end).sqrMagnitude;
            yield return null;
        }
        rb2D.MovePosition(end);
        isMoving = false;
        OnMoveDone(true);
    }

    protected virtual bool AttemptMove(int xDir, int yDir) {
        RaycastHit2D hit;
        RaycastHit2D indestructible;
        bool canMove = Move(xDir, yDir, out hit, out indestructible);

        if (indestructible.transform != null)
            return false;

        if (hit.transform == null && canMove)
            return true;

        return OnCantMove(hit.transform);
    } 

    protected abstract bool OnCantMove(Transform transform);

    protected virtual IEnumerator OnMoveDone(bool success) {
        yield return null;
    }

    protected bool AttemptMoveToNextStepInPath(Vector3 origin, Vector3 target) {
        Tile start = BreadthFirstSearch(origin, target);
        Vector3 nextStep = start.next.position - origin;
        return AttemptMove((int) nextStep.x, (int) nextStep.y);
    }

    // Do not use concurently, tiles aren't cloned
    private Tile BreadthFirstSearch(Vector3 origin, Vector3 destination) {
        Tile firstStep = null;
        // Positions remaining to evaluate
        Queue<Tile> queue = new Queue<Tile>();
        // Start search from destination
        // End search at the begining of the path
        Tile start = null;
        Tile end = null;
        foreach (Tile tile in GameManager.instance.GetBoardTiles()) {
            if (IsTileWalkable(tile)) {
                if (tile.position == destination)
                    start = tile;
                if (tile.position == origin)
                    end = tile;
                if (start != null && end != null)
                    break;
            }
        }
        if (start != null && end != null) {
            start.discovered = true;
            queue.Enqueue(start);
            // While there is positions to evaluate
            while (queue.Count > 0) {
                Tile current = queue.Dequeue();
                if (current == end) {
                    firstStep = current;
                    break;
                }
                List<Tile> adjTiles = current.GetAdjacentTiles();
                List<Tile> validTiles = new List<Tile>(adjTiles);
                // Clean up adjacent tiles of movement blocking object lying on them
                foreach (Tile tile in adjTiles)
                    if (IsTileWalkable(tile))
                        foreach (Tile otherTile in adjTiles)
                            if (tile != otherTile 
                            && tile.position == otherTile.position
                            && tile.position != end.position
                            && !IsTileWalkable(otherTile)
                            && otherTile.gameObject.activeSelf) {
                                validTiles.Remove(tile);
                                validTiles.Remove(otherTile);
                                break;
                            }
                // Clean up adjacent tiles of solo movement blocking object
                foreach (Tile tile in adjTiles)
                    if (!IsTileWalkable(tile))
                        validTiles.Remove(tile);
                // Add valid adjacent tiles to evaluation queue
                foreach (Tile tile in validTiles)
                    if (!tile.discovered) {
                        tile.discovered = true;
                        // store path
                        tile.next = current;
                        queue.Enqueue(tile);
                    }
            }
        }
        // Reset discovery status for next search
        foreach (Tile tile in GameManager.instance.GetBoardTiles())
            tile.discovered = false;
        if (firstStep == null) {
            Debug.Log("origin " + origin.x + " " + origin.y);
            Debug.Log("destination " + destination.x + " " + destination.y);
        }
        return firstStep;
    }

    private bool IsTileWalkable(Tile tile) {
        return !tile.IsObjectInLayer(blockingLayer) && !tile.IsObjectInLayer(indestructibleLayer);
    }
}
