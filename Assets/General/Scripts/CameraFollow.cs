using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    
    [Tooltip("Bunu elle ayarlamana gerek yok, oyun başlayınca otomatik hesaplanır.")]
    public Vector3 offset; 
    
    public float smoothSpeed = 0.125f;

    [Header("Shake Settings")]
    private bool isShaking = false;
    private Vector3 shakeOffset = Vector3.zero;

    void Start()
    {
        // --- BU KISIM EKLENDİ ---
        // Oyun başladığı an, kameran karakterine göre nerede duruyorsa
        // o mesafeyi "offset" olarak kaydeder.
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Hedef pozisyonu, başlangıçta hesaplanan offset'e göre belirle
        Vector3 desiredPosition = target.position + offset;
        
        // 2. Yumuşak geçiş yap
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 3. Sarsıntı varsa ekle
        if (isShaking)
        {
            transform.position = smoothedPosition + shakeOffset;
        }
        else
        {
            transform.position = smoothedPosition;
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            shakeOffset = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isShaking = false;
        shakeOffset = Vector3.zero;
    }
}