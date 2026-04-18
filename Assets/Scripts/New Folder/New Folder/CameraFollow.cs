using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3f, -10f);
    [SerializeField] private float smoothSpeed = 0.125f;

    [Header("=== SCREEN SHAKE ===")]
    [SerializeField] private float defaultShakeDuration = 0.35f;   // thời gian rung
    [SerializeField] private float defaultShakeMagnitude = 0.75f;  // độ mạnh rung

    private Transform _target;
    private float _shakeTimeLeft = 0f;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private Vector3 _shakeOffset = Vector3.zero;

    /// <summary>
    /// Gọi hàm này khi muốn rung màn hình (có thể truyền giá trị khác để thay đổi mạnh/yếu)
    /// </summary>
    public void TriggerShake(float duration = -1f, float magnitude = -1f)
    {
        _shakeDuration = duration > 0 ? duration : defaultShakeDuration;
        _shakeMagnitude = magnitude > 0 ? magnitude : defaultShakeMagnitude;
        _shakeTimeLeft = _shakeDuration;
    }

    private void Update()
    {
        if (_shakeTimeLeft > 0f)
        {
            // Rung ngẫu nhiên + giảm dần theo thời gian
            _shakeOffset = Random.insideUnitSphere * _shakeMagnitude * (_shakeTimeLeft / _shakeDuration);
            _shakeTimeLeft -= Time.deltaTime;
        }
        else
        {
            _shakeOffset = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desiredPos = _target.position + offset + _shakeOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
    }

    public void SetTarget(Transform target) => _target = target;
}