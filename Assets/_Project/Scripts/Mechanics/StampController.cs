using System;
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
    /// Controla os 3 carimbos oficiais na Zona D (Repouso, Reencarnação e Expurgo).
    /// Permite arrastar ou clicar em um carimbo e aplicá-lo sobre a Certidão de Passamento na Zona C,
    /// renderizando o selo correspondente no papel e registrando hasStampApplied = true no VerticalSliceGameManager.
    /// </summary>
    public class StampController : MonoBehaviour
    {
        public const string PropsSortingLayerName = "20_DeskProps_Fixed";
        public const string ActiveToolSortingLayerName = "40_ActiveTool_VFX";

        // Paleta oficial dos 3 carimbos
        public static readonly Color ColorRepouso = new Color32(0xD4, 0xAF, 0x37, 0xFF);       // #D4AF37
        public static readonly Color ColorReencarnacao = new Color32(0x2B, 0x5B, 0x84, 0xFF);  // #2B5B84
        public static readonly Color ColorExpurgo = new Color32(0x9E, 0x2A, 0x2B, 0xFF);       // #9E2A2B

        private static readonly Color ColorHandleWood = new Color32(0x2E, 0x23, 0x1E, 0xFF);   // #2E231E
        private static readonly Color ColorLabelLight = new Color32(0xDF, 0xD5, 0xC0, 0xFF);   // #DFD5C0

        [Serializable]
        public class StampSlot
        {
            public VerdictType verdict;
            public string displayLabel;
            public Color stampColor;
            public BoxCollider2D collider;
            public Transform visualTransform;
            [HideInInspector] public Vector3 restingWorldPosition;
        }

        [Header("Referências")]
        [SerializeField] private VerticalSliceGameManager gameManager;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private TMP_FontAsset customFontAsset;

        [Header("Carimbos (Zona D — Auto-criados se vazios)")]
        [SerializeField] private List<StampSlot> stampSlots = new List<StampSlot>();
        [SerializeField] private bool autoCreateVisualPlaceholders = true;
        [SerializeField] private float placeholderSpacingY = 1.15f;

        [Header("Interação")]
        [SerializeField] private float dragThresholdPixels = 12f;

        private readonly List<UnityEngine.Object> _generatedResources = new List<UnityEngine.Object>();

        private static StampController s_activeInstance;
        private static int s_lastStampAppliedFrame = -1;

        private StampSlot _selectedStamp;
        private bool _isHoldingSelectedStamp;
        private bool _hasDraggedBeyondThreshold;
        private Vector2 _pressScreenPos;

        public static bool IsAnyStampEquipped => s_activeInstance != null && s_activeInstance._selectedStamp != null;
        public static bool WasStampAppliedThisFrame => s_lastStampAppliedFrame == Time.frameCount;

        public event Action<VerdictType, DraggableDocument> OnStampAppliedToCertificate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_activeInstance = null;
            s_lastStampAppliedFrame = -1;
        }

        private void Awake()
        {
            s_activeInstance = this;

            if (autoCreateVisualPlaceholders && (stampSlots == null || stampSlots.Count == 0))
            {
                BuildPlaceholderStamps();
            }

            CacheRestingPositions();
        }

        private void OnEnable()
        {
            s_activeInstance = this;
            ResolveDependencies();

            if (gameManager != null)
            {
                gameManager.OnCaseLoaded += HandleCaseLoaded;
            }
        }

        private void OnDisable()
        {
            ReturnSelectedStampToRest();

            if (gameManager != null)
            {
                gameManager.OnCaseLoaded -= HandleCaseLoaded;
            }

            if (s_activeInstance == this)
            {
                s_activeInstance = null;
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
            ResolveDependencies();
            if (targetCamera == null)
            {
                return;
            }

            if (!TryReadInput(
                out Vector2 mouseScreenPos,
                out bool leftDown,
                out bool leftHeld,
                out bool leftUp,
                out bool cancelDown))
            {
                return;
            }

            Vector3 mouseWorldPos = ScreenToWorldPoint(mouseScreenPos);
            Vector2 mouseWorldPoint2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            if (cancelDown && _selectedStamp != null)
            {
                ReturnSelectedStampToRest();
                return;
            }

            // Acompanha o cursor enquanto o carimbo está equipado ou sendo arrastado
            if (_selectedStamp != null && _selectedStamp.visualTransform != null)
            {
                _selectedStamp.visualTransform.position = new Vector3(
                    mouseWorldPos.x,
                    mouseWorldPos.y,
                    transform.position.z - 0.15f);
            }

            // Modo arraste contínuo (Mouse0 mantido pressionado)
            if (_isHoldingSelectedStamp)
            {
                if (leftHeld && Vector2.Distance(mouseScreenPos, _pressScreenPos) >= dragThresholdPixels)
                {
                    _hasDraggedBeyondThreshold = true;
                }

                if (leftUp || !leftHeld)
                {
                    _isHoldingSelectedStamp = false;

                    if (_hasDraggedBeyondThreshold)
                    {
                        // Fluxo arrastar-e-soltar: tenta carimbar onde soltou e devolve o carimbo à base
                        TryApplyStampAtPoint(_selectedStamp, mouseWorldPoint2D);
                        ReturnSelectedStampToRest();
                    }
                    // Se foi apenas um clique curto, mantém equipado no cursor até o próximo clique sobre a Certidão.
                }

                return;
            }

            if (!leftDown)
            {
                return;
            }

            // 1. Verifica se o jogador clicou em um dos 3 carimbos da Zona D
            StampSlot clickedStamp = FindStampSlotAtPoint(mouseWorldPoint2D);
            if (clickedStamp != null)
            {
                if (_selectedStamp == clickedStamp)
                {
                    // Clicar no mesmo carimbo novamente devolve à base (toggle)
                    ReturnSelectedStampToRest();
                    return;
                }

                SelectStamp(clickedStamp, mouseScreenPos);
                return;
            }

            // 2. Se já há um carimbo equipado (modo clique), tenta aplicar na Certidão de Passamento na Zona C
            if (_selectedStamp != null)
            {
                if (TryApplyStampAtPoint(_selectedStamp, mouseWorldPoint2D))
                {
                    ReturnSelectedStampToRest();
                }
                else
                {
                    // Clicar fora da Certidão devolve o carimbo para a base
                    ReturnSelectedStampToRest();
                }
            }
        }

        private void SelectStamp(StampSlot slot, Vector2 pressScreenPos)
        {
            ReturnSelectedStampToRest();

            // Evita conflito de ferramentas equipadas simultaneamente
            if (RevealingInkController.ActiveInstance != null)
            {
                RevealingInkController.ActiveInstance.UnequipTool();
            }

            _selectedStamp = slot;
            _isHoldingSelectedStamp = true;
            _hasDraggedBeyondThreshold = false;
            _pressScreenPos = pressScreenPos;

            SetStampSortingLayer(slot, ActiveToolSortingLayerName, 40);
        }

        private void ReturnSelectedStampToRest()
        {
            if (_selectedStamp == null)
            {
                return;
            }

            if (_selectedStamp.visualTransform != null)
            {
                _selectedStamp.visualTransform.position = _selectedStamp.restingWorldPosition;
            }

            SetStampSortingLayer(_selectedStamp, PropsSortingLayerName, 2);

            _selectedStamp = null;
            _isHoldingSelectedStamp = false;
            _hasDraggedBeyondThreshold = false;
        }

        private bool TryApplyStampAtPoint(StampSlot slot, Vector2 worldPoint)
        {
            if (slot == null || gameManager == null)
            {
                return false;
            }

            if (gameManager.CurrentState != DeskState.InspectingCase)
            {
                return false;
            }

            if (!IsPointInsideZoneC(worldPoint))
            {
                return false;
            }

            DraggableDocument certificate = FindCertificateAtPoint(worldPoint);
            if (certificate == null)
            {
                return false;
            }

            Vector3 localPos3D = certificate.transform.InverseTransformPoint(new Vector3(worldPoint.x, worldPoint.y, certificate.transform.position.z));
            TMP_FontAsset font = customFontAsset != null ? customFontAsset : gameManager.CustomFontAsset;

            certificate.ApplyStampSeal(
                slot.verdict,
                slot.stampColor,
                slot.displayLabel,
                new Vector2(localPos3D.x, localPos3D.y),
                font);

            certificate.BringToFront();
            s_lastStampAppliedFrame = Time.frameCount;

            gameManager.RegisterAppliedStamp(slot.verdict);
            OnStampAppliedToCertificate?.Invoke(slot.verdict, certificate);
            return true;
        }

        private bool IsPointInsideZoneC(Vector2 worldPoint)
        {
            Vector2 min = gameManager != null ? gameManager.DeskBoundsMin : new Vector2(-4.5f, -2.5f);
            Vector2 max = gameManager != null ? gameManager.DeskBoundsMax : new Vector2(4.5f, 0.5f);

            return worldPoint.x >= min.x && worldPoint.x <= max.x &&
                   worldPoint.y >= min.y && worldPoint.y <= max.y;
        }

        private DraggableDocument FindCertificateAtPoint(Vector2 worldPoint)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
            DraggableDocument bestCert = null;
            int highestOrder = int.MinValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null || !hits[i].TryGetComponent(out DraggableDocument doc))
                {
                    continue;
                }

                if (!doc.IsDeathCertificate || !doc.isActiveAndEnabled)
                {
                    continue;
                }

                if (doc.CurrentSortingOrder > highestOrder)
                {
                    highestOrder = doc.CurrentSortingOrder;
                    bestCert = doc;
                }
            }

            return bestCert;
        }

        private StampSlot FindStampSlotAtPoint(Vector2 worldPoint)
        {
            for (int i = 0; i < stampSlots.Count; i++)
            {
                StampSlot slot = stampSlots[i];
                if (slot != null && slot.collider != null && slot.collider.OverlapPoint(worldPoint))
                {
                    return slot;
                }
            }

            return null;
        }

        private void HandleCaseLoaded(SoulCaseData caseData, int caseIndex)
        {
            ReturnSelectedStampToRest();
        }

        private void CacheRestingPositions()
        {
            for (int i = 0; i < stampSlots.Count; i++)
            {
                StampSlot slot = stampSlots[i];
                if (slot != null && slot.visualTransform != null)
                {
                    slot.restingWorldPosition = slot.visualTransform.position;
                }
            }
        }

        private void BuildPlaceholderStamps()
        {
            stampSlots = new List<StampSlot>(3);
            Sprite solidSprite = CreateSolidSprite();
            string propLayer = ResolveSortingLayerOrDefault(PropsSortingLayerName);

            (VerdictType verdict, string label, Color color)[] definitions =
            {
                (VerdictType.Repouso, "REPOUSO", ColorRepouso),
                (VerdictType.Reencarnacao, "REENCARNAÇÃO", ColorReencarnacao),
                (VerdictType.Expurgo, "EXPURGO", ColorExpurgo)
            };

            for (int i = 0; i < definitions.Length; i++)
            {
                var def = definitions[i];
                float offsetY = (1 - i) * placeholderSpacingY;

                GameObject stampObj = new GameObject($"Stamp_{def.verdict}");
                stampObj.transform.SetParent(transform, false);
                stampObj.transform.localPosition = new Vector3(0f, offsetY, 0f);

                BoxCollider2D col = stampObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.65f, 0.85f);

                // Base colorida do carimbo
                GameObject baseObj = new GameObject("Stamp_Pad");
                baseObj.transform.SetParent(stampObj.transform, false);
                baseObj.transform.localPosition = new Vector3(0f, -0.14f, 0f);
                baseObj.transform.localScale = new Vector3(1.6f, 0.52f, 1f);
                SpriteRenderer baseSr = baseObj.AddComponent<SpriteRenderer>();
                baseSr.sprite = solidSprite;
                baseSr.color = def.color;
                baseSr.sortingLayerName = propLayer;
                baseSr.sortingOrder = 2;

                // Cabo de madeira mogno (#2E231E)
                GameObject handleObj = new GameObject("Stamp_Handle");
                handleObj.transform.SetParent(stampObj.transform, false);
                handleObj.transform.localPosition = new Vector3(0f, 0.24f, 0f);
                handleObj.transform.localScale = new Vector3(0.55f, 0.34f, 1f);
                SpriteRenderer handleSr = handleObj.AddComponent<SpriteRenderer>();
                handleSr.sprite = solidSprite;
                handleSr.color = ColorHandleWood;
                handleSr.sortingLayerName = propLayer;
                handleSr.sortingOrder = 3;

                // Rótulo de identificação
                GameObject labelObj = new GameObject("Stamp_Label");
                labelObj.transform.SetParent(stampObj.transform, false);
                labelObj.transform.localPosition = new Vector3(0f, -0.14f, -0.01f);

                TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
                tmp.rectTransform.sizeDelta = new Vector2(1.55f, 0.45f);
                tmp.text = def.label;
                tmp.fontSize = 1.6f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.color = ColorLabelLight;

                if (customFontAsset != null)
                {
                    tmp.font = customFontAsset;
                }
                else if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
                {
                    tmp.font = TMP_Settings.defaultFontAsset;
                }

                MeshRenderer mr = tmp.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sortingLayerName = propLayer;
                    mr.sortingOrder = 4;
                }

                stampSlots.Add(new StampSlot
                {
                    verdict = def.verdict,
                    displayLabel = def.label,
                    stampColor = def.color,
                    collider = col,
                    visualTransform = stampObj.transform,
                    restingWorldPosition = stampObj.transform.position
                });
            }
        }

        private static void SetStampSortingLayer(StampSlot slot, string layerName, int baseOrder)
        {
            if (slot?.visualTransform == null)
            {
                return;
            }

            string resolvedLayer = ResolveSortingLayerOrDefault(layerName);

            SpriteRenderer[] sprites = slot.visualTransform.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].sortingLayerName = resolvedLayer;
                sprites[i].sortingOrder = baseOrder + i;
            }

            MeshRenderer[] meshes = slot.visualTransform.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i].sortingLayerName = resolvedLayer;
                meshes[i].sortingOrder = baseOrder + sprites.Length + i;
            }
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

        private void ResolveDependencies()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<VerticalSliceGameManager>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }
        }

        private Vector3 ScreenToWorldPoint(Vector2 screenPosition)
        {
            float depth = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
            worldPoint.z = transform.position.z;
            return worldPoint;
        }

        private static bool TryReadInput(
            out Vector2 screenPosition,
            out bool leftDown,
            out bool leftHeld,
            out bool leftUp,
            out bool cancelDown)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                screenPosition = Vector2.zero;
                leftDown = false;
                leftHeld = false;
                leftUp = false;
                cancelDown = false;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            leftDown = mouse.leftButton.wasPressedThisFrame;
            leftHeld = mouse.leftButton.isPressed;
            leftUp = mouse.leftButton.wasReleasedThisFrame;
            cancelDown = mouse.rightButton.wasPressedThisFrame ||
                         (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
            return true;
#else
            screenPosition = Input.mousePosition;
            leftDown = Input.GetMouseButtonDown(0);
            leftHeld = Input.GetMouseButton(0);
            leftUp = Input.GetMouseButtonUp(0);
            cancelDown = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
            return true;
#endif
        }

        private static string ResolveSortingLayerOrDefault(string layerName)
        {
            return SortingLayer.NameToID(layerName) != 0 ? layerName : "Default";
        }
    }
}
