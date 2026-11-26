using System;
using System.Collections.Generic;
using TMPro;
using TribunalDasAlmas.Core;
using UnityEngine;

namespace TribunalDasAlmas.Mechanics
{
    /// <summary>
    /// Exibe e controla o relógio analógico do turno na Zona A (DD-012).
    /// - Caso 1 (isOnboardingNoTimer == true): permanece estático/pausado em 08:00.
    /// - A partir do Caso 2: avança continuamente e monitora o tempo da alma na bancada.
    ///   Após 90 segundos sem despacho, ativa a animação de "Inquietação/Ansiedade"
    ///   (trepidação visual leve) na silhueta da alma na Zona A.
    /// </summary>
    public class ShiftClockController : MonoBehaviour
    {
        public const string PropsSortingLayerName = "20_DeskProps_Fixed";
        public const string BoothSortingLayerName = "00_Background_Booth";

        private static readonly Color ColorClockRim = new Color32(0x2E, 0x23, 0x1E, 0xFF);      // #2E231E Mogno
        private static readonly Color ColorClockFace = new Color32(0xDF, 0xD5, 0xC0, 0xFF);     // #DFD5C0 Creme
        private static readonly Color ColorClockHands = new Color32(0x1C, 0x20, 0x26, 0xFF);    // #1C2026 Ponteiros
        private static readonly Color ColorSoulSilhouette = new Color32(0x1C, 0x20, 0x26, 0xD8);

        [Header("Referências")]
        [SerializeField] private VerticalSliceGameManager gameManager;
        [SerializeField] private Transform hourHandPivot;
        [SerializeField] private Transform minuteHandPivot;
        [SerializeField] private TextMeshPro digitalReadoutText;

        [Header("Silhueta da Alma (Zona A — Ansiedade > 90s)")]
        [SerializeField] private Transform soulSilhouetteTransform;
        [SerializeField] private float anxietyThresholdSeconds = 90f;
        [SerializeField] private float anxietyTremorAmplitude = 0.045f;
        [SerializeField] private float anxietyTremorFrequency = 22f;
        [SerializeField] private float anxietyRotationAmplitude = 1.8f;

        [Header("Configuração do Tempo de Turno")]
        [SerializeField] private float startHour = 8f;
        [SerializeField] private float inGameMinutesPerRealSecond = 1.0f;

        [Header("Placeholders Visuais")]
        [SerializeField] private bool autoCreateVisualPlaceholders = true;

        private readonly List<UnityEngine.Object> _generatedResources = new List<UnityEngine.Object>();

        private float _shiftElapsedMinutes;
        private float _caseElapsedRealSeconds;
        private bool _isTimerPaused = true;
        private bool _isSoulAnxious;

        private Vector3 _silhouetteBaseLocalPos;
        private Quaternion _silhouetteBaseLocalRot = Quaternion.identity;
        private bool _hasCachedSilhouettePose;

        public bool IsTimerPaused => _isTimerPaused;
        public bool IsSoulAnxious => _isSoulAnxious;
        public float CaseElapsedRealSeconds => _caseElapsedRealSeconds;
        public float CurrentShiftHour => startHour + (_shiftElapsedMinutes / 60f);

        public event Action<bool> OnSoulAnxietyChanged;

        private void Awake()
        {
            if (autoCreateVisualPlaceholders && (hourHandPivot == null || minuteHandPivot == null))
            {
                BuildPlaceholderClock();
            }

            if (autoCreateVisualPlaceholders && soulSilhouetteTransform == null)
            {
                BuildPlaceholderSoulSilhouette();
            }

            CacheSilhouetteBasePose();
            ApplyClockHandAngles(startHour, 0f);
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (gameManager != null)
            {
                gameManager.OnCaseLoaded += HandleCaseLoaded;
                gameManager.OnStateChanged += HandleDeskStateChanged;
                gameManager.OnAllCasesCompleted += HandleAllCasesCompleted;
            }
        }

        private void Start()
        {
            ResolveDependencies();

            // Sincroniza caso o primeiro caso já tenha sido carregado antes do Start deste componente.
            if (gameManager != null && gameManager.CurrentCase != null)
            {
                ConfigureForCase(gameManager.CurrentCase);
            }
        }

        private void OnDisable()
        {
            RestoreSilhouettePose();

            if (gameManager != null)
            {
                gameManager.OnCaseLoaded -= HandleCaseLoaded;
                gameManager.OnStateChanged -= HandleDeskStateChanged;
                gameManager.OnAllCasesCompleted -= HandleAllCasesCompleted;
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
            bool isInspecting = gameManager == null || gameManager.CurrentState == DeskState.InspectingCase;

            if (!_isTimerPaused && isInspecting)
            {
                _shiftElapsedMinutes += Time.deltaTime * inGameMinutesPerRealSecond;
                _caseElapsedRealSeconds += Time.deltaTime;

                float totalMinutes = _shiftElapsedMinutes;
                float currentHours = startHour + (totalMinutes / 60f);
                float currentMinutes = totalMinutes % 60f;
                ApplyClockHandAngles(currentHours, currentMinutes);

                if (!_isSoulAnxious && _caseElapsedRealSeconds > anxietyThresholdSeconds)
                {
                    SetSoulAnxietyState(true);
                }
            }

            if (_isSoulAnxious && isInspecting)
            {
                UpdateSoulAnxietyTremor();
            }
        }

        private void HandleCaseLoaded(SoulCaseData caseData, int caseIndex)
        {
            ConfigureForCase(caseData);
        }

        private void HandleDeskStateChanged(DeskState newState)
        {
            if (newState != DeskState.InspectingCase && _isSoulAnxious)
            {
                RestoreSilhouettePose();
            }
        }

        private void HandleAllCasesCompleted(int correctCount, int totalCount)
        {
            _isTimerPaused = true;
            SetSoulAnxietyState(false);

            if (soulSilhouetteTransform != null)
            {
                soulSilhouetteTransform.gameObject.SetActive(false);
            }
        }

        private void ConfigureForCase(SoulCaseData caseData)
        {
            _caseElapsedRealSeconds = 0f;
            SetSoulAnxietyState(false);

            if (soulSilhouetteTransform != null)
            {
                soulSilhouetteTransform.gameObject.SetActive(caseData != null);
            }

            if (caseData == null)
            {
                _isTimerPaused = true;
                return;
            }

            if (caseData.isOnboardingNoTimer)
            {
                // DD-012: Caso 1 (Onboarding) mantém o relógio estático em 08:00.
                _isTimerPaused = true;
                _shiftElapsedMinutes = 0f;
                ApplyClockHandAngles(startHour, 0f);
                Debug.Log("[ShiftClockController] Onboarding ativo (DD-012): relógio pausado em 08:00.");
            }
            else
            {
                _isTimerPaused = false;
                Debug.Log($"[ShiftClockController] Relógio de turno em andamento para '{caseData.soulName}'.");
            }
        }

        private void SetSoulAnxietyState(bool anxious)
        {
            if (_isSoulAnxious == anxious)
            {
                return;
            }

            _isSoulAnxious = anxious;

            if (!_isSoulAnxious)
            {
                RestoreSilhouettePose();
            }
            else
            {
                Debug.LogWarning(
                    $"[ShiftClockController] Alma aguardando há mais de {anxietyThresholdSeconds:0}s sem despacho. " +
                    "Estado alterado para 'Inquietação/Ansiedade'.");
            }

            OnSoulAnxietyChanged?.Invoke(_isSoulAnxious);
        }

        private void UpdateSoulAnxietyTremor()
        {
            if (soulSilhouetteTransform == null || !_hasCachedSilhouettePose)
            {
                return;
            }

            float t = Time.time * anxietyTremorFrequency;
            float offsetX = (Mathf.PerlinNoise(t, 0.37f) - 0.5f) * 2f * anxietyTremorAmplitude;
            float offsetY = (Mathf.PerlinNoise(0.73f, t * 0.85f) - 0.5f) * 2f * (anxietyTremorAmplitude * 0.6f);
            float rotZ = Mathf.Sin(t * 0.9f) * anxietyRotationAmplitude;

            soulSilhouetteTransform.localPosition = _silhouetteBaseLocalPos + new Vector3(offsetX, offsetY, 0f);
            soulSilhouetteTransform.localRotation = _silhouetteBaseLocalRot * Quaternion.Euler(0f, 0f, rotZ);
        }

        private void ApplyClockHandAngles(float hours24, float minutes)
        {
            // Em 2D (eixo Z), rotação negativa gira no sentido horário (12h = 0°, 03h = -90°, 08h = -240°).
            float minuteAngle = -(minutes / 60f) * 360f;
            float hourAngle = -((hours24 % 12f) / 12f) * 360f;

            if (minuteHandPivot != null)
            {
                minuteHandPivot.localRotation = Quaternion.Euler(0f, 0f, minuteAngle);
            }

            if (hourHandPivot != null)
            {
                hourHandPivot.localRotation = Quaternion.Euler(0f, 0f, hourAngle);
            }

            if (digitalReadoutText != null)
            {
                int displayHour = Mathf.FloorToInt(hours24) % 24;
                int displayMin = Mathf.FloorToInt(minutes) % 60;
                digitalReadoutText.text = $"{displayHour:00}:{displayMin:00}";
            }
        }

        private void CacheSilhouetteBasePose()
        {
            if (soulSilhouetteTransform == null)
            {
                return;
            }

            _silhouetteBaseLocalPos = soulSilhouetteTransform.localPosition;
            _silhouetteBaseLocalRot = soulSilhouetteTransform.localRotation;
            _hasCachedSilhouettePose = true;
        }

        private void RestoreSilhouettePose()
        {
            if (soulSilhouetteTransform == null || !_hasCachedSilhouettePose)
            {
                return;
            }

            soulSilhouetteTransform.localPosition = _silhouetteBaseLocalPos;
            soulSilhouetteTransform.localRotation = _silhouetteBaseLocalRot;
        }

        private void BuildPlaceholderClock()
        {
            Sprite circleSprite = CreateCircleSprite(64);
            Sprite solidSprite = CreateSolidSprite();
            string propLayer = ResolveSortingLayerOrDefault(PropsSortingLayerName);

            // Aro externo (#2E231E) e mostrador (#DFD5C0)
            CreateSpriteObject("Clock_Rim", transform, Vector3.zero, new Vector3(1.55f, 1.55f, 1f), circleSprite, ColorClockRim, propLayer, 1);
            CreateSpriteObject("Clock_Face", transform, new Vector3(0f, 0f, -0.005f), new Vector3(1.36f, 1.36f, 1f), circleSprite, ColorClockFace, propLayer, 2);

            // Marcadores de 12h, 3h, 6h, 9h
            for (int i = 0; i < 12; i++)
            {
                float rad = i * 30f * Mathf.Deg2Rad;
                Vector3 tickPos = new Vector3(Mathf.Sin(rad) * 0.54f, Mathf.Cos(rad) * 0.54f, -0.008f);
                bool isQuarter = (i % 3 == 0);
                Vector3 tickScale = isQuarter ? new Vector3(0.05f, 0.14f, 1f) : new Vector3(0.03f, 0.08f, 1f);
                GameObject tick = CreateSpriteObject($"Tick_{i}", transform, tickPos, tickScale, solidSprite, ColorClockHands, propLayer, 3);
                tick.transform.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            }

            // Pivô do ponteiro das horas
            GameObject hourPivotObj = new GameObject("Clock_HourHandPivot");
            hourPivotObj.transform.SetParent(transform, false);
            hourPivotObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            hourHandPivot = hourPivotObj.transform;
            CreateSpriteObject("HourHand_Visual", hourHandPivot, new Vector3(0f, 0.19f, 0f), new Vector3(0.065f, 0.38f, 1f), solidSprite, ColorClockHands, propLayer, 4);

            // Pivô do ponteiro dos minutos
            GameObject minutePivotObj = new GameObject("Clock_MinuteHandPivot");
            minutePivotObj.transform.SetParent(transform, false);
            minutePivotObj.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            minuteHandPivot = minutePivotObj.transform;
            CreateSpriteObject("MinuteHand_Visual", minuteHandPivot, new Vector3(0f, 0.27f, 0f), new Vector3(0.045f, 0.54f, 1f), solidSprite, ColorClockHands, propLayer, 5);

            // Pino central
            CreateSpriteObject("Clock_CenterPin", transform, new Vector3(0f, 0f, -0.02f), new Vector3(0.11f, 0.11f, 1f), circleSprite, ColorClockRim, propLayer, 6);

            // Pequeno visor de apoio abaixo do relógio
            GameObject textObj = new GameObject("Clock_DigitalReadout");
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = new Vector3(0f, -0.98f, -0.02f);

            digitalReadoutText = textObj.AddComponent<TextMeshPro>();
            digitalReadoutText.rectTransform.sizeDelta = new Vector2(1.6f, 0.4f);
            digitalReadoutText.fontSize = 1.9f;
            digitalReadoutText.fontStyle = FontStyles.Bold;
            digitalReadoutText.alignment = TextAlignmentOptions.Center;
            digitalReadoutText.color = ColorClockFace;
            if (digitalReadoutText.font == null && TMP_Settings.defaultFontAsset != null)
            {
                digitalReadoutText.font = TMP_Settings.defaultFontAsset;
            }

            MeshRenderer mr = digitalReadoutText.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = propLayer;
                mr.sortingOrder = 6;
            }
        }

        private void BuildPlaceholderSoulSilhouette()
        {
            Sprite circleSprite = CreateCircleSprite(48);
            Sprite solidSprite = CreateSolidSprite();
            string boothLayer = ResolveSortingLayerOrDefault(BoothSortingLayerName);

            GameObject silhouetteObj = new GameObject("ZoneA_SoulSilhouette");
            silhouetteObj.transform.SetParent(transform, false);
            silhouetteObj.transform.localPosition = new Vector3(-2.6f, -0.25f, 0.1f);
            soulSilhouetteTransform = silhouetteObj.transform;

            // Cabeça e ombros estilizados (silhueta na janela da Zona A)
            CreateSpriteObject("Silhouette_Head", soulSilhouetteTransform, new Vector3(0f, 0.55f, 0f), new Vector3(0.72f, 0.78f, 1f), circleSprite, ColorSoulSilhouette, boothLayer, 5);
            CreateSpriteObject("Silhouette_Torso", soulSilhouetteTransform, new Vector3(0f, -0.35f, 0f), new Vector3(1.35f, 1.15f, 1f), solidSprite, ColorSoulSilhouette, boothLayer, 5);
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

        private Sprite CreateCircleSprite(int diameter)
        {
            Texture2D tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = diameter * 0.5f;
            float sqrRadius = radius * radius;
            Color32[] pixels = new Color32[diameter * diameter];

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = (x + 0.5f) - radius;
                    float dy = (y + 0.5f) - radius;
                    float distSqr = dx * dx + dy * dy;
                    byte alpha = distSqr <= sqrRadius ? (byte)255 : (byte)0;
                    pixels[y * diameter + x] = new Color32(255, 255, 255, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), diameter);
            _generatedResources.Add(tex);
            _generatedResources.Add(sprite);
            return sprite;
        }

        private static GameObject CreateSpriteObject(
            string objectName,
            Transform parent,
            Vector3 localPos,
            Vector3 localScale,
            Sprite sprite,
            Color color,
            string sortingLayer,
            int sortingOrder)
        {
            GameObject obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            return obj;
        }

        private void ResolveDependencies()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<VerticalSliceGameManager>();
            }
        }

        private static string ResolveSortingLayerOrDefault(string layerName)
        {
            return SortingLayer.NameToID(layerName) != 0 ? layerName : "Default";
        }
    }
}
