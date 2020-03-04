using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MovingObject
{
    public int playerDamage;
    public AudioClip enemyAttack1;
    public AudioClip enemyAttack2;

    private Animator animator;
    private Transform target;
    private bool skipMove;

    protected override void Start() {
        GameManager.instance.AddEnemyToList(this);
        animator = GetComponent<Animator>();
        target = GameObject.FindGameObjectWithTag("Player").transform;
        base.Start();
    }

    public void MoveEnemy() {
        if (skipMove) {
            skipMove = false;
            return;
        }

        bool moveSuccess = false;
        int xDir = 0;
        int yDir = 0;

        for (int tries = 0; tries < 2; tries++) {
            if ((yDir == 0 && xDir != 0) || Mathf.Abs(target.position.x - transform.position.x) < float.Epsilon) {
                yDir = target.position.y > transform.position.y ? 1 : -1;
                xDir = 0;
            } else {
                xDir = target.position.x > transform.position.x ? 1 : -1;
                yDir = 0;
            }

            moveSuccess = AttemptMove(xDir, yDir);
            if (moveSuccess)
                break;
        }

        if (moveSuccess) {
            skipMove = true;
        }
    }

    protected override bool OnCantMove(Transform transform) {
        Player player = transform.GetComponent<Player>();
        if (player != null) {
            animator.SetTrigger("enemyAttack");

            player.LoseFood(playerDamage);

            SoundManager.instance.RandomizeSfx(enemyAttack1, enemyAttack2);
            return true;
        }
        return false;
    }
}
