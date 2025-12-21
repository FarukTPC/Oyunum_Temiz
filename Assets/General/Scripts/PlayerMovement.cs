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
    
    [Header("Ladder Settings")]
    public float ladderClimbSpeed = 3f; 
    public float ladderCheckDistance = 0.8f; 
    public float ladderDepthOffset = 0.4f; 
    public float ladderExitForwardOffset = 1.0f;
    public float ladderExitCheckOffset = 1.0f;

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

        // Yer Kontrolü (Ground Check)
        isGrounded = characterController.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        // Yerçekimi düzeltmesi (Yerdeyken aşağı kuvvet uygula ki 'isGrounded' titremesin)
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        // --- DÜZELTME BURADA ---
        // Sadece hareket edebiliyorsan, F'ye basıyorsan VE YERDEYSEN (isGrounded) çalışır.
        // Havada zıplarken F'ye basarsan hiçbir şey olmaz.
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
        
        Debug.DrawRay(highPoint, Vector3.down * 4.0f, Color.green, 2f);

        if (Physics.Raycast(highPoint, Vector3.down, out topHit, 4.0f, col.gameObject.layer))
        {
            return topHit.point;
        }
        
        if (Physics.Raycast(highPoint, Vector3.down, out topHit, 4.0f))
        {
             if(topHit.collider == col) return topHit.point;
        }

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
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            transform.position = currentPos;
            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        transform.position = targetPos; 
        characterController.enabled = true; 
        velocity = Vector3.zero;
        canMove = true; 
    }

    // --- MERDİVEN ---
    private bool CheckLadder()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.8f;
        Debug.DrawRay(rayOrigin, transform.forward * ladderCheckDistance, Color.cyan, 2f);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, ladderCheckDistance, ladderLayer))
        {
            StartLadderClimbing(hit.collider, hit.normal);
            return true;
        }
        return false;
    }

    private void StartLadderClimbing(Collider ladderCol, Vector3 hitNormal)
    {
        isClimbingLadder = true;
        currentLadderCollider = ladderCol;
        canMove = false; 
        velocity = Vector3.zero; 
        if(animator != null) animator.SetBool("IsClimbing", true);

        float targetX = ladderCol.bounds.center.x;
        float targetZ = ladderCol.bounds.center.z + (hitNormal.z * ladderDepthOffset);
        Vector3 snapPosition = new Vector3(targetX, transform.position.y, targetZ);

        if (Mathf.Abs(hitNormal.x) > 0.5f)
        {
            snapPosition.z = ladderCol.bounds.center.z;
            snapPosition.x = ladderCol.bounds.center.x + (hitNormal.x * ladderDepthOffset);
        }
        transform.position = snapPosition;
    }

    private void HandleLadderMovement()
    {
        float inputY = Input.GetAxis("Vertical");
        if(animator != null) animator.SetFloat("ClimbSpeed", inputY, 0.1f, Time.deltaTime);
        characterController.Move(Vector3.up * inputY * ladderClimbSpeed * Time.deltaTime);

        float ladderTopY = currentLadderCollider.bounds.max.y;
        if (inputY > 0 && transform.position.y >= ladderTopY - ladderExitCheckOffset) ExitLadderTop();
        if (inputY < 0 && characterController.isGrounded) ExitLadderBottom();
        if (Input.GetKeyDown(KeyCode.F)) ExitLadderBottom();
    }

    private void ExitLadderTop()
    {
        isClimbingLadder = false;
        if(animator != null) { animator.SetBool("IsClimbing", false); animator.SetFloat("ClimbSpeed", 0f); }
        Vector3 exitPos = transform.position;
        exitPos.y = currentLadderCollider.bounds.max.y + 0.2f; 
        exitPos += transform.forward * ladderExitForwardOffset; 
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
        if(animator != null) { animator.SetBool("IsClimbing", false); animator.SetFloat("ClimbSpeed", 0f); }
        canMove = true;
        currentLadderCollider = null;
    }

    private bool CheckInteractable()
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