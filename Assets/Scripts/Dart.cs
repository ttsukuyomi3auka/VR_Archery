using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class Dart : MonoBehaviour
{
    [Header("SteamVR Настройки")]
    [SerializeField] private SteamVR_Action_Boolean grabAction;
    [SerializeField] private SteamVR_Input_Sources handType;
    [SerializeField] private Transform grabPoint;
    
    [Header("Физика")]
    [SerializeField] private float throwMultiplier = 1.5f;
    [SerializeField] private float stickDepth = 0.03f;
    
    private Rigidbody rb;
    private bool isGrabbed = false;
    private bool isStuck = false;
    private SteamVR_Behaviour_Pose controllerPose;
    private FixedJoint fixedJoint;
    
    // Для расчета скорости
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 50f;
        
        // Настройки для лучшей физики
        rb.solverIterations = 15;
        rb.solverVelocityIterations = 10;
    }
    
    private void Start()
    {
        // Находим контроллер
        var trackedObject = GetComponentInParent<SteamVR_Behaviour_Pose>();
        if (trackedObject != null)
        {
            controllerPose = trackedObject;
            handType = controllerPose.inputSource;
        }
        
        previousPosition = transform.position;
    }
    
    private void Update()
    {
        if (isGrabbed && controllerPose != null)
        {
            // Следим за контроллером
            UpdateGrabbedPosition();
            
            // Расчет скорости
            currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
            previousPosition = transform.position;
            
            // Проверка отпускания
            if (grabAction != null && grabAction.GetStateUp(handType))
            {
                Release();
            }
        }
    }
    
    public void Grab(SteamVR_Behaviour_Pose newControllerPose)
    {
        if (isStuck) return;
        
        controllerPose = newControllerPose;
        handType = controllerPose.inputSource;
        isGrabbed = true;
        
        // Создаем FixedJoint для прикрепления к контроллеру
        fixedJoint = gameObject.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = controllerPose.GetComponent<Rigidbody>();
        fixedJoint.breakForce = Mathf.Infinity;
        fixedJoint.breakTorque = Mathf.Infinity;
        
        // Настраиваем физику при удержании
        rb.interpolation = RigidbodyInterpolation.None;
        rb.useGravity = false;
        
        // Позиционируем точку захвата
        if (grabPoint != null)
        {
            transform.position = controllerPose.transform.position + 
                controllerPose.transform.rotation * grabPoint.localPosition;
            transform.rotation = controllerPose.transform.rotation * grabPoint.localRotation;
        }
        
    }
    
    private void UpdateGrabbedPosition()
    {
        if (controllerPose == null || grabPoint == null) return;
        
        // Плавное следование за контроллером
        Vector3 targetPosition = controllerPose.transform.position + 
            controllerPose.transform.rotation * grabPoint.localPosition;
        Quaternion targetRotation = controllerPose.transform.rotation * grabPoint.localRotation;
        
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.5f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.5f);
    }
    
    private void Release()
    {
        if (!isGrabbed || isStuck) return;
        
        // Удаляем FixedJoint
        if (fixedJoint != null)
        {
            Destroy(fixedJoint);
        }
        
        // Восстанавливаем физику
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = true;
        
        // Применяем скорость броска
        Vector3 throwVelocity = controllerPose.GetVelocity() * throwMultiplier;
        Vector3 throwAngularVelocity = controllerPose.GetAngularVelocity() * throwMultiplier;
        
        rb.velocity = throwVelocity;
        rb.angularVelocity = throwAngularVelocity;
        
        // Сбрасываем состояние
        isGrabbed = false;
        controllerPose = null;
        
    }
    
    public void StickToTarget(Transform target, Vector3 contactPoint, Vector3 surfaceNormal)
    {
        if (isStuck) return;
        
        isStuck = true;
        
        // Отключаем захват если держим
        if (isGrabbed && controllerPose != null)
        {
            Release();
        }
        
        
        // Фиксируем дротик
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Выключаем коллайдер
        GetComponent<Collider>().enabled = false;
        
        // Вычисляем финальную позицию и вращение
        Vector3 targetPosition = contactPoint + surfaceNormal * stickDepth;
        Quaternion targetRotation = CalculateStickRotation(surfaceNormal);
        
        // Плавное движение к точке
        StartCoroutine(MoveToStickPosition(targetPosition, targetRotation));
        
        // Делаем дочерним объектом мишени
        transform.SetParent(target, true);
        
    }
    
    private Quaternion CalculateStickRotation(Vector3 surfaceNormal)
    {
        // Вычисляем вращение так, чтобы дротик был перпендикулярен поверхности
        Vector3 forward = -surfaceNormal;
        Vector3 up = Vector3.Cross(forward, Vector3.right);
        
        // Если up почти нулевой, используем альтернативную ось
        if (up.magnitude < 0.01f)
        {
            up = Vector3.Cross(forward, Vector3.forward);
        }
        
        return Quaternion.LookRotation(forward, up.normalized);
    }
    
    private System.Collections.IEnumerator MoveToStickPosition(Vector3 targetPos, Quaternion targetRot)
    {
        float duration = 0.08f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smooth step
            
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetPos;
        transform.rotation = targetRot;
    }
    
    
    private AudioClip GetHitSound()
    {
        // Возвращаем случайный звук попадания из массива
        return null; // Замените на ваши звуки
    }
}