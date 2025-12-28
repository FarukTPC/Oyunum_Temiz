using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables

    [HideInInspector] public bool preventRunning = false;
    [HideInInspector] public bool preventCrouching = false; // <--- YENİ: Eğilme yasağı

    [Header("Interaction")]
    public bool canMove = true; 
    [SerializeField] private float interactDistance = 1.2f;
    [SerializeField] private LayerMask interactLayer; 

    // --- PARKUR SİSTEMİ ---
    [Header("Parkour & Climbing Layers")]
    public LayerMask medWallLayer; 
    public LayerMask bigWallLayer; 
    public LayerMask climbableLayer; 
    public LayerMask ladderLayer; 

    [Header("Parkour Animation Triggers")]
    public string bigWallAnimTrigger = "BigClimb";
    public string medWallAnimTrigger = "MedClimb";
    public string smallClimbAnimTrigger = "Climb"; 

    [Header("Parkour Check Settings")]
    public float parkourRayHeight = 0.2f; 
    public float wallCheckDistance = 1.0f; 
    public float landOffset = 0.1f; 

    [Header("Parkour Animation Durations")]
    public float medWallClimbDuration = 1.5f;
    public float bigWallClimbDuration = 2.5f;
    public float smallClimbDuration = 1.0f;
    
    [Header("Ladder System (Realistik)")]
    public float ladderClimbSpeed = 3f; 
    public float ladderCheckDistance = 1.0f; 
    
    [Tooltip("Karakter merdivenden ne kadar uzakta (derinlikte) dursun?")]
    public float ladderDepthOffset = 0.4f; 

    [Tooltip("Merdivene tırmanırken karakterin Y açısı kaç olsun?")]
    public float ladderFaceRotation = 0f;
    
    [Header("Ladder Animation & Physics")]
    public float ladderBottomOnDuration = 1.0f; 
    public float ladderTopOnDuration = 1.3f; 
    
    [Tooltip("Çıkış animasyonu süresi (Kısa kalırsa snap olur, biraz artırdık)")]
    public float ladderTopOffDuration = 1.6f; 

    [Tooltip("Platforma çıkınca ne kadar ileri gitsin?")]
    public float ladderExitForwardOffset = 0.6f;

    [Tooltip("Yukarıdan merdivene inmek için ne kadar önden tarasın?")]
    public float ladderTopCheckOffset = 0.4f; 

    [Header("Movement Curves (Koddan Otomatik Ayarlanır)")]
    public AnimationCurve topExitYCurve; // Yükselme
    public AnimationCurve topExitZCurve; // İlerleme
    public AnimationCurve topEntryYCurve; // İniş

    // Animator Trigger İsimleri
    private string animLadderBottomOn = "LadderBottomOn";
    private string animLadderTopOn = "LadderTopOn";
    private string animLadderTopOff = "LadderTopOff";

    private bool isClimbingLadder = false;
    private Collider currentLadderCollider;

    [Header("Movement Settings")]
    public float speed = 4f; 
    [Range(0, 360)] public float streetAngle = 0f; 

    // NOT: Zıplama Ayarları Kaldırıldı.

    [Header("Crouch Settings")]
    public float crouchHeight = 1.0f; 
    public float crouchSpeed = 2.0f;  
    
    private float originalHeight;   
    private Vector3 originalCenter; 
    private Vector3 crouchCenter;   

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f; 
    
    private CharacterController characterController;
    private Animator animator;
    
    private Vector3 velocity;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isCrouching = false;

    #endregion

    #region Unity Methods

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        originalHeight = characterController.height;
        originalCenter = characterController.center;
        crouchCenter = new Vector3(originalCenter.x, originalCenter.y * 0.5f, originalCenter.z);

        // Curve'leri başlat
        SetupDefaultCurves();
    }

    private void SetupDefaultCurves()
    {
        // --- MERDİVEN DÜZELTMESİ: L ŞEKLİNDE HAREKET ---
        
        // Y Eğrisi (Yükselme): Hızlıca yüksel (Ayaklar yere takılmasın)
        // Animasyonun %50'sinde yüksekliğin %95'ine ulaş.
        topExitYCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 0.95f), new Keyframe(1, 1));

        // Z Eğrisi (İlerleme): Yükselme bitene kadar bekle.
        // Animasyonun %50'sine kadar neredeyse hiç ileri gitme (%10). Sonra git.
        topExitZCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 0.1f), new Keyframe(1, 1));

        // İniş (Entry): Standart
        topEntryYCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 0.3f), new Keyframe(1, 1));
    }

    private void Update()
    {
        if (isClimbingLadder)
        {
            HandleLadderMovement();
            return; 
        }

        // --- HAREKET KİLİDİ VE HARD STOP ---
        // Eğer hareket iznimiz yoksa (Saldırıyorsak, blokluyorsak vs.)
        if (!canMove)
        {
            // Animator'ı anında durdur
            animator.SetFloat("Speed", 0f);
            
            // Yerçekimi hariç tüm hızı öldür (KAYMAYI ÖNLER)
            velocity.x = 0;
            velocity.z = 0;
            
            // Sadece yerçekimi uygula (havada asılı kalmasın diye)
            isGrounded = characterController.isGrounded;
            if (isGrounded && velocity.y < 0) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);

            // Başka hiçbir işlem yapma ve çık
            return; 
        }

        isGrounded = characterController.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        if (canMove && Input.GetKeyDown(KeyCode.F) && isGrounded)
        {
            CheckAllInteractions();
        }

        // Hareket izni varsa normal işlemlere devam et
        HandleMovement();
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    #endregion

    #region Interaction Logic

    // --- YENİ EKLENEN FONKSİYON: Zorla Ayağa Kaldır ---
    // CombatSystem saldırı anında bunu çağıracak.
    public void StopCrouch()
    {
        if (isCrouching)
        {
            isCrouching = false;
            animator.SetBool("IsCrouching", false);

            // Saldırı anında 'HandleMovement' çalışmayacağı için (canMove=false olduğu için),
            // boyu ve merkezi burada MANUEL olarak düzeltmeliyiz.
            characterController.height = originalHeight;
            characterController.center = originalCenter;
        }
    }

    private void CheckAllInteractions()
    {
        if (CheckLadder()) return;
        if (CheckParkourAction(bigWallLayer, bigWallAnimTrigger, bigWallClimbDuration)) return;
        if (CheckParkourAction(medWallLayer, medWallAnimTrigger, medWallClimbDuration)) return;
        if (CheckParkourAction(climbableLayer, smallClimbAnimTrigger, smallClimbDuration)) return;
        CheckInteractable();
    }

    // --- MERDİVEN SİSTEMİ ---
    private bool CheckLadder()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.8f;
        RaycastHit hit;
        
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, ladderCheckDistance, ladderLayer))
        {
            StartCoroutine(EnterLadderBottomCoroutine(hit.collider, hit.normal));
            return true;
        }

        Vector3 downCheckOrigin = transform.position + (transform.forward * ladderTopCheckOffset) + (Vector3.up * 1.0f);
        if (Physics.SphereCast(downCheckOrigin, 0.5f, Vector3.down, out hit, 3.0f, ladderLayer))
        {
            StartCoroutine(EnterLadderTopCoroutine(hit.collider, hit.transform.forward)); 
            return true;
        }

        return false;
    }

    private IEnumerator EnterLadderBottomCoroutine(Collider ladderCol, Vector3 hitNormal)
    {
        canMove = false;
        characterController.enabled = false;
        
        if (animator != null) animator.SetTrigger(animLadderBottomOn);

        Vector3 startPos = transform.position;
        Vector3 targetPos = CalculateLadderSnapPosition(ladderCol, hitNormal);
        
        float elapsedTime = 0f;
        while (elapsedTime < ladderBottomOnDuration)
        {
            float t = Mathf.Sin((elapsedTime / ladderBottomOnDuration) * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        currentLadderCollider = ladderCol;
        isClimbingLadder = true;
        characterController.enabled = true;
        
        if (animator != null) animator.SetBool("IsClimbing", true);
    }

    private IEnumerator EnterLadderTopCoroutine(Collider ladderCol, Vector3 ladderForward)
    {
        canMove = false;
        characterController.enabled = false;

        if (animator != null) animator.SetTrigger(animLadderTopOn);

        transform.rotation = Quaternion.Euler(0, ladderFaceRotation + streetAngle, 0); 

        Vector3 startPos = transform.position;
        Vector3 finalPos = CalculateLadderSnapPosition(ladderCol, ladderForward);
        finalPos.y = ladderCol.bounds.max.y - 1.2f; 

        float elapsedTime = 0f;
        while (elapsedTime < ladderTopOnDuration)
        {
            float t = elapsedTime / ladderTopOnDuration;
            
            float yProgress = topEntryYCurve.Evaluate(t);
            float xzProgress = Mathf.SmoothStep(0, 1, t);

            Vector3 currentPos;
            currentPos.x = Mathf.Lerp(startPos.x, finalPos.x, xzProgress);
            currentPos.z = Mathf.Lerp(startPos.z, finalPos.z, xzProgress);
            currentPos.y = Mathf.Lerp(startPos.y, finalPos.y, yProgress);

            transform.position = currentPos;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = finalPos;
        transform.rotation = Quaternion.Euler(0, ladderFaceRotation + streetAngle, 0); 
        currentLadderCollider = ladderCol;
        isClimbingLadder = true;
        characterController.enabled = true;

        if (animator != null) animator.SetBool("IsClimbing", true);
    }

    private void ExitLadderTop()
    {
        if (!isClimbingLadder) return; 
        StartCoroutine(ExitLadderTopCoroutine());
    }

    private IEnumerator ExitLadderTopCoroutine()
    {
        isClimbingLadder = false; 
        canMove = false; 
        characterController.enabled = false;

        if (animator != null)
        {
            animator.SetBool("IsClimbing", false);
            animator.SetTrigger(animLadderTopOff); 
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos;
        targetPos.y = currentLadderCollider.bounds.max.y + 0.05f; 
        targetPos += transform.forward * ladderExitForwardOffset; 

        float elapsedTime = 0f;
        while (elapsedTime < ladderTopOffDuration)
        {
            float t = elapsedTime / ladderTopOffDuration;
            
            // --- EĞRİLER DEVREDE ---
            float yProgress = topExitYCurve.Evaluate(t);
            float zProgress = topExitZCurve.Evaluate(t);

            Vector3 currentPos;
            currentPos.x = Mathf.Lerp(startPos.x, targetPos.x, zProgress); 
            currentPos.z = Mathf.Lerp(startPos.z, targetPos.z, zProgress); 
            currentPos.y = Mathf.Lerp(startPos.y, targetPos.y, yProgress); 

            transform.position = currentPos;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        characterController.enabled = true;
        velocity = Vector3.zero;
        canMove = true;
        currentLadderCollider = null;
    }

    private Vector3 CalculateLadderSnapPosition(Collider ladderCol, Vector3 forwardDir)
    {
        Vector3 center = ladderCol.bounds.center;
        Vector3 snapPos = center;
        snapPos.x = center.x; 

        float zDirection = ladderCol.transform.forward.z;
        if (Mathf.Abs(zDirection) < 0.1f) zDirection = -1f; 
        zDirection = Mathf.Sign(zDirection);

        snapPos.z = center.z + (zDirection * ladderDepthOffset);
        snapPos.y = transform.position.y;

        return snapPos;
    }

    private void HandleLadderMovement()
    {
        float inputY = Input.GetAxis("Vertical");
        if(animator != null) animator.SetFloat("ClimbSpeed", inputY, 0.1f, Time.deltaTime);
        
        Vector3 move = Vector3.up * inputY * ladderClimbSpeed * Time.deltaTime;
        characterController.Move(move);

        // --- TETİKLEME YÜKSEKLİĞİ ---
        float exitThreshold = currentLadderCollider.bounds.max.y - 1.0f;

        if (inputY > 0 && transform.position.y >= exitThreshold)
        {
            ExitLadderTop();
        }

        if (inputY < 0 && characterController.isGrounded)
        {
            isClimbingLadder = false;
            canMove = true;
            currentLadderCollider = null;
            if(animator != null) animator.SetBool("IsClimbing", false);
        }
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            isClimbingLadder = false;
            canMove = true;
            currentLadderCollider = null;
            if(animator != null) animator.SetBool("IsClimbing", false);
        }
    }

    // --- PARKUR SİSTEMİ ---
    private bool CheckParkourAction(LayerMask layer, string triggerName, float duration)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * parkourRayHeight;
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, wallCheckDistance, layer))
        {
            Vector3 topPosition = FindTopPoint(hit.collider, hit.point);
            if (topPosition == Vector3.zero) 
                topPosition = new Vector3(hit.point.x, hit.collider.bounds.max.y, hit.point.z);

            Vector3 targetPos = topPosition + (transform.forward * landOffset); 
            StartCoroutine(PerformParkourMove(targetPos, duration, triggerName));
            return true;
        }
        return false;
    }

    private Vector3 FindTopPoint(Collider col, Vector3 hitPoint)
    {
        Vector3 highPoint = hitPoint + Vector3.up * 3.0f + transform.forward * 0.2f;
        RaycastHit topHit;
        if (Physics.Raycast(highPoint, Vector3.down, out topHit, 4.0f, col.gameObject.layer)) return topHit.point;
        if (Physics.Raycast(highPoint, Vector3.down, out topHit, 4.0f)) if(topHit.collider == col) return topHit.point;
        return Vector3.zero;
    }

    private IEnumerator PerformParkourMove(Vector3 targetPos, float duration, string animTrigger)
    {
        canMove = false; 
        characterController.enabled = false; 
        if(animator != null) animator.SetTrigger(animTrigger);

        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.position = targetPos; 
        characterController.enabled = true; 
        velocity = Vector3.zero;
        canMove = true; 
    }

    // --- GENEL HAREKET ---
    private bool CheckInteractable()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 1.0f, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null) { interactable.Interact(this); return true; }
        }
        return false;
    }

    private void HandleMovement()
    {
        if (isGrounded)
        {
            float input = Input.GetAxis("Horizontal");
            bool isRunning = Input.GetKey(KeyCode.LeftShift) && !preventRunning;
            
            // --- C TUŞU KONTROLÜ (GÜNCELLENDİ) ---
            if (Input.GetKeyDown(KeyCode.C) && !preventCrouching) 
            { 
                isCrouching = !isCrouching; 
                animator.SetBool("IsCrouching", isCrouching); 
            }

            if (isCrouching) { characterController.height = crouchHeight; characterController.center = crouchCenter; }
            else { characterController.height = originalHeight; characterController.center = originalCenter; }

            if (Mathf.Abs(input) >= 0.1f)
            {
                float currentSpeed = speed * (isCrouching ? crouchSpeed : (isRunning ? 3f : 1f));
                Vector3 direction = Quaternion.Euler(0, streetAngle, 0) * Vector3.forward;
                moveDirection = direction * (input > 0 ? 1 : -1) * currentSpeed;
                float lookAngle = input > 0 ? streetAngle : streetAngle + 180f;
                transform.rotation = Quaternion.Euler(0f, lookAngle, 0f);
                animator.SetFloat("Speed", isCrouching ? currentSpeed : (isRunning ? 1f : 0.5f), 0.1f, Time.deltaTime);
            }
            else { moveDirection = Vector3.zero; animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime); }

            // Zıplama bloğu kaldırıldı.
        }
        characterController.Move(moveDirection * Time.deltaTime);
    }

    public void InstantStop()
    {
        // 1. Yönü ve Hızı tamamen öldür
        moveDirection = Vector3.zero;
        velocity = new Vector3(0, velocity.y, 0); 
        
        // 2. Animasyon hızını anında kes
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    #endregion
}