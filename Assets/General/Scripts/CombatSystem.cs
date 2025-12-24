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
        public float hitTime = 0.4f;    // Vuruşun temas anı

        [Tooltip("Bu saldırının toplam süresi ne kadar? (Bu süre bitmeden yeni saldırı yapılamaz)")]
        public float totalDuration = 1.0f; // Kilitlenme süresi

        [Header("Menzil")]
        public float attackRange = 1.5f; 
        public float attackRadius = 0.5f; 
        
        [Tooltip("Kombo sıfırlanma toleransı")]
        public float comboResetTime = 0.8f; 
        
        public bool stopMovement = true;
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

    // Durumlar
    private bool isBlocking = false;
    private bool isAttacking = false; // BU ARTIK KRİTİK KİLİT NOKTASI
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
        
        // Eğer blokluyorsa veya ZATEN SALDIRIYORSA hiçbir tuşa basamaz.
        if (isBlocking || isAttacking) return;

        HandleAttacks();
    }

    #endregion

    #region Logic

    private void HandleBlocking()
    {
        if (isAttacking) return; // Saldırırken blok yapamazsın

        if (Input.GetMouseButton(1))
        {
            isBlocking = true;
            animator.SetBool(blockAnimBool, true);
            playerMovement.canMove = false; 
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
        // Kombo zaman aşımı
        if (Time.time - lastAttackTime > GetCurrentComboResetTime())
        {
            currentComboIndex = 0;
        }

        // --- SOL TIK ---
        if (Input.GetMouseButtonDown(0))
        {
            bool specialTriggered = false;
            foreach (var special in specialAttacks)
            {
                if (Input.GetKey(special.modifierKey))
                {
                    StartCoroutine(PerformAttackRoutine(special));
                    specialTriggered = true;
                    break; 
                }
            }

            if (!specialTriggered)
            {
                if (punchComboList.Count > 0)
                {
                    StartCoroutine(PerformAttackRoutine(punchComboList[currentComboIndex]));
                    currentComboIndex++;
                    if (currentComboIndex >= punchComboList.Count) currentComboIndex = 0;
                }
            }
        }

        // --- DİĞER AKSİYONLAR (Tekme) ---
        if (Input.GetKeyDown(kickAttack.triggerKey))
        {
            StartCoroutine(PerformAttackRoutine(kickAttack));
        }
    }

// CombatSystem.cs içindeki PerformAttackRoutine fonksiyonunu bununla değiştir:

private IEnumerator PerformAttackRoutine(AttackData attack)
{
    // --- BURASI DEĞİŞTİ ---
    // 1. Kilidi Vur ve ANINDA DURDUR
    isAttacking = true; 
    
    if (attack.stopMovement) 
    {
        playerMovement.InstantStop(); // <--- YENİ EKLEDİĞİMİZ FREN KODU
        playerMovement.canMove = false;
    }
    // ----------------------

    // 2. Animasyonu Başlat
    animator.SetTrigger(attack.animTrigger);
    lastAttackTime = Time.time;

    // 3. Vuruş Anına Kadar Bekle
    yield return new WaitForSeconds(attack.hitTime);

    // 4. Hasar Ver
    CheckHit(attack);

    // 5. Animasyonun Geri Kalanını Bekle
    float remainingTime = attack.totalDuration - attack.hitTime;
    if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

    // 6. Kilidi Aç
    isAttacking = false;
    if (!isBlocking) playerMovement.canMove = true;
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