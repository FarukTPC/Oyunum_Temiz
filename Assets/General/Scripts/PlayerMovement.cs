using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables

    [Header("Interaction")]
    public bool canMove = true; 
    [SerializeField] private float interactDistance = 1.5f;
    [SerializeField] private LayerMask interactLayer; 

    [Header("Parkour Settings")]
    [SerializeField] private LayerMask climbLayer; 
    [SerializeField] private float maxClimbHeight = 2.5f; 
    [SerializeField] private float climbDuration = 1.2f;
    [Range(0.1f, 1.0f)] public float verticalClimbPercent = 0.8f;
    [Range(0.0f, 0.9f)] public float forwardMoveStartPercent = 0.4f;
    [Range(0.0f, 1.0f)] public float forwardOffset = 0.2f; 

    [Header("Ladder System")]
    public LayerMask ladderLayer; 
    public float ladderClimbSpeed = 1.5f; 
    
    [Tooltip("Merdiven yüzeyinden ne kadar uzakta/derinde durulacak?")]
    [Range(0.0f, 2.0f)] public float ladderDepthOffset = 0.5f; 

    [Tooltip("Merdiven bitince karakter platformun içine ne kadar girsin?")]
    public float ladderExitForwardOffset = 1.2f;

    [Tooltip("Çıkış işlemi tepeden kaç metre önce başlasın? (Havaya tırmanmayı önlemek için bunu 1.0 veya 1.2 yap)")]
    public float ladderExitCheckOffset = 1.0f; // <--- YENİ KRİTİK AYAR

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

        if (canMove && Input.GetKeyDown(KeyCode.F))
        {
            CheckInteractions();
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

    #region Custom Functions

    private void CheckInteractions()
    {
        if (CheckForLadder()) return;
        if (CheckForInteractable()) return;
        CheckForParkour();
    }

    private bool CheckForLadder()
    {
        // 1. GERİDEN TARAMA (Bulletproof)
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f - (transform.forward * 0.5f);
        Ray ray = new Ray(rayOrigin, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1.5f, ladderLayer))
        {
            StartLadderClimbing(hit.collider, hit.normal);
            return true;
        }
        return false;
    }

    private void StartLadderClimbing(Collider ladderCol, Vector3 surfaceNormal)
    {
        isClimbingLadder = true;
        currentLadderCollider = ladderCol;
        
        canMove = false; 
        velocity = Vector3.zero;

        if(animator != null) animator.SetBool("IsClimbing", true);

        // 1. DÖNME
        transform.rotation = Quaternion.LookRotation(-surfaceNormal);
        
        // 2. POZİSYON (HİBRİT)
        float centerX = ladderCol.bounds.center.x;
        float centerZ = ladderCol.bounds.center.z;
        Vector3 ladderCenterPos = new Vector3(centerX, transform.position.y, centerZ);

        Vector3 finalPos = ladderCenterPos + (surfaceNormal * ladderDepthOffset);
        
        transform.position = finalPos;
    }

    private void HandleLadderMovement()
    {
        float inputY = Input.GetAxis("Vertical");

        if(animator != null)
        {
            animator.SetFloat("ClimbSpeed", inputY, 0.1f, Time.deltaTime);
        }

        if (Mathf.Abs(inputY) > 0.1f)
        {
            Vector3 climbVelocity = Vector3.up * inputY * ladderClimbSpeed;
            characterController.Move(climbVelocity * Time.deltaTime);
        }

        // --- ÇIKIŞ KONTROLÜ (GÜNCELLENDİ) ---
        float ladderTopY = currentLadderCollider.bounds.max.y;
        
        // Mantık: Ayaklar (transform.position.y), Merdiven Tepesinden (ladderTopY)
        // 'ladderExitCheckOffset' kadar aşağıdayken çıkışı tetikle.
        // Örn: Offset 1.2 ise, ayaklar tepeye 1.2m kala çıkış yapar.
        if (inputY > 0 && transform.position.y >= ladderTopY - ladderExitCheckOffset)
        {
            ExitLadderTop();
        }

        if (inputY < 0 && characterController.isGrounded)
        {
            ExitLadderBottom();
        }
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            ExitLadderBottom();
        }
    }

    private void ExitLadderTop()
    {
        isClimbingLadder = false;
        if(animator != null) 
        {
            animator.SetBool("IsClimbing", false);
            animator.SetFloat("ClimbSpeed", 0f); 
        }
        
        Vector3 exitPos = transform.position;
        
        // Yükseklik: Tepenin 20 cm üstü
        exitPos.y = currentLadderCollider.bounds.max.y + 0.2f; 
        
        // İleri Gitme: Inspector ayarı kadar
        exitPos += transform.forward * ladderExitForwardOffset; 

        // Işınlanma
        characterController.enabled = false;
        transform.position = exitPos;
        characterController.enabled = true;

        velocity = Vector3.zero;

        canMove = true;
        currentLadderCollider = null;
    }

    private void ExitLadderBottom()
    {
        isClimbingLadder = false;
        if(animator != null) 
        {
            animator.SetBool("IsClimbing", false);
            animator.SetFloat("ClimbSpeed", 0f);
        }
        canMove = true;
        currentLadderCollider = null;
    }

    // --- DİĞER FONKSİYONLAR ---

    private bool CheckForInteractable()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 1.0f, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                return true; 
            }
        }
        return false;
    }

    private void CheckForParkour()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        RaycastHit wallHit;
        if (Physics.Raycast(rayOrigin, transform.forward, out wallHit, 2.5f, climbLayer))
        {
            Vector3 topRayOrigin = wallHit.point + (Vector3.up * maxClimbHeight) + (transform.forward * 0.5f);
            RaycastHit topHit;
            if (Physics.Raycast(topRayOrigin, Vector3.down, out topHit, maxClimbHeight + 0.5f, climbLayer))
            {
                Vector3 finalTargetPoint = new Vector3(wallHit.point.x, topHit.point.y, wallHit.point.z);
                finalTargetPoint += transform.forward * forwardOffset; 
                StartCoroutine(PerformDynamicClimb(finalTargetPoint));
            }
        }
    }

    private IEnumerator PerformDynamicClimb(Vector3 targetPoint)
    {
        canMove = false; 
        characterController.enabled = false; 
        if(animator != null) animator.SetTrigger("Climb");

        Vector3 startPos = transform.position;
        Vector3 lookPos = targetPoint;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        float elapsedTime = 0f;
        while (elapsedTime < climbDuration)
        {
            float t = elapsedTime / climbDuration;
            float yRatio = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / verticalClimbPercent));
            float forwardDuration = 1.0f - forwardMoveStartPercent;
            if (forwardDuration < 0.01f) forwardDuration = 0.01f;
            float xzRatio = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - forwardMoveStartPercent) / forwardDuration));

            float currentY = Mathf.Lerp(startPos.y, targetPoint.y, yRatio);
            Vector3 currentXZ = Vector3.Lerp(new Vector3(startPos.x, 0, startPos.z), new Vector3(targetPoint.x, 0, targetPoint.z), xzRatio);
            transform.position = new Vector3(currentXZ.x, currentY, currentXZ.z);

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        transform.position = targetPoint; 
        characterController.enabled = true; 
        velocity = Vector3.zero;
        yield return null; 
        canMove = true; 
    }

    private void HandleMovement()
    {
        if (isGrounded)
        {
            float input = Input.GetAxis("Horizontal");
            bool isRunning = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching; 
                animator.SetBool("IsCrouching", isCrouching);
            }

            if (isCrouching)
            {
                characterController.height = crouchHeight;
                characterController.center = crouchCenter;
            }
            else
            {
                characterController.height = originalHeight;
                characterController.center = originalCenter;
            }

            if (Mathf.Abs(input) >= 0.1f)
            {
                float currentSpeed = speed; 
                if (isCrouching) currentSpeed = crouchSpeed; 
                else if (isRunning) currentSpeed = speed * 3f;

                Vector3 direction = Quaternion.Euler(0, streetAngle, 0) * Vector3.forward;
                moveDirection = direction * (input > 0 ? 1 : -1) * currentSpeed;

                float lookAngle = input > 0 ? streetAngle : streetAngle + 180f;
                transform.rotation = Quaternion.Euler(0f, lookAngle, 0f);

                float animValue = isCrouching ? currentSpeed : (isRunning ? 1f : 0.5f);
                animator.SetFloat("Speed", animValue, 0.1f, Time.deltaTime);
            }
            else
            {
                moveDirection = Vector3.zero;
                animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            }

            if (Input.GetButtonDown("Jump") && !isCrouching)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetTrigger("Jump");
            }
        }
        characterController.Move(moveDirection * Time.deltaTime);
    }

    #endregion
}