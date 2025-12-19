using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables

    [Header("Interaction Settings (Etkileşim)")]
    [Tooltip("Karakter şu an kontrol edilebilir mi? (Tırmanırken false olacak)")]
    public bool canMove = true; 
    
    [Tooltip("Etkileşim mesafesi (Örn: 2 metre)")]
    [SerializeField] private float interactDistance = 2.0f;
    [SerializeField] private LayerMask interactLayer; // Sadece 'Interactable' layer'ını görsün diye

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
        // 1. Yer Kontrolü (Her zaman çalışmalı)
        isGrounded = characterController.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        // --- ETKİLEŞİM KONTROLÜ (F Tuşu) ---
        // Sadece hareket edebiliyorsak etkileşime girebiliriz
        if (canMove && Input.GetKeyDown(KeyCode.F))
        {
            CheckInteraction();
        }

        // 2. Hareket Mantığı (Sadece canMove aktifse çalışır!)
        if (canMove)
        {
            HandleMovement();
        }
        else
        {
            // Hareket kilitliyken animasyonun 'Speed' değerini sıfırla ki koşuyor gibi görünmesin
            animator.SetFloat("Speed", 0f);
            moveDirection = Vector3.zero;
        }

        // 3. Yerçekimi (Tırmanırken yerçekimi de kapansın isteyebiliriz ama şimdilik açık kalsın)
        if (canMove)
        {
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }
    }

    #endregion

    #region Custom Functions

    private void CheckInteraction()
    {
        // Karakterin merkezinden ileriye doğru ışın at
        Ray ray = new Ray(transform.position + Vector3.up * 1f, transform.forward);
        RaycastHit hit;

        // Debug için sahnede çizgiyi görelim (Sadece Scene ekranında görünür)
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.yellow, 2f);

        // Eğer ışın bir şeye çarparsa
        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            // Çarptığı objede "IInteractable" kimliği var mı?
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Varmış! O zaman etkileşimi başlat.
                interactable.Interact(this); // 'this' diyerek kendimizi (Player) gönderiyoruz
            }
        }
    }

    private void HandleMovement()
    {
        if (isGrounded)
        {
            float input = Input.GetAxis("Horizontal");
            bool isRunning = Input.GetKey(KeyCode.LeftShift);

            // --- EĞİLME ---
            if (Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching; 
                animator.SetBool("IsCrouching", isCrouching);
            }

            // Boyut Ayarı
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

            // --- HIZ ---
            if (Mathf.Abs(input) >= 0.1f)
            {
                float currentSpeed = speed; 
                
                if (isCrouching) currentSpeed = crouchSpeed; 
                else if (isRunning) currentSpeed = speed * 1.5f;

                Vector3 direction = Quaternion.Euler(0, streetAngle, 0) * Vector3.forward;
                moveDirection = direction * (input > 0 ? 1 : -1) * currentSpeed;

                float lookAngle = input > 0 ? streetAngle : streetAngle + 180f;
                transform.rotation = Quaternion.Euler(0f, lookAngle, 0f);

                // Animasyon
                float animValue = 0f;
                if (isCrouching) animValue = currentSpeed;
                else animValue = isRunning ? 1f : 0.5f;
                
                animator.SetFloat("Speed", animValue, 0.1f, Time.deltaTime);
            }
            else
            {
                moveDirection = Vector3.zero;
                animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            }

            // --- ZIPLAMA ---
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