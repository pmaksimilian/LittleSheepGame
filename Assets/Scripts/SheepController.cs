using UnityEngine;
using System.Collections;

public class SheepController : MonoBehaviour
{

    [SerializeField] private float wanderRadius = 1.5f;
    [SerializeField] private float moveSpeed = 0.7f;
    [SerializeField] private float minWaitTime = 3f;
    [SerializeField] private float maxWaitTime = 7f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float fleeDistance = 2f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private GameObject sheepVisual;

    private Transform playerTransform;
    private Vector2 currentTarget;
    private bool isWaiting = false;
    private Animator animator;
    private float lastInputX;
    private float lastInputY;

    void Start()
    {
        animator = sheepVisual.GetComponent<Animator>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(WaitAtPoint());
    }

    void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer < fleeDistance)
        {
            FleeFromPlayer();
        }
        else if (!isWaiting)
        {
            MoveToWaypoint();
        }
    }

    private void FleeFromPlayer()
    {
        Vector2 fleeDirection = (Vector2)transform.position - (Vector2)playerTransform.position;
        Vector2 fleeTarget = (Vector2)transform.position + fleeDirection.normalized * fleeDistance;

        transform.position = Vector2.MoveTowards(transform.position, fleeTarget, fleeSpeed * Time.deltaTime);

        StartCoroutine(WaitAtPoint());
    }

    private void MoveToWaypoint()
    {
        Vector2 direction = (currentTarget - (Vector2)transform.position).normalized;

        if (direction.magnitude > 0f)
        {
            lastInputX = direction.x;
            lastInputY = direction.y;
        }

        transform.position = Vector2.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);

        animator.SetFloat("InputX", direction.x);
        animator.SetFloat("InputY", direction.y);
        animator.SetBool("IsWalking", direction.magnitude > 0f);

        if (Vector2.Distance(transform.position, currentTarget) < 0.1f)
        {
            StartCoroutine(WaitAtPoint());
        }
    }

    private IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        animator.SetBool("IsWalking", false);
        animator.SetFloat("LastInputX", lastInputX);
        animator.SetFloat("LastInputY", lastInputY);

        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        currentTarget = GetValidRandomPosition();
        isWaiting = false;
    }

    private Vector2 GetRandomNearbyPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        return (Vector2)transform.position + randomOffset;
    }

    private Vector2 GetValidRandomPosition()
    {
        Vector2 candidate;

        for (int i = 0; i < 10; i++)
        {
            candidate = GetRandomNearbyPosition();
            if (IsPositionValid(candidate))
            {
                return candidate;
            }
        }
        return (Vector2)transform.position; // fallback to current position if no valid position found
    }

    private bool IsPositionValid(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, wanderRadius * 2, obstacleLayer);
        return hit == null || hit.gameObject == gameObject; // valid if no hit or hit self
    }

}
