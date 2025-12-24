using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerMovement))]
public class CombatSystem : MonoBehaviour
{
    #region Data Structures

    [System.Serializable]
    public class AttackData
    {
        [Header("Tanımlama")]
        public string attackName;       
        public float damage;            
        
        [Header("Animasyon Ayarları")]
        public string animTrigger;      
        
        [Tooltip("Animasyon başladıktan kaç saniye sonra hasar işlesin?")]
        public float hitTime = 0.4f;

        [Tooltip("Bu saldırının toplam süresi ne kadar?")]
        public float totalDuration = 1.0f;

        [Header("Hareket Ayarları")]
        [Tooltip("Saldırı sırasında karakter tamamen dursun mu? (Uppercut/Tekme için TRUE yap)")]
        public bool stopMovement = true;

        [Tooltip("Saldırı sırasında karakter yürüyebilsin ama KOŞAMASIN. (Yumruklar için TRUE yap)")]
        public bool forceWalk = false; // <--- YENİ ÖZELLİK

        [Header("Menzil")]
        public float attackRange = 1.5f; 
        public float attackRadius = 0.5f; 
        
        [Tooltip("Kombo sıfırlanma toleransı")]
        public float comboResetTime = 0.8f; 
    }

    [System.Serializable]
    public class SpecialAttackData : AttackData
    {
        [Header("Tetikleyici (Basılı Tutulacak)")]
        public KeyCode modifierKey; 
    }

    [System.Serializable]
    public class ActionAttackData : AttackData
    {
        [Header("Tetikleyici (Bas-Çek)")]
        public KeyCode triggerKey; 
    }

    #endregion

    #region Variables

    [Header("Temel Saldırılar (Sol Tık Kombo)")]
    public List<AttackData> punchComboList; 
    private int currentComboIndex = 0;
    private float lastAttackTime = 0;

    [Header("Özel Saldırılar (Q + Sol Tık)")]
    public List<SpecialAttackData> specialAttacks; 

    [Header("Diğer Aksiyonlar (Tekme vb.)")]
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

    #endregion

    #region Unity Methods

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();

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
            playerMovement.InstantStop(); // Blok açınca da zınk diye dursun
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
        {
            currentComboIndex = 0;
        }

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
        // 1. Hareket Kısıtlamalarını Uygula
        if (attack.stopMovement) 
        {
            // Tamamen durdur (Uppercut / Tekme)
            playerMovement.canMove = false;
            playerMovement.InstantStop();
        }
        else if (attack.forceWalk)
        {
            // Sadece yürümeye zorla (Yumruk)
            playerMovement.preventRunning = true;
        }

        // 2. Input KİLİTLENSİN
        isAttacking = true; 

        // 3. Animasyon
        animator.SetTrigger(attack.animTrigger);
        lastAttackTime = Time.time;

        // 4. Vuruş Bekle
        yield return new WaitForSeconds(attack.hitTime);

        // 5. Hasar Ver
        CheckHit(attack);

        // --- INPUT KİLİDİNİ AÇ (Kombo için) ---
        isAttacking = false; 

        // 6. Animasyonun Geri Kalanını Bekle (Kısıtlamalar Devam Ediyor)
        float remainingTime = attack.totalDuration - attack.hitTime;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        // 7. Kısıtlamaları Kaldır
        // Eğer oyuncu bu arada yeni bir saldırıya başlamadıysa (Kombo yapmadıysa)
        if (!isAttacking && !isBlocking) 
        {
            playerMovement.canMove = true;
            playerMovement.preventRunning = false; // Koşma yasağını kaldır
        }
    }

    private void CheckHit(AttackData attack)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(hitPoint.position, attack.attackRadius, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.gameObject == gameObject) continue;
            Debug.Log($"<color=red>VURULDU:</color> {enemy.name} | Hasar: {attack.damage}");
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