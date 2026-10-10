
using UnityEngine;
using System.Collections;

public class FireDragonHead : MonoBehaviour
{
    [Header("MOVEMENT")]
    public float speed = 7f;
    public float returnSpeed = 8f;

    [Header("ATTACK")]
    public float waitBeforeReturn = 13f;
    public int attackCount = 2;
    public float hitDistance = 0.5f;

    [Header("DAMAGE")]
    public int damage = 60;

    private Transform target;
    private Vector2 flyDirection;
    private bool initialized;
    private bool finished;
    private int attacksDone;

    public void SetTarget(
        Transform newTarget,
        int newDamage,
        float newSpeed,
        float newLifetime)
    {
        target = newTarget;
        damage = newDamage;
        speed = newSpeed;

        if (target != null)
        {
            flyDirection =
                ((Vector2)target.position -
                 (Vector2)transform.position).normalized;
        }

        initialized = true;

        StopAllCoroutines();
        StartCoroutine(AttackCycle());

        Destroy(gameObject, newLifetime);
    }

    private IEnumerator AttackCycle()
    {
        while (attacksDone < attackCount && !finished)
        {
            if (target == null)
                break;

            // 1. Фиксируем направление на игрока.
            Vector2 directionToPlayer =
                (Vector2)target.position -
                (Vector2)transform.position;

            if (directionToPlayer.sqrMagnitude > 0.001f)
                flyDirection = directionToPlayer.normalized;

            // 2. Летим строго прямо 13 секунд.
            // Игрок может увернуться — голова не поворачивает за ним.
            float timer = 0f;

            while (timer < waitBeforeReturn)
            {
                MoveStraight(flyDirection, speed);

                timer += Time.deltaTime;
                yield return null;
            }

            if (target == null)
                break;

            // 3. Через 13 секунд фиксируем новое направление
            // на текущее положение игрока.
            directionToPlayer =
                (Vector2)target.position -
                (Vector2)transform.position;

            if (directionToPlayer.sqrMagnitude > 0.001f)
                flyDirection = directionToPlayer.normalized;

            // 4. Летим прямо на игрока, не меняя направление.
            while (target != null)
            {
                float distance = Vector2.Distance(
                    transform.position,
                    target.position
                );

                if (distance <= hitDistance)
                    break;

                MoveStraight(flyDirection, returnSpeed);
                yield return null;
            }

            attacksDone++;

            // 5. Повторяем цикл ещё раз.
            if (attacksDone < attackCount && target != null)
                yield return null;
        }

        finished = true;
        Destroy(gameObject);
    }

    private void MoveStraight(Vector2 direction, float moveSpeed)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.position +=
            (Vector3)(direction.normalized *
                      moveSpeed * Time.deltaTime);

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (finished)
            return;

        Player2D playerScript =
            other.GetComponentInParent<Player2D>();

        if (playerScript != null)
            playerScript.TakeDamage(damage);
    }
}
