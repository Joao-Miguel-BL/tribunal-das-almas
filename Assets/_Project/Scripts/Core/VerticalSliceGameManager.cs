using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TribunalDasAlmas.Mechanics;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TribunalDasAlmas.Core
{
    /// <summary>
    /// Estados principais da bancada de trabalho da Vertical Slice.
    /// </summary>
    public enum DeskState
    {
        Idle,
        LoadingCase,
        InspectingCase,
        TransitioningCase,
        ShiftCompleted
    }

    /// <summary>
    /// Máquina de estados principal e gerenciador visual da bancada para os 3 casos
    /// da Vertical Slice de "O Tribunal das Almas (Setor de Triagem 7)".
    /// </summary>
    public class VerticalSliceGameManager : MonoBehaviour
    {
        private const string LogPrefix = "[VerticalSliceGameManager]";
        private const string SortingLayerDeskSurface = "10_DeskSurface";
        private const string SortingLayerDeskProps = "20_DeskProps_Fixed";
        private const string SortingLayerDraggableDocs = "30_DraggableDocs";

        // Paleta de cores oficial
        private static readonly Color ColorDeskSurface = new Color32(0x4A, 0x54, 0x48, 0xFF); // #4A5448 Oliva Decadente
        private static readonly Color ColorDeskFrame = new Color32(0x2E, 0x23, 0x1E, 0xFF);   // #2E231E Mogno Desgastado
        private static readonly Color ColorPaperCream = new Color32(0xDF, 0xD5, 0xC0, 0xFF);  // #DFD5C0 Creme Certidão
        private static readonly Color ColorInkOutline = new Color32(0x1C, 0x20, 0x26, 0xFF);  // #1C2026 Contorno / Tinta

        [Header("Casos da Vertical Slice (Ordem Sequencial)")]
        [SerializeField] private List<SoulCaseData> verticalSliceCases = new List<SoulCaseData>();
        [SerializeField] private bool autoStartOnPlay = true;

        [Header("Dimensões e Limites da Bancada (Zona C)")]
        [SerializeField] private Vector2 deskBoundsMin = new Vector2(-4.5f, -2.5f);
        [SerializeField] private Vector2 deskBoundsMax = new Vector2(4.5f, 0.5f);
        [SerializeField] private Vector2 deskVisualSize = new Vector2(11.0f, 5.2f);
        [SerializeField] private Vector2 deskVisualCenter = new Vector2(0f, -1.0f);
        [SerializeField] private float frameThickness = 0.35f;

        [Header("Layout dos Documentos")]
        [SerializeField] private Vector2 certificateSize = new Vector2(3.4f, 4.3f);
        [SerializeField] private Vector2 certificateSpawnPos = new Vector2(-1.4f, -0.95f);
        [SerializeField] private Vector2 auxiliarySlipSize = new Vector2(2.7f, 2.2f);
        [SerializeField] private Vector2 auxiliarySlipSpawnPos = new Vector2(2.15f, -1.05f);
        [SerializeField] private TMP_FontAsset customFontAsset;

        [Header("Transições")]
        [SerializeField] private float documentSlideDuration = 0.35f;
        [SerializeField] private float offscreenSlideOffsetX = 9.5f;

        private readonly List<DraggableDocument> _activeDocuments = new List<DraggableDocument>();
        private readonly List<Texture2D> _generatedTextures = new List<Texture2D>();
        private readonly List<Sprite> _generatedSprites = new List<Sprite>();

        private Sprite _solidWhiteSprite;
        private Sprite _borderedPaperSprite;

        private Transform _deskVisualRoot;
        private Transform _documentsRoot;
        private TextMeshPro _statusHeaderText;

        private Coroutine _transitionCoroutine;
        private int _currentCaseIndex = -1;
        private int _correctVerdictsCount;
        private DeskState _currentState = DeskState.Idle;

        public DeskState CurrentState => _currentState;
        public int CurrentCaseIndex => _currentCaseIndex;
        public int TotalCases => verticalSliceCases != null ? verticalSliceCases.Count : 0;
        public int CorrectVerdictsCount => _correctVerdictsCount;
        public SoulCaseData CurrentCase =>
            verticalSliceCases != null && _currentCaseIndex >= 0 && _currentCaseIndex < verticalSliceCases.Count
                ? verticalSliceCases[_currentCaseIndex]
                : null;

        public event Action<DeskState> OnStateChanged;
        public event Action<SoulCaseData, int> OnCaseLoaded;
        public event Action<SoulCaseData, VerdictType, bool> OnCaseCompleted;
        public event Action<int, int> OnAllCasesCompleted;

        private void Awake()
        {
            CreateProceduralSprites();
            BuildDeskPlaceholders();
            EnsureDocumentsContainer();
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartVerticalSlice();
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _generatedSprites.Count; i++)
            {
                if (_generatedSprites[i] != null)
                {
                    Destroy(_generatedSprites[i]);
                }
            }
            _generatedSprites.Clear();

            for (int i = 0; i < _generatedTextures.Count; i++)
            {
                if (_generatedTextures[i] != null)
                {
                    Destroy(_generatedTextures[i]);
                }
            }
            _generatedTextures.Clear();
        }

        /// <summary>
        /// Inicia ou reinicia a sequência de casos da Vertical Slice a partir do Caso 1.
        /// </summary>
        public void StartVerticalSlice()
        {
            if (verticalSliceCases == null || verticalSliceCases.Count == 0)
            {
                Debug.LogError(
                    $"{LogPrefix} Nenhum SoulCaseData atribuído em 'verticalSliceCases'. " +
                    "Execute 'Tools/2. Gerar Casos Vertical Slice' e vincule os 3 casos no Inspector.",
                    this);
                UpdateStatusBanner("SEM CASOS CONFIGURADOS");
                SetState(DeskState.Idle);
                return;
            }

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            ClearActiveDocumentsImmediate();
            _correctVerdictsCount = 0;
            _currentCaseIndex = 0;

            _transitionCoroutine = StartCoroutine(LoadCaseRoutine(_currentCaseIndex, animateOutPrevious: false));
        }

        /// <summary>
        /// Registra a conclusão do caso atual sem especificar veredito e avança suavemente para o próximo.
        /// </summary>
        public void CompleteCurrentCase()
        {
            if (CurrentCase == null)
            {
                Debug.LogWarning($"{LogPrefix} Não há caso ativo para concluir.");
                return;
            }

            CompleteCurrentCase(CurrentCase.expectedVerdict);
        }

        /// <summary>
        /// Registra o veredito submetido para o caso atual e avança suavemente para o próximo caso da sequência.
        /// </summary>
        public void CompleteCurrentCase(VerdictType submittedVerdict)
        {
            if (_currentState != DeskState.InspectingCase)
            {
                Debug.LogWarning(
                    $"{LogPrefix} CompleteCurrentCase ignorado pois o estado atual é '{_currentState}'.");
                return;
            }

            SoulCaseData completedCase = CurrentCase;
            if (completedCase == null)
            {
                Debug.LogWarning($"{LogPrefix} Caso atual é nulo.");
                return;
            }

            bool isCorrect = submittedVerdict == completedCase.expectedVerdict;
            if (isCorrect)
            {
                _correctVerdictsCount++;
            }

            Debug.Log(
                $"{LogPrefix} Caso {_currentCaseIndex + 1}/{TotalCases} concluído: '{completedCase.soulName}' | " +
                $"Veredito submetido: {submittedVerdict} | Esperado: {completedCase.expectedVerdict} | " +
                $"Resultado: {(isCorrect ? "CORRETO" : "DIVERGENTE")}");

            OnCaseCompleted?.Invoke(completedCase, submittedVerdict, isCorrect);
            AdvanceToNextCase();
        }

        /// <summary>
        /// Avança suavemente para o próximo caso ou finaliza o turno caso o Caso 3 já tenha sido concluído.
        /// </summary>
        public void AdvanceToNextCase()
        {
            if (_currentState == DeskState.TransitioningCase || _currentState == DeskState.LoadingCase)
            {
                return;
            }

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            int nextIndex = _currentCaseIndex + 1;
            if (nextIndex < TotalCases)
            {
                _currentCaseIndex = nextIndex;
                _transitionCoroutine = StartCoroutine(LoadCaseRoutine(_currentCaseIndex, animateOutPrevious: true));
            }
            else
            {
                _transitionCoroutine = StartCoroutine(CompleteShiftRoutine());
            }
        }

        private IEnumerator LoadCaseRoutine(int caseIndex, bool animateOutPrevious)
        {
            SetState(animateOutPrevious ? DeskState.TransitioningCase : DeskState.LoadingCase);

            if (animateOutPrevious && _activeDocuments.Count > 0)
            {
                yield return SlideActiveDocumentsOutRoutine();
                ClearActiveDocumentsImmediate();
            }

            SoulCaseData caseData = verticalSliceCases[caseIndex];
            if (caseData == null)
            {
                Debug.LogError($"{LogPrefix} O caso no índice {caseIndex} está nulo na lista 'verticalSliceCases'.");
                SetState(DeskState.Idle);
                _transitionCoroutine = null;
                yield break;
            }

            SetState(DeskState.LoadingCase);

            List<(DraggableDocument doc, Vector3 targetPos)> spawnedDocs = SpawnCaseDocuments(caseData, caseIndex);

            string timerTag = caseData.isOnboardingNoTimer ? "ONBOARDING (SEM TIMER)" : "TURNO ATIVO";
            UpdateStatusBanner($"SETOR DE TRIAGEM 7  |  CASO {caseIndex + 1}/{TotalCases}: {caseData.soulName.ToUpperInvariant()}  |  {timerTag}");

            yield return SlideDocumentsInRoutine(spawnedDocs);

            for (int i = 0; i < spawnedDocs.Count; i++)
            {
                spawnedDocs[i].doc.SetInteractable(true);
            }

            SetState(DeskState.InspectingCase);
            _transitionCoroutine = null;

            Debug.Log($"{LogPrefix} Caso {caseIndex + 1}/{TotalCases} carregado: {caseData.soulName} ({caseData.occupation}).");
            OnCaseLoaded?.Invoke(caseData, caseIndex);
        }

        private IEnumerator CompleteShiftRoutine()
        {
            SetState(DeskState.TransitioningCase);

            if (_activeDocuments.Count > 0)
            {
                yield return SlideActiveDocumentsOutRoutine();
                ClearActiveDocumentsImmediate();
            }

            SetState(DeskState.ShiftCompleted);
            UpdateStatusBanner($"EXPEDIENTE CONCLUÍDO  |  ACERTOS: {_correctVerdictsCount}/{TotalCases}");

            Debug.Log($"{LogPrefix} Todos os {TotalCases} casos da Vertical Slice foram concluídos! Acertos: {_correctVerdictsCount}/{TotalCases}.");
            OnAllCasesCompleted?.Invoke(_correctVerdictsCount, TotalCases);
            _transitionCoroutine = null;
        }

        private List<(DraggableDocument doc, Vector3 targetPos)> SpawnCaseDocuments(SoulCaseData caseData, int caseIndex)
        {
            List<(DraggableDocument doc, Vector3 targetPos)> spawned = new List<(DraggableDocument, Vector3)>();

            // Documento Principal: Certidão de Óbito
            Vector3 certTargetPos = new Vector3(
                Mathf.Clamp(certificateSpawnPos.x, deskBoundsMin.x, deskBoundsMax.x),
                Mathf.Clamp(certificateSpawnPos.y, deskBoundsMin.y, deskBoundsMax.y),
                0f);

            string certificateBody =
                $"<b>CERTIDÃO DE ÓBITO — CASO 0{caseIndex + 1}</b>\n" +
                $"────────────────────────\n" +
                $"<b>Alma:</b> {caseData.soulName}\n" +
                $"<b>Ocupação:</b> {caseData.occupation}\n\n" +
                $"<b>Resumo do Laudo:</b>\n{caseData.certificateSummary}";

            DraggableDocument certificateDoc = CreatePaperDocument(
                $"Doc_Certidao_Caso0{caseIndex + 1}_{caseData.soulName}",
                certificateSize,
                certificateBody,
                certTargetPos);

            // Se o caso possuir trecho censurado (M3 / DD-011), anexa a tarja interativa RedactedArea
            if (caseData.hasRedactedText)
            {
                AttachRedactedAreaToDocument(certificateDoc, caseData);
            }

            spawned.Add((certificateDoc, certTargetPos));

            // Documento Auxiliar: Guia de Triagem
            Vector3 auxTargetPos = new Vector3(
                Mathf.Clamp(auxiliarySlipSpawnPos.x, deskBoundsMin.x, deskBoundsMax.x),
                Mathf.Clamp(auxiliarySlipSpawnPos.y, deskBoundsMin.y, deskBoundsMax.y),
                0f);

            string relicDisplay = string.IsNullOrWhiteSpace(caseData.relicName)
                ? "Nenhuma relíquia registrada."
                : caseData.relicName;

            string auxBody =
                $"<b>FICHA DE TRIAGEM</b>\n" +
                $"──────────────────\n" +
                $"<b>Relíquia:</b>\n{relicDisplay}\n\n" +
                $"<b>Protocolo:</b>\n" +
                $"{(caseData.isOnboardingNoTimer ? "Avaliação inicial sem limite de tempo." : "Verifique provas antes do carimbo.")}";

            DraggableDocument auxDoc = CreatePaperDocument(
                $"Doc_FichaTriagem_Caso0{caseIndex + 1}",
                auxiliarySlipSize,
                auxBody,
                auxTargetPos);

            spawned.Add((auxDoc, auxTargetPos));

            // Se o caso possuir relíquia física (M2), instancia o item arrastável com IsRelic = true
            if (!string.IsNullOrWhiteSpace(caseData.relicName))
            {
                Vector3 relicTargetPos = new Vector3(
                    Mathf.Clamp(auxiliarySlipSpawnPos.x, deskBoundsMin.x, deskBoundsMax.x),
                    Mathf.Clamp(auxiliarySlipSpawnPos.y + 1.4f, deskBoundsMin.y, deskBoundsMax.y),
                    0f);

                DraggableDocument relicItem = CreatePaperDocument(
                    $"Relic_Caso0{caseIndex + 1}_{caseData.relicName}",
                    new Vector2(2.0f, 1.0f),
                    $"<b>[RELÍQUIA]</b>\n{caseData.relicName}",
                    relicTargetPos);
                relicItem.IsRelic = true;

                spawned.Add((relicItem, relicTargetPos));
            }

            return spawned;
        }

        private void AttachRedactedAreaToDocument(DraggableDocument document, SoulCaseData caseData)
        {
            GameObject redactedObj = new GameObject("RedactedArea_Tarja");
            redactedObj.transform.SetParent(document.transform, false);
            redactedObj.transform.localPosition = new Vector3(0f, -certificateSize.y * 0.32f, -0.02f);

            RedactedArea redactedArea = redactedObj.AddComponent<RedactedArea>();
            redactedArea.Configure(caseData, new Vector2(certificateSize.x - 0.5f, 0.95f), customFontAsset);
        }

        private DraggableDocument CreatePaperDocument(
            string objectName,
            Vector2 paperSize,
            string contentText,
            Vector3 targetDeskPosition)
        {
            GameObject docObj = new GameObject(objectName);
            docObj.transform.SetParent(_documentsRoot, false);
            docObj.transform.position = new Vector3(
                targetDeskPosition.x + offscreenSlideOffsetX,
                targetDeskPosition.y,
                targetDeskPosition.z);

            // Papel Certidão (#DFD5C0) com contorno de 1px (#1C2026) via sprite 9-sliced
            SpriteRenderer paperRenderer = docObj.AddComponent<SpriteRenderer>();
            paperRenderer.sprite = _borderedPaperSprite;
            paperRenderer.drawMode = SpriteDrawMode.Sliced;
            paperRenderer.size = paperSize;
            paperRenderer.color = Color.white; // Cores já baked no sprite para preservar preenchimento + borda de 1px
            paperRenderer.sortingLayerName = ResolveSortingLayerOrDefault(SortingLayerDraggableDocs);
            paperRenderer.sortingOrder = 0;

            BoxCollider2D collider2D = docObj.AddComponent<BoxCollider2D>();
            collider2D.size = paperSize;

            SortingGroup sortingGroup = docObj.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = ResolveSortingLayerOrDefault(SortingLayerDraggableDocs);

            DraggableDocument draggable = docObj.AddComponent<DraggableDocument>();
            draggable.ConfigureDeskBounds(deskBoundsMin, deskBoundsMax);
            draggable.SetInteractable(false);

            // Texto TextMeshPro (World Space) filho do documento para acompanhar o SortingGroup
            GameObject textObj = new GameObject("DocumentText");
            textObj.transform.SetParent(docObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            ConfigureTmpComponent(tmp, paperSize - new Vector2(0.35f, 0.35f), contentText, 2.0f, TextAlignmentOptions.TopLeft);
            tmp.color = ColorInkOutline;

            MeshRenderer textRenderer = tmp.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingLayerName = ResolveSortingLayerOrDefault(SortingLayerDraggableDocs);
                textRenderer.sortingOrder = 1;
            }

            _activeDocuments.Add(draggable);
            return draggable;
        }

        private void BuildDeskPlaceholders()
        {
            if (_deskVisualRoot != null)
            {
                return;
            }

            GameObject deskRootObj = new GameObject("Desk_VisualPlaceholders");
            deskRootObj.transform.SetParent(transform, false);
            _deskVisualRoot = deskRootObj.transform;

            string deskLayer = ResolveSortingLayerOrDefault(SortingLayerDeskSurface);
            string propsLayer = ResolveSortingLayerOrDefault(SortingLayerDeskProps);

            // Moldura da Bancada (#2E231E - Mogno Desgastado)
            GameObject frameObj = new GameObject("Desk_Frame_Mogno");
            frameObj.transform.SetParent(_deskVisualRoot, false);
            frameObj.transform.position = new Vector3(deskVisualCenter.x, deskVisualCenter.y, 0.2f);
            frameObj.transform.localScale = new Vector3(
                deskVisualSize.x + frameThickness * 2f,
                deskVisualSize.y + frameThickness * 2f,
                1f);

            SpriteRenderer frameRenderer = frameObj.AddComponent<SpriteRenderer>();
            frameRenderer.sprite = _solidWhiteSprite;
            frameRenderer.color = ColorDeskFrame;
            frameRenderer.sortingLayerName = deskLayer;
            frameRenderer.sortingOrder = 0;

            // Tampo da Bancada (#4A5448 - Oliva Decadente)
            GameObject surfaceObj = new GameObject("Desk_Surface_Oliva");
            surfaceObj.transform.SetParent(_deskVisualRoot, false);
            surfaceObj.transform.position = new Vector3(deskVisualCenter.x, deskVisualCenter.y, 0.1f);
            surfaceObj.transform.localScale = new Vector3(deskVisualSize.x, deskVisualSize.y, 1f);

            SpriteRenderer surfaceRenderer = surfaceObj.AddComponent<SpriteRenderer>();
            surfaceRenderer.sprite = _solidWhiteSprite;
            surfaceRenderer.color = ColorDeskSurface;
            surfaceRenderer.sortingLayerName = deskLayer;
            surfaceRenderer.sortingOrder = 1;

            // Placa de cabeçalho fixa na moldura superior da bancada
            GameObject headerObj = new GameObject("Desk_StatusHeader");
            headerObj.transform.SetParent(_deskVisualRoot, false);
            headerObj.transform.position = new Vector3(
                deskVisualCenter.x,
                deskVisualCenter.y + (deskVisualSize.y * 0.5f) + (frameThickness * 0.5f),
                0f);

            _statusHeaderText = headerObj.AddComponent<TextMeshPro>();
            ConfigureTmpComponent(
                _statusHeaderText,
                new Vector2(deskVisualSize.x, frameThickness),
                "SETOR DE TRIAGEM 7 — AGUARDANDO CASO",
                2.1f,
                TextAlignmentOptions.Center);
            _statusHeaderText.color = ColorPaperCream;

            MeshRenderer headerRenderer = _statusHeaderText.GetComponent<MeshRenderer>();
            if (headerRenderer != null)
            {
                headerRenderer.sortingLayerName = propsLayer;
                headerRenderer.sortingOrder = 5;
            }
        }

        private void EnsureDocumentsContainer()
        {
            if (_documentsRoot != null)
            {
                return;
            }

            GameObject rootObj = new GameObject("ActiveDocuments");
            rootObj.transform.SetParent(transform, false);
            _documentsRoot = rootObj.transform;
        }

        private void CreateProceduralSprites()
        {
            // Sprite branco sólido (1x1 unidade em PPU=4) para o tampo e moldura
            Texture2D solidTex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] solidPixels = new Color32[16];
            for (int i = 0; i < solidPixels.Length; i++)
            {
                solidPixels[i] = new Color32(255, 255, 255, 255);
            }

            solidTex.SetPixels32(solidPixels);
            solidTex.Apply();

            _solidWhiteSprite = Sprite.Create(
                solidTex,
                new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f),
                4f);

            _generatedTextures.Add(solidTex);
            _generatedSprites.Add(_solidWhiteSprite);

            // Sprite 9-sliced do Papel Certidão (#DFD5C0) com contorno exato de 1px em #1C2026
            const int texSize = 16;
            Texture2D paperTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 fillColor = ColorPaperCream;
            Color32 borderColor = ColorInkOutline;
            Color32[] paperPixels = new Color32[texSize * texSize];

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == texSize - 1 || y == texSize - 1;
                    paperPixels[y * texSize + x] = isBorder ? borderColor : fillColor;
                }
            }

            paperTex.SetPixels32(paperPixels);
            paperTex.Apply();

            _borderedPaperSprite = Sprite.Create(
                paperTex,
                new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(1f, 1f, 1f, 1f));

            _generatedTextures.Add(paperTex);
            _generatedSprites.Add(_borderedPaperSprite);
        }

        private void ConfigureTmpComponent(
            TextMeshPro tmp,
            Vector2 rectSize,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            if (customFontAsset != null)
            {
                tmp.font = customFontAsset;
            }
            else if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            if (tmp.font == null)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Nenhuma fonte TextMeshPro padrão encontrada. " +
                    "Importe 'Window > TextMeshPro > Import TMP Essential Resources' ou atribua 'customFontAsset'.",
                    this);
            }

            RectTransform rect = tmp.rectTransform;
            rect.sizeDelta = rectSize;

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        private IEnumerator SlideDocumentsInRoutine(List<(DraggableDocument doc, Vector3 targetPos)> docs)
        {
            if (docs.Count == 0)
            {
                yield break;
            }

            Vector3[] startPositions = new Vector3[docs.Count];
            for (int i = 0; i < docs.Count; i++)
            {
                startPositions[i] = docs[i].doc.transform.position;
            }

            float duration = Mathf.Max(0.01f, documentSlideDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                for (int i = 0; i < docs.Count; i++)
                {
                    if (docs[i].doc != null)
                    {
                        docs[i].doc.transform.position = Vector3.Lerp(startPositions[i], docs[i].targetPos, t);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < docs.Count; i++)
            {
                if (docs[i].doc != null)
                {
                    docs[i].doc.transform.position = docs[i].targetPos;
                }
            }
        }

        private IEnumerator SlideActiveDocumentsOutRoutine()
        {
            int count = _activeDocuments.Count;
            if (count == 0)
            {
                yield break;
            }

            Vector3[] startPositions = new Vector3[count];
            Vector3[] targetPositions = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                DraggableDocument doc = _activeDocuments[i];
                if (doc == null)
                {
                    continue;
                }

                doc.SetInteractable(false);
                doc.ResetZoom(instant: false);

                startPositions[i] = doc.transform.position;
                targetPositions[i] = new Vector3(
                    startPositions[i].x - offscreenSlideOffsetX,
                    startPositions[i].y,
                    startPositions[i].z);
            }

            float duration = Mathf.Max(0.01f, documentSlideDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                for (int i = 0; i < count; i++)
                {
                    if (_activeDocuments[i] != null)
                    {
                        _activeDocuments[i].transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], t);
                    }
                }

                yield return null;
            }
        }

        private void ClearActiveDocumentsImmediate()
        {
            for (int i = 0; i < _activeDocuments.Count; i++)
            {
                if (_activeDocuments[i] != null)
                {
                    Destroy(_activeDocuments[i].gameObject);
                }
            }

            _activeDocuments.Clear();
        }

        private void UpdateStatusBanner(string message)
        {
            if (_statusHeaderText != null)
            {
                _statusHeaderText.text = message;
            }
        }

        private void SetState(DeskState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);
        }

        private static string ResolveSortingLayerOrDefault(string layerName)
        {
            return SortingLayer.NameToID(layerName) != 0 ? layerName : "Default";
        }

#if UNITY_EDITOR
        private void Reset()
        {
            TryAutoPopulateDefaultCases();
        }

        [ContextMenu("Auto-Carregar Casos da Vertical Slice")]
        private void TryAutoPopulateDefaultCases()
        {
            string[] defaultPaths =
            {
                "Assets/_Project/ScriptableObjects/Cases/Case01_Zulmira.asset",
                "Assets/_Project/ScriptableObjects/Cases/Case02_Valdir.asset",
                "Assets/_Project/ScriptableObjects/Cases/Case03_Artur.asset"
            };

            verticalSliceCases.Clear();

            foreach (string path in defaultPaths)
            {
                SoulCaseData caseAsset = AssetDatabase.LoadAssetAtPath<SoulCaseData>(path);
                if (caseAsset != null)
                {
                    verticalSliceCases.Add(caseAsset);
                }
            }
        }
#endif
    }
}
