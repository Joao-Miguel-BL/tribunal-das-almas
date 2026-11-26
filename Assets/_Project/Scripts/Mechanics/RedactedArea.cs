using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TribunalDasAlmas.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TribunalDasAlmas.Mechanics
{
    /// <summary>
    /// Mecânica M3 — Área Tarjada do Documento (DD-011).
    /// Anexada à tarja preta (#1C2026) sobre trechos censurados da certidão.
    /// Quando a Tinta Reveladora é equipada, exibe halo violeta pulsante (#BB8FCE, alpha 0.3..0.9 via Sine Wave).
    /// Ao clicar com a tinta equipada e gotas > 0, consome 1 gota, dissolve a tarja suavemente,
    /// revela hiddenRedactedText do SoulCaseData e marca evidenceUnlocked = true.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RedactedArea : MonoBehaviour
    {
        public const string DocumentSortingLayerName = "30_DraggableDocs";

        private static readonly Color ColorRedactionBar = new Color32(0x1C, 0x20, 0x26, 0xFF);   // #1C2026 Tarja Preta
        private static readonly Color ColorVioletHalo = new Color32(0xBB, 0x8F, 0xCE, 0xFF);     // #BB8FCE Halo Violeta (DD-011)
        private static readonly Color ColorRevealedInk = new Color32(0x4A, 0x23, 0x5A, 0xFF);    // Tinta revelada legível no papel creme

        [Header("Dados do Caso")]
        [SerializeField] private SoulCaseData soulCaseData;

        [Header("Referências Visuais")]
        [SerializeField] private SpriteRenderer redactionBarRenderer;
        [SerializeField] private SpriteRenderer violetHaloRenderer;
        [SerializeField] private TextMeshPro hiddenTextMesh;
        [SerializeField] private Camera targetCamera;

        [Header("Pulso do Halo Violeta (DD-011)")]
        [SerializeField] private float haloMinAlpha = 0.3f;
        [SerializeField] private float haloMaxAlpha = 0.9f;
        [SerializeField] private float haloPulseSpeed = 5.0f;
        [SerializeField] private Vector2 haloPadding = new Vector2(0.14f, 0.14f);

        [Header("Animação de Dissolução")]
        [SerializeField] private float dissolveDuration = 0.65f;

        [Header("Estado")]
        [SerializeField] private bool evidenceUnlocked;

        private readonly List<UnityEngine.Object> _generatedResources = new List<UnityEngine.Object>();

        private BoxCollider2D _areaCollider;
        private DraggableDocument _parentDocument;
        private bool _isHaloActive;
        private bool _isDissolving;
        private Vector2 _configuredSize = new Vector2(2.8f, 0.85f);

        public bool EvidenceUnlocked => evidenceUnlocked;
        public bool IsDissolving => _isDissolving;

        public event Action<RedactedArea, string> OnAreaRevealed;

        private void Awake()
        {
            _areaCollider = GetComponent<BoxCollider2D>();
            _parentDocument = GetComponentInParent<DraggableDocument>();

            if (_areaCollider.size.sqrMagnitude > 0.001f)
            {
                _configuredSize = _areaCollider.size;
            }

            EnsureVisualComponents();
            ApplyInitialVisualState();
        }

        private void OnEnable()
        {
            RevealingInkController.OnRevealingInkEquippedChanged += HandleInkToolEquippedChanged;
            // Sincroniza estado imediatamente caso o documento seja instanciado com a tinta já equipada.
            HandleInkToolEquippedChanged(RevealingInkController.IsInkToolEquipped);
        }

        private void OnDisable()
        {
            RevealingInkController.OnRevealingInkEquippedChanged -= HandleInkToolEquippedChanged;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _generatedResources.Count; i++)
            {
                if (_generatedResources[i] != null)
                {
                    Destroy(_generatedResources[i]);
                }
            }
            _generatedResources.Clear();
        }

        private void Update()
        {
            UpdateVioletHaloPulse();

            if (evidenceUnlocked || _isDissolving || !RevealingInkController.IsInkToolEquipped)
            {
                return;
            }

            if (!ResolveCameraIfNeeded())
            {
                return;
            }

            if (!TryReadLeftClick(out Vector2 mouseScreenPos))
            {
                return;
            }

            Vector3 worldPos = ScreenToWorldPoint(mouseScreenPos);
            Vector2 worldPoint2D = new Vector2(worldPos.x, worldPos.y);

            if (_areaCollider != null && _areaCollider.OverlapPoint(worldPoint2D) && IsParentDocumentOnTop(worldPoint2D))
            {
                TryApplyRevealingInk();
            }
        }

        /// <summary>
        /// Configura dinamicamente a área tarjada a partir de um SoulCaseData (usado pelo VerticalSliceGameManager ou prefabs).
        /// </summary>
        public void Configure(SoulCaseData caseData, Vector2 areaSize, TMP_FontAsset fontAsset = null)
        {
            soulCaseData = caseData;
            _configuredSize = areaSize;
            evidenceUnlocked = false;
            _isDissolving = false;

            if (_areaCollider == null)
            {
                _areaCollider = GetComponent<BoxCollider2D>();
            }
            _areaCollider.size = _configuredSize;

            EnsureVisualComponents();

            if (redactionBarRenderer != null)
            {
                redactionBarRenderer.transform.localScale = new Vector3(_configuredSize.x, _configuredSize.y, 1f);
            }

            if (violetHaloRenderer != null)
            {
                violetHaloRenderer.transform.localScale = new Vector3(
                    _configuredSize.x + haloPadding.x,
                    _configuredSize.y + haloPadding.y,
                    1f);
            }

            if (hiddenTextMesh != null)
            {
                hiddenTextMesh.rectTransform.sizeDelta = _configuredSize - new Vector2(0.2f, 0.12f);
                hiddenTextMesh.fontSize = Mathf.Clamp(_configuredSize.y * 1.85f, 1.75f, 3.4f);
                if (fontAsset != null)
                {
                    hiddenTextMesh.font = fontAsset;
                }
                hiddenTextMesh.text = ResolveHiddenText();
            }

            ApplyInitialVisualState();
            HandleInkToolEquippedChanged(RevealingInkController.IsInkToolEquipped);
        }

        /// <summary>
        /// Tenta aplicar a Tinta Reveladora nesta tarja, consumindo 1 gota e iniciando a dissolução.
        /// </summary>
        public bool TryApplyRevealingInk()
        {
            if (evidenceUnlocked || _isDissolving)
            {
                return false;
            }

            RevealingInkController inkController = RevealingInkController.ActiveInstance;
            if (inkController == null || !inkController.IsEquipped || inkController.InkDrops <= 0)
            {
                return false;
            }

            if (!inkController.TryConsumeInkDrop())
            {
                return false;
            }

            StartCoroutine(DissolveRedactionRoutine());
            return true;
        }

        private IEnumerator DissolveRedactionRoutine()
        {
            _isDissolving = true;
            _isHaloActive = false;
            SetHaloVisible(false);

            string revealedText = ResolveHiddenText();
            if (hiddenTextMesh != null)
            {
                hiddenTextMesh.text = revealedText;
                hiddenTextMesh.gameObject.SetActive(true);
                SetTextAlpha(hiddenTextMesh, 0f);
            }

            Vector3 initialBarScale = redactionBarRenderer != null
                ? redactionBarRenderer.transform.localScale
                : Vector3.one;

            float duration = Mathf.Max(0.05f, dissolveDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Dissolução suave da tarja preta (#1C2026)
                if (redactionBarRenderer != null)
                {
                    Color barColor = ColorRedactionBar;
                    barColor.a = Mathf.Lerp(1f, 0f, smoothT);
                    redactionBarRenderer.color = barColor;

                    // Leve afinamento vertical para reforçar sensação de tinta dissolvendo o bloco
                    redactionBarRenderer.transform.localScale = new Vector3(
                        initialBarScale.x,
                        Mathf.Lerp(initialBarScale.y, initialBarScale.y * 0.25f, smoothT),
                        initialBarScale.z);
                }

                // Aparição progressiva do texto oculto do ScriptableObject
                if (hiddenTextMesh != null)
                {
                    float textFade = Mathf.Clamp01((t - 0.15f) / 0.85f);
                    SetTextAlpha(hiddenTextMesh, Mathf.SmoothStep(0f, 1f, textFade));
                }

                yield return null;
            }

            if (redactionBarRenderer != null)
            {
                redactionBarRenderer.enabled = false;
            }

            if (hiddenTextMesh != null)
            {
                SetTextAlpha(hiddenTextMesh, 1f);
            }

            if (_areaCollider != null)
            {
                _areaCollider.enabled = false;
            }

            _isDissolving = false;
            evidenceUnlocked = true;

            Debug.Log($"[RedactedArea] Evidência oculta revelada e desbloqueada: \"{revealedText}\"");

            OnAreaRevealed?.Invoke(this, revealedText);
            RevealingInkController.NotifyEvidenceRevealed(this, revealedText);
        }

        private void HandleInkToolEquippedChanged(bool isEquipped)
        {
            _isHaloActive = isEquipped && !evidenceUnlocked && !_isDissolving;
            SetHaloVisible(_isHaloActive);
        }

        private void UpdateVioletHaloPulse()
        {
            if (!_isHaloActive || violetHaloRenderer == null)
            {
                return;
            }

            // DD-011: Pulso via alpha de 0.3 a 0.9 com Sine Wave em #BB8FCE
            float sineWave01 = (Mathf.Sin(Time.time * haloPulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(haloMinAlpha, haloMaxAlpha, sineWave01);

            Color haloColor = ColorVioletHalo;
            haloColor.a = alpha;
            violetHaloRenderer.color = haloColor;
        }

        private void SetHaloVisible(bool visible)
        {
            if (violetHaloRenderer == null)
            {
                return;
            }

            violetHaloRenderer.enabled = visible;
            if (!visible)
            {
                Color c = ColorVioletHalo;
                c.a = 0f;
                violetHaloRenderer.color = c;
            }
        }

        private void EnsureVisualComponents()
        {
            string docLayer = ResolveSortingLayerOrDefault(DocumentSortingLayerName);
            Sprite solidSprite = null;

            if (violetHaloRenderer == null)
            {
                solidSprite ??= CreateSolidSprite();
                GameObject haloObj = new GameObject("Redacted_VioletHalo");
                haloObj.transform.SetParent(transform, false);
                haloObj.transform.localPosition = new Vector3(0f, 0f, 0.002f);
                haloObj.transform.localScale = new Vector3(
                    _configuredSize.x + haloPadding.x,
                    _configuredSize.y + haloPadding.y,
                    1f);

                violetHaloRenderer = haloObj.AddComponent<SpriteRenderer>();
                violetHaloRenderer.sprite = solidSprite;
                violetHaloRenderer.color = new Color(ColorVioletHalo.r, ColorVioletHalo.g, ColorVioletHalo.b, 0f);
                violetHaloRenderer.sortingLayerName = docLayer;
                violetHaloRenderer.sortingOrder = 2;
                violetHaloRenderer.enabled = false;
            }

            if (redactionBarRenderer == null)
            {
                redactionBarRenderer = GetComponent<SpriteRenderer>();
                if (redactionBarRenderer == null)
                {
                    solidSprite ??= CreateSolidSprite();
                    GameObject barObj = new GameObject("Redacted_BlackBar");
                    barObj.transform.SetParent(transform, false);
                    barObj.transform.localPosition = Vector3.zero;
                    barObj.transform.localScale = new Vector3(_configuredSize.x, _configuredSize.y, 1f);

                    redactionBarRenderer = barObj.AddComponent<SpriteRenderer>();
                    redactionBarRenderer.sprite = solidSprite;
                }

                redactionBarRenderer.color = ColorRedactionBar;
                redactionBarRenderer.sortingLayerName = docLayer;
                redactionBarRenderer.sortingOrder = 4;
            }

            if (hiddenTextMesh == null)
            {
                GameObject textObj = new GameObject("Redacted_HiddenText");
                textObj.transform.SetParent(transform, false);
                textObj.transform.localPosition = new Vector3(0f, 0f, -0.005f);

                hiddenTextMesh = textObj.AddComponent<TextMeshPro>();
                hiddenTextMesh.rectTransform.sizeDelta = _configuredSize - new Vector2(0.12f, 0.08f);
                hiddenTextMesh.fontSize = 1.75f;
                hiddenTextMesh.fontStyle = FontStyles.Italic;
                hiddenTextMesh.alignment = TextAlignmentOptions.MidlineLeft;
                hiddenTextMesh.textWrappingMode = TextWrappingModes.Normal;
                hiddenTextMesh.color = ColorRevealedInk;

                if (hiddenTextMesh.font == null && TMP_Settings.defaultFontAsset != null)
                {
                    hiddenTextMesh.font = TMP_Settings.defaultFontAsset;
                }

                MeshRenderer mr = hiddenTextMesh.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sortingLayerName = docLayer;
                    mr.sortingOrder = 3;
                }
            }
        }

        private void ApplyInitialVisualState()
        {
            if (redactionBarRenderer != null)
            {
                redactionBarRenderer.color = ColorRedactionBar;
                redactionBarRenderer.enabled = !evidenceUnlocked;
            }

            if (hiddenTextMesh != null)
            {
                hiddenTextMesh.text = ResolveHiddenText();
                hiddenTextMesh.gameObject.SetActive(evidenceUnlocked);
                SetTextAlpha(hiddenTextMesh, evidenceUnlocked ? 1f : 0f);
            }

            if (_areaCollider != null)
            {
                _areaCollider.enabled = !evidenceUnlocked;
            }

            SetHaloVisible(false);
        }

        private string ResolveHiddenText()
        {
            if (soulCaseData == null)
            {
                VerticalSliceGameManager manager = FindFirstObjectByType<VerticalSliceGameManager>();
                if (manager != null)
                {
                    soulCaseData = manager.CurrentCase;
                }
            }

            if (soulCaseData != null && !string.IsNullOrWhiteSpace(soulCaseData.hiddenRedactedText))
            {
                return soulCaseData.hiddenRedactedText;
            }

            return "[Evidência revelada]";
        }

        private bool IsParentDocumentOnTop(Vector2 worldPoint)
        {
            if (_parentDocument == null)
            {
                return true;
            }

            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
            int parentOrder = _parentDocument.CurrentSortingOrder;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null &&
                    hits[i].TryGetComponent(out DraggableDocument otherDoc) &&
                    otherDoc != _parentDocument &&
                    otherDoc.CurrentSortingOrder > parentOrder)
                {
                    return false;
                }
            }

            return true;
        }

        private Sprite CreateSolidSprite()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _generatedResources.Add(tex);
            _generatedResources.Add(sprite);
            return sprite;
        }

        private static void SetTextAlpha(TextMeshPro tmp, float alpha)
        {
            Color c = tmp.color;
            c.a = alpha;
            tmp.color = c;
        }

        private bool ResolveCameraIfNeeded()
        {
            if (targetCamera != null)
            {
                return true;
            }

            targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            return targetCamera != null;
        }

        private Vector3 ScreenToWorldPoint(Vector2 screenPosition)
        {
            float depth = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
            worldPoint.z = transform.position.z;
            return worldPoint;
        }

        private static bool TryReadLeftClick(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            return true;
#else
            if (!Input.GetMouseButtonDown(0))
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = Input.mousePosition;
            return true;
#endif
        }

        private static string ResolveSortingLayerOrDefault(string layerName)
        {
            return SortingLayer.NameToID(layerName) != 0 ? layerName : "Default";
        }
    }
}
