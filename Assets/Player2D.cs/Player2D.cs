using UnityEngine;
using UnityEngine.InputSystem;

public class Player2D : MonoBehaviour
{
    [Header("MOVEMENT")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("GROUND CHECK")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("PLAYER HP")]
    public int maxHealth = 200;
    private int currentHealth;

    private Rigidbody2D rb;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        CheckGround();
        Jump();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        float horizontal = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                horizontal = -1f;
            }

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
            {
                horizontal = 1f;
            }
        }

        rb.linearVelocity = new Vector2(
            horizontal * moveSpeed,
            rb.linearVelocity.y
        );
    }

    private void Jump()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame &&
            isGrounded)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            Debug.Log(" ПРЫЖОК!");
        }
    }

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        isGrounded = hit != null;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log(
            "Игрок получил " + damage + " урона"
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(" Игрок умер");
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }
    }
}