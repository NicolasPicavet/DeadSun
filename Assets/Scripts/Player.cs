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
    public Text foodText;
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

    // Start is called before the first frame update
    protected override void Start()
    {
        animator = GetComponent<Animator>();

        food = GameManager.instance.playerFoodPoints;

        foodText.text = "Food: " + food;

        GameManager.instance.ApplyVision(this);

        base.Start();
        
    }

    private void OnDisable() {
        GameManager.instance.playerFoodPoints = food;
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.instance.playersTurn) return;

        int horizontal = 0;
        int vertical = 0;

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
        if (base.AttemptMove(xDir, yDir)) {
            food--;
            foodText.text = "Food: " + food;

            //RaycastHit2D hit;
            SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);

            CheckIfGameOver(GameOverSource.MOVE);

            GameManager.instance.playersTurn = false;

            return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "Exit") {
            Invoke("Restart", restartLevelDelay);
            enabled = false;
        } else if (other.tag == "Food") {
            food += pointsPerFood;
            foodText.text = "+" + pointsPerFood + " Food: " + food;
            SoundManager.instance.RandomizeSfx(eatSound1, eatSound2);
            other.gameObject.SetActive(false);
        } else if (other.tag == "Soda") {
            food += pointsPerSoda;
            foodText.text = "+" + pointsPerSoda + " Food: " + food;
            SoundManager.instance.RandomizeSfx(drinkSound1, drinkSound2);
            other.gameObject.SetActive(false);
        }
    }

    protected override bool OnCantMove(Transform transform) {
        Wall wall = transform.GetComponent<Wall>();
        if (wall != null) {
            wall.DamageWall(wallDamage);
            animator.SetTrigger("playerChop");
            return true;
        }
        Enemy enemy = transform.GetComponent<Enemy>();
        if (enemy != null) {
            // TODO hit enemy
            animator.SetTrigger("playerChop");
            return true;
        }
        return false;
    }

    private void Restart() {
        SceneManager.LoadScene("MainScene");
    }

    public void LoseFood (int loss) {
        animator.SetTrigger("playerHit");
        food -= loss;
        foodText.text = "-" + loss + " Food: " + food;
        CheckIfGameOver(GameOverSource.ENEMY);
    }

    private void CheckIfGameOver(GameOverSource source) {
        if (food <= 0) {
            SoundManager.instance.PlaySingle(gameOverSound);
            SoundManager.instance.musicSource.Stop();
            GameManager.instance.GameOver(source);
        }
    }

    protected override void OnMoveDone() {
        GameManager.instance.ApplyVision(this);

        base.OnMoveDone();
    }
}
