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
    /// Controla a Alavanca Pneumática de despacho (Zona D).
    /// Ao ser clicada e puxada, valida se hasStampApplied == true:
    /// - Se falso: trava na metade do curso com som/VFX metálico e pisca a Certidão de Passamento em vermelho suave 2x.
    /// - Se verdadeiro: executa a descida completa da alavanca, recolhe os papéis pela fresta inferior (Zona E)
    ///   e dispara a validação de conformidade no VerticalSliceGameManager.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PneumaticLeverController : MonoBehaviour
    {
        public const string PropsSortingLayerName = "20_DeskProps_Fixed";
        public const string VfxSortingLayerName = "40_ActiveTool_VFX";

        private static readonly Color ColorHousing = new Color32(0x2E, 0x23, 0x1E, 0xFF);      // #2E231E Mogno
        private static readonly Color ColorMetalRod = new Color32(0x34, 0x3C, 0x47, 0xFF);     // #343C47 Metálico
        private static readonly Color ColorHandleKnob = new Color32(0x9E, 0x2A, 0x2B, 0xFF);   // #9E2A2B Alça
        private static readonly Color ColorMetallicSpark = new Color32(0xDF, 0xD5, 0xC0, 0xFF);

        [Header("Referências")]
        [SerializeField] private VerticalSliceGameManager gameManager;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform leverHandleTransform;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip metallicJamClip;
        [SerializeField] private AudioClip pneumaticDispatchClip;
        [SerializeField] private ParticleSystem metallicJamSparks;

        [Header("Curso da Alavanca")]
        [SerializeField] private float topLocalY = 0.65f;
        [SerializeField] private float bottomLocalY = -0.65f;
        [SerializeField] private float pullTriggerThreshold = 0.45f;
        [SerializeField] private float jamTravelDuration = 0.14f;
        [SerializeField] private float fullPullDuration = 0.22f;
        [SerializeField] private float returnDuration = 0.28f;

        [Header("Placeholders Visuais")]
        [SerializeField] private bool autoCreateVisualPlaceholders = true;

        private readonly List<UnityEngine.Object> _generatedResources = new List<UnityEngine.Object>();

        private BoxCollider2D _leverCollider;
        private bool _isDraggingLever;
        private bool _isAnimating;
        private float _currentProgress; // 0 = topo, 1 = fundo
        private float _dragStartWorldY;
        private bool _hasMovedDuringDrag;

        private AudioClip _proceduralJamClip;
        private AudioClip _proceduralDispatchClip;

        public bool IsAnimating => _isAnimating;

        public event Action OnLeverJammedNoStamp;
        public event Action OnLeverDispatched;

        private void Awake()
        {
            _leverCollider = GetComponent<BoxCollider2D>();

            if (autoCreateVisualPlaceholders && leverHandleTransform == null)
            {
                BuildPlaceholderLever();
            }

            EnsureAudioAndVfx();
            SetLeverProgress(0f);
        }

        private void OnEnable()
        {
            ResolveDependencies();
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
            if (_isAnimating)
            {
                return;
            }

            ResolveDependencies();
            if (targetCamera == null)
            {
                return;
            }

            if (!TryReadMouseState(out Vector2 mouseScreenPos, out bool leftDown, out bool leftHeld, out bool leftUp))
            {
                return;
            }

            Vector3 mouseWorldPos = ScreenToWorldPoint(mouseScreenPos);
            Vector2 mouseWorldPoint2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            if (_isDraggingLever)
            {
                float travelDistance = Mathf.Max(0.2f, topLocalY - bottomLocalY);
                float deltaDown = _dragStartWorldY - mouseWorldPos.y;

                if (Mathf.Abs(deltaDown) > 0.05f)
                {
                    _hasMovedDuringDrag = true;
                }

                // Se não houver carimbo, trava fisicamente em 0.5 durante o arraste
                bool canDispatch = gameManager != null && gameManager.HasStampApplied;
                float maxAllowedProgress = canDispatch ? 1f : 0.5f;
                float draggedProgress = Mathf.Clamp(deltaDown / travelDistance, 0f, maxAllowedProgress);
                SetLeverProgress(draggedProgress);

                if (draggedProgress >= pullTriggerThreshold)
                {
                    _isDraggingLever = false;
                    TriggerLeverAction();
                    return;
                }

                if (leftUp || !leftHeld)
                {
                    _isDraggingLever = false;

                    // Clique simples direto na alavanca também aciona a tentativa de puxada
                    if (!_hasMovedDuringDrag)
                    {
                        TriggerLeverAction();
                    }
                    else
                    {
                        StartCoroutine(ReturnLeverToTopRoutine());
                    }
                }
            }
            else if (leftDown && _leverCollider != null && _leverCollider.OverlapPoint(mouseWorldPoint2D))
            {
                if (gameManager != null && gameManager.CurrentState == DeskState.InspectingCase)
                {
                    _isDraggingLever = true;
                    _hasMovedDuringDrag = false;
                    _dragStartWorldY = mouseWorldPos.y;
                }
            }
        }

        /// <summary>
        /// Aciona programaticamente ou via input a verificação da alavanca pneumática.
        /// </summary>
        public void TriggerLeverAction()
        {
            if (_isAnimating)
            {
                return;
            }

            ResolveDependencies();
            if (gameManager == null || gameManager.CurrentState != DeskState.InspectingCase)
            {
                return;
            }

            if (!gameManager.HasStampApplied)
            {
                StartCoroutine(SafetyJamRoutine());
            }
            else
            {
                StartCoroutine(DispatchSuccessRoutine());
            }
        }

        private IEnumerator SafetyJamRoutine()
        {
            _isAnimating = true;

            // Desce até metade do curso (0.5) e trava
            yield return AnimateProgressRoutine(_currentProgress, 0.5f, jamTravelDuration);

            // Som + VFX metálico de trava de segurança
            PlayClip(metallicJamClip != null ? metallicJamClip : _proceduralJamClip, 0.7f);
            if (metallicJamSparks != null)
            {
                metallicJamSparks.Clear(true);
                metallicJamSparks.Play(true);
            }

            // Certidão de Passamento pisca em vermelho suave 2 vezes
            gameManager.FlashCurrentCertificateWarning(2);

            Debug.LogWarning("[PneumaticLeverController] Trava de segurança acionada: aplique um carimbo na Certidão antes de despachar.");
            OnLeverJammedNoStamp?.Invoke();

            // Breve vibração metálica na trava
            if (leverHandleTransform != null)
            {
                Vector3 jamPos = leverHandleTransform.localPosition;
                float shakeElapsed = 0f;
                const float shakeDuration = 0.16f;
                while (shakeElapsed < shakeDuration)
                {
                    shakeElapsed += Time.deltaTime;
                    float jitterX = Mathf.Sin(shakeElapsed * 95f) * 0.045f;
                    leverHandleTransform.localPosition = new Vector3(jamPos.x + jitterX, jamPos.y, jamPos.z);
                    yield return null;
                }
                leverHandleTransform.localPosition = jamPos;
            }

            yield return AnimateProgressRoutine(_currentProgress, 0f, returnDuration);
            _isAnimating = false;
        }

        private IEnumerator DispatchSuccessRoutine()
        {
            _isAnimating = true;

            PlayClip(pneumaticDispatchClip != null ? pneumaticDispatchClip : _proceduralDispatchClip, 0.75f);

            // Descida completa da alavanca
            yield return AnimateProgressRoutine(_currentProgress, 1f, fullPullDuration);

            OnLeverDispatched?.Invoke();

            // Invoca a validação de conformidade e o recolhimento pela fresta inferior (Zona E) no GameManager
            gameManager.CompleteCurrentCase(gameManager.SelectedVerdict);

            yield return new WaitForSeconds(0.15f);
            yield return AnimateProgressRoutine(_currentProgress, 0f, returnDuration);

            _isAnimating = false;
        }

        private IEnumerator ReturnLeverToTopRoutine()
        {
            _isAnimating = true;
            yield return AnimateProgressRoutine(_currentProgress, 0f, returnDuration * 0.7f);
            _isAnimating = false;
        }

        private IEnumerator AnimateProgressRoutine(float fromProgress, float toProgress, float duration)
        {
            float safeDuration = Mathf.Max(0.02f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
                SetLeverProgress(Mathf.Lerp(fromProgress, toProgress, t));
                yield return null;
            }

            SetLeverProgress(toProgress);
        }

        private void SetLeverProgress(float progress)
        {
            _currentProgress = Mathf.Clamp01(progress);
            if (leverHandleTransform == null)
            {
                return;
            }

            Vector3 localPos = leverHandleTransform.localPosition;
            localPos.y = Mathf.Lerp(topLocalY, bottomLocalY, _currentProgress);
            leverHandleTransform.localPosition = localPos;
        }

        private void BuildPlaceholderLever()
        {
            Sprite solidSprite = CreateSolidSprite();
            string propLayer = ResolveSortingLayerOrDefault(PropsSortingLayerName);

            _leverCollider.size = new Vector2(1.1f, 1.9f);

            // Caixa / trilho da alavanca
            CreatePart("Lever_Housing", transform, Vector3.zero, new Vector3(0.75f, 1.85f, 1f), solidSprite, ColorHousing, propLayer, 1);
            CreatePart("Lever_Slot", transform, new Vector3(0f, 0f, -0.005f), new Vector3(0.16f, 1.45f, 1f), solidSprite, ColorMetalRod, propLayer, 2);

            // Manopla móvel da alavanca
            GameObject handleObj = new GameObject("Lever_Handle");
            handleObj.transform.SetParent(transform, false);
            handleObj.transform.localPosition = new Vector3(0f, topLocalY, -0.01f);
            leverHandleTransform = handleObj.transform;

            CreatePart("Handle_Knob", leverHandleTransform, Vector3.zero, new Vector3(0.95f, 0.34f, 1f), solidSprite, ColorHandleKnob, propLayer, 4);

            // Rótulo "DESPACHO"
            GameObject labelObj = new GameObject("Lever_Label");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = new Vector3(0f, -1.18f, -0.01f);

            TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(1.8f, 0.45f);
            tmp.text = "DESPACHO";
            tmp.fontSize = 1.6f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ColorMetallicSpark;
            if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            MeshRenderer mr = tmp.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = propLayer;
                mr.sortingOrder = 5;
            }
        }

        private void EnsureAudioAndVfx()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            if (metallicJamClip == null)
            {
                _proceduralJamClip = CreateProceduralMetallicClankClip();
                _generatedResources.Add(_proceduralJamClip);
            }

            if (pneumaticDispatchClip == null)
            {
                _proceduralDispatchClip = CreateProceduralPneumaticWhooshClip();
                _generatedResources.Add(_proceduralDispatchClip);
            }

            if (metallicJamSparks == null)
            {
                GameObject vfxObj = new GameObject("VFX_LeverJamSparks");
                vfxObj.transform.SetParent(transform, false);
                vfxObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);

                metallicJamSparks = vfxObj.AddComponent<ParticleSystem>();
                metallicJamSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ParticleSystem.MainModule main = metallicJamSparks.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 0.25f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.0f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
                main.startColor = ColorMetallicSpark;
                main.gravityModifier = 0.9f;

                ParticleSystem.EmissionModule emission = metallicJamSparks.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

                ParticleSystem.ShapeModule shape = metallicJamSparks.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.12f;

                ParticleSystemRenderer psr = metallicJamSparks.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    psr.sortingLayerName = ResolveSortingLayerOrDefault(VfxSortingLayerName);
                    psr.sortingOrder = 15;

                    Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Sprites/Default");
                    if (shader != null)
                    {
                        Material mat = new Material(shader);
                        mat.color = Color.white;
                        psr.sharedMaterial = mat;
                        _generatedResources.Add(mat);
                    }
                }
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        private static AudioClip CreateProceduralMetallicClankClip()
        {
            const int sampleRate = 22050;
            const int sampleCount = (int)(sampleRate * 0.18f);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 24f);
                float tone = Mathf.Sin(2f * Mathf.PI * 310f * t) * 0.5f +
                             Mathf.Sin(2f * Mathf.PI * 745f * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * 1490f * t) * 0.15f;
                samples[i] = tone * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("SFX_LeverMetallicJam", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateProceduralPneumaticWhooshClip()
        {
            const int sampleRate = 22050;
            const int sampleCount = (int)(sampleRate * 0.30f);
            float[] samples = new float[sampleCount];
            System.Random rng = new System.Random(7);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float env = Mathf.Sin(Mathf.PI * t) * (1f - t * 0.4f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                samples[i] = noise * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("SFX_PneumaticDispatch", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
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

        private static void CreatePart(
            string partName,
            Transform parent,
            Vector3 localPos,
            Vector3 localScale,
            Sprite sprite,
            Color color,
            string sortingLayer,
            int sortingOrder)
        {
            GameObject part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;

            SpriteRenderer sr = part.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
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

        private static bool TryReadMouseState(
            out Vector2 screenPosition,
            out bool leftDown,
            out bool leftHeld,
            out bool leftUp)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                screenPosition = Vector2.zero;
                leftDown = false;
                leftHeld = false;
                leftUp = false;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            leftDown = mouse.leftButton.wasPressedThisFrame;
            leftHeld = mouse.leftButton.isPressed;
            leftUp = mouse.leftButton.wasReleasedThisFrame;
            return true;
#else
            screenPosition = Input.mousePosition;
            leftDown = Input.GetMouseButtonDown(0);
            leftHeld = Input.GetMouseButton(0);
            leftUp = Input.GetMouseButtonUp(0);
            return true;
#endif
        }

        private static string ResolveSortingLayerOrDefault(string layerName)
        {
            return SortingLayer.NameToID(layerName) != 0 ? layerName : "Default";
        }
    }
}
