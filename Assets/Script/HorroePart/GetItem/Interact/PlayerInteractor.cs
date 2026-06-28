using HorrorGame.Interaction;
using System;
using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// カメラ前方にRayを飛ばしてIInteractableを検出し、
    /// インタラクトキーが押されたときに対象の操作を行う。
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform _interactCamera;
        [SerializeField] private float _interactRange = 2.5f;
        [SerializeField] private LayerMask _interactLayer;
        [SerializeField] private InputPlayerController _inputController;

        public event Action<string> OnFocusChanged;
        private IInteractable _currentTarget;
        private float _elapsedTime;
        private const float kDetectInterval = 0.1f;

        // 外部から強制表示するヒント（null = 通常検出に戻る）
        private string _forcedHint;

        private void Awake()
        {
            DebugCustom.ValidateFields(this,
                (nameof(_interactCamera), _interactCamera),
                (nameof(_inputController), _inputController));
        }

        private void OnEnable()
        {
            _inputController.OnInteractPerformed += TryInteract;
        }

        private void OnDisable()
        {
            _inputController.OnInteractPerformed -= TryInteract;
        }

        /// <summary>
        /// ヒントを強制表示する。null を渡すと通常の Raycast 検出に戻る。
        /// コンポーネントが無効化されていても呼び出せる。
        /// </summary>
        public void SetForcedHint(string hint)
        {
            _forcedHint = hint;
            OnFocusChanged?.Invoke(_forcedHint);
        }

        private void Update()
        {
            if (_forcedHint != null) return; // 強制ヒント中は検出スキップ

            _elapsedTime += Time.deltaTime;
            if (_elapsedTime < kDetectInterval) return;

            _elapsedTime = 0f;
            DetectTarget();
        }

        /// <summary>
        /// 前方にRayを飛ばして直前のIInteractableを検出する。
        /// 変化があったときのみ OnFocus/OnLoseFocus を発火。
        /// </summary>
        private void DetectTarget()
        {
            IInteractable newTarget = null;

            if (Physics.Raycast(_interactCamera.position, _interactCamera.forward,
                    out RaycastHit hit, _interactRange, _interactLayer))
            {
                newTarget = hit.collider.GetComponent<IInteractable>();
            }

            if (newTarget == _currentTarget) return;

            _currentTarget = newTarget;

            OnFocusChanged?.Invoke(_currentTarget?.HintText);
        }

        private void TryInteract()
        {
            if (_currentTarget == null || !_currentTarget.CanInteract) return;
            _currentTarget.OnInteract();
        }

        private void OnDrawGizmosSelected()
        {
            if (_interactCamera == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_interactCamera.position,
                    _interactCamera.forward * _interactRange);
        }
    }
}
