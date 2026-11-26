using System;
using System.Collections;
using TMPro;
using TribunalDasAlmas.Core;
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
        [SerializeField] private bool isDeathCertificate;

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

        private Coroutine _warningFlashCoroutine;
        private Color _originalRendererColor = Color.white;
        private bool _hasCachedRendererColor;
        private GameObject _appliedStampVisual;
        private Texture2D _generatedStampTexture;
        private Sprite _generatedStampSprite;

        public bool IsRelic
        {
            get => isRelic;
            set => isRelic = value;
        }

        public bool IsDeathCertificate
        {
            get => isDeathCertificate;
            set => isDeathCertificate = value;
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

            RestoreWarningFlashColor();
        }

        private void OnDestroy()
        {
            if (_generatedStampSprite != null)
            {
                Destroy(_generatedStampSprite);
            }

            if (_generatedStampTexture != null)
            {
                Destroy(_generatedStampTexture);
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
        /// Aplica um selo visual de carimbo diretamente sobre o papel do documento,
        /// acompanhando o SortingGroup e o arraste da Certidão de Passamento.
        /// </summary>
        public void ApplyStampSeal(
            VerdictType verdict,
            Color sealColor,
            string sealLabel,
            Vector2 localPosition,
            TMP_FontAsset fontAsset = null)
        {
            if (_appliedStampVisual != null)
            {
                Destroy(_appliedStampVisual);
                _appliedStampVisual = null;
            }

            EnsureStampBorderSprite();

            GameObject stampObj = new GameObject($"AppliedStamp_{verdict}");
            stampObj.transform.SetParent(transform, false);

            // Mantém o selo proporcional ao documento e dentro dos limites internos do papel
            Vector2 paperSize = _boxCollider != null ? _boxCollider.size : new Vector2(3.4f, 4.3f);
            float scaleFactor = Mathf.Clamp(paperSize.x / 3.4f, 0.85f, 2.5f);
            Vector2 sealSize = new Vector2(1.75f * scaleFactor, 0.58f * scaleFactor);

            Vector2 halfSize = paperSize * 0.5f;
            float marginX = sealSize.x * 0.52f;
            float marginY = sealSize.y * 0.55f;
            float clampedX = Mathf.Clamp(localPosition.x, -Mathf.Max(0.2f, halfSize.x - marginX), Mathf.Max(0.2f, halfSize.x - marginX));
            float clampedY = Mathf.Clamp(localPosition.y, -Mathf.Max(0.2f, halfSize.y - marginY), Mathf.Max(0.2f, halfSize.y - marginY));

            stampObj.transform.localPosition = new Vector3(clampedX, clampedY, -0.025f);
            stampObj.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-11f, -5f));

            string docLayer = _sortingGroup != null && !string.IsNullOrEmpty(_sortingGroup.sortingLayerName)
                ? _sortingGroup.sortingLayerName
                : DefaultSortingLayerName;

            // Moldura do selo (9-sliced com centro translúcido para parecer tinta de carimbo)
            SpriteRenderer frameSr = stampObj.AddComponent<SpriteRenderer>();
            frameSr.sprite = _generatedStampSprite;
            frameSr.drawMode = SpriteDrawMode.Sliced;
            frameSr.size = sealSize;
            frameSr.color = new Color(sealColor.r, sealColor.g, sealColor.b, 0.92f);
            frameSr.sortingLayerName = docLayer;
            frameSr.sortingOrder = 6;

            // Texto do selo
            GameObject labelObj = new GameObject("StampLabel");
            labelObj.transform.SetParent(stampObj.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0f, -0.005f);

            TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = sealSize * 0.94f;
            tmp.text = sealLabel.ToUpperInvariant();
            tmp.fontSize = 2.0f * scaleFactor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = new Color(sealColor.r, sealColor.g, sealColor.b, 0.96f);

            if (fontAsset != null)
            {
                tmp.font = fontAsset;
            }
            else if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            MeshRenderer textMr = tmp.GetComponent<MeshRenderer>();
            if (textMr != null)
            {
                textMr.sortingLayerName = docLayer;
                textMr.sortingOrder = 7;
            }

            _appliedStampVisual = stampObj;
        }

        /// <summary>
        /// Pisca suavemente o papel do documento em vermelho (feedback de trava de segurança ao tentar despachar sem carimbo).
        /// </summary>
        public void FlashWarningRed(int flashCount = 2, float singleFlashDuration = 0.24f)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (_warningFlashCoroutine != null)
            {
                StopCoroutine(_warningFlashCoroutine);
                RestoreWarningFlashColor();
            }

            _warningFlashCoroutine = StartCoroutine(WarningFlashRoutine(Mathf.Max(1, flashCount), Mathf.Max(0.08f, singleFlashDuration)));
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
            if (RevealingInkController.WasInkConsumedThisFrame || StampController.WasStampAppliedThisFrame)
            {
                return true;
            }

            // Se um carimbo estiver equipado sobre a Certidão de Passamento, prioriza a aplicação do carimbo.
            if (isDeathCertificate && StampController.IsAnyStampEquipped)
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

        private IEnumerator WarningFlashRoutine(int flashCount, float singleFlashDuration)
        {
            SpriteRenderer paperRenderer = GetComponent<SpriteRenderer>();
            if (paperRenderer == null)
            {
                _warningFlashCoroutine = null;
                yield break;
            }

            if (!_hasCachedRendererColor)
            {
                _originalRendererColor = paperRenderer.color;
                _hasCachedRendererColor = true;
            }

            Color softWarningRed = new Color(1.0f, 0.56f, 0.56f, _originalRendererColor.a);
            float halfDuration = singleFlashDuration * 0.5f;

            for (int i = 0; i < flashCount; i++)
            {
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / halfDuration);
                    paperRenderer.color = Color.Lerp(_originalRendererColor, softWarningRed, t);
                    yield return null;
                }

                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / halfDuration);
                    paperRenderer.color = Color.Lerp(softWarningRed, _originalRendererColor, t);
                    yield return null;
                }
            }

            RestoreWarningFlashColor();
            _warningFlashCoroutine = null;
        }

        private void RestoreWarningFlashColor()
        {
            if (!_hasCachedRendererColor)
            {
                return;
            }

            SpriteRenderer paperRenderer = GetComponent<SpriteRenderer>();
            if (paperRenderer != null)
            {
                paperRenderer.color = _originalRendererColor;
            }
        }

        private void EnsureStampBorderSprite()
        {
            if (_generatedStampSprite != null)
            {
                return;
            }

            const int size = 16;
            _generatedStampTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 borderPixel = new Color32(255, 255, 255, 255);
            Color32 fillPixel = new Color32(255, 255, 255, 34);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x <= 1 || y <= 1 || x >= size - 2 || y >= size - 2;
                    pixels[y * size + x] = isBorder ? borderPixel : fillPixel;
                }
            }

            _generatedStampTexture.SetPixels32(pixels);
            _generatedStampTexture.Apply();

            _generatedStampSprite = Sprite.Create(
                _generatedStampTexture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(2f, 2f, 2f, 2f));
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
