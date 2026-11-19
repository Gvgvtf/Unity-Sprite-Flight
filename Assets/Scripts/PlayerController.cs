using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public float thrustForce = 1f;
    Rigidbody2D rb;
    public float maxSpeed = 5f;
    public ParticleSystem boosterFlame;
    public float flameRotationOffset = 0f;
    private float elapsedTime = 0f;
    private float score = 0f;
    public float scoreMultiplier = 10f;
    public UIDocument uiDocument;
    private Label scoreText;
    private Label highScoreText;
    public GameObject explosionEffect;
    private Button restartButton;
    public GameObject borderParent;
    public AudioSource thrusterSound;
    public InputAction moveForward;
    public InputAction lookPosition;

    void Start()
    {
        moveForward.Enable();
        lookPosition.Enable();

        rb = GetComponent<Rigidbody2D>();

        scoreText = uiDocument.rootVisualElement.Q<Label>("ScoreLabel");
        highScoreText = uiDocument.rootVisualElement.Q<Label>("HighScoreLabel");  // new
        restartButton = uiDocument.rootVisualElement.Q<Button>("RestartButton");

        restartButton.style.display = DisplayStyle.None;
        highScoreText.style.display = DisplayStyle.None;   // hide until game over

        restartButton.clicked += ReloadScene;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        score = Mathf.FloorToInt(elapsedTime * scoreMultiplier);
        scoreText.text = "Score: " + score;

        if (moveForward.IsPressed())
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(lookPosition.ReadValue<Vector2>());
            Vector2 direction = (mousePos - transform.position).normalized;

            transform.up = direction;
            rb.AddForce(direction * thrustForce);

            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

            // keep the flame's facing locked to the ship's current heading
            var main = boosterFlame.main;
            main.startRotation = (angle + flameRotationOffset) * Mathf.Deg2Rad;
        }

        if (moveForward.WasPressedThisFrame())
        {
            boosterFlame.Play();
            thrusterSound.Play();
        }
        else if (moveForward.WasReleasedThisFrame())
        {
            boosterFlame.Stop();
            thrusterSound.Stop();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        int currentScore = Mathf.FloorToInt(score);
        int savedHighScore = PlayerPrefs.GetInt("HighScore", 0);
        bool isNewHighScore = currentScore > savedHighScore;

        if (isNewHighScore)
        {
            PlayerPrefs.SetInt("HighScore", currentScore);
            PlayerPrefs.Save();
            savedHighScore = currentScore;
        }

        highScoreText.style.display = DisplayStyle.Flex;

        if (isNewHighScore)
        {
            highScoreText.text = "New High Score: " + savedHighScore;

            // Scheduler survives Destroy(gameObject) since it's tied to the
            // visual element, not the MonoBehaviour
            bool visible = true;
            Color originalColor = highScoreText.resolvedStyle.color;

            highScoreText.schedule.Execute(() =>
            {
                visible = !visible;
                highScoreText.style.color = new StyleColor(visible ? originalColor : Color.clear);
            }).Every(500);
        }
        else
        {
            highScoreText.text = "High Score: " + savedHighScore;
        }

        Destroy(gameObject);
        Instantiate(explosionEffect, transform.position, transform.rotation);
        restartButton.style.display = DisplayStyle.Flex;
        borderParent.SetActive(false);
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}