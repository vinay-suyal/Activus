using System;
using UnityEngine;

namespace DefaultNamespace{
    public class CameraZoomHandler : MonoBehaviour{
        [SerializeField] private Transform targetTransform;
        private Camera targetCamera;
        internal static event Action<int> onCameraZoom;

        private void Awake() {
            targetCamera = GetComponent<Camera>();
        }

        private void Update() {
            targetCamera.orthographicSize = 10f * targetTransform.localScale.x;
            targetTransform.localPosition = new Vector3(0f, 0f, 130f);
            onCameraZoom?.Invoke((int)(targetTransform.localScale.x * 100));
        }
    }
}