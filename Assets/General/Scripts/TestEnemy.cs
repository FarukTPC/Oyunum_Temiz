using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TestEnemy : MonoBehaviour
{
    // ... (Üst kısımdaki değişkenler AYNI KALSIN) ...
    [Header("Detection Settings (Radar)")]
    public Transform playerTarget; 
    
    [UnityEngine.Range(1.0f, 20.0f)]
    public float detectionRange = 8.0f;

    [UnityEngine.Range(1.0f, 5.0f)]
    public float attackRange = 1.5f; 

    [Header("Combat Settings")]
    public float timeBetweenAttacks = 2.0f; 
    public float enemyDamage = 10f; 

    [Header("Patrol Settings")]
    public float patrolRadius = 5.0f; 
    public float waitTime = 2.0f; 

    private NavMeshAgent agent;
    private Animator animator;
    private bool inCombatMode = false;
    private float lastAttackTime;
    private Vector3 startPoint;
    private float patrolTimer;

    private void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        startPoint = transform.position;
        patrolTimer = waitTime; 

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
        }
        
        agent.updateRotation = false; 
    }

    private void Update()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        
        float speedPercent = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);

        if (distanceToPlayer <= detectionRange)
        {
            EngageCombat(distanceToPlayer);
        }
        else
        {
            Patrol();
        }
    }

    private void EngageCombat(float distance)
    {
        inCombatMode = true;
        animator.SetBool("InCombat", true);
        RotateTowards(playerTarget.position);

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
        }
        else
        {
            agent.isStopped = true;
            if (Time.time >= lastAttackTime + timeBetweenAttacks)
            {
                Attack();
            }
        }
    }

    // --- GÜNCELLENEN KISIM: RANDOM SALDIRI ---
    private void Attack()
    {
        lastAttackTime = Time.time;
        
        // 1 ile 6 arasında (6 dahil değil) rastgele sayı seç: 1, 2, 3, 4, 5
        int randomAttack = Random.Range(1, 6); 
        
        // Tetikleyici ismini oluştur: "Attack1", "Attack2" ... "Attack5"
        string triggerName = "Attack" + randomAttack;
        
        animator.SetTrigger(triggerName);

        // Hasar Verme
        PlayerHealth playerHealth = playerTarget.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(enemyDamage);
        }
    }
    // ------------------------------------------

    // ... (Patrol, RotateTowards, TakeHit gibi diğer fonksiyonlar AYNI KALSIN) ...
    private void Patrol()
    {
        inCombatMode = false;
        animator.SetBool("InCombat", false);
        
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= waitTime)
            {
                Vector3 newPos = RandomNavSphere(startPoint, patrolRadius, -1);
                agent.SetDestination(newPos);
                patrolTimer = 0;
            }
        }
        else
        {
            if(agent.velocity.sqrMagnitude > 0.1f)
                RotateTowards(agent.steeringTarget);
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    private void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; 
        if (direction == Vector3.zero) return;
        
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        float speed = inCombatMode ? 10f : 5f; 
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * speed);
    }

    public void TakeHit(int hitType)
    {
        if (animator == null) return;
        agent.isStopped = true; 
        CancelInvoke("ResumeMovement");
        Invoke("ResumeMovement", 1.0f);

        switch (hitType)
        {
            case 0: animator.SetTrigger("HitNormal"); break;
            case 1: animator.SetTrigger("HitKick"); break;
            case 2: animator.SetTrigger("HitHeavy"); break;
            default: animator.SetTrigger("HitNormal"); break;
        }
    }

    private void ResumeMovement()
    {
        if(agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(startPoint == Vector3.zero ? transform.position : startPoint, patrolRadius);
    }
}