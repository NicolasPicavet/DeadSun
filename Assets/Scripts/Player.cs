using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameOverSource {
    ENEMY, MOVE
}

public class Player : MovingObject {
    public const int pointsPerFood = 10;
    public const int pointsPerSoda = 15;
    

    public int wallDamage = 1;
    public float restartLevelDelay = 1f;
    public AudioClip moveSound1;
    public AudioClip moveSound2;
    public AudioClip eatSound1;
    public AudioClip eatSound2;
    public AudioClip drinkSound1;
    public AudioClip drinkSound2;
    public AudioClip gameOverSound;

    private Animator animator;
    private int food;
    private Vector2 touchOrigin = -Vector2.one;
    private Text foodText;

    protected override void Start() {
        speed = 12f;

        animator = GetComponent<Animator>();

        food = GameManager.instance.playerFoodPoints;

        foodText = GameObject.Find("FoodText").GetComponent<Text>();
        UpdateFoodText();

        GameManager.instance.ApplyVision(this);

        base.Start();
    }

    private void OnDisable() {
        GameManager.instance.playerFoodPoints = food;
    }

    void Update() {
        if (!GameManager.instance.playersTurn || GameManager.instance.enemiesMoving || GameManager.instance.playerMoving)
            return;

        int horizontal = 0;
        int vertical = 0;

        GameManager.instance.playerMoving = true;
        StartCoroutine(Attempting());

        #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER

            horizontal = (int) Input.GetAxisRaw("Horizontal");
            vertical = (int) Input.GetAxisRaw("Vertical");

            if (horizontal != 0)
                vertical = 0;
        
        #else

            if(Input.touchCount > 0) {
                Touch myTouch = Input.touches[0];

                if (myTouch.phase == TouchPhase.Began) {
                    touchOrigin = myTouch.position;
                } else if (myTouch.phase == TouchPhase.Ended && toucheOrigin.x >= 0) {
                    Vector2 touchEnd = myTouch.position;
                    float x = touchEnd.x - touchOrigin.x;
                    float y = touchEnd.y - touchOrigin.y;
                    touchOrigin.x = -1;

                    if (Mathf.Abs(x) > Mathf.Abs(y))
                        horizontal = x > 0 ? 1 : -1;
                    else
                        vertical = y > 0 ? 1 : -1;
                }
            }

        #endif

        if (horizontal != 0 || vertical != 0)
            AttemptMove(horizontal, vertical);
    }

    protected override bool AttemptMove(int xDir, int yDir) {
        Vector3 positionPreMove = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        bool moveSuccess = !CheckIfGameOver(GameOverSource.MOVE, food - 1) && base.AttemptMove(xDir, yDir);
        if (moveSuccess) {
            food--;
            UpdateFoodText();

            SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);

            GameManager.instance.playerPosition = positionPreMove + new Vector3(xDir, yDir, 0);
            GameManager.instance.playersTurn = false;
        }
        return moveSuccess;
    }

    private IEnumerator Attempting() {
        yield return new WaitForSeconds(GameManager.instance.turnDelay);
        GameManager.instance.playerMoving = false;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "Exit") {
            Invoke("Restart", restartLevelDelay);
            enabled = false;
        } else if (other.tag == "Food") {
            food += pointsPerFood;
            UpdateFoodText("+" + pointsPerFood + " ");
            SoundManager.instance.RandomizeSfx(eatSound1, eatSound2);
            other.gameObject.SetActive(false);
        } else if (other.tag == "Soda") {
            food += pointsPerSoda;
            UpdateFoodText("+" + pointsPerSoda + " ");
            SoundManager.instance.RandomizeSfx(drinkSound1, drinkSound2);
            other.gameObject.SetActive(false);
        }
    }

    protected override bool OnCantMove(Transform hit) {
        
        float PushCord(float origin, float target) {
            return origin + ((target - origin) * 2);
        }

        Vector3 GetPush(Vector3 origin, Vector3 target) {
            return new Vector3(PushCord(origin.x, target.x), PushCord(origin.y, target.y), PushCord(origin.z, target.z));
        }

        if (hit != null) {
            Wall wall = hit.GetComponent<Wall>();
            if (wall != null) {
                wall.DamageWall(wallDamage);
                animator.SetTrigger("playerChop");
                return true;
            }
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.MoveEnemy(GetPush(transform.position, enemy.transform.position), true);
                animator.SetTrigger("playerChop");
                return true;
            }
        }
        return false;
    }

    private void Restart() {
        SceneManager.LoadScene("MainScene");
    }

    public void LoseFood (int loss) {
        animator.SetTrigger("playerHit");
        food -= loss;
        UpdateFoodText("-" + loss + " ");
        CheckIfGameOver(GameOverSource.ENEMY, food);
    }

    private bool CheckIfGameOver(GameOverSource source, int food) {
        if (food <= 0) {
            SoundManager.instance.PlaySingle(gameOverSound);
            SoundManager.instance.musicSource.Stop();
            enabled = false;
            GameManager.instance.GameOver(source);
            return true;
        }
        return false;
    }

    protected override IEnumerator OnMoveDone(bool success) {
        GameManager.instance.ApplyVision(this);

        return base.OnMoveDone(success);
    }

    private void UpdateFoodText(string gain = "") {
        foodText.text = gain + "Food: " + food;
    }
}
