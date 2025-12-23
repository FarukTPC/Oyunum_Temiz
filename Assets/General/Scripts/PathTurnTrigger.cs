using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PathTurnTrigger : MonoBehaviour
{
    [Header("Angle Settings")]
    [Tooltip("Sokağın giriş açısı (Genelde 0)")]
    public float angleA = 0f;

    [Tooltip("Sokağın çıkış açısı (Dönülecek yön, örn: 90)")]
    public float angleB = 90f;

    [Header("Debug")]
    [Tooltip("Gizmo rengi")]
    public Color gizmoColor = new Color(1, 0.92f, 0.016f, 0.4f);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                SwitchAngle(player);
            }
        }
    }

    private void SwitchAngle(PlayerMovement player)
    {
        // Oyuncunun şu anki açısı hangisine daha yakın?
        float currentAngle = player.streetAngle;

        float distToA = Mathf.Abs(Mathf.DeltaAngle(currentAngle, angleA));
        float distToB = Mathf.Abs(Mathf.DeltaAngle(currentAngle, angleB));

        // Eğer A açısındaysak (0) -> B'ye (90) geç
        if (distToA < distToB)
        {
            player.streetAngle = angleB;
            Debug.Log($"İleri Gidiliyor: Açı {angleB} yapıldı.");
        }
        // Eğer B açısındaysak (90) -> A'ya (0) geç (Geri Dönüş)
        else
        {
            player.streetAngle = angleA;
            Debug.Log($"Geri Dönülüyor: Açı {angleA} yapıldı.");
        }
    }

    // --- EDİTÖRDE KAREYİ GÖRMEK İÇİN ---
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            // Dönüş ve boyut ayarlarını uygula
            Gizmos.matrix = transform.localToWorldMatrix;
            
            // İçini boya
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(box.center, box.size);
            
            // Çerçevesini çiz
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(box.center, box.size);

            // Yön oklarını çiz (Global koordinatta)
            Gizmos.matrix = Matrix4x4.identity;
            
            // A Açısı Oku (Giriş)
            Gizmos.color = Color.white;
            Vector3 dirA = Quaternion.Euler(0, angleA, 0) * Vector3.forward;
            Gizmos.DrawRay(transform.position, dirA * 2f);

            // B Açısı Oku (Çıkış)
            Gizmos.color = Color.blue;
            Vector3 dirB = Quaternion.Euler(0, angleB, 0) * Vector3.forward;
            Gizmos.DrawRay(transform.position, dirB * 2f);
        }
    }
}