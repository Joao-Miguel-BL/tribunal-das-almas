using System;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TribunalDasAlmas.Mechanics
{
    /// <summary>
    /// Controla a interação 2D de documentos arrastáveis na bancada:
    /// arraste com Mouse0, elevação temporária de SortingGroup, zoom focal com Mouse1
    /// e retorno suave (soft clamp) para os limites da Zona C ao soltar fora da bancada.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SortingGroup))]
    public class DraggableDocument : MonoBehaviour
    {
        public const string DefaultSortingLayerName = "30_DraggableDocs";

        [Header("Identificação")]
        [SerializeField] private bool isRelic;

        [Header("Referências")]
        [SerializeField] private Camera targetCamera;

        [Header("Ordenação (SortingGroup)")]
        [SerializeField] private string sortingLayerName = DefaultSortingLayerName;
        [SerializeField] private int dragOrderBoost = 500;

        [Header("Zoom Focal (Botão Direito / Mouse1)")]
        [SerializeField] private float normalScaleMultiplier = 1.0f;
        [SerializeField] private float zoomedScaleMultiplier = 1.35f;
        [SerializeField] private float zoomDuration = 0.15f;

        [Header("Limites da Bancada (Zona C - Soft Clamp)")]
        [SerializeField] private Vector2 deskBoundsMin = new Vector2(-4.5f, -2.5f);
        [SerializeField] private Vector2 deskBoundsMax = new Vector2(4.5f, 0.5f);
        [SerializeField] private float softClampDuration = 0.25f;

        private static int s_highestRestingOrder;
        private static DraggableDocument s_currentlyDragged;

        private BoxCollider2D _boxCollider;
        private SortingGroup _sortingGroup;
        private Rigidbody2D _rigidbody2D;

        private bool _isInteractable = true;
        private bool _isDragging;
        private Vector3 _dragOffset;
        private float _initialZ;
        private int _restingOrder;

        private Vector3 _baseScale;
        private bool _isZoomed;
        private bool _isZoomAnimating;
        private float _zoomElapsed;
        private Vector3 _zoomStartScale;
        private Vector3 _zoomTargetScale;

        private bool _isSoftClamping;
        private float _clampElapsed;
        private Vector3 _clampStartPosition;
        private Vector3 _clampTargetPosition;

        private bool _isRejectionSliding;
        private Vector2 _rejectionVelocity;
        private float _rejectionDeceleration;

        public bool IsRelic
        {
            get => isRelic;
            set => isRelic = value;
        }

        public bool IsDragging => _isDragging;
        public bool IsZoomed => _isZoomed;
        public int CurrentSortingOrder => _sortingGroup != null ? _sortingGroup.sortingOrder : 0;

        public event Action<DraggableDocument> OnDragStarted;
        public event Action<DraggableDocument> OnDragEnded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_highestRestingOrder = 0;
            s_currentlyDragged = null;
        }

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider2D>();
            _sortingGroup = GetComponent<SortingGroup>();
            _rigidbody2D = GetComponent<Rigidbody2D>();

            _baseScale = transform.localScale;
            if (_baseScale == Vector3.zero)
            {
                _baseScale = Vector3.one;
            }

            _initialZ = transform.position.z;

            ApplySortingLayer();
            AssignInitialRestingOrder();
        }

        private void Start()
        {
            ResolveCameraIfNeeded();
        }

        private void OnDisable()
        {
            if (_isDragging)
            {
                EndDrag(applySoftClamp: false);
            }
        }

        private void Update()
        {
            UpdateZoomAnimation();
            UpdateRejectionImpulse();
            UpdateSoftClampAnimation();

            if (!_isInteractable)
            {
                return;
            }

            if (!ResolveCameraIfNeeded())
            {
                return;
            }

            if (!TryReadMouseState(
                out Vector2 mouseScreenPos,
                out bool leftDown,
                out bool leftHeld,
                out bool leftUp,
                out bool rightDown))
            {
                return;
            }

            Vector3 mouseWorldPos = ScreenToDocumentWorldPoint(mouseScreenPos);
            Vector2 mouseWorldPoint2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            if (_isDragging)
            {
                if (leftHeld)
                {
                    Vector3 targetPos = mouseWorldPos + _dragOffset;
                    targetPos.z = _initialZ;
                    transform.position = targetPos;
                }

                if (leftUp || !leftHeld)
                {
                    EndDrag(applySoftClamp: true);
                }
            }
            else if (leftDown &&
                     s_currentlyDragged == null &&
                     !IsClickBlockedByActiveTool(mouseWorldPoint2D) &&
                     IsTopmostDocumentAtPoint(mouseWorldPoint2D))
            {
                BeginDrag(mouseWorldPos);
            }

            if (rightDown && IsTopmostDocumentAtPoint(mouseWorldPoint2D))
            {
                ToggleFocalZoom();
            }
        }

        /// <summary>
        /// Habilita ou desabilita a interação do documento (útil durante transições de caso).
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;

            if (!interactable && _isDragging)
            {
                EndDrag(applySoftClamp: false);
            }
        }

        /// <summary>
        /// Permite ajustar programaticamente os limites da Zona C.
        /// </summary>
        public void ConfigureDeskBounds(Vector2 minBounds, Vector2 maxBounds)
        {
            deskBoundsMin = new Vector2(
                Mathf.Min(minBounds.x, maxBounds.x),
                Mathf.Min(minBounds.y, maxBounds.y));
            deskBoundsMax = new Vector2(
                Mathf.Max(minBounds.x, maxBounds.x),
                Mathf.Max(minBounds.y, maxBounds.y));
        }

        /// <summary>
        /// Restaura a escala normal do documento sem zoom.
        /// </summary>
        public void ResetZoom(bool instant = false)
        {
            _isZoomed = false;
            Vector3 targetScale = _baseScale * normalScaleMultiplier;

            if (instant || zoomDuration <= 0f)
            {
                _isZoomAnimating = false;
                transform.localScale = targetScale;
                return;
            }

            StartZoomTransition(targetScale);
        }

        /// <summary>
        /// Cancela uma animação de soft clamp em andamento (ex.: quando uma relíquia é aceita no prato da balança).
        /// </summary>
        public void CancelSoftClamp()
        {
            _isSoftClamping = false;
        }

        /// <summary>
        /// Aplica um pulso de força física para empurrar o documento para fora de uma área inválida (ex.: prato da balança).
        /// Utiliza Rigidbody2D dinâmico quando disponível ou simula impulso com desaceleração suave em 2D.
        /// </summary>
        public void ApplyRejectionImpulse(Vector2 direction, float impulseStrength = 7.5f, float deceleration = 22f)
        {
            Vector2 safeDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _isSoftClamping = false;

            if (_rigidbody2D != null && _rigidbody2D.bodyType == RigidbodyType2D.Dynamic)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.AddForce(safeDir * impulseStrength, ForceMode2D.Impulse);
                return;
            }

            _rejectionVelocity = safeDir * impulseStrength;
            _rejectionDeceleration = Mathf.Max(1f, deceleration);
            _isRejectionSliding = true;
        }

        /// <summary>
        /// Eleva este documento para o topo da pilha de documentos em repouso.
        /// </summary>
        public void BringToFront()
        {
            s_highestRestingOrder++;
            _restingOrder = s_highestRestingOrder;

            if (_sortingGroup != null && !_isDragging)
            {
                _sortingGroup.sortingOrder = _restingOrder;
            }
        }

        private void ApplySortingLayer()
        {
            if (_sortingGroup == null)
            {
                return;
            }

            string targetLayer = string.IsNullOrWhiteSpace(sortingLayerName)
                ? DefaultSortingLayerName
                : sortingLayerName;

            if (SortingLayer.NameToID(targetLayer) != 0 || targetLayer == "Default")
            {
                _sortingGroup.sortingLayerName = targetLayer;
            }
            else
            {
                Debug.LogWarning(
                    $"[DraggableDocument] Sorting Layer '{targetLayer}' não encontrada no TagManager. " +
                    "Execute 'Tools/1. Configurar Sorting Layers'.",
                    this);
            }
        }

        private void AssignInitialRestingOrder()
        {
            s_highestRestingOrder++;
            _restingOrder = s_highestRestingOrder;
            _sortingGroup.sortingOrder = _restingOrder;
        }

        private void BeginDrag(Vector3 mouseWorldPos)
        {
            _isDragging = true;
            s_currentlyDragged = this;

            _isSoftClamping = false;
            _isRejectionSliding = false;
            _rejectionVelocity = Vector2.zero;
            _initialZ = transform.position.z;
            _dragOffset = transform.position - mouseWorldPos;
            _dragOffset.z = 0f;

            // Promove a ordem de repouso e aplica boost temporário enquanto arrasta.
            s_highestRestingOrder++;
            _restingOrder = s_highestRestingOrder;
            _sortingGroup.sortingOrder = _restingOrder + dragOrderBoost;

            OnDragStarted?.Invoke(this);
        }

        private void EndDrag(bool applySoftClamp)
        {
            _isDragging = false;

            if (s_currentlyDragged == this)
            {
                s_currentlyDragged = null;
            }

            if (_sortingGroup != null)
            {
                _sortingGroup.sortingOrder = _restingOrder;
            }

            OnDragEnded?.Invoke(this);

            if (applySoftClamp && !_isRejectionSliding)
            {
                StartSoftClampIfOutOfBounds();
            }
        }

        private void ToggleFocalZoom()
        {
            _isZoomed = !_isZoomed;

            if (_isZoomed)
            {
                BringToFront();
            }

            float targetMultiplier = _isZoomed ? zoomedScaleMultiplier : normalScaleMultiplier;
            StartZoomTransition(_baseScale * targetMultiplier);
        }

        private void StartZoomTransition(Vector3 targetScale)
        {
            _zoomStartScale = transform.localScale;
            _zoomTargetScale = targetScale;
            _zoomElapsed = 0f;

            if (zoomDuration <= 0f)
            {
                transform.localScale = _zoomTargetScale;
                _isZoomAnimating = false;
                return;
            }

            _isZoomAnimating = true;
        }

        private void UpdateZoomAnimation()
        {
            if (!_isZoomAnimating)
            {
                return;
            }

            _zoomElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_zoomElapsed / zoomDuration);
            transform.localScale = Vector3.Lerp(_zoomStartScale, _zoomTargetScale, t);

            if (t >= 1f)
            {
                transform.localScale = _zoomTargetScale;
                _isZoomAnimating = false;
            }
        }

        private void StartSoftClampIfOutOfBounds()
        {
            Vector3 currentPos = transform.position;
            Vector3 clampedPos = new Vector3(
                Mathf.Clamp(currentPos.x, deskBoundsMin.x, deskBoundsMax.x),
                Mathf.Clamp(currentPos.y, deskBoundsMin.y, deskBoundsMax.y),
                _initialZ);

            if ((currentPos - clampedPos).sqrMagnitude <= 0.00001f)
            {
                _isSoftClamping = false;
                return;
            }

            _clampStartPosition = currentPos;
            _clampTargetPosition = clampedPos;
            _clampElapsed = 0f;

            if (softClampDuration <= 0f)
            {
                transform.position = _clampTargetPosition;
                _isSoftClamping = false;
                return;
            }

            _isSoftClamping = true;
        }

        private void UpdateRejectionImpulse()
        {
            if (!_isRejectionSliding)
            {
                return;
            }

            Vector3 pos = transform.position;
            pos.x += _rejectionVelocity.x * Time.deltaTime;
            pos.y += _rejectionVelocity.y * Time.deltaTime;
            pos.z = _initialZ;
            transform.position = pos;

            _rejectionVelocity = Vector2.MoveTowards(
                _rejectionVelocity,
                Vector2.zero,
                _rejectionDeceleration * Time.deltaTime);

            if (_rejectionVelocity.sqrMagnitude <= 0.01f)
            {
                _rejectionVelocity = Vector2.zero;
                _isRejectionSliding = false;
                StartSoftClampIfOutOfBounds();
            }
        }

        private bool IsClickBlockedByActiveTool(Vector2 worldPoint)
        {
            if (RevealingInkController.WasInkConsumedThisFrame)
            {
                return true;
            }

            // Se a Tinta Reveladora estiver equipada e o clique atingir uma área tarjada revelável,
            // evita arrastar o documento acidentalmente durante a aplicação da tinta.
            if (!RevealingInkController.IsInkToolEquipped)
            {
                return false;
            }

            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null &&
                    hits[i].TryGetComponent(out RedactedArea redactedArea) &&
                    redactedArea.isActiveAndEnabled &&
                    !redactedArea.EvidenceUnlocked)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateSoftClampAnimation()
        {
            if (!_isSoftClamping)
            {
                return;
            }

            _clampElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_clampElapsed / softClampDuration);
            transform.position = Vector3.Lerp(_clampStartPosition, _clampTargetPosition, t);

            if (t >= 1f)
            {
                transform.position = _clampTargetPosition;
                _isSoftClamping = false;
            }
        }

        private bool IsTopmostDocumentAtPoint(Vector2 worldPoint)
        {
            if (_boxCollider == null || !_boxCollider.OverlapPoint(worldPoint))
            {
                return false;
            }

            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
            DraggableDocument topmost = this;
            int highestOrder = CurrentSortingOrder;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                if (!hit.TryGetComponent(out DraggableDocument candidate) || !candidate.isActiveAndEnabled || !candidate._isInteractable)
                {
                    continue;
                }

                int candidateOrder = candidate.CurrentSortingOrder;
                if (candidateOrder > highestOrder ||
                    (candidateOrder == highestOrder && candidate.GetInstanceID() > topmost.GetInstanceID()))
                {
                    highestOrder = candidateOrder;
                    topmost = candidate;
                }
            }

            return topmost == this;
        }

        private bool ResolveCameraIfNeeded()
        {
            if (targetCamera != null)
            {
                return true;
            }

            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }

            return targetCamera != null;
        }

        private Vector3 ScreenToDocumentWorldPoint(Vector2 screenPosition)
        {
            float depth = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, depth);
            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(screenPoint);
            worldPoint.z = _initialZ;
            return worldPoint;
        }

        private static bool TryReadMouseState(
            out Vector2 screenPosition,
            out bool leftDown,
            out bool leftHeld,
            out bool leftUp,
            out bool rightDown)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                screenPosition = Vector2.zero;
                leftDown = false;
                leftHeld = false;
                leftUp = false;
                rightDown = false;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            leftDown = mouse.leftButton.wasPressedThisFrame;
            leftHeld = mouse.leftButton.isPressed;
            leftUp = mouse.leftButton.wasReleasedThisFrame;
            rightDown = mouse.rightButton.wasPressedThisFrame;
            return true;
#else
            screenPosition = Input.mousePosition;
            leftDown = Input.GetMouseButtonDown(0);
            leftHeld = Input.GetMouseButton(0);
            leftUp = Input.GetMouseButtonUp(0);
            rightDown = Input.GetMouseButtonDown(1);
            return true;
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = new Vector3(
                (deskBoundsMin.x + deskBoundsMax.x) * 0.5f,
                (deskBoundsMin.y + deskBoundsMax.y) * 0.5f,
                transform.position.z);
            Vector3 size = new Vector3(
                Mathf.Abs(deskBoundsMax.x - deskBoundsMin.x),
                Mathf.Abs(deskBoundsMax.y - deskBoundsMin.y),
                0.05f);

            Gizmos.color = new Color(0.29f, 0.85f, 0.55f, 0.85f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
