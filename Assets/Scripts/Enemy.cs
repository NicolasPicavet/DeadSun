using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MovingObject
{
    public int playerDamage;
    public AudioClip enemyAttack1;
    public AudioClip enemyAttack2;

    private Animator animator;
    private bool skipMove;
    private Enemy nextEnemyToMove;

    protected override void Start() {
        speed = 5f;
        GameManager.instance.AddEnemyToList(this);
        animator = GetComponent<Animator>();
        base.Start();
    }

    public void MoveEnemy(Vector3 target, bool resetSkip = false) {
        if (!resetSkip && skipMove) {
            skipMove = false;
            OnMoveDone(true);
            return;
        }

        bool moveSuccess = AttemptMoveToNextStepInPath(transform.position, target);

        if (resetSkip || moveSuccess)
            skipMove = true;
    }

    public void MoveEnemy() {
        MoveEnemy(GameManager.instance.playerPosition);
    }

    protected override bool OnCantMove(Transform transform) {
        if (transform != null) {
            Player player = transform.GetComponent<Player>();
            if (player != null) {
                animator.SetTrigger("enemyAttack");

                player.LoseFood(playerDamage);

                SoundManager.instance.RandomizeSfx(enemyAttack1, enemyAttack2);
                return true;
            }
        }
        return false;
    }

    public void SetNextEnemyToMove(Enemy nextEnemy) {
        nextEnemyToMove = nextEnemy;
    }

    protected override IEnumerator OnMoveDone(bool success) {
        if (nextEnemyToMove != null)
            nextEnemyToMove.MoveEnemy();
        nextEnemyToMove = null;

        return base.OnMoveDone(success);
    }
}
