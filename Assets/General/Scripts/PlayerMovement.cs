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
    
    [Tooltip("Karakter en fazla ne kadar yükseğe tırmanabilir?")]
    [SerializeField] private float maxClimbHeight = 2.5f; 
    
    [Tooltip("Tırmanma animasyonu süresi")]
    [SerializeField] private float climbDuration = 1.2f;

    [Header("Climb Precision Settings (Hassas Ayarlar)")]
    [Tooltip("Animasyonun % kaçında YUKARI çıkma işlemi bitsin?")]
    [Range(0.1f, 1.0f)] public float verticalClimbPercent = 0.8f;

    [Tooltip("Animasyonun % kaçında İLERİ gitme işlemi başlasın?")]
    [Range(0.0f, 0.9f)] public float forwardMoveStartPercent = 0.4f;

    [Tooltip("Tırmanma bitince karakter kenardan ne kadar içeri girsin?")]
    [Range(0.0f, 1.0f)] public float forwardOffset = 0.2f; 

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
        // Normal etkileşim (Kapı vs.) hala 1.0f (Omuz hizası) kalsın
        Ray ray = new Ray(transform.position + Vector3.up * 1.0f, transform.forward);
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

        CheckForClimb();
    }

    private void CheckForClimb()
    {
        // --- DEĞİŞİKLİK BURADA: AYAK BİLEĞİ HİZASI ---
        // 0.3f yetmediği için 0.2f (20 cm) yaptık.
        // Bu neredeyse kaldırım taşı hizasıdır.
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        RaycastHit wallHit;

        Debug.DrawRay(rayOrigin, transform.forward * 2.5f, Color.red, 2f);

        if (Physics.Raycast(rayOrigin, transform.forward, out wallHit, 2.5f, climbLayer))
        {
            // Duvar bulundu. Tepesini bulalım.
            // Lazerin çıkış noktası duvarın yüzeyi + max yükseklik.
            // forward * 0.5f ile duvarın içine girip aşağı tarıyoruz.
            Vector3 topRayOrigin = wallHit.point + (Vector3.up * maxClimbHeight) + (transform.forward * 0.5f);
            RaycastHit topHit;
            
            Debug.DrawRay(topRayOrigin, Vector3.down * (maxClimbHeight + 0.2f), Color.blue, 2f); // +0.2f (Garanti olsun)

            // Raycast mesafesini biraz artırdık (maxClimbHeight + 0.5f) ki zemini kesin bulsun
            if (Physics.Raycast(topRayOrigin, Vector3.down, out topHit, maxClimbHeight + 0.5f, climbLayer))
            {
                // Hedef Nokta: Yükseklik tepeden, Konum duvardan
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
            
            float yRatio = Mathf.Clamp01(t / verticalClimbPercent);
            
            float forwardDuration = 1.0f - forwardMoveStartPercent;
            if (forwardDuration < 0.01f) forwardDuration = 0.01f;
            float xzRatio = Mathf.Clamp01((t - forwardMoveStartPercent) / forwardDuration);

            yRatio = Mathf.SmoothStep(0f, 1f, yRatio);
            xzRatio = Mathf.SmoothStep(0f, 1f, xzRatio);

            float currentY = Mathf.Lerp(startPos.y, targetPoint.y, yRatio);
            Vector3 currentXZ = Vector3.Lerp(new Vector3(startPos.x, 0, startPos.z), new Vector3(targetPoint.x, 0, targetPoint.z), xzRatio);
            
            transform.position = new Vector3(currentXZ.x, currentY, currentXZ.z);

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        // --- SAKİN İNİŞ (Gömülme Yok) ---
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