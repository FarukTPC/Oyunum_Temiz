using UnityEngine;

public class TestEnemy : MonoBehaviour
{
    [Header("Detection Settings")]
    public Transform playerTarget;
    public float combatDistance = 4.0f;
    public float faceSpeed = 5.0f;

    private Animator animator;
    private bool inCombatMode = false;

    private void Start()
    {
        animator = GetComponent<Animator>();

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
        }
    }

    private void Update()
    {
        if (playerTarget == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool shouldBeInCombat = distanceToPlayer <= combatDistance;

        if (shouldBeInCombat != inCombatMode)
        {
            inCombatMode = shouldBeInCombat;
            animator.SetBool("InCombat", inCombatMode);
        }

        if (inCombatMode)
        {
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            direction.y = 0;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * faceSpeed);
        }
    }

    public void TakeHit(int hitType)
    {
        if (animator == null) return;
        switch (hitType)
        {
            case 0:
            animator.SetTrigger("HitNormal");
            break;
            case 1:
            animator.SetTrigger("HitKick");
            break;
            case 2:
            animator.SetTrigger("HitHeavy");
            break;
            default:
            animator.SetTrigger("HitNormal");
            break;
        }
    }
}
