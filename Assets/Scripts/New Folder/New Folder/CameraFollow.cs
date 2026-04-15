using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3f, -10f);
    [SerializeField] private float smoothSpeed = 0.125f;

    private Transform _target;

    private void LateUpdate()
    {
        if (_target == null) return;
        Vector3 desiredPos = _target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
    }

    public void SetTarget(Transform target) => _target = target;
}