using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.TheoremGames.SimpleRuntimeGizmos.Scripts
{
    public class SrGizmoController : MonoBehaviour
    {
        public Camera Camera;
        public SrGizmo[] Gizmos;
        public float MovementSpeedMultiplier;

        private GameObject _selectedObject;
        private SrGizmoType _selectedGizmoType = SrGizmoType.None;
        private int _currentGizmoIndex;

        void Start()
        {
            if (Camera == null)
                Camera = Camera.main ?? throw new Exception("Could not find main camera");

            InstantiateGizmos();
        }

        private void InstantiateGizmos()
        {
            for (var i = 0; i < Gizmos.Length; i++)
            {
                Gizmos[i] = Instantiate(Gizmos[i]);
                Gizmos[i].gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (Camera == null) return;

            if (Input.GetMouseButtonDown(2))
            {
                SelectObject();
                SelectNextGizmo();
                HideRestrictedAxis();
            }
            else
            {
                if (Input.GetMouseButtonDown(1))
                {
                    _selectedObject = null;
                    SetupGizmo(GetActiveGizmo(), false);
                }
            }

            if (GetActiveGizmo()?.GizmoType == SrGizmoType.Rotation) Rotate();
            if (GetActiveGizmo()?.GizmoType == SrGizmoType.Translation || GetActiveGizmo().GizmoType == SrGizmoType.Scale) TranslateOrScale();
            SetGizmoPosition(GetActiveGizmo());
        }

        private void SelectNextGizmo()
        {
            while (_currentGizmoIndex < Gizmos.Length)
            {
                _currentGizmoIndex++;

                if (_currentGizmoIndex >= Gizmos.Length) _currentGizmoIndex = 0;

                SetupGizmo(_currentGizmoIndex - 1 >= 0 ? Gizmos[_currentGizmoIndex - 1] : Gizmos[Gizmos.Length - 1], false);

                if (IsFullyRestricted(Gizmos[_currentGizmoIndex])) continue;
                SetupGizmo(Gizmos[_currentGizmoIndex], true);
                _selectedGizmoType = Gizmos[_currentGizmoIndex].GizmoType;
                break;
            }
        }

        private SrGizmo GetActiveGizmo()
        {
            return Gizmos[_currentGizmoIndex];
        }

        private void SetupGizmo(SrGizmo srGizmo, bool enableGizmo)
        {
            if (_selectedObject != null && enableGizmo)
            {
                SetGizmoPosition(srGizmo);
                srGizmo.gameObject.SetActive(true);
            }
            else
            {
                srGizmo.gameObject.SetActive(false);
                _selectedGizmoType = SrGizmoType.None;
            }
        }

        private void SetGizmoPosition(SrGizmo gizmo)
        {
            if (_selectedObject == null || gizmo == null) return;
            var offSetData = _selectedObject.GetComponent<SrGizmoPositionOffset>();
            var offSet = offSetData?.PositionOffset ?? Vector3.zero;
            gizmo.transform.position = _selectedObject.transform.position + offSet;
            gizmo.transform.rotation = _selectedObject.transform.rotation;
        }

        private void HideRestrictedAxis()
        {
            var gizmo = GetActiveGizmo();
            if (gizmo == null) return;

            var axisRestrictions = _selectedObject?.GetComponents<SrRestrictAxis>()?.Where(ar => ar.GizmoType == gizmo.GizmoType) ?? Enumerable.Empty<SrRestrictAxis>();

            var srRestrictAxises = axisRestrictions as SrRestrictAxis[] ?? axisRestrictions.ToArray();
            foreach (var axis in gizmo.AxisList ?? Enumerable.Empty<SrGizmoAxis>())
            foreach (var mesh in axis.GizmoMeshes)
                mesh.enabled = srRestrictAxises.Any(ar => ar.RestrictAxisTo == axis.SrAxis) || !srRestrictAxises.Any();
        }

        private void TranslateOrScale()
        {
            if (_selectedObject == null) return;

            foreach (var axis in EnumerateAxis())
            {
                var mousePosition = Camera.ScreenToWorldPoint(Input.mousePosition);
                var currentPosition = _selectedObject.transform.position;
                var offSet = Vector3.zero;

                offSet = ApplyTransformScale(axis, offSet);

                switch (_selectedGizmoType)
                {
                    case SrGizmoType.Translation:
                        _selectedObject.transform.Translate(offSet);
                        SetupGizmo(GetActiveGizmo(), _selectedGizmoType == SrGizmoType.Translation);
                        break;
                    case SrGizmoType.Scale:
                        _selectedObject.transform.localScale += offSet;
                        SetupGizmo(GetActiveGizmo(), _selectedGizmoType == SrGizmoType.Scale);
                        break;
                    case SrGizmoType.None:
                        break;
                    case SrGizmoType.Rotation:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private Vector3 ApplyTransformScale(SrGizmoAxis axis, Vector3 offSet)
        {
            switch (axis.SrAxis)
            {
                case SrAxisType.X:
                    offSet = Camera.transform.right * GetTranslateScaleDelta(SrAxisType.X);
                    offSet = new Vector3(offSet.x, 0.0f, 0.0f);
                    break;
                case SrAxisType.Y:
                    offSet = Camera.transform.up * GetTranslateScaleDelta(SrAxisType.Y);
                    offSet = new Vector3(0.0f, offSet.y, 0.0f);
                    break;
                case SrAxisType.Z:
                    offSet = Camera.transform.right * GetTranslateScaleDelta(SrAxisType.Z);
                    offSet = new Vector3(0.0f, 0.0f, offSet.z);
                    break;
                case SrAxisType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return offSet;
        }

        private float GetTranslateScaleDelta(SrAxisType srAxis)
        {
            var distance = Vector3.Distance(Camera.transform.position, _selectedObject.transform.position);
            distance *= 2.0f;
            var delta = Time.deltaTime * distance * MovementSpeedMultiplier;

            switch (srAxis)
            {
                case SrAxisType.Z:
                case SrAxisType.X:
                    return Input.GetAxis("Mouse X") * delta;

                case SrAxisType.Y:
                    return Input.GetAxis("Mouse Y") * delta;
                case SrAxisType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(srAxis), srAxis, null);
            }

            return 0f;
        }

        private float GetRotationDelta(float speed)
        {
            var delta = (Input.GetAxis("Mouse X") - Input.GetAxis("Mouse Y")) * MovementSpeedMultiplier * Time.deltaTime;
            delta *= speed;
            return delta;
        }

        private bool IsFullyRestricted(SrGizmo srGizmo)
        {
            return _selectedObject?.GetComponents<SrRestrictAxis>()?.Any(ar =>
                ar.RestrictAxisTo == SrAxisType.None && ar.GizmoType == srGizmo.GizmoType) ?? false;
        }

        private bool RestrictedAxis(SrGizmo srGizmo, SrGizmoAxis srGizmoAxis)
        {
            var axisRestriction = GetAxisRestriction(srGizmo);
            var srRestrictAxises = axisRestriction as SrRestrictAxis[] ?? axisRestriction.ToArray();
            var axisRestrictionCount = srRestrictAxises.Count(ar => ar.GizmoType == srGizmo.GizmoType);
            return srRestrictAxises.Any(ar => ar.GizmoType == srGizmo.GizmoType && ar.RestrictAxisTo == srGizmoAxis.SrAxis) || axisRestrictionCount == 0;
        }

        private IEnumerable<SrGizmoAxis> EnumerateAxis()
        {
            var activeGizmo = GetActiveGizmo();
            return activeGizmo?.AxisList.Where(ga => ga.IsPressed && RestrictedAxis(activeGizmo, ga)) ??
                   Enumerable.Empty<SrGizmoAxis>();
        }

        public IEnumerable<SrRestrictAxis> GetAxisRestriction(SrGizmo srGizmo)
        {
            return _selectedObject?.GetComponents<SrRestrictAxis>()?.Where(ra => ra.GizmoType == srGizmo?.GizmoType) ?? Enumerable.Empty<SrRestrictAxis>();
        }

        private void Rotate()
        {
            if (_selectedObject == null) return;

            foreach (var axis in EnumerateAxis())
            {
                var delta = GetRotationDelta(axis.MovementSpeed);
                var rightVector = Camera.transform.right;
                rightVector *= 2;
                rightVector.z = 0f;
                rightVector.y = 0f;

                var upVector = Camera.transform.up;
                upVector *= 2;
                upVector.x = 0f;
                upVector.z = 0f;

                var forwardVector = Camera.transform.forward;
                forwardVector *= 2;
                forwardVector.x = 0f;
                forwardVector.y = 0f;

                switch (axis.SrAxis)
                {
                    case SrAxisType.X:
                        _selectedObject.transform.Rotate(-rightVector, delta);
                        break;
                    case SrAxisType.Y:
                        _selectedObject.transform.Rotate(-upVector, delta);
                        break;
                    case SrAxisType.Z:
                        _selectedObject.transform.Rotate(-forwardVector, delta);
                        break;
                    case SrAxisType.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                SetupGizmo(GetActiveGizmo(), _selectedGizmoType == SrGizmoType.Rotation);
            }
        }

        private void SelectObject()
        {
            var ray = Camera.ScreenPointToRay(Input.mousePosition);
            var hit = Physics.Raycast(ray, out var hitInfo);

            if (hit && hitInfo.transform.GetComponent<SrGizmoAxis>() == null)
            {
                _selectedObject = hitInfo.collider.gameObject;
                return;
            }

            _selectedObject = null;
            _selectedGizmoType = SrGizmoType.None;
        }
    }
}