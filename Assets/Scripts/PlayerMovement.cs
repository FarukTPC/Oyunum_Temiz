using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables

    [Header("Movement Settings")]
    [SerializeField] private float speed = 4f; 
    [Range(0, 360)] public float streetAngle = 0f; 

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1.2f; 

    [Header("Crouch Settings (Eğilme)")]
    [SerializeField] private float crouchHeight = 1.0f; // Eğilince boyu kaç olsun?
    [SerializeField] private float crouchSpeed = 2.0f;  // Eğilince ne kadar yavaşlasın?
    
    private float originalHeight;   // Normal boy
    private Vector3 originalCenter; // Normal merkez
    private Vector3 crouchCenter;   // Eğik merkez

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -20f; 
    
    // Bileşenler
    private CharacterController characterController;
    private Animator animator;
    
    // Durum Değişkenleri
    private Vector3 velocity;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isCrouching = false; // Eğilme durumunu hafızada tutan değişken (Toggle için)

    #endregion

    #region Unity Methods

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Başlangıç değerlerini kaydet
        originalHeight = characterController.height;
        originalCenter = characterController.center;
        
        // Eğilince merkez hafif aşağı kaymalı
        crouchCenter = new Vector3(originalCenter.x, originalCenter.y * 0.5f, originalCenter.z);
    }

    private void Update()
    {
        // 1. Yer Kontrolü
        isGrounded = characterController.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        // 2. Hareket Mantığı
        HandleMovement();

        // 3. Yerçekimi
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    #endregion

    #region Custom Functions

    private void HandleMovement()
    {
        if (isGrounded)
        {
            float input = Input.GetAxis("Horizontal");
            bool isRunning = Input.GetKey(KeyCode.LeftShift);

            // --- EĞİLME (TOGGLE MANTIĞI) ---
            // C tuşuna bir kez basılınca durumu tersine çevir (Açıksa kapat, kapalıysa aç)
            if (Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching; 
                
                // Animasyona bildir
                animator.SetBool("IsCrouching", isCrouching);
            }

            // Duruma göre Karakterin Fiziksel Boyunu Ayarla
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

            // --- HIZ VE YÖN HESABI ---
            if (Mathf.Abs(input) >= 0.1f)
            {
                float currentSpeed = speed;

                // Eğer eğiliyorsak ZORLA yavaşlat (Koşma tuşuna bassa bile)
                if (isCrouching) 
                {
                    currentSpeed = crouchSpeed;
                }
                else if (isRunning) 
                {
                    currentSpeed = speed * 1.5f;
                }

                // Yön Vektörü
                Vector3 direction = Quaternion.Euler(0, streetAngle, 0) * Vector3.forward;
                moveDirection = direction * (input > 0 ? 1 : -1) * currentSpeed;

                // Karakterin Dönüşü
                float lookAngle = input > 0 ? streetAngle : streetAngle + 180f;
                transform.rotation = Quaternion.Euler(0f, lookAngle, 0f);

                // Animasyon Hızı
                float animSpeed = isRunning ? 1f : 0.5f;
                if(isCrouching) animSpeed = 0.5f; // Eğilirken yürüme animasyonu
                
                animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
            }
            else
            {
                moveDirection = Vector3.zero;
                animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            }

            // --- ZIPLAMA ---
            // KURAL: Sadece eğilmiyorsak (!isCrouching) zıplayabiliriz.
            if (Input.GetButtonDown("Jump") && !isCrouching)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetTrigger("Jump");
            }
        }
        
        // Havadayken kontrol yok (Momentum)
        characterController.Move(moveDirection * Time.deltaTime);
    }

    #endregion
}