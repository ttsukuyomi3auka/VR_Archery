using UnityEngine;
using System.Collections.Generic;

public class Target : MonoBehaviour
{
    [Header("VR Настройки мишени")]
    [SerializeField] private float minStickVelocity = 1.5f;
    [SerializeField] private LayerMask dartLayer;
    
    [Header("Зоны очков")]
    [SerializeField] private List<TargetZone> scoringZones = new List<TargetZone>();
    
    private Dictionary<Collider, Dart> stuckDarts = new Dictionary<Collider, Dart>();
    
    [System.Serializable]
    public class TargetZone
    {
        public Collider zoneCollider;
        public int scoreValue = 10;
        public Color zoneColor = Color.white;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Проверяем слой дротика
        if (((1 << collision.gameObject.layer) & dartLayer) == 0)
            return;
        
        Dart dart = collision.gameObject.GetComponent<Dart>();
        if (dart == null) return;
        
        // Проверяем скорость для реалистичного втыкания
        if (collision.relativeVelocity.magnitude < minStickVelocity)
        {
            // Дротик отскочит если брошен слабо
            return;
        }
        
        // Проверяем, что дротик еще не в мишени
        if (stuckDarts.ContainsKey(collision.collider))
            return;
        
        // Находим точку контакта
        ContactPoint contact = collision.contacts[0];
        
        // Вычисляем нормаль поверхности с учетом возможного меш коллайдера
        Vector3 surfaceNormal = GetAdjustedSurfaceNormal(contact);
        
        // Прилипаем к мишени
        dart.StickToTarget(transform, contact.point, surfaceNormal);
        
        // Добавляем в словарь
        stuckDarts.Add(collision.collider, dart);
        
        // Вибрация контроллера
        TriggerHapticFeedback(dart);
        
        // Визуальная обратная связь
        StartCoroutine(AnimateHit(contact.point));
        
        // Подсчет очков
        CalculateScore(contact.point, dart);
    }
    
    private Vector3 GetAdjustedSurfaceNormal(ContactPoint contact)
    {
        // Для кривых поверхностей мишени
        RaycastHit hit;
        if (Physics.Raycast(contact.point + contact.normal * 0.1f, 
                          -contact.normal, out hit, 0.2f, dartLayer))
        {
            return hit.normal;
        }
        
        return contact.normal;
    }
    
    private void TriggerHapticFeedback(Dart dart)
    {
        // Ищем контроллер, который бросил дротик
        // (можно расширить для сетевой игры)
    }
    
    private System.Collections.IEnumerator AnimateHit(Vector3 position)
    {
        // Анимация попадания (например, временное изменение цвета)
        // Можно добавить систему частиц
        yield return null;
    }
    
    private void CalculateScore(Vector3 hitPoint, Dart dart)
    {
        int totalScore = 0;
        TargetZone hitZone = null;
        
        // Проверяем в какую зону попал дротик
        foreach (var zone in scoringZones)
        {
            if (zone.zoneCollider.bounds.Contains(hitPoint))
            {
                totalScore += zone.scoreValue;
                hitZone = zone;
                break;
            }
        }
        
        // Если не попали в зону, даем минимальные очки
        if (hitZone == null)
        {
            totalScore = 1;
        }
        
        
    }
    
    
    // Метод для удаления всех дротиков (например, для новой игры)
    public void ClearDarts()
    {
        foreach (var dart in stuckDarts.Values)
        {
            if (dart != null)
                Destroy(dart.gameObject);
        }
        stuckDarts.Clear();
    }
}