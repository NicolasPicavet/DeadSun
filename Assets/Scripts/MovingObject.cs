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
}
