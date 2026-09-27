using UnityEngine;
using UnityEngine.InputSystem;

public class Player2D : MonoBehaviour
{
    [Header("MOVEMENT")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("PLAYER HP")]
    public int maxHealth = 200;
    private int currentHealth;

    [Header("GROUND CHECK")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("DEBUG")]
    public bool showGroundDebug = true;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool dead;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("❌ На Player нет Rigidbody2D!");
            return;
        }

        currentHealth = maxHealth;
    }


    private void Update()
    {
        if (dead)
            return;

        Move();

        // Проверяем землю
        CheckGround();

        // Прыжок
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("SPACE нажат. Grounded = " + isGrounded);

            if (isGrounded)
            {
                Jump();
            }
            else
            {
                Debug.Log("❌ Прыжок невозможен: игрок не на земле!");
            }
        }
    }


    // =========================================
    // ДВИЖЕНИЕ
    // =========================================

    private void Move()
    {
        if (rb == null)
            return;

        float move = 0f;

        if (Keyboard.current != null)
        {
            // A
            if (Keyboard.current.aKey.isPressed)
            {
                move = -1f;
            }

            // D
            if (Keyboard.current.dKey.isPressed)
            {
                move = 1f;
            }

            // Стрелка влево
            if (Keyboard.current.leftArrowKey.isPressed)
            {
                move = -1f;
            }

            // Стрелка вправо
            if (Keyboard.current.rightArrowKey.isPressed)
            {
                move = 1f;
            }
        }

        rb.linearVelocity = new Vector2(
            move * moveSpeed,
            rb.linearVelocity.y
        );


        // Разворот игрока
        if (move > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (move < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }


    // =========================================
    // ПРОВЕРКА ЗЕМЛИ
    // =========================================

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            isGrounded = false;

            if (showGroundDebug)
            {
                Debug.LogWarning(
                    "⚠️ GroundCheck не назначен в Inspector!"
                );
            }

            return;
        }

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );


        // Отладка
        if (showGroundDebug)
        {
            // Можно включить/выключить через Inspector
        }
    }


    // =========================================
    // ПРЫЖОК
    // =========================================

    private void Jump()
    {
        if (rb == null)
            return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );

        Debug.Log("🦘 ИГРОК ПРЫГНУЛ!");
    }


    // =========================================
    // ПОЛУЧЕНИЕ УРОНА
    // =========================================

    public void TakeDamage(int damage)
    {
        if (dead)
            return;

        currentHealth -= damage;

        Debug.Log(
            "Игрок получил " + damage +
            " урона. HP: " +
            currentHealth + "/" +
            maxHealth
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    // =========================================
    // СМЕРТЬ
    // =========================================

    private void Die()
    {
        dead = true;

        Debug.Log("💀 ИГРОК УМЕР!");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Destroy(gameObject);
    }


    // =========================================
    // GIZMOS
    // =========================================

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded
                ? Color.green
                : Color.red;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }
    }
}