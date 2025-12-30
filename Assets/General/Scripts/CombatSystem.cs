using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(AudioSource))]
public class CombatSystem : MonoBehaviour
{
    #region Data Structures

    [System.Serializable]
    public class AttackData
    {
        [Header("Tanımlama")]
        public string attackName;       
        public float damage;            
        
        [Header("Hit Reaction (Düşman Tepkisi)")]
        [Tooltip("0: Normal Yumruk, 1: Tekme, 2: Uppercut")]
        public int hitReactionID = 0; // <--- YENİ: Vuruş tipi kimliği

        [Header("Görsel & İşitsel (Juice)")]
        [Tooltip("Saldırı tuşuna basıldığı an çıkan RÜZGAR/ISLIK sesi.")]
        public AudioClip windSound; 

        [Tooltip("Sadece düşmana temas edince çıkan VURUŞ/ET sesi.")]
        public AudioClip impactSound; 

        [Range(0f, 1f)] public float shakeMagnitude = 0.1f;
        public float shakeDuration = 0.15f; 

        [Header("Animasyon Ayarları")]
        public string animTrigger;      
        public float hitTime = 0.4f;
        public float totalDuration = 1.0f;

        [Header("Hareket Ayarları")]
        public bool stopMovement = true;
        public bool forceWalk = false;

        [Header("Menzil")]
        public float attackRange = 1.5f; 
        public float attackRadius = 0.5f; 
        public float comboResetTime = 0.8f; 
    }

    [System.Serializable]
    public class SpecialAttackData : AttackData
    {
        public KeyCode modifierKey; 
    }

    [System.Serializable]
    public class ActionAttackData : AttackData
    {
        public KeyCode triggerKey; 
    }

    #endregion

    #region Variables

    [Header("Global Efektler")]
    public GameObject hitEffectPrefab; 
    public CameraFollow cameraScript;

    [Header("Temel Saldırılar")]
    public List<AttackData> punchComboList; 
    private int currentComboIndex = 0;
    private float lastAttackTime = 0;

    [Header("Özel & Aksiyon Saldırıları")]
    public List<SpecialAttackData> specialAttacks; 
    public ActionAttackData kickAttack; 

    [Header("Blok Ayarları")]
    public string blockAnimBool = "IsBlocking"; 

    [Header("Ayarlar")]
    public LayerMask enemyLayer; 
    public Transform hitPoint;   

    private bool isBlocking = false;
    private bool isAttacking = false;
    
    private PlayerMovement playerMovement;
    private Animator animator;
    private AudioSource audioSource;

    #endregion

    #region Unity Methods

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (cameraScript == null) 
        {
            if(Camera.main != null) cameraScript = Camera.main.GetComponent<CameraFollow>();
        }

        if (hitPoint == null)
        {
            GameObject tempHit = new GameObject("CombatHitPoint");
            tempHit.transform.parent = transform;
            tempHit.transform.localPosition = new Vector3(0, 1.0f, 0.8f); 
            hitPoint = tempHit.transform;
        }
    }

    private void Update()
    {
        HandleBlocking();
        if (isBlocking || isAttacking) return;
        HandleAttacks();
    }

    #endregion

    #region Logic

    private void HandleBlocking()
    {
        if (isAttacking) return;

        if (Input.GetMouseButton(1))
        {
            isBlocking = true;
            animator.SetBool(blockAnimBool, true);
            playerMovement.canMove = false; 
            playerMovement.InstantStop();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isBlocking = false;
            animator.SetBool(blockAnimBool, false);
            playerMovement.canMove = true;
        }
    }

    private void HandleAttacks()
    {
        if (Time.time - lastAttackTime > GetCurrentComboResetTime())
            currentComboIndex = 0;

        if (Input.GetMouseButtonDown(0))
        {
            bool specialTriggered = false;
            foreach (var special in specialAttacks)
            {
                if (Input.GetKey(special.modifierKey))
                {
                    StartCoroutine(PerformAttackRoutine(special));
                    return; 
                }
            }

            if (!specialTriggered)
            {
                if (punchComboList.Count > 0)
                {
                    StartCoroutine(PerformAttackRoutine(punchComboList[currentComboIndex]));
                    currentComboIndex++;
                    if (currentComboIndex >= punchComboList.Count) currentComboIndex = 0;
                    return; 
                }
            }
        }

        if (Input.GetKeyDown(kickAttack.triggerKey))
        {
            StartCoroutine(PerformAttackRoutine(kickAttack));
            return;
        }
    }

    private IEnumerator PerformAttackRoutine(AttackData attack)
    {
        playerMovement.StopCrouch();        
        playerMovement.preventCrouching = true; 

        if (attack.stopMovement) 
        {
            playerMovement.canMove = false;
            playerMovement.InstantStop();
        }
        else if (attack.forceWalk)
        {
            playerMovement.preventRunning = true;
        }

        isAttacking = true; 

        if (attack.windSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(attack.windSound);
        }

        animator.SetTrigger(attack.animTrigger);
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(attack.hitTime);

        CheckHit(attack);

        isAttacking = false; 

        float remainingTime = attack.totalDuration - attack.hitTime;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        if (!isAttacking && !isBlocking) 
        {
            playerMovement.canMove = true;
            playerMovement.preventRunning = false; 
            playerMovement.preventCrouching = false; 
        }
    }

    private void CheckHit(AttackData attack)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(hitPoint.position, attack.attackRadius, enemyLayer);
        
        bool hasHitAnyEnemy = false;

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.gameObject == gameObject) continue; 

            hasHitAnyEnemy = true;
            Debug.Log($"<color=red>VURULDU:</color> {enemy.name} | Tip: {attack.hitReactionID}");

            // --- YENİ KISIM: Vuruş Tipini (ID) Gönderiyoruz ---
            TestEnemy dummy = enemy.GetComponent<TestEnemy>();
            if (dummy != null)
            {
                // Artık sadece "Vuruldum" demiyoruz, "Şu şekilde vuruldum" diyoruz.
                dummy.TakeHit(attack.hitReactionID);
            }
            // -----------------------------------------------

            if (hitEffectPrefab != null)
            {
                Vector3 impactPoint = enemy.ClosestPoint(hitPoint.position);
                GameObject vfx = Instantiate(hitEffectPrefab, impactPoint, Quaternion.LookRotation(hitPoint.forward));
                Destroy(vfx, 2.0f);
            }
        }

        if (hasHitAnyEnemy)
        {
            if (cameraScript != null)
            {
                cameraScript.TriggerShake(attack.shakeDuration, attack.shakeMagnitude);
            }

            if (attack.impactSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.8f, 1.2f);
                audioSource.PlayOneShot(attack.impactSound);
            }
        }
    }

    private float GetCurrentComboResetTime()
    {
        if (punchComboList.Count == 0) return 1f;
        int safeIndex = Mathf.Clamp(currentComboIndex - 1, 0, punchComboList.Count - 1);
        return punchComboList[safeIndex].comboResetTime;
    }

    private void OnDrawGizmosSelected()
    {
        if (hitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hitPoint.position, 0.5f);
        }
    }

    #endregion
}