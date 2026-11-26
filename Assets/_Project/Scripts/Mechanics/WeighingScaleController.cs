using System;
using System.Collections;
using System.Collections.Generic;
using TribunalDasAlmas.Core;
using UnityEngine;

namespace TribunalDasAlmas.Mechanics
{
    /// <summary>
    /// Mecânica M2 — Balança do Pesar (DD-013).
    /// Anexada à estrutura fixa da balança (SortingLayer "20_DeskProps_Fixed", #2E231E / #343C47).
    /// Detecta objetos no Trigger2D do prato esquerdo: rejeita documentos comuns com impulso físico
    /// e pesa relíquias válidas de acordo com o karmicWeight da alma ativa.
    /// </summary>
    public class WeighingScaleController : MonoBehaviour
    {
        public const string ScaleSortingLayerName = "20_DeskProps_Fixed";
        public const string VfxSortingLayerName = "40_ActiveTool_VFX";

        // Paleta oficial da balança e VFX
        private static readonly Color ColorWoodBase = new Color32(0x2E, 0x23, 0x1E, 0xFF);        // #2E231E Mogno Desgastado
        private static readonly Color ColorMetal = new Color32(0x34, 0x3C, 0x47, 0xFF);           // #343C47 Metálico
        private static readonly Color ColorPurpleSoot = new Color32(0x6C, 0x34, 0x83, 0xFF);      // #6C3483 Fuligem Púrpura
        private static readonly Color ColorGoldenGlow = new Color32(0xD4, 0xAF, 0x37, 0xFF);      // #D4AF37 Brilho Dourado

        [Header("Referências Externas")]
        [SerializeField] private VerticalSliceGameManager gameManager;
        [SerializeField] private Camera shakeCamera;

        [Header("Estrutura da Balança (Auto-criada se vazia)")]
        [SerializeField] private Transform beamPivot;
        [SerializeField] private Collider2D leftPanTrigger;
        [SerializeField] private Transform leftPanAnchor;
        [SerializeField] private Transform rightPanAnchor;

        [Header("Ângulos e Dinâmica da Haste")]
        [SerializeField] private float neutralAngle = 0f;
        [SerializeField] private float heavyGuiltAngle = -20f;
        [SerializeField] private float altruisticLevitationAngle = 20f;
        [SerializeField] private float smoothTiltSpeed = 6.5f;
        [SerializeField] private float abruptHeavyTiltSpeed = 28f;

        [Header("Rejeição de Documentos Comuns")]
        [SerializeField] private Vector2 rejectionDirection = new Vector2(1f, 0.35f);
        [SerializeField] private float rejectionImpulseStrength = 8.5f;
        [SerializeField] private float rejectionCooldown = 0.25f;

        [Header("Efeitos Visuais e Screen-Shake")]
        [SerializeField] private ParticleSystem heavyGuiltSootParticles;
        [SerializeField] private ParticleSystem altruisticGoldParticles;
        [SerializeField] private float heavyGuiltShakePixels = 2f;
        [SerializeField] private float heavyGuiltShakeDuration = 0.2f;

        [Header("Placeholders Visuais")]
        [SerializeField] private bool autoCreateVisualPlaceholders = true;

        private readonly HashSet<DraggableDocument> _documentsInPan = new HashSet<DraggableDocument>();
        private readonly Dictionary<DraggableDocument, float> _rejectionTimestamps = new Dictionary<DraggableDocument, float>();
        private readonly List<UnityEngine.Object> _generatedResources = new List<UnityEngine.Object>();

        private DraggableDocument _weighedRelic;
        private float _targetBeamAngle;
        private float _currentBeamAngle;
        private float _activeTiltSpeed;

        private Coroutine _cameraShakeCoroutine;
        private Vector3 _cameraOriginalPosition;
        private bool _isShakingCamera;

        public DraggableDocument CurrentWeighedRelic => _weighedRelic;
        public float CurrentBeamAngle => _currentBeamAngle;

        public event Action<KarmicWeightType, DraggableDocument> OnRelicWeighed;
        public event Action<DraggableDocument> OnDocumentRejected;

        private void Awake()
        {
            _activeTiltSpeed = smoothTiltSpeed;

            if (autoCreateVisualPlaceholders && (beamPivot == null || leftPanTrigger == null))
            {
                BuildPlaceholderScaleStructure();
            }

            ConfigureLeftPanTrigger();
            EnsureParticleSystems();
            ApplySortingLayerToScaleRenderers();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (gameManager != null)
            {
                gameManager.OnCaseLoaded += HandleCaseLoaded;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnCaseLoaded -= HandleCaseLoaded;
            }

            RestoreCameraPositionIfShaking();
        }

        private void OnDestroy()
        {
            RestoreCameraPositionIfShaking();

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
            UpdateBeamRotation();
            UpdateWeighedRelicPosition();
            EvaluateDocumentsInPan();
        }

        /// <summary>
        /// Reseta a haste para a horizontal (0 graus) e desliga os efeitos ativos.
        /// </summary>
        public void ResetScale()
        {
            _weighedRelic = null;
            _documentsInPan.Clear();
            _targetBeamAngle = neutralAngle;
            _activeTiltSpeed = smoothTiltSpeed;
            StopAllScaleParticles();
        }

        /// <summary>
        /// Repassa entrada no Trigger2D do prato esquerdo (pode ser chamado pelo próprio GameObject ou pelo relay no filho).
        /// </summary>
        internal void NotifyPanTriggerEnter(Collider2D other)
        {
            if (other == null || !other.TryGetComponent(out DraggableDocument doc))
            {
                return;
            }

            _documentsInPan.Add(doc);
            TryEvaluateDocument(doc);
        }

        /// <summary>
        /// Repassa permanência no Trigger2D do prato esquerdo para reagir assim que o jogador soltar o item.
        /// </summary>
        internal void NotifyPanTriggerStay(Collider2D other)
        {
            if (other == null || !other.TryGetComponent(out DraggableDocument doc))
            {
                return;
            }

            _documentsInPan.Add(doc);
            TryEvaluateDocument(doc);
        }

        /// <summary>
        /// Repassa saída do Trigger2D do prato esquerdo.
        /// </summary>
        internal void NotifyPanTriggerExit(Collider2D other)
        {
            if (other == null || !other.TryGetComponent(out DraggableDocument doc))
            {
                return;
            }

            _documentsInPan.Remove(doc);

            if (_weighedRelic == doc)
            {
                ReleaseWeighedRelic();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (leftPanTrigger == null || leftPanTrigger.gameObject == gameObject)
            {
                NotifyPanTriggerEnter(other);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (leftPanTrigger == null || leftPanTrigger.gameObject == gameObject)
            {
                NotifyPanTriggerStay(other);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (leftPanTrigger == null || leftPanTrigger.gameObject == gameObject)
            {
                NotifyPanTriggerExit(other);
            }
        }

        private void EvaluateDocumentsInPan()
        {
            if (!object.ReferenceEquals(_weighedRelic, null) &&
                (_weighedRelic == null || !_weighedRelic.gameObject.activeInHierarchy || _weighedRelic.IsDragging))
            {
                ReleaseWeighedRelic();
            }

            if (_documentsInPan.Count == 0)
            {
                return;
            }

            // Copia defensiva para evitar modificação durante iteração.
            List<DraggableDocument> snapshot = new List<DraggableDocument>(_documentsInPan);
            for (int i = 0; i < snapshot.Count; i++)
            {
                DraggableDocument doc = snapshot[i];
                if (doc == null)
                {
                    _documentsInPan.Remove(doc);
                    continue;
                }

                TryEvaluateDocument(doc);
            }
        }

        private void TryEvaluateDocument(DraggableDocument doc)
        {
            if (doc == null || doc.IsDragging)
            {
                return;
            }

            if (!doc.IsRelic)
            {
                RejectNonRelicDocument(doc);
                return;
            }

            if (_weighedRelic == doc)
            {
                return;
            }

            AcceptAndWeighRelic(doc);
        }

        private void RejectNonRelicDocument(DraggableDocument doc)
        {
            float now = Time.time;
            if (_rejectionTimestamps.TryGetValue(doc, out float lastRejectedAt) &&
                now - lastRejectedAt < rejectionCooldown)
            {
                return;
            }

            _rejectionTimestamps[doc] = now;
            _documentsInPan.Remove(doc);

            Vector2 awayFromPan = rejectionDirection;
            if (leftPanTrigger != null)
            {
                Vector2 delta = (Vector2)doc.transform.position - (Vector2)leftPanTrigger.bounds.center;
                if (delta.sqrMagnitude > 0.01f)
                {
                    // Combina o vetor de afastamento com a direção padrão para sempre empurrar para fora do prato.
                    awayFromPan = (delta.normalized + rejectionDirection.normalized).normalized;
                }
            }

            doc.ApplyRejectionImpulse(awayFromPan, rejectionImpulseStrength);
            Debug.Log($"[WeighingScaleController] Documento comum '{doc.name}' rejeitado do prato da Balança do Pesar.");
            OnDocumentRejected?.Invoke(doc);
        }

        private void AcceptAndWeighRelic(DraggableDocument relic)
        {
            _weighedRelic = relic;
            relic.CancelSoftClamp();
            ResolveDependencies();

            KarmicWeightType weight = KarmicWeightType.Neutral;
            if (gameManager != null && gameManager.CurrentCase != null)
            {
                weight = gameManager.CurrentCase.karmicWeight;
            }
            else
            {
                Debug.LogWarning("[WeighingScaleController] Nenhum caso ativo encontrado no VerticalSliceGameManager; utilizando peso Neutral.");
            }

            StopAllScaleParticles();

            switch (weight)
            {
                case KarmicWeightType.Neutral:
                    _targetBeamAngle = neutralAngle;
                    _activeTiltSpeed = smoothTiltSpeed;
                    break;

                case KarmicWeightType.HeavyGuilt:
                    // Inclinação brusca de -20 graus, partículas de fuligem púrpura (#6C3483) e screen-shake de 2px por 0.2s.
                    _targetBeamAngle = heavyGuiltAngle;
                    _activeTiltSpeed = abruptHeavyTiltSpeed;
                    PlayParticles(heavyGuiltSootParticles);
                    TriggerCameraShake(heavyGuiltShakePixels, heavyGuiltShakeDuration);
                    break;

                case KarmicWeightType.AltruisticLevitation:
                    // Inclinação de +20 graus (prato sobe) e brilho com partículas douradas (#D4AF37).
                    _targetBeamAngle = altruisticLevitationAngle;
                    _activeTiltSpeed = smoothTiltSpeed;
                    PlayParticles(altruisticGoldParticles);
                    break;
            }

            Debug.Log(
                $"[WeighingScaleController] Relíquia '{relic.name}' aferida. " +
                $"Peso cármico: {weight} -> ângulo alvo: {_targetBeamAngle:0.#}°.");

            OnRelicWeighed?.Invoke(weight, relic);
        }

        private void ReleaseWeighedRelic()
        {
            _weighedRelic = null;
            _targetBeamAngle = neutralAngle;
            _activeTiltSpeed = smoothTiltSpeed;
            StopAllScaleParticles();
        }

        private void UpdateBeamRotation()
        {
            if (beamPivot == null)
            {
                return;
            }

            _currentBeamAngle = Mathf.Lerp(_currentBeamAngle, _targetBeamAngle, 1f - Mathf.Exp(-_activeTiltSpeed * Time.deltaTime));
            if (Mathf.Abs(_currentBeamAngle - _targetBeamAngle) < 0.05f)
            {
                _currentBeamAngle = _targetBeamAngle;
            }

            beamPivot.localRotation = Quaternion.Euler(0f, 0f, _currentBeamAngle);

            // Mantém os pratos nivelados na horizontal enquanto a haste inclina.
            Quaternion counterRotation = Quaternion.Euler(0f, 0f, -_currentBeamAngle);
            if (leftPanAnchor != null)
            {
                leftPanAnchor.localRotation = counterRotation;
            }

            if (rightPanAnchor != null)
            {
                rightPanAnchor.localRotation = counterRotation;
            }
        }

        private void UpdateWeighedRelicPosition()
        {
            if (_weighedRelic == null || _weighedRelic.IsDragging || leftPanAnchor == null)
            {
                return;
            }

            Vector3 anchorPos = leftPanAnchor.position;
            Vector3 desiredPos = new Vector3(anchorPos.x, anchorPos.y + 0.15f, _weighedRelic.transform.position.z);
            _weighedRelic.transform.position = Vector3.Lerp(
                _weighedRelic.transform.position,
                desiredPos,
                1f - Mathf.Exp(-14f * Time.deltaTime));
        }

        private void TriggerCameraShake(float pixelAmplitude, float duration)
        {
            ResolveDependencies();
            if (shakeCamera == null || duration <= 0f || pixelAmplitude <= 0f)
            {
                return;
            }

            if (_cameraShakeCoroutine != null)
            {
                StopCoroutine(_cameraShakeCoroutine);
                RestoreCameraPositionIfShaking();
            }

            _cameraShakeCoroutine = StartCoroutine(CameraShakeRoutine(pixelAmplitude, duration));
        }

        private IEnumerator CameraShakeRoutine(float pixelAmplitude, float duration)
        {
            _cameraOriginalPosition = shakeCamera.transform.position;
            _isShakingCamera = true;

            float worldAmplitude = ConvertPixelsToWorldUnits(shakeCamera, pixelAmplitude);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - Mathf.Clamp01(elapsed / duration);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * (worldAmplitude * damper);

                shakeCamera.transform.position = new Vector3(
                    _cameraOriginalPosition.x + offset.x,
                    _cameraOriginalPosition.y + offset.y,
                    _cameraOriginalPosition.z);

                yield return null;
            }

            RestoreCameraPositionIfShaking();
            _cameraShakeCoroutine = null;
        }

        private void RestoreCameraPositionIfShaking()
        {
            if (_isShakingCamera && shakeCamera != null)
            {
                shakeCamera.transform.position = _cameraOriginalPosition;
            }

            _isShakingCamera = false;
        }

        private static float ConvertPixelsToWorldUnits(Camera cam, float pixels)
        {
            if (cam == null || Screen.height <= 0)
            {
                return 0.02f;
            }

            if (cam.orthographic)
            {
                float worldHeight = cam.orthographicSize * 2f;
                return (pixels / Screen.height) * worldHeight;
            }

            return pixels * 0.01f;
        }

        private void HandleCaseLoaded(SoulCaseData newCase, int caseIndex)
        {
            ResetScale();
        }

        private void ResolveDependencies()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<VerticalSliceGameManager>();
            }

            if (shakeCamera == null)
            {
                shakeCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }
        }

        private void ConfigureLeftPanTrigger()
        {
            if (leftPanTrigger == null)
            {
                return;
            }

            leftPanTrigger.isTrigger = true;

            // Garante um Rigidbody2D Kinematic no prato para que Triggers 2D funcionem
            // mesmo com DraggableDocuments que possuem apenas BoxCollider2D.
            if (!leftPanTrigger.TryGetComponent(out Rigidbody2D rb))
            {
                rb = leftPanTrigger.gameObject.AddComponent<Rigidbody2D>();
            }
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            if (leftPanTrigger.gameObject != gameObject)
            {
                if (!leftPanTrigger.TryGetComponent(out ScalePanTriggerRelay relay))
                {
                    relay = leftPanTrigger.gameObject.AddComponent<ScalePanTriggerRelay>();
                }
                relay.Initialize(this);
            }
        }

        private void BuildPlaceholderScaleStructure()
        {
            Sprite solidSprite = CreateSolidSprite();
            string propLayer = ResolveSortingLayerOrDefault(ScaleSortingLayerName);

            // Base de mogno (#2E231E)
            CreatePartSprite("Scale_Base", transform, new Vector3(0f, -0.95f, 0f), new Vector3(1.5f, 0.25f, 1f), solidSprite, ColorWoodBase, propLayer, 0);
            // Coluna central metálica (#343C47)
            CreatePartSprite("Scale_Pillar", transform, new Vector3(0f, -0.1f, 0f), new Vector3(0.18f, 1.55f, 1f), solidSprite, ColorMetal, propLayer, 1);

            // Pivô da haste
            GameObject pivotObj = new GameObject("Scale_BeamPivot");
            pivotObj.transform.SetParent(transform, false);
            pivotObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            beamPivot = pivotObj.transform;

            CreatePartSprite("Scale_BeamBar", beamPivot, Vector3.zero, new Vector3(2.4f, 0.12f, 1f), solidSprite, ColorMetal, propLayer, 2);

            // Prato esquerdo (com Trigger2D de medição)
            GameObject leftPanObj = new GameObject("Scale_LeftPan");
            leftPanObj.transform.SetParent(beamPivot, false);
            leftPanObj.transform.localPosition = new Vector3(-1.05f, -0.45f, 0f);
            leftPanAnchor = leftPanObj.transform;

            CreatePartSprite("LeftPan_Chain", leftPanAnchor, new Vector3(0f, 0.22f, 0f), new Vector3(0.04f, 0.45f, 1f), solidSprite, ColorMetal, propLayer, 1);
            CreatePartSprite("LeftPan_Plate", leftPanAnchor, Vector3.zero, new Vector3(0.95f, 0.14f, 1f), solidSprite, ColorMetal, propLayer, 3);

            BoxCollider2D panCollider = leftPanObj.AddComponent<BoxCollider2D>();
            panCollider.isTrigger = true;
            panCollider.size = new Vector2(1.1f, 0.9f);
            panCollider.offset = new Vector2(0f, 0.35f);
            leftPanTrigger = panCollider;

            // Prato direito (contrapeso fixo)
            GameObject rightPanObj = new GameObject("Scale_RightPan");
            rightPanObj.transform.SetParent(beamPivot, false);
            rightPanObj.transform.localPosition = new Vector3(1.05f, -0.45f, 0f);
            rightPanAnchor = rightPanObj.transform;

            CreatePartSprite("RightPan_Chain", rightPanAnchor, new Vector3(0f, 0.22f, 0f), new Vector3(0.04f, 0.45f, 1f), solidSprite, ColorMetal, propLayer, 1);
            CreatePartSprite("RightPan_Plate", rightPanAnchor, Vector3.zero, new Vector3(0.95f, 0.14f, 1f), solidSprite, ColorMetal, propLayer, 3);
        }

        private void EnsureParticleSystems()
        {
            Transform vfxParent = leftPanAnchor != null ? leftPanAnchor : transform;

            if (heavyGuiltSootParticles == null)
            {
                heavyGuiltSootParticles = CreateProceduralParticles(
                    "VFX_HeavyGuilt_PurpleSoot",
                    vfxParent,
                    ColorPurpleSoot,
                    gravityModifier: 0.35f,
                    startSpeedMin: 0.4f,
                    startSpeedMax: 1.3f,
                    emissionRate: 28f,
                    looping: false);
            }

            if (altruisticGoldParticles == null)
            {
                altruisticGoldParticles = CreateProceduralParticles(
                    "VFX_Altruistic_GoldenGlow",
                    vfxParent,
                    ColorGoldenGlow,
                    gravityModifier: -0.25f,
                    startSpeedMin: 0.3f,
                    startSpeedMax: 0.9f,
                    emissionRate: 22f,
                    looping: true);
            }
        }

        private ParticleSystem CreateProceduralParticles(
            string objectName,
            Transform parent,
            Color particleColor,
            float gravityModifier,
            float startSpeedMin,
            float startSpeedMax,
            float emissionRate,
            bool looping)
        {
            GameObject vfxObj = new GameObject(objectName);
            vfxObj.transform.SetParent(parent, false);
            vfxObj.transform.localPosition = new Vector3(0f, 0.15f, -0.1f);

            ParticleSystem ps = vfxObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = looping;
            main.duration = 0.8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeedMin, startSpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = particleColor;
            main.gravityModifier = gravityModifier;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = looping ? emissionRate : 0f;
            if (!looping)
            {
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
            }

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(particleColor, 0f), new GradientColorKey(particleColor, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = ResolveSortingLayerOrDefault(VfxSortingLayerName);
                renderer.sortingOrder = 10;

                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.color = Color.white;
                    renderer.sharedMaterial = mat;
                    _generatedResources.Add(mat);
                }
            }

            return ps;
        }

        private void ApplySortingLayerToScaleRenderers()
        {
            string propLayer = ResolveSortingLayerOrDefault(ScaleSortingLayerName);
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingLayerName = propLayer;
            }
        }

        private static void PlayParticles(ParticleSystem ps)
        {
            if (ps == null)
            {
                return;
            }

            ps.Clear(true);
            ps.Play(true);
        }

        private void StopAllScaleParticles()
        {
            if (heavyGuiltSootParticles != null)
            {
                heavyGuiltSootParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (altruisticGoldParticles != null)
            {
                altruisticGoldParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

        private static void CreatePartSprite(
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

        private static string ResolveSortingLayerOrDefault(string layerName)
        {
            return SortingLayer.NameToID(layerName) != 0 ? layerName : "Default";
        }
    }

    /// <summary>
    /// Componente auxiliar anexado ao prato esquerdo da balança para repassar callbacks Trigger2D
    /// ao WeighingScaleController pai.
    /// </summary>
    internal class ScalePanTriggerRelay : MonoBehaviour
    {
        private WeighingScaleController _controller;

        public void Initialize(WeighingScaleController controller)
        {
            _controller = controller;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            _controller?.NotifyPanTriggerEnter(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            _controller?.NotifyPanTriggerStay(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            _controller?.NotifyPanTriggerExit(other);
        }
    }
}
