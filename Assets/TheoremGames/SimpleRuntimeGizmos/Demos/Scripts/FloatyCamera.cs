using System;
using UnityEngine;

namespace Assets.Tools.FloatyCamera
{
    public class FloatyCamera : MonoBehaviour {

        public float MoveSpeed = 0.15f;
        public float ScrollSpeed = 2f;
        public float SensitivityX = 4f;
        public float SensitivityY = 4f;
        public float MinimumY = -40f;
        public float MaximumY = 40f;
        public float ManualRotationAcceleration= 40f;

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private Quaternion _startRotation;
        private float _rotationX;
        private float _rotationY;

        void Start () {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _targetPosition = _startPosition;

            _rotationX = _startRotation.x;
            _rotationY = _startRotation.y;
        }

        void Update () {
            transform.position = Vector3.Lerp(transform.position, _targetPosition, 3f * Time.deltaTime);

            var forward = Input.GetAxis("Vertical") * MoveSpeed;

            var sideways = Input.GetAxis("Horizontal") * MoveSpeed * 0.8f;
            if (Input.GetMouseButton(1) || Math.Abs(forward) > 0.0f || Math.Abs(sideways) > 0.0f) {
                _rotationX += Input.GetAxis("Mouse X") * SensitivityX;
                _rotationY = Mathf.Clamp(_rotationY + (Input.GetAxis("Mouse Y") * SensitivityY), MinimumY, MaximumY);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(-_rotationY, _rotationX, 0), ManualRotationAcceleration * Time.deltaTime);
            forward += Input.GetAxis("Mouse ScrollWheel") * ScrollSpeed;
            _targetPosition += transform.rotation * (Vector3.forward * forward);
            _targetPosition += transform.rotation * (Vector3.right * sideways);
            if(_targetPosition.y < -1f) _targetPosition.y = -1f;
        }

    }
}
