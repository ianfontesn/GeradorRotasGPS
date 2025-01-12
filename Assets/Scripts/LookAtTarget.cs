using UnityEngine;

public class LookAtTarget : MonoBehaviour
{
    [SerializeField]
    private Transform _toRotate;

    private Transform _target;
    private bool isTargetSet = false;

    protected virtual void Update()
    {
        if (isTargetSet)
        {
            Rotate();
        }
        
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        isTargetSet = true;
    }

    private void Rotate()
    {
        Vector3 dirToTarget = (_target.position - _toRotate.position).normalized;
        _toRotate.LookAt(_toRotate.position - dirToTarget, Vector3.up);
    }

}

