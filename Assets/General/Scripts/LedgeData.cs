using UnityEngine;

public class LedgeData : MonoBehaviour
{
    [Header("Ledge Settings")]
    [Tooltip("Karakter duvardan ne kadar uzakta asılı kalsın?")]
    public float hangDepthOffset = 0.5f; // EKSİK OLAN BU
    
    [Header("Corner Settings (Köşeler)")]
    public bool hasRightCorner = false; // Sağ tarafında dönülecek bir yer var mı?
    public bool hasLeftCorner = false;  // Sol tarafında dönülecek bir yer var mı?
    
    [Tooltip("Eğer sağa dönerse yeni sokak açısı ne olacak?")]
    public float rightCornerStreetAngle = 90f;
    
    [Tooltip("Eğer sola dönerse yeni sokak açısı ne olacak?")]
    public float leftCornerStreetAngle = -90f;

    [Tooltip("Dönüş sonrası karakterin ışınlanacağı hedef nokta (Empty Object)")]
    public Transform rightCornerTarget; // EKSİK OLAN BU
    public Transform leftCornerTarget;  // EKSİK OLAN BU

    // Objenin sınırlarını otomatik hesaplar
    public float GetLeftLimit()
    {
        // Collider yoksa hata vermesin diye kontrol ekleyebiliriz ama şimdilik basit tutalım
        return GetComponent<Collider>().bounds.min.x;
    } 

    public float GetRightLimit()
    {
        return GetComponent<Collider>().bounds.max.x;
    }

    public float GetTopY()
    {
        return GetComponent<Collider>().bounds.max.y;
    }
}