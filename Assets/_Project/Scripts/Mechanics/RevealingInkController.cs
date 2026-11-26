using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TribunalDasAlmas.Mechanics
{
    /// <summary>
    /// Mecânica M3 — Controlador do Tinteiro de Tinta Reveladora (Zona D, DD-011).
    /// Gerencia o estoque máximo de 3 gotas por turno, equipa a ferramenta no cursor ao clicar
    /// com Mouse0 e emite o sinal global que ativa o halo violeta pulsante nas RedactedAreas.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RevealingInkController : MonoBehaviour
    {
        public const int MaxInkDrops = 3;
        public const string InkwellSortingLayerName = "20_DeskProps_Fixed";
        public const string ActiveToolSortingLayerName = "40_ActiveTool_VFX";

        private static readonly Color ColorInkBottle = new Color32(0x34, 0x3C, 0x47, 0xFF);   // #343C47 Vidro/Metal escuro
        private static readonly Color ColorInkLiquid = new Color32(0x6C, 0x34, 0x83, 0xFF);   // #6C3483 Tinta Reveladora
        private static readonly Color ColorInkViolet = new Color32(0xBB, 0x8F, 0xCE, 0xFF);   // #BB8FCE Halo / Contador
        private static readonly Color ColorInkEmpty = new Color32(0x2E, 0x23, 0x1E, 0xFF);    // #2E231E Vazio

        [Header("Estoque de Tinta (Turno)")]
        [SerializeField, Range(0, MaxInkDrops)] private int inkDrops = MaxInkDrops;

        [Header("Referências")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer inkwellLiquidRenderer;
        [SerializeField] private TextMeshPro dropsCounterText;
        [SerializeField] private Transform cursorToolVisual;
        [SerializeField] private bool autoCreateVisualPlaceholders = true;

        private readonly List<UnityEngine.Object> _generatedResources = new List<UnityEngine.Object>();

        private BoxCollider2D _inkwellCollider;
        private bool _isEquipped;
        private static int s_lastInkConsumedFrame = -1;

        public static RevealingInkController ActiveInstance { get; private set; }
        public static bool IsInkToolEquipped => ActiveInstance != null && ActiveInstance._isEquipped && ActiveInstance.inkDrops > 0;
        public static bool WasInkConsumedThisFrame => s_lastInkConsumedFrame == Time.frameCount;

        /// <summary>
        /// Evento global (DD-011): notifica todas as RedactedAreas quando a Tinta Reveladora
        /// é equipada (true) ou desequipada (false).
        /// </summary>
        public static event Action<bool> OnRevealingInkEquippedChanged;

        /// <summary>
        /// Notifica alterações no estoque de gotas de tinta: (gotasRestantes, maximoGotas).
        /// </summary>
        public static event Action<int, int> OnInkDropsChanged;

        /// <summary>
        /// Notifica quando uma área tarjada é dissolvida e a evidência é desbloqueada.
        /// </summary>
        public static event Action<RedactedArea, string> OnEvidenceRevealed;

        public int InkDrops => inkDrops;
        public bool HasInk => inkDrops > 0;
        public bool IsEquipped => _isEquipped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ActiveInstance = null;
            s_lastInkConsumedFrame = -1;
            OnRevealingInkEquippedChanged = null;
            OnInkDropsChanged = null;
            OnEvidenceRevealed = null;
        }

        private void Awake()
        {
            ActiveInstance = this;
            _inkwellCollider = GetComponent<BoxCollider2D>();
            inkDrops = Mathf.Clamp(inkDrops, 0, MaxInkDrops);

            if (autoCreateVisualPlaceholders && inkwellLiquidRenderer == null)
            {
                BuildInkwellPlaceholders();
            }

            if (cursorToolVisual == null)
            {
                BuildCursorToolIndicator();
            }

            if (cursorToolVisual != null)
            {
                cursorToolVisual.gameObject.SetActive(false);
            }

            RefreshInkwellVisuals();
        }

        private void OnEnable()
        {
            ActiveInstance = this;
            ResolveCameraIfNeeded();
        }

        private void OnDisable()
        {
            if (_isEquipped)
            {
                SetEquipped(false);
            }

            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }
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
            if (!ResolveCameraIfNeeded())
            {
                return;
            }

            if (!TryReadInput(out Vector2 mouseScreenPos, out bool leftDown, out bool escapeDown))
            {
                return;
            }

            Vector3 mouseWorldPos = ScreenToWorldPoint(mouseScreenPos);
            Vector2 mouseWorldPoint2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            if (_isEquipped && cursorToolVisual != null)
            {
                cursorToolVisual.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, transform.position.z - 0.1f);
            }

            if (_isEquipped && escapeDown)
            {
                UnequipTool();
                return;
            }

            if (leftDown && _inkwellCollider != null && _inkwellCollider.OverlapPoint(mouseWorldPoint2D))
            {
                ToggleEquipFromInkwell();
            }
        }

        /// <summary>
        /// Equipa ou desequipa a Tinta Reveladora ao clicar no tinteiro.
        /// </summary>
        public void ToggleEquipFromInkwell()
        {
            if (_isEquipped)
            {
                SetEquipped(false);
                return;
            }

            if (inkDrops <= 0)
            {
                Debug.LogWarning("[RevealingInkController] Estoque de Tinta Reveladora esgotado (0/3 gotas neste turno).");
                return;
            }

            SetEquipped(true);
        }

        /// <summary>
        /// Desequipa explicitamente a Tinta Reveladora do cursor.
        /// </summary>
        public void UnequipTool()
        {
            if (_isEquipped)
            {
                SetEquipped(false);
            }
        }

        /// <summary>
        /// Consome 1 gota de tinta caso a ferramenta esteja equipada e haja gotas disponíveis.
        /// Desequipa automaticamente caso o estoque chegue a zero.
        /// </summary>
        public bool TryConsumeInkDrop()
        {
            if (!_isEquipped || inkDrops <= 0)
            {
                return false;
            }

            inkDrops = Mathf.Max(0, inkDrops - 1);
            s_lastInkConsumedFrame = Time.frameCount;
            RefreshInkwellVisuals();
            OnInkDropsChanged?.Invoke(inkDrops, MaxInkDrops);

            Debug.Log($"[RevealingInkController] 1 gota de Tinta Reveladora consumida. Restam {inkDrops}/{MaxInkDrops} gotas.");

            if (inkDrops == 0)
            {
                SetEquipped(false);
            }

            return true;
        }

        /// <summary>
        /// Recarrega o estoque máximo de 3 gotas de tinta para um novo turno.
        /// </summary>
        public void RefillInkForNewShift()
        {
            inkDrops = MaxInkDrops;
            RefreshInkwellVisuals();
            OnInkDropsChanged?.Invoke(inkDrops, MaxInkDrops);
        }

        /// <summary>
        /// Notificação interna disparada por RedactedArea ao concluir a revelação de uma evidência.
        /// </summary>
        internal static void NotifyEvidenceRevealed(RedactedArea area, string revealedText)
        {
            OnEvidenceRevealed?.Invoke(area, revealedText);
        }

        private void SetEquipped(bool equipped)
        {
            bool effectiveState = equipped && inkDrops > 0;
            if (_isEquipped == effectiveState)
            {
                return;
            }

            _isEquipped = effectiveState;

            if (cursorToolVisual != null)
            {
                cursorToolVisual.gameObject.SetActive(_isEquipped);
            }

            Debug.Log(_isEquipped
                ? $"[RevealingInkController] Tinta Reveladora equipada no cursor ({inkDrops}/{MaxInkDrops} gotas)."
                : "[RevealingInkController] Tinta Reveladora devolvida ao tinteiro.");

            OnRevealingInkEquippedChanged?.Invoke(_isEquipped);
        }

        private void RefreshInkwellVisuals()
        {
            if (inkwellLiquidRenderer != null)
            {
                float fillRatio = (float)inkDrops / MaxInkDrops;
                inkwellLiquidRenderer.color = inkDrops > 0
                    ? Color.Lerp(ColorInkEmpty, ColorInkLiquid, Mathf.Lerp(0.45f, 1f, fillRatio))
                    : ColorInkEmpty;
            }

            if (dropsCounterText != null)
            {
                dropsCounterText.text = $"TINTA\n{inkDrops}/{MaxInkDrops}";
            }
        }

        private void BuildInkwellPlaceholders()
        {
            Sprite solidSprite = CreateSolidSprite();
            string propLayer = ResolveSortingLayerOrDefault(InkwellSortingLayerName);

            _inkwellCollider.size = new Vector2(0.9f, 1.0f);

            // Corpo do tinteiro
            GameObject bottleObj = new GameObject("Inkwell_Bottle");
            bottleObj.transform.SetParent(transform, false);
            bottleObj.transform.localScale = new Vector3(0.85f, 0.9f, 1f);
            SpriteRenderer bottleSr = bottleObj.AddComponent<SpriteRenderer>();
            bottleSr.sprite = solidSprite;
            bottleSr.color = ColorInkBottle;
            bottleSr.sortingLayerName = propLayer;
            bottleSr.sortingOrder = 1;

            // Líquido violeta visível no interior
            GameObject liquidObj = new GameObject("Inkwell_Liquid");
            liquidObj.transform.SetParent(transform, false);
            liquidObj.transform.localPosition = new Vector3(0f, -0.05f, -0.01f);
            liquidObj.transform.localScale = new Vector3(0.62f, 0.55f, 1f);
            inkwellLiquidRenderer = liquidObj.AddComponent<SpriteRenderer>();
            inkwellLiquidRenderer.sprite = solidSprite;
            inkwellLiquidRenderer.color = ColorInkViolet;
            inkwellLiquidRenderer.sortingLayerName = propLayer;
            inkwellLiquidRenderer.sortingOrder = 2;

            // Contador de gotas (TextMeshPro)
            GameObject labelObj = new GameObject("Inkwell_Counter");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = new Vector3(0f, -0.72f, -0.02f);

            dropsCounterText = labelObj.AddComponent<TextMeshPro>();
            dropsCounterText.rectTransform.sizeDelta = new Vector2(1.4f, 0.5f);
            dropsCounterText.fontSize = 1.8f;
            dropsCounterText.alignment = TextAlignmentOptions.Center;
            dropsCounterText.color = ColorInkViolet;
            if (dropsCounterText.font == null && TMP_Settings.defaultFontAsset != null)
            {
                dropsCounterText.font = TMP_Settings.defaultFontAsset;
            }

            MeshRenderer textRenderer = dropsCounterText.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingLayerName = propLayer;
                textRenderer.sortingOrder = 3;
            }
        }

        private void BuildCursorToolIndicator()
        {
            Sprite solidSprite = CreateSolidSprite();
            string vfxLayer = ResolveSortingLayerOrDefault(ActiveToolSortingLayerName);

            GameObject cursorObj = new GameObject("RevealingInk_CursorDropper");
            cursorObj.transform.SetParent(transform, false);

            // Ponta do conta-gotas iluminada em violeta (#BB8FCE)
            GameObject tipObj = new GameObject("Dropper_Tip");
            tipObj.transform.SetParent(cursorObj.transform, false);
            tipObj.transform.localPosition = new Vector3(0.18f, 0.18f, 0f);
            tipObj.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            tipObj.transform.localScale = new Vector3(0.16f, 0.45f, 1f);

            SpriteRenderer tipSr = tipObj.AddComponent<SpriteRenderer>();
            tipSr.sprite = solidSprite;
            tipSr.color = ColorInkViolet;
            tipSr.sortingLayerName = vfxLayer;
            tipSr.sortingOrder = 50;

            cursorToolVisual = cursorObj.transform;
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

        private static bool TryReadInput(out Vector2 screenPosition, out bool leftDown, out bool escapeDown)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                screenPosition = Vector2.zero;
                leftDown = false;
                escapeDown = false;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            leftDown = mouse.leftButton.wasPressedThisFrame;
            escapeDown = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            return true;
#else
            screenPosition = Input.mousePosition;
            leftDown = Input.GetMouseButtonDown(0);
            escapeDown = Input.GetKeyDown(KeyCode.Escape);
            return true;
#endif
        }

        private static string ResolveSortingLayerOrDefault(string layerName)
        {
            return SortingLayer.NameToID(layerName) != 0 ? layerName : "Default";
        }
    }
}
