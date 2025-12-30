using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TestEnemy : MonoBehaviour
{
    [Header("Detection Settings (Radar)")]
    public Transform playerTarget; 
    
    [UnityEngine.Range(1.0f, 20.0f)]
    public float detectionRange = 8.0f;

    // --- DÜZELTME 1: Varsayılan değeri 1.5'ten 1.0'a düşürdük ---
    [Tooltip("Düşmanın vurmak için ne kadar yaklaşması gerekiyor?")]
    [UnityEngine.Range(0.5f, 3.0f)]
    public float attackRange = 0.5f; // Burun buruna gelmesi için kısalttık

    [Header("Movement Settings (Hız Ayarları)")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 6.0f;

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

        // --- DÜZELTME 2: Agent'ın kendi frenini kapatıyoruz ---
        // Böylece "attackRange" içine girene kadar duraksamadan koşacak.
        agent.stoppingDistance = 0f; 
        // -----------------------------------------------------

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) 
            {
                // Root objeyi (Karakterin kendisini) aldığımızdan emin oluyoruz.
                playerTarget = playerObj.transform;
            }
        }
        
        agent.updateRotation = false; 
    }

    private void Update()
    {
        if (playerTarget == null) return;

        // Mesafeyi ölçerken "Player'ın Merkezi" ile "Enemy'nin Merkezi" arasını ölçüyoruz.
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        
        float speedPercent = agent.velocity.magnitude / runSpeed;
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
            agent.speed = runSpeed; 
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
        }
        else
        {
            // Menzile girdi, DUR ve SALDIR
            agent.isStopped = true;
            
            // Eğer hala çok uzaktaysa (Colliderlar yüzünden) hafifçe kaydırılabilir
            // Ama 1.0f genelde yeterli bir yakınlıktır.

            if (Time.time >= lastAttackTime + timeBetweenAttacks)
            {
                Attack();
            }
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;
        
        int randomAttack = Random.Range(1, 6); 
        string triggerName = "Attack" + randomAttack;
        
        animator.SetTrigger(triggerName);

        PlayerHealth playerHealth = playerTarget.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(enemyDamage);
        }
    }

    private void Patrol()
    {
        inCombatMode = false;
        animator.SetBool("InCombat", false);
        agent.speed = walkSpeed;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= waitTime)
            {
                Vector3 newPos = GetRandomZPoint();
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

    private Vector3 GetRandomZPoint()
    {
        float randomZ = Random.Range(-patrolRadius, patrolRadius);
        Vector3 targetPos = new Vector3(startPoint.x, startPoint.y, startPoint.z + randomZ);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return startPoint;
    }

    private void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; 
        if (direction == Vector3.zero) return;
        
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        float rotateSpeed = inCombatMode ? 10f : 5f; 
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotateSpeed);
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
        Vector3 center = startPoint == Vector3.zero ? transform.position : startPoint;
        Gizmos.DrawLine(center + Vector3.forward * patrolRadius, center + Vector3.back * patrolRadius);
        
        // Saldırı menzilini de sarı bir küreyle görelim
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}