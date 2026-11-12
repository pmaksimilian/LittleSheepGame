using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SheepController : MonoBehaviour
{
    [Header("Wandering")]
    [SerializeField] private float wanderRadius = 1.5f;
    [SerializeField] private float moveSpeed = 0.7f;
    [SerializeField] private float minWaitTime = 3f;
    [SerializeField] private float maxWaitTime = 7f;
    [SerializeField] private float stuckTimeout = 1.5f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Fleeing")]
    [SerializeField] private float fleeDistance = 3f;
    [SerializeField] private float fleeSpeed = 5f;

    [Header("Flock Flee")]
    [SerializeField] private float neighborRadius = 2.5f;   // who influences my flee direction
    [SerializeField] private float alarmRadius = 2f;        // how far a panicking sheep alarms calm ones
    [SerializeField] private float alarmDuration = 1f;      // keeps fleeing this long after the trigger ends
    [SerializeField] private float fleeTurnSpeed = 8f;      // higher = snappier direction changes
    [SerializeField] private float dogWeight = 1f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 0.3f;
    [SerializeField] private float separationWeight = 0.8f;

    [Header("References")]
    [SerializeField] private GameObject sheepVisual;

    private static readonly List<SheepController> allSheep = new List<SheepController>();

    private Transform playerTransform;
    private Vector2 currentTarget;
    private bool isWaiting = false;
    private bool isFleeing = false;
    private Animator animator;
    private BoxCollider2D box;
    private float lastInputX;
    private float lastInputY;
    private Coroutine waitCoroutine;

    private float moveTimer;
    private Vector2 lastPos;

    private float alarmTimer;
    private bool directlyScared;      // dog is within fleeDistance (only these sheep alarm others)
    private Vector2 currentFleeDir;

    private ContactFilter2D filter;
    private readonly Collider2D[] overlapBuffer = new Collider2D[8];
    private readonly RaycastHit2D[] castBuffer = new RaycastHit2D[8];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        allSheep.Clear();
    }

    void OnEnable()
    {
        allSheep.Add(this);
    }

    void OnDisable()
    {
        allSheep.Remove(this);
    }

    void Start()
    {
        animator = sheepVisual.GetComponent<Animator>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        box = GetComponent<BoxCollider2D>();
        if (box == null) box = GetComponentInChildren<BoxCollider2D>();
        if (box == null)
        {
            Debug.LogError("SheepController: no BoxCollider2D found on " + name, this);
            enabled = false;
            return;
        }

        filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayer);
        filter.useTriggers = false;

        currentTarget = transform.position;
        StartCoroutine(WaitAtPoint());
    }

    void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        directlyScared = distanceToPlayer < fleeDistance;

        if (directlyScared)
        {
            alarmTimer = alarmDuration;
        }
        else
        {
            CheckForAlarm();
            if (alarmTimer > 0f) alarmTimer -= Time.deltaTime;
        }

        if (alarmTimer > 0f)
        {
            isFleeing = true;
            FleeFromPlayer();
        }
        else
        {
            if (isFleeing)
            {
                isFleeing = false;
                currentFleeDir = Vector2.zero;
                StartWaiting();
            }
            else if (!isWaiting)
            {
                MoveToWaypoint();
            }
        }
    }

    // ---------- Collision helpers (always ignore our own collider) ----------

    private Vector2 BoxSize()
    {
        return box.bounds.size * 0.95f; // slightly smaller so we don't snag on touching surfaces
    }

    // Offset from transform.position to the collider's center
    private Vector2 BoxOffset()
    {
        return (Vector2)box.bounds.center - (Vector2)transform.position;
    }

    private bool IsAreaBlocked(Vector2 position)
    {
        int count = Physics2D.OverlapBox(position + BoxOffset(), BoxSize(), 0f, filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i] != box) return true;
        }
        return false;
    }

    private bool IsPathClear(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;

        int count = Physics2D.BoxCast(from + BoxOffset(), BoxSize(), 0f, dir / dist, filter, castBuffer, dist);
        for (int i = 0; i < count; i++)
        {
            if (castBuffer[i].collider != box) return false;
        }
        return true;
    }

    // ---------- Fleeing ----------

    // Only sheep the dog is directly scaring can alarm others.
    // Alarmed sheep don't pass it on, so panic spreads one short hop and can't chain across the field.
    private void CheckForAlarm()
    {
        Vector2 pos = transform.position;
        float alarmSqr = alarmRadius * alarmRadius;

        foreach (SheepController other in allSheep)
        {
            if (other == this || !other.directlyScared) continue;

            if (((Vector2)other.transform.position - pos).sqrMagnitude <= alarmSqr)
            {
                alarmTimer = alarmDuration;
                return;
            }
        }
    }

    private Vector2 ComputeFlockFleeDirection()
    {
        Vector2 pos = transform.position;
        Vector2 away = (pos - (Vector2)playerTransform.position).normalized;

        Vector2 alignmentSum = Vector2.zero;
        Vector2 centerSum = Vector2.zero;
        Vector2 separation = Vector2.zero;
        int count = 0;

        foreach (SheepController other in allSheep)
        {
            if (other == this || !other.isFleeing) continue;

            Vector2 otherPos = other.transform.position;
            Vector2 offset = pos - otherPos;
            float dist = offset.magnitude;
            if (dist > neighborRadius) continue;

            count++;
            alignmentSum += other.currentFleeDir;
            centerSum += otherPos;

            // Push away harder the closer the neighbor is
            if (dist > 0.001f)
                separation += (offset / dist) * (1f - dist / neighborRadius);
        }

        Vector2 desired = away * dogWeight;

        if (count > 0)
        {
            Vector2 alignment = alignmentSum / count;
            Vector2 cohesion = Vector2.ClampMagnitude((centerSum / count) - pos, 1f);
            separation = Vector2.ClampMagnitude(separation, 1f);

            desired += alignment * alignmentWeight
                     + cohesion * cohesionWeight
                     + separation * separationWeight;
        }

        // Smooth toward the new direction to avoid jitter
        if (currentFleeDir.sqrMagnitude > 0.0001f)
        {
            desired = Vector2.Lerp(currentFleeDir, desired.normalized, fleeTurnSpeed * Time.deltaTime);
        }

        return desired.sqrMagnitude > 0.0001f ? desired.normalized : away;
    }

    private Vector2 GetFleeDirection(Vector2 desired)
    {
        Vector2 pos = transform.position;
        float[] angles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f };

        foreach (float angle in angles)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * desired;
            if (IsPathClear(pos, pos + dir * 0.5f))
                return dir;
        }
        return Vector2.zero; // boxed in
    }

    private void FleeFromPlayer()
    {
        Vector2 desired = ComputeFlockFleeDirection();
        Vector2 fleeDirection = GetFleeDirection(desired);
        currentFleeDir = fleeDirection;

        if (fleeDirection != Vector2.zero)
        {
            lastInputX = fleeDirection.x;
            lastInputY = fleeDirection.y;
            transform.position += (Vector3)(fleeDirection * fleeSpeed * Time.deltaTime);
        }

        animator.SetFloat("InputX", fleeDirection.x);
        animator.SetFloat("InputY", fleeDirection.y);
        animator.SetBool("IsWalking", fleeDirection != Vector2.zero);
    }

    // ---------- Wandering ----------

    private void MoveToWaypoint()
    {
        Vector2 pos = transform.position;
        Vector2 direction = (currentTarget - pos).normalized;

        if (direction.sqrMagnitude > 0f)
        {
            lastInputX = direction.x;
            lastInputY = direction.y;
        }

        transform.position = Vector2.MoveTowards(pos, currentTarget, moveSpeed * Time.deltaTime);

        animator.SetFloat("InputX", direction.x);
        animator.SetFloat("InputY", direction.y);
        animator.SetBool("IsWalking", true);

        // Stuck detection: if we barely moved over the timeout window, give up on this target
        moveTimer += Time.deltaTime;
        if (moveTimer >= stuckTimeout)
        {
            bool stuck = Vector2.Distance(transform.position, lastPos) < moveSpeed * stuckTimeout * 0.3f;
            moveTimer = 0f;
            lastPos = transform.position;
            if (stuck)
            {
                StartWaiting();
                return;
            }
        }

        if (Vector2.Distance(transform.position, currentTarget) < 0.1f)
        {
            StartWaiting();
        }
    }

    private void StartWaiting()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
        }
        waitCoroutine = StartCoroutine(WaitAtPoint());
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
        moveTimer = 0f;
        lastPos = transform.position;
        isWaiting = false;
    }

    private Vector2 GetRandomNearbyPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        return (Vector2)transform.position + randomOffset;
    }

    private Vector2 GetValidRandomPosition()
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 candidate = GetRandomNearbyPosition();
            if (IsPositionValid(candidate))
            {
                return candidate;
            }
        }
        return transform.position; // fallback: stay put
    }

    private bool IsPositionValid(Vector2 position)
    {
        if (IsAreaBlocked(position)) return false;
        return IsPathClear(transform.position, position);
    }
}