using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Rigidbody))]
public class Dart : MonoBehaviour
{
    private Rigidbody rb;
    private bool isStuck = false;
    private float timeOfThrow;

    [Tooltip("Поверните дротик, если он летит боком. (Например, 90, 0, 0)")]
    public Vector3 rotationOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Если дротик застрял или он Kinematic (например, в руке), не меняем поворот
        if (isStuck || rb.isKinematic) return;

        // Если дротик летит (скорость больше минимума), поворачиваем его носом по ходу движения
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            // Берем направление скорости
            Quaternion lookRot = Quaternion.LookRotation(rb.velocity);
            // Добавляем корректировку поворота (если модель сделана криво)
            transform.rotation = lookRot * Quaternion.Euler(rotationOffset);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1. Игнорируем столкновения, если уже застряли или если дротик в руке (kinematic)
        if (isStuck || rb.isKinematic) return;

        // 2. Игнорируем попадания сразу после броска (чтобы не задеть руку метателя)
        if (Time.time < timeOfThrow + 0.15f) return;

        // 3. Игнорируем слабые удары (чтобы дротик не втыкался, если его просто уронили)
        if (collision.relativeVelocity.magnitude < 2f) return;

        Stick(collision);
    }

    private void Stick(Collision collision)
    {
        isStuck = true;

        // Останавливаем физику
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Прикрепляем к объекту, в который попали
        transform.SetParent(collision.transform);
    }

    private void OnAttachedToHand(Hand hand)
    {
        isStuck = false;
        // Не вызываем SetParent(null) здесь, чтобы не отрывать от руки
    }

    private void OnDetachedFromHand(Hand hand)
    {
        timeOfThrow = Time.time;
        isStuck = false;
        // Здесь тоже не обязательно SetParent(null), SteamVR делает это сам, но для надежности можно сбросить, если логика сложная
    }

    public void OnPickUp()
    {
        isStuck = false;
        transform.SetParent(null);
    }
}
