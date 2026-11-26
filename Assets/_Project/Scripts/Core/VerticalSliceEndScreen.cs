using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TribunalDasAlmas.Core
{
    /// <summary>
    /// Painel final de encerramento da Vertical Slice em Canvas Overlay.
    /// Exibido automaticamente ao concluir a validação do Caso 3, apresentando
    /// a mensagem de conclusão e o botão "Reiniciar Sandbox" para recarregar Sandbox_VerticalSlice.unity.
    /// </summary>
    public class VerticalSliceEndScreen : MonoBehaviour
    {
        public const string DefaultSandboxSceneName = "Sandbox_VerticalSlice";
        public const string CompletionTitleText = "Vertical Slice Concluída com Sucesso - O Tribunal das Almas";

        private static readonly Color ColorBackdrop = new Color(0.08f, 0.09f, 0.11f, 0.86f);
        private static readonly Color ColorPanelBg = new Color32(0xDF, 0xD5, 0xC0, 0xFF);      // #DFD5C0 Creme Certidão
        private static readonly Color ColorPanelBorder = new Color32(0x2E, 0x23, 0x1E, 0xFF);  // #2E231E Mogno
        private static readonly Color ColorInkText = new Color32(0x1C, 0x20, 0x26, 0xFF);      // #1C2026 Tinta
        private static readonly Color ColorButtonBg = new Color32(0x4A, 0x54, 0x48, 0xFF);     // #4A5448 Oliva

        [Header("Referências")]
        [SerializeField] private VerticalSliceGameManager gameManager;
        [SerializeField] private string sandboxSceneName = DefaultSandboxSceneName;
        [SerializeField] private TMP_FontAsset customFontAsset;

        private Canvas _overlayCanvas;
        private GameObject _panelRoot;
        private TextMeshProUGUI _summaryText;
        private bool _isBound;

        private void Awake()
        {
            BuildOverlayCanvas();
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<VerticalSliceGameManager>();
            }

            BindToGameManager(gameManager);
        }

        private void OnDisable()
        {
            UnbindFromGameManager();
        }

        /// <summary>
        /// Vincula explicitamente este painel ao VerticalSliceGameManager.
        /// </summary>
        public void BindToGameManager(VerticalSliceGameManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (_isBound && gameManager == manager)
            {
                return;
            }

            UnbindFromGameManager();
            gameManager = manager;
            gameManager.OnAllCasesCompleted += HandleAllCasesCompleted;
            _isBound = true;
        }

        /// <summary>
        /// Exibe o painel final com o resumo de conformidade do turno.
        /// </summary>
        public void Show(int correctVerdicts, int totalCases)
        {
            EnsureEventSystemExists();

            if (_summaryText != null)
            {
                _summaryText.text = $"Setor de Triagem 7 — Conformidade final: {correctVerdicts}/{totalCases} vereditos corretos.";
            }

            SetPanelVisible(true);
        }

        /// <summary>
        /// Recarrega a cena Sandbox_VerticalSlice.unity (com fallback seguro caso não esteja nas Build Settings).
        /// </summary>
        public void RestartSandbox()
        {
            Time.timeScale = 1f;

            string targetScene = string.IsNullOrWhiteSpace(sandboxSceneName)
                ? DefaultSandboxSceneName
                : sandboxSceneName;

            if (Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.buildIndex >= 0)
            {
                Debug.LogWarning(
                    $"[VerticalSliceEndScreen] Cena '{targetScene}' não encontrada nos Build Profiles. " +
                    $"Recarregando cena ativa '{activeScene.name}'.");
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            // Fallback gracioso caso a cena ainda não esteja adicionada aos Build Profiles durante testes no Editor
            if (gameManager != null)
            {
                Debug.LogWarning(
                    $"[VerticalSliceEndScreen] Cena '{targetScene}' não encontrada nos Build Profiles. " +
                    "Reiniciando sequência diretamente via VerticalSliceGameManager.");
                SetPanelVisible(false);
                gameManager.StartVerticalSlice();
            }
        }

        private void HandleAllCasesCompleted(int correctVerdicts, int totalCases)
        {
            Show(correctVerdicts, totalCases);
        }

        private void UnbindFromGameManager()
        {
            if (_isBound && gameManager != null)
            {
                gameManager.OnAllCasesCompleted -= HandleAllCasesCompleted;
            }

            _isBound = false;
        }

        private void SetPanelVisible(bool visible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }
        }

        private void BuildOverlayCanvas()
        {
            if (_overlayCanvas != null)
            {
                return;
            }

            GameObject canvasObj = new GameObject("VerticalSlice_EndScreenCanvas");
            canvasObj.transform.SetParent(transform, false);

            _overlayCanvas = canvasObj.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Fundo escurecido em tela cheia
            _panelRoot = new GameObject("EndScreen_Backdrop");
            _panelRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform backdropRect = _panelRoot.AddComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            Image backdropImage = _panelRoot.AddComponent<Image>();
            backdropImage.color = ColorBackdrop;

            // Moldura externa mogno (#2E231E)
            GameObject borderObj = new GameObject("EndScreen_CardBorder");
            borderObj.transform.SetParent(_panelRoot.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(920f, 420f);

            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.color = ColorPanelBorder;

            // Painel central em papel certidão (#DFD5C0)
            GameObject cardObj = new GameObject("EndScreen_Card");
            cardObj.transform.SetParent(borderObj.transform, false);
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = new Vector2(10f, 10f);
            cardRect.offsetMax = new Vector2(-10f, -10f);

            Image cardImage = cardObj.AddComponent<Image>();
            cardImage.color = ColorPanelBg;

            // Título principal
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.08f, 0.55f);
            titleRect.anchorMax = new Vector2(0.92f, 0.88f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            ConfigureUiText(titleTmp, CompletionTitleText, 38f, FontStyles.Bold, ColorInkText);

            // Subtítulo com resumo
            GameObject summaryObj = new GameObject("SummaryText");
            summaryObj.transform.SetParent(cardObj.transform, false);
            RectTransform summaryRect = summaryObj.AddComponent<RectTransform>();
            summaryRect.anchorMin = new Vector2(0.08f, 0.36f);
            summaryRect.anchorMax = new Vector2(0.92f, 0.54f);
            summaryRect.offsetMin = Vector2.zero;
            summaryRect.offsetMax = Vector2.zero;

            _summaryText = summaryObj.AddComponent<TextMeshProUGUI>();
            ConfigureUiText(_summaryText, "Setor de Triagem 7 — Expediente encerrado.", 24f, FontStyles.Normal, ColorInkText);

            // Botão "Reiniciar Sandbox"
            GameObject buttonObj = new GameObject("RestartSandboxButton");
            buttonObj.transform.SetParent(cardObj.transform, false);
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.12f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.12f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(360f, 76f);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = ColorButtonBg;

            Button restartButton = buttonObj.AddComponent<Button>();
            restartButton.targetGraphic = buttonImage;
            restartButton.onClick.AddListener(RestartSandbox);

            GameObject btnTextObj = new GameObject("ButtonText");
            btnTextObj.transform.SetParent(buttonObj.transform, false);
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
            ConfigureUiText(btnTmp, "Reiniciar Sandbox", 28f, FontStyles.Bold, ColorPanelBg);
        }

        private void ConfigureUiText(
            TextMeshProUGUI tmp,
            string content,
            float fontSize,
            FontStyles style,
            Color color)
        {
            if (customFontAsset != null)
            {
                tmp.font = customFontAsset;
            }
            else if (gameManager != null && gameManager.CustomFontAsset != null)
            {
                tmp.font = gameManager.CustomFontAsset;
            }
            else if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.color = color;
        }

        private static void EnsureEventSystemExists()
        {
            EventSystem current = EventSystem.current != null
                ? EventSystem.current
                : FindFirstObjectByType<EventSystem>();

            if (current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                current = esObj.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (current.GetComponent<InputSystemUIInputModule>() == null)
            {
                StandaloneInputModule legacyModule = current.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                {
                    Destroy(legacyModule);
                }

                current.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (current.GetComponent<StandaloneInputModule>() == null)
            {
                current.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }
}
