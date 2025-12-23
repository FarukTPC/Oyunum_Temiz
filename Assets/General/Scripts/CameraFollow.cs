using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Player objesi
    public PlayerMovement playerScript;

    [Header("Settings")]
    public float followSpeed = 5f;   // Takip hızı
    public float rotationSpeed = 2f; // Dönüş hızı
    
    // --- ÖNEMLİ DEĞİŞİKENLER ---
    private Vector3 _startOffset;      // Oyun başındaki mesafe farkı
    private Quaternion _startRotation; // Oyun başındaki kamera açısı

    private void Start()
    {
        // 1. Hedefi Bul
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                playerScript = player.GetComponent<PlayerMovement>();
            }
        }

        if (target != null)
        {
            // 2. SAHNEDEKİ DURUŞU KAYDET (ÇÖZÜM BURASI)
            // Oyun başladığında kamera karaktere göre nerede duruyorsa, o mesafeyi kaydet.
            // Ayrıca kameranın o anki açısını da kaydet.
            // Önceki kod burayı "Sıfır" kabul edip bozuyordu.
            
            _startOffset = transform.position - target.position;
            _startRotation = transform.rotation;
        }
    }

    private void LateUpdate()
    {
        if (target == null || playerScript == null) return;

        // --- HESAPLAMA ---
        
        // 1. Oyuncunun şu anki sokak açısını al
        float currentStreetAngle = playerScript.streetAngle;

        // 2. Bu açıya denk gelen bir rotasyon oluştur (Y ekseninde)
        Quaternion targetTurnRotation = Quaternion.Euler(0, currentStreetAngle, 0);

        // --- POZİSYON HESABI ---
        // Başlangıçtaki mesafeyi (Offset), sokağın açısı kadar döndür.
        // Yani sokağa 90 derece dönersek, kameranın durduğu yer de 90 derece döner.
        Vector3 rotatedOffset = targetTurnRotation * _startOffset;
        Vector3 finalPosition = target.position + rotatedOffset;

        // --- ROTASYON HESABI ---
        // Kameranın başlangıçtaki açısını (_startRotation), sokağın açısı kadar döndür.
        // Böylece kamera 2.5D yan bakış açısını koruyarak döner.
        Quaternion finalRotation = targetTurnRotation * _startRotation;

        // --- HAREKET (SMOOTH) ---
        transform.position = Vector3.Lerp(transform.position, finalPosition, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
    }
}