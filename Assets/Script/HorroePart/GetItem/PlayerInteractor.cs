using UnityEngine;
using HorrorGame.Interaction;

namespace HorrorGame.Player
{
    /// <summary>
    /// プレイヤーの前方にRayを飛ばし、IInteractableを実装した
    /// オブジェクトとのインタラクションを制御するコンポーネント。
    /// カメラTransformを基準にRayを飛ばすため、PlayerのCameraを
    /// InspectorのinteractCameraにアサインすること。
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform interactCamera;
        [SerializeField] private float interactRange = 2.5f;
        [SerializeField] private LayerMask interactLayer;
        [SerializeField] private InputPlayerController inputController;

        private IInteractable currentTarget;

        private void OnEnable()
        {
            inputController.OnInteractPerformed += TryInteract;
        }

        private void OnDisable()
        {
            inputController.OnInteractPerformed -= TryInteract;
        }

        private void Update()
        {
            DetectTarget();
        }

        /// <summary>
        /// 毎フレームRayを飛ばして最前面のIInteractableを更新する。
        /// フォーカス変化があった場合のみOnFocus/OnLoseFocusを呼ぶ。
        /// </summary>
        private void DetectTarget()
        {
            IInteractable newTarget = null;

            if (Physics.Raycast(interactCamera.position, interactCamera.forward,
                    out RaycastHit hit, interactRange, interactLayer))
            {
                newTarget = hit.collider.GetComponent<IInteractable>();
            }

            if (newTarget == currentTarget) return;

            currentTarget?.OnLoseFocus();
            currentTarget = newTarget;
            currentTarget?.OnFocus();
        }

        private void TryInteract()
        {
            if (currentTarget == null || !currentTarget.CanInteract) return;
            currentTarget.OnInteract();
        }

        private void OnDrawGizmosSelected()
        {
            if (interactCamera == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(interactCamera.position,
                interactCamera.forward * interactRange);
        }
    }
}