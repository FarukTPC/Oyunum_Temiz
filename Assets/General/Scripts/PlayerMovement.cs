using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables

    [Header("Interaction (Etkileşim)")]
    public bool canMove = true; 
    [SerializeField] private float interactDistance = 1.5f;
    [SerializeField] private LayerMask interactLayer; 

    [Header("Auto-Climb Settings (Otomatik Tırmanma)")]
    [Tooltip("Sadece bu layerdaki objelere tırmanır")]
    [SerializeField] private LayerMask climbLayer; 
    [SerializeField] private float maxClimbHeight = 2.5f; 
    [SerializeField] private float climbDuration = 1.2f;

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

    [Header("Gravity Settings")]
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
        isGrounded = characterController.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        if (canMove && Input.GetKeyDown(KeyCode.F))
        {
            CheckInteraction();
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

    private void CheckInteraction()
    {
        // 1. Önce özel etkileşim var mı (Kapı, Sandık vs.)
        Ray ray = new Ray(transform.position + Vector3.up * 1f, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                return; 
            }
        }

        // 2. Yoksa Tırmanma var mı?
        CheckForClimb();
    }

    private void CheckForClimb()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
        RaycastHit wallHit;

        if (Physics.Raycast(rayOrigin, transform.forward, out wallHit, 1f, climbLayer))
        {
            Vector3 topRayOrigin = wallHit.point + (Vector3.up * maxClimbHeight) + (transform.forward * 0.5f);
            RaycastHit topHit;
            
            if (Physics.Raycast(topRayOrigin, Vector3.down, out topHit, maxClimbHeight + 0.5f, climbLayer))
            {
                StartCoroutine(PerformDynamicClimb(topHit.point));
            }
        }
    }

    private IEnumerator PerformDynamicClimb(Vector3 targetPoint)
    {
        canMove = false; 
        if(animator != null) animator.SetTrigger("Climb");

        Vector3 lookPos = targetPoint;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        yield return new WaitForSeconds(climbDuration);

        characterController.enabled = false;
        transform.position = targetPoint + Vector3.up * 0.1f; 
        characterController.enabled = true;

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
                else if (isRunning) currentSpeed = speed * 1.5f;

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