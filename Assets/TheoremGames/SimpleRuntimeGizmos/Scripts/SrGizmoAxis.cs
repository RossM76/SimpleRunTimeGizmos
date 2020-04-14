using System.Linq;
using UnityEngine;

namespace Assets.TheoremGames.SimpleRuntimeGizmos.Scripts
{
    public class SrGizmoAxis : MonoBehaviour
    {
        public SrAxisType SrAxis;
        public MeshRenderer[] GizmoMeshes;
        [HideInInspector]
        public bool IsPressed;
        public float MovementSpeed = 100;
        public LayerMask ClickLayer;

        public delegate void Moving(SrAxisType srAxis);
        public static event Moving OnMovement;
        public delegate void MovingStopped();
        public static event MovingStopped OnMovementStopped;

        private bool _axisClicked;

        void Awake()
        {
            SrGizmoAxis.OnMovement += Movement;
            SrGizmoAxis.OnMovementStopped += MovementStopped;
        }

        private void MovementStopped()
        {
            gameObject.SetActive(true);
            _axisClicked = false;
        }

        private void Movement(SrAxisType srAxis)
        {
            gameObject.SetActive(srAxis == this.SrAxis);
        }

        private bool IsAxisClicked()
        {
            if (Input.GetMouseButton(0))
            {
                if (!_axisClicked)
                {
                    var hitAxis = HitAxis();
                    _axisClicked = hitAxis?.GetComponent<SrGizmoAxis>()?.SrAxis == SrAxis;
                }

                if (_axisClicked) OnMovement?.Invoke(SrAxis);

                return _axisClicked;
            }

            OnMovementStopped?.Invoke();
            return false;
        }

        private SrGizmoAxis HitAxis()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hitData = Physics.RaycastAll(ray, Mathf.Infinity, ClickLayer);

            return hitData.FirstOrDefault(h => h.collider?.GetComponent<SrGizmoAxis>() != null).collider?.gameObject.GetComponent<SrGizmoAxis>();
        }

        void Update()
        {
            IsPressed = IsAxisClicked();
        }
    }
}
