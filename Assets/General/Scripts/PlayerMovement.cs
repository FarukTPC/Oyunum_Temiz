using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables

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
    public float ladderTopOnDuration = 1.3f; // Biraz arttırdık, sarkma hissi için
    public float ladderTopOffDuration = 1.3f; // Biraz arttırdık, çıkma ağırlığı için

    [Tooltip("Platforma çıkınca ne kadar ileri gitsin?")]
    public float ladderExitForwardOffset = 0.6f;

    [Tooltip("Yukarıdan merdivene inmek için ne kadar önden tarasın?")]
    public float ladderTopCheckOffset = 0.4f; 

    [Header("Movement Curves (ÖNEMLİ: Grafikleri Ayarla)")]
    [Tooltip("Tepeden Çıkış: Y (Yükselme) Hareketi")]
    public AnimationCurve topExitYCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [Tooltip("Tepeden Çıkış: Z (İleri Gitme) Hareketi")]
    public AnimationCurve topExitZCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    
    [Tooltip("Tepeden İniş: Sarkma Hareketi (Y)")]
    public AnimationCurve topEntryYCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    // Animator Trigger İsimleri
    private string animLadderBottomOn = "LadderBottomOn";
    private string animLadderTopOn = "LadderTopOn";
    private string animLadderTopOff = "LadderTopOff";

    private bool isClimbingLadder = false;
    private Collider currentLadderCollider;

    [Header("Movement Settings")]
    public float speed = 4f; 
    [Range(0, 360)] public float streetAngle = 0f; 

    [Header("Jump Settings")]
    public float jumpHeight = 1.2f; 

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

        // --- VARSAYILAN EĞRİLERİ OLUŞTUR (Eğer boşsa) ---
        if(topExitYCurve.length == 0 || topExitYCurve.keys[1].value == 0) SetupDefaultCurves();
    }

    private void SetupDefaultCurves()
    {
        // Çıkış (Exit): Önce hızlı yüksel, sonra yavaşça ileri git
        topExitYCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.4f, 0.8f), new Keyframe(1, 1));
        topExitZCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.4f, 0.1f), new Keyframe(1, 1));

        // İniş (Entry): Önce yavaşça sark, sonra hızlan
        topEntryYCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 0.3f), new Keyframe(1, 1));
    }

    private void Update()
    {
        if (isClimbingLadder)
        {
            HandleLadderMovement();
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

        if (canMove)
        {
            HandleMovement();
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
            moveDirection = Vector3.zero;
        }
    }

    #endregion

    #region Interaction Logic

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
        // 1. AŞAĞIDAN YUKARI
        Vector3 rayOrigin = transform.position + Vector3.up * 0.8f;
        RaycastHit hit;
        Debug.DrawRay(rayOrigin, transform.forward * ladderCheckDistance, Color.cyan, 2f);

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, ladderCheckDistance, ladderLayer))
        {
            StartCoroutine(EnterLadderBottomCoroutine(hit.collider, hit.normal));
            return true;
        }

        // 2. YUKARIDAN AŞAĞI
        Vector3 downCheckOrigin = transform.position + (transform.forward * ladderTopCheckOffset) + (Vector3.up * 1.0f);
        Debug.DrawRay(downCheckOrigin, Vector3.down * 3.0f, Color.yellow, 2f);

        if (Physics.SphereCast(downCheckOrigin, 0.5f, Vector3.down, out hit, 3.0f, ladderLayer))
        {
            StartCoroutine(EnterLadderTopCoroutine(hit.collider, hit.transform.forward)); 
            return true;
        }

        return false;
    }

    // --- 1. AŞAĞIDAN GİRİŞ (Düzeltildi) ---
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
            float t = elapsedTime / ladderBottomOnDuration;
            
            // "Ease Out" hareketi (Yavaşça dur)
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
            
            transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        currentLadderCollider = ladderCol;
        isClimbingLadder = true;
        characterController.enabled = true;
        
        if (animator != null) animator.SetBool("IsClimbing", true);
    }

    // --- 2. YUKARIDAN GİRİŞ (REALİSTİK SARKMA) ---
    private IEnumerator EnterLadderTopCoroutine(Collider ladderCol, Vector3 ladderForward)
    {
        canMove = false;
        characterController.enabled = false;

        if (animator != null) animator.SetTrigger(animLadderTopOn);

        transform.rotation = Quaternion.Euler(0, ladderFaceRotation + streetAngle, 0); 

        Vector3 startPos = transform.position;
        Vector3 finalPos = CalculateLadderSnapPosition(ladderCol, ladderForward);
        finalPos.y = ladderCol.bounds.max.y - 1.2f; 

        // Platformun kenarında hafif havada başlasın (Ayakları boşluğa gelsin)
        Vector3 edgePos = startPos + (transform.forward * 0.3f);

        float elapsedTime = 0f;
        while (elapsedTime < ladderTopOnDuration)
        {
            float t = elapsedTime / ladderTopOnDuration;
            
            // CURVE KULLANIMI: Y ekseni özel grafiğe göre iner
            float yProgress = topEntryYCurve.Evaluate(t);
            float xzProgress = Mathf.SmoothStep(0, 1, t); // Yatayda yumuşak geçiş

            Vector3 currentPos;
            
            // Yatayda: Kenardan -> Merdivene
            currentPos.x = Mathf.Lerp(startPos.x, finalPos.x, xzProgress);
            currentPos.z = Mathf.Lerp(startPos.z, finalPos.z, xzProgress);
            
            // Dikeyde: Kenardan -> Aşağı sark
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

    // --- 3. TEPEDEN ÇIKIŞ (REALİSTİK VAULT) ---
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
            
            // --- CURVE SİSTEMİ ---
            // Y Grafiği: Karakterin yukarı ne zaman çıkacağını belirler.
            // Z Grafiği: Karakterin ileri ne zaman atılacağını belirler.
            
            float yProgress = topExitYCurve.Evaluate(t);
            float zProgress = topExitZCurve.Evaluate(t);

            Vector3 currentPos;
            
            // X: Linear git (Zaten kilitli, fark etmez)
            currentPos.x = Mathf.Lerp(startPos.x, targetPos.x, zProgress); 
            
            // Z: İleri gitme eğrisi
            currentPos.z = Mathf.Lerp(startPos.z, targetPos.z, zProgress); 
            
            // Y: Yükselme eğrisi
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

    // --- HEDEF HESAPLAMA ---
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

        float ladderTopY = currentLadderCollider.bounds.max.y;
        
        if (inputY > 0 && transform.position.y >= ladderTopY - 1.2f)
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

    // --- PARKUR (Değişmedi) ---
    private bool CheckParkourAction(LayerMask layer, string triggerName, float duration)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * parkourRayHeight;
        RaycastHit hit;
        Debug.DrawRay(rayOrigin, transform.forward * wallCheckDistance, Color.red, 2f);

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
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            if (Input.GetKeyDown(KeyCode.C)) { isCrouching = !isCrouching; animator.SetBool("IsCrouching", isCrouching); }

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

            if (Input.GetButtonDown("Jump") && !isCrouching) { velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); animator.SetTrigger("Jump"); }
        }
        characterController.Move(moveDirection * Time.deltaTime);
    }

    #endregion
}