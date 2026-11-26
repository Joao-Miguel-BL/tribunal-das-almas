using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TribunalDasAlmas.Core;
using TribunalDasAlmas.Mechanics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TribunalDasAlmas.Core.Editor
{
    /// <summary>
    /// Automação Editor para construir, configurar, conectar referências e salvar a cena jogável
    /// completa da Vertical Slice de "O Tribunal das Almas (Setor de Triagem 7)".
    /// </summary>
    public static class VerticalSliceSceneBuilder
    {
        private const string LogPrefix = "[VerticalSliceSceneBuilder]";
        private const string SceneFolderPath = "Assets/_Project/Scenes";
        private const string SceneAssetPath = "Assets/_Project/Scenes/Sandbox_VerticalSlice.unity";
        private const string PrefabsFolderPath = "Assets/_Project/Prefabs";
        private const string SpritesFolderPath = "Assets/_Project/Sprites/Placeholders";

        private const string LayerBackgroundBooth = "00_Background_Booth";
        private const string LayerDeskSurface = "10_DeskSurface";
        private const string LayerDeskPropsFixed = "20_DeskProps_Fixed";
        private const string LayerDraggableDocs = "30_DraggableDocs";

        // Paleta Oficial 70-20-10
        private static readonly Color ColorBoothWall = new Color32(0x34, 0x3C, 0x47, 0xFF);        // #343C47 Parede / Metálico
        private static readonly Color ColorWoodMahogany = new Color32(0x2E, 0x23, 0x1E, 0xFF);     // #2E231E Mogno Desgastado
        private static readonly Color ColorDeskFelt = new Color32(0x4A, 0x54, 0x48, 0xFF);         // #4A5448 Oliva Decadente
        private static readonly Color ColorPaperCream = new Color32(0xDF, 0xD5, 0xC0, 0xFF);       // #DFD5C0 Creme Certidão
        private static readonly Color ColorInkDark = new Color32(0x1C, 0x20, 0x26, 0xFF);          // #1C2026 Tarja / Contorno
        private static readonly Color ColorRevealingInk = new Color32(0x6C, 0x34, 0x83, 0xFF);     // #6C3483 Tinta Reveladora
        private static readonly Color ColorVioletHalo = new Color32(0xBB, 0x8F, 0xCE, 0xFF);       // #BB8FCE Halo Violeta
        private static readonly Color ColorStampRepouso = new Color32(0xD4, 0xAF, 0x37, 0xFF);     // #D4AF37 Repouso
        private static readonly Color ColorStampReencarnacao = new Color32(0x2B, 0x5B, 0x84, 0xFF);// #2B5B84 Reencarnação
        private static readonly Color ColorStampExpurgo = new Color32(0x9E, 0x2A, 0x2B, 0xFF);     // #9E2A2B Expurgo
        private static readonly Color ColorGlassTint = new Color(0.55f, 0.64f, 0.68f, 0.24f);
        private static readonly Color ColorSilhouette = new Color(0.11f, 0.13f, 0.15f, 0.85f);

        private static readonly string[] CaseAssetPaths =
        {
            "Assets/_Project/ScriptableObjects/Cases/Case01_Zulmira.asset",
            "Assets/_Project/ScriptableObjects/Cases/Case02_Valdir.asset",
            "Assets/_Project/ScriptableObjects/Cases/Case03_Artur.asset"
        };

        private sealed class PlaceholderAssets
        {
            public Sprite Square;
            public Sprite Circle;
            public Sprite PaperBordered;
            public Sprite WiredGlass;
        }

        [MenuItem("Tools/3. Montar Cena Vertical Slice")]
        public static void BuildVerticalSliceScene()
        {
            try
            {
                // Garante pré-requisitos essenciais (Sorting Layers e 3 ScriptableObjects de casos)
                ProjectSetupTool.ConfigureSortingLayers();
                List<SoulCaseData> loadedCases = LoadOrGenerateCases();

                EnsureFolderHierarchyExists(SceneFolderPath);
                EnsureFolderHierarchyExists(PrefabsFolderPath);
                EnsureFolderHierarchyExists(SpritesFolderPath);

                // Gera sprites persistentes no projeto para que nada fique Missing após salvar/reabrir
                PlaceholderAssets sprites = EnsurePersistentPlaceholderSprites();

                // Cria nova cena limpa
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // 1. Câmera Principal (Ortográfica + PixelPerfectCamera URP 2D) + Luz Global 2D + EventSystem
                Camera mainCamera = CreateMainCamera();
                CreateGlobalLight2D();
                CreateEventSystem();

                // 2. GameManager e Tela Final
                GameObject managerObj = new GameObject("_GameManager");
                VerticalSliceGameManager gameManager = managerObj.AddComponent<VerticalSliceGameManager>();
                VerticalSliceEndScreen endScreen = managerObj.AddComponent<VerticalSliceEndScreen>();

                // 3. Construção das Zonas A, B, C, D e E com a Paleta Oficial 70-20-10
                TextMeshPro quotaHeaderText = BuildZoneA(sprites, gameManager);
                BuildZoneB(sprites, gameManager, mainCamera);
                List<GameObject> previewDocs = BuildZoneC(sprites, mainCamera, loadedCases);
                BuildZoneD(sprites, gameManager, mainCamera);
                BuildZoneEBottomSlot(sprites);

                // 4. Conectar todas as referências no _GameManager e VerticalSliceEndScreen
                ConfigureGameManager(gameManager, loadedCases, quotaHeaderText, previewDocs);
                ConfigureEndScreen(endScreen, gameManager);

                // 5. Salvar a cena em Assets/_Project/Scenes/Sandbox_VerticalSlice.unity e registrar nas Build Settings
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene, SceneAssetPath);
                if (!saved)
                {
                    Debug.LogError($"{LogPrefix} Falha ao salvar a cena em '{SceneAssetPath}'.");
                    return;
                }

                AddSceneToBuildSettings(SceneAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SceneAsset savedSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneAssetPath);
                if (savedSceneAsset != null)
                {
                    EditorGUIUtility.PingObject(savedSceneAsset);
                }

                Debug.Log(
                    $"{LogPrefix} Cena '{SceneAssetPath}' montada, vinculada aos {loadedCases.Count} casos e adicionada às Build Settings com sucesso!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Erro ao montar a cena da Vertical Slice: {ex}");
            }
        }

        private static Camera CreateMainCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 0f, -10f);

            // Câmera ortográfica padrão em 16:9 sem PixelPerfectCamera/upscaleRT
            // para renderizar textos TextMeshPro nítidos em resolução nativa (1920x1080).
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 11.25f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ColorInkDark;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<UniversalAdditionalCameraData>();

            return cam;
        }

        private static void CreateGlobalLight2D()
        {
            GameObject lightObj = new GameObject("Global Light 2D");
            Light2D globalLight = lightObj.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 1f;
            globalLight.color = Color.white;
        }

        private static void CreateEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>
        /// Zona A — Fundo/Guichê (#343C47, Sorting Layer "00_Background_Booth"):
        /// parede, vidro aramado, silhueta da alma, quadro de cotas e relógio analógico de shift.
        /// </summary>
        private static TextMeshPro BuildZoneA(PlaceholderAssets sprites, VerticalSliceGameManager gameManager)
        {
            GameObject zoneA = new GameObject("ZoneA_BoothBackground");
            string boothLayer = ResolveSortingLayerOrDefault(LayerBackgroundBooth);
            string propsLayer = ResolveSortingLayerOrDefault(LayerDeskPropsFixed);

            // Parede do guichê (#343C47)
            CreateSpriteChild("Booth_Wall", zoneA.transform, new Vector3(0f, 6.75f, 1.0f), new Vector3(42f, 9.5f, 1f), sprites.Square, ColorBoothWall, boothLayer, 0);

            // Rodapé/divisória superior de madeira mogno (#2E231E)
            CreateSpriteChild("Booth_UpperTrim", zoneA.transform, new Vector3(0f, 2.2f, 0.9f), new Vector3(42f, 0.45f, 1f), sprites.Square, ColorWoodMahogany, boothLayer, 2);

            // Moldura e abertura da janela de vidro aramado
            GameObject windowRoot = new GameObject("Booth_WiredGlassWindow");
            windowRoot.transform.SetParent(zoneA.transform, false);
            windowRoot.transform.position = new Vector3(-4.2f, 6.6f, 0.8f);

            CreateSpriteChild("Window_Frame", windowRoot.transform, Vector3.zero, new Vector3(15.6f, 7.2f, 1f), sprites.Square, ColorWoodMahogany, boothLayer, 1);
            CreateSpriteChild("Window_Interior", windowRoot.transform, new Vector3(0f, 0f, -0.02f), new Vector3(14.8f, 6.4f, 1f), sprites.Square, new Color32(0x23, 0x29, 0x31, 0xFF), boothLayer, 2);

            // Silhueta da Alma (animada em inquietação pelo ShiftClockController após 90s)
            GameObject silhouetteObj = new GameObject("Soul_Silhouette");
            silhouetteObj.transform.SetParent(windowRoot.transform, false);
            silhouetteObj.transform.localPosition = new Vector3(0f, -0.8f, -0.04f);

            CreateSpriteChild("Silhouette_Head", silhouetteObj.transform, new Vector3(0f, 1.8f, 0f), new Vector3(2.2f, 2.4f, 1f), sprites.Circle, ColorSilhouette, boothLayer, 3);
            CreateSpriteChild("Silhouette_Shoulders", silhouetteObj.transform, new Vector3(0f, -0.6f, 0f), new Vector3(4.8f, 3.2f, 1f), sprites.Square, ColorSilhouette, boothLayer, 3);

            // Vidro Aramado sobre a silhueta
            SpriteRenderer glassSr = CreateSpriteChild("Window_WiredGlassOverlay", windowRoot.transform, new Vector3(0f, 0f, -0.06f), Vector3.one, sprites.WiredGlass, Color.white, boothLayer, 5);
            glassSr.drawMode = SpriteDrawMode.Tiled;
            glassSr.size = new Vector2(14.8f, 6.4f);

            CreateSpriteChild("Window_GlassTint", windowRoot.transform, new Vector3(0f, 0f, -0.05f), new Vector3(14.8f, 6.4f, 1f), sprites.Square, ColorGlassTint, boothLayer, 4);

            // Quadro de Cotas na parede direita do guichê
            GameObject quotaObj = new GameObject("Booth_QuotaBoard");
            quotaObj.transform.SetParent(zoneA.transform, false);
            quotaObj.transform.position = new Vector3(9.4f, 6.8f, 0.7f);

            CreateSpriteChild("QuotaBoard_Frame", quotaObj.transform, Vector3.zero, new Vector3(9.6f, 5.2f, 1f), sprites.Square, ColorWoodMahogany, boothLayer, 3);
            CreateSpriteChild("QuotaBoard_Slate", quotaObj.transform, new Vector3(0f, 0f, -0.02f), new Vector3(9.0f, 4.6f, 1f), sprites.Square, ColorInkDark, boothLayer, 4);

            TextMeshPro quotaText = CreateWorldTmp(
                "QuotaBoard_Text",
                quotaObj.transform,
                new Vector3(0f, 0f, -0.05f),
                new Vector2(8.6f, 4.2f),
                "SETOR DE TRIAGEM 7\nQUADRO DE COTAS\nAGUARDANDO CASO...",
                5.4f,
                TextAlignmentOptions.Center,
                Color.white,
                boothLayer,
                6,
                FontStyles.Bold,
                autoSize: true,
                minFontSize: 3.8f);

            // Relógio Analógico de Shift (Zona A) com ShiftClockController
            GameObject clockObj = new GameObject("ZoneA_ShiftClock");
            clockObj.transform.SetParent(zoneA.transform, false);
            clockObj.transform.position = new Vector3(16.8f, 7.1f, 0.5f);

            CreateSpriteChild("Clock_Rim", clockObj.transform, Vector3.zero, new Vector3(4.1f, 4.1f, 1f), sprites.Circle, ColorWoodMahogany, propsLayer, 1);
            CreateSpriteChild("Clock_Face", clockObj.transform, new Vector3(0f, 0f, -0.01f), new Vector3(3.6f, 3.6f, 1f), sprites.Circle, ColorPaperCream, propsLayer, 2);

            for (int i = 0; i < 12; i++)
            {
                float rad = i * 30f * Mathf.Deg2Rad;
                Vector3 tickPos = new Vector3(Mathf.Sin(rad) * 1.45f, Mathf.Cos(rad) * 1.45f, -0.02f);
                bool quarter = (i % 3 == 0);
                Vector3 tickScale = quarter ? new Vector3(0.14f, 0.38f, 1f) : new Vector3(0.08f, 0.22f, 1f);
                SpriteRenderer tickSr = CreateSpriteChild($"Clock_Tick_{i}", clockObj.transform, tickPos, tickScale, sprites.Square, ColorInkDark, propsLayer, 3);
                tickSr.transform.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            }

            GameObject hourPivot = new GameObject("Clock_HourHandPivot");
            hourPivot.transform.SetParent(clockObj.transform, false);
            hourPivot.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            CreateSpriteChild("HourHand", hourPivot.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.18f, 1.0f, 1f), sprites.Square, ColorInkDark, propsLayer, 4);

            GameObject minutePivot = new GameObject("Clock_MinuteHandPivot");
            minutePivot.transform.SetParent(clockObj.transform, false);
            minutePivot.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            CreateSpriteChild("MinuteHand", minutePivot.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.12f, 1.45f, 1f), sprites.Square, ColorInkDark, propsLayer, 5);

            CreateSpriteChild("Clock_CenterPin", clockObj.transform, new Vector3(0f, 0f, -0.05f), new Vector3(0.28f, 0.28f, 1f), sprites.Circle, ColorWoodMahogany, propsLayer, 6);

            TextMeshPro clockReadout = CreateWorldTmp(
                "Clock_Readout",
                clockObj.transform,
                new Vector3(0f, -2.75f, -0.05f),
                new Vector2(4.4f, 1.2f),
                "08:00",
                5.8f,
                TextAlignmentOptions.Center,
                Color.white,
                propsLayer,
                6,
                FontStyles.Bold);

            ShiftClockController clockController = clockObj.AddComponent<ShiftClockController>();
            SerializedObject clockSo = new SerializedObject(clockController);
            clockSo.FindProperty("gameManager").objectReferenceValue = gameManager;
            clockSo.FindProperty("hourHandPivot").objectReferenceValue = hourPivot.transform;
            clockSo.FindProperty("minuteHandPivot").objectReferenceValue = minutePivot.transform;
            clockSo.FindProperty("digitalReadoutText").objectReferenceValue = clockReadout;
            clockSo.FindProperty("soulSilhouetteTransform").objectReferenceValue = silhouetteObj.transform;
            clockSo.FindProperty("autoCreateVisualPlaceholders").boolValue = false;
            clockSo.ApplyModifiedPropertiesWithoutUndo();

            return quotaText;
        }

        /// <summary>
        /// Zona B — Borda Esquerda da Bancada (#2E231E) e Balança do Pesar (#343C47 / #2E231E, "20_DeskProps_Fixed").
        /// </summary>
        private static void BuildZoneB(PlaceholderAssets sprites, VerticalSliceGameManager gameManager, Camera mainCamera)
        {
            GameObject zoneB = new GameObject("ZoneB_WeighingScaleArea");
            string deskLayer = ResolveSortingLayerOrDefault(LayerDeskSurface);
            string propsLayer = ResolveSortingLayerOrDefault(LayerDeskPropsFixed);

            // Extensão esquerda da bancada em Mogno Desgastado (#2E231E)
            CreateSpriteChild("Desk_LeftBorder", zoneB.transform, new Vector3(-15.1f, -4.5f, 0.5f), new Vector3(10.2f, 13.8f, 1f), sprites.Square, ColorWoodMahogany, deskLayer, 0);

            // Estrutura da Balança do Pesar
            GameObject scaleObj = new GameObject("WeighingScale");
            scaleObj.transform.SetParent(zoneB.transform, false);
            scaleObj.transform.position = new Vector3(-15.0f, -3.6f, 0f);

            // Base Mogno (#2E231E) com detalhe metálico (#343C47)
            CreateSpriteChild("Scale_BaseWood", scaleObj.transform, new Vector3(0f, -3.4f, 0f), new Vector3(4.6f, 0.85f, 1f), sprites.Square, ColorWoodMahogany, propsLayer, 1);
            CreateSpriteChild("Scale_BasePlate", scaleObj.transform, new Vector3(0f, -2.9f, -0.01f), new Vector3(4.0f, 0.35f, 1f), sprites.Square, ColorBoothWall, propsLayer, 2);

            // Suporte vertical metálico (#343C47)
            CreateSpriteChild("Scale_Pillar", scaleObj.transform, new Vector3(0f, -0.25f, 0f), new Vector3(0.55f, 5.2f, 1f), sprites.Square, ColorBoothWall, propsLayer, 2);

            // Pivô da Haste
            GameObject beamPivot = new GameObject("Scale_BeamPivot");
            beamPivot.transform.SetParent(scaleObj.transform, false);
            beamPivot.transform.localPosition = new Vector3(0f, 2.15f, -0.02f);

            CreateSpriteChild("Scale_Beam", beamPivot.transform, Vector3.zero, new Vector3(7.4f, 0.36f, 1f), sprites.Square, ColorBoothWall, propsLayer, 3);
            CreateSpriteChild("Scale_FulcrumPin", beamPivot.transform, new Vector3(0f, 0f, -0.01f), new Vector3(0.55f, 0.55f, 1f), sprites.Circle, ColorWoodMahogany, propsLayer, 4);

            // Prato Esquerdo (com Trigger2D de aferição)
            GameObject leftPanObj = new GameObject("Scale_LeftPan_Trigger");
            leftPanObj.transform.SetParent(beamPivot.transform, false);
            leftPanObj.transform.localPosition = new Vector3(-3.1f, -1.75f, 0f);

            CreateSpriteChild("LeftPan_Chain", leftPanObj.transform, new Vector3(0f, 0.88f, 0f), new Vector3(0.12f, 1.75f, 1f), sprites.Square, ColorBoothWall, propsLayer, 2);
            CreateSpriteChild("LeftPan_Plate", leftPanObj.transform, Vector3.zero, new Vector3(2.9f, 0.38f, 1f), sprites.Square, ColorBoothWall, propsLayer, 4);

            BoxCollider2D panTrigger = leftPanObj.AddComponent<BoxCollider2D>();
            panTrigger.isTrigger = true;
            panTrigger.size = new Vector2(3.3f, 2.6f);
            panTrigger.offset = new Vector2(0f, 1.0f);

            Rigidbody2D panRb = leftPanObj.AddComponent<Rigidbody2D>();
            panRb.bodyType = RigidbodyType2D.Kinematic;
            panRb.gravityScale = 0f;

            // Prato Direito (contrapeso fixo)
            GameObject rightPanObj = new GameObject("Scale_RightPan");
            rightPanObj.transform.SetParent(beamPivot.transform, false);
            rightPanObj.transform.localPosition = new Vector3(3.1f, -1.75f, 0f);

            CreateSpriteChild("RightPan_Chain", rightPanObj.transform, new Vector3(0f, 0.88f, 0f), new Vector3(0.12f, 1.75f, 1f), sprites.Square, ColorBoothWall, propsLayer, 2);
            CreateSpriteChild("RightPan_Plate", rightPanObj.transform, Vector3.zero, new Vector3(2.9f, 0.38f, 1f), sprites.Square, ColorBoothWall, propsLayer, 4);

            CreateWorldTmp(
                "Scale_Label",
                scaleObj.transform,
                new Vector3(0f, -4.55f, -0.05f),
                new Vector2(8.0f, 1.3f),
                "BALANÇA DO PESAR",
                4.8f,
                TextAlignmentOptions.Center,
                Color.white,
                propsLayer,
                5,
                FontStyles.Bold);

            WeighingScaleController scaleController = scaleObj.AddComponent<WeighingScaleController>();
            SerializedObject scaleSo = new SerializedObject(scaleController);
            scaleSo.FindProperty("gameManager").objectReferenceValue = gameManager;
            scaleSo.FindProperty("shakeCamera").objectReferenceValue = mainCamera;
            scaleSo.FindProperty("beamPivot").objectReferenceValue = beamPivot.transform;
            scaleSo.FindProperty("leftPanTrigger").objectReferenceValue = panTrigger;
            scaleSo.FindProperty("leftPanAnchor").objectReferenceValue = leftPanObj.transform;
            scaleSo.FindProperty("rightPanAnchor").objectReferenceValue = rightPanObj.transform;
            scaleSo.FindProperty("autoCreateVisualPlaceholders").boolValue = false;
            scaleSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Zona C — Tampo de Feltro Central (#4A5448, "10_DeskSurface") e Prefab/Preview de Documento
        /// Creme Certidão (#DFD5C0) com DraggableDocument, SortingGroup e RedactedArea (#1C2026) em "30_DraggableDocs".
        /// </summary>
        private static List<GameObject> BuildZoneC(
            PlaceholderAssets sprites,
            Camera mainCamera,
            List<SoulCaseData> loadedCases)
        {
            GameObject zoneC = new GameObject("ZoneC_DeskSurface");
            string deskLayer = ResolveSortingLayerOrDefault(LayerDeskSurface);

            // Moldura da bancada (#2E231E) e Tampo de Feltro Oliva Decadente (#4A5448)
            CreateSpriteChild("Desk_CenterFrame", zoneC.transform, new Vector3(0f, -4.5f, 0.45f), new Vector3(20.8f, 13.8f, 1f), sprites.Square, ColorWoodMahogany, deskLayer, 0);
            CreateSpriteChild("Desk_FeltSurface", zoneC.transform, new Vector3(0f, -4.5f, 0.4f), new Vector3(19.6f, 12.6f, 1f), sprites.Square, ColorDeskFelt, deskLayer, 1);

            // Cria e salva Prefab reutilizável de Documento com RedactedArea em Assets/_Project/Prefabs/
            SoulCaseData sampleCase = loadedCases.Count >= 3 ? loadedCases[2] : (loadedCases.Count > 0 ? loadedCases[0] : null);
            GameObject docPrefab = CreateAndSaveDocumentPrefab(sprites, sampleCase);

            // Instancia um exemplar de preview na Zona C (removido automaticamente ao iniciar o Caso 1 em Play Mode)
            List<GameObject> previewDocs = new List<GameObject>();
            if (docPrefab != null)
            {
                GameObject previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(docPrefab);
                previewInstance.name = "Preview_CertidaoDePassamento";
                previewInstance.transform.SetParent(zoneC.transform, false);
                previewInstance.transform.position = new Vector3(-2.6f, -4.2f, 0f);

                DraggableDocument dragComp = previewInstance.GetComponent<DraggableDocument>();
                if (dragComp != null)
                {
                    SerializedObject dragSo = new SerializedObject(dragComp);
                    dragSo.FindProperty("targetCamera").objectReferenceValue = mainCamera;
                    dragSo.FindProperty("deskBoundsMin").vector2Value = new Vector2(-8.2f, -8.8f);
                    dragSo.FindProperty("deskBoundsMax").vector2Value = new Vector2(8.2f, 0.4f);
                    dragSo.ApplyModifiedPropertiesWithoutUndo();
                }

                RedactedArea redactedComp = previewInstance.GetComponentInChildren<RedactedArea>(true);
                if (redactedComp != null)
                {
                    SerializedObject redSo = new SerializedObject(redactedComp);
                    redSo.FindProperty("targetCamera").objectReferenceValue = mainCamera;
                    redSo.ApplyModifiedPropertiesWithoutUndo();
                }

                previewDocs.Add(previewInstance);
            }

            return previewDocs;
        }

        private static GameObject CreateAndSaveDocumentPrefab(PlaceholderAssets sprites, SoulCaseData sampleCase)
        {
            string docLayer = ResolveSortingLayerOrDefault(LayerDraggableDocs);
            Vector2 paperSize = new Vector2(6.8f, 8.4f);

            GameObject tempRoot = new GameObject("PF_DraggableDocument");
            SpriteRenderer paperSr = tempRoot.AddComponent<SpriteRenderer>();
            paperSr.sprite = sprites.PaperBordered;
            paperSr.drawMode = SpriteDrawMode.Sliced;
            paperSr.size = paperSize;
            paperSr.color = Color.white;
            paperSr.sortingLayerName = docLayer;
            paperSr.sortingOrder = 0;

            BoxCollider2D docCol = tempRoot.AddComponent<BoxCollider2D>();
            docCol.size = paperSize;

            SortingGroup sg = tempRoot.AddComponent<SortingGroup>();
            sg.sortingLayerName = docLayer;
            sg.sortingOrder = 1;

            DraggableDocument draggable = tempRoot.AddComponent<DraggableDocument>();
            SerializedObject dragSo = new SerializedObject(draggable);
            dragSo.FindProperty("isDeathCertificate").boolValue = true;
            dragSo.FindProperty("deskBoundsMin").vector2Value = new Vector2(-8.2f, -8.8f);
            dragSo.FindProperty("deskBoundsMax").vector2Value = new Vector2(8.2f, 0.4f);
            dragSo.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldTmp(
                "Document_Text",
                tempRoot.transform,
                new Vector3(0f, 0.7f, -0.01f),
                paperSize - new Vector2(0.75f, 2.3f),
                "<b>CERTIDÃO DE PASSAMENTO</b>\n────────────────────\nDocumento arrastável (Zona C)\nClique Esq.: Arrastar\nClique Dir.: Zoom 1.35x",
                5.2f,
                TextAlignmentOptions.TopLeft,
                ColorInkDark,
                docLayer,
                1,
                FontStyles.Normal,
                autoSize: true,
                minFontSize: 3.8f);

            // Área Tarjada (#1C2026) com halo violeta (#BB8FCE) e RedactedArea
            GameObject redactedObj = new GameObject("RedactedArea_Tarja");
            redactedObj.transform.SetParent(tempRoot.transform, false);
            redactedObj.transform.localPosition = new Vector3(0f, -2.65f, -0.02f);

            Vector2 barSize = new Vector2(5.8f, 1.75f);
            BoxCollider2D redCol = redactedObj.AddComponent<BoxCollider2D>();
            redCol.size = barSize;

            SpriteRenderer haloSr = CreateSpriteChild(
                "Redacted_VioletHalo",
                redactedObj.transform,
                new Vector3(0f, 0f, 0.004f),
                new Vector3(barSize.x + 0.3f, barSize.y + 0.3f, 1f),
                sprites.Square,
                new Color(ColorVioletHalo.r, ColorVioletHalo.g, ColorVioletHalo.b, 0f),
                docLayer,
                2);
            haloSr.enabled = false;

            SpriteRenderer barSr = CreateSpriteChild(
                "Redacted_BlackBar",
                redactedObj.transform,
                Vector3.zero,
                new Vector3(barSize.x, barSize.y, 1f),
                sprites.Square,
                ColorInkDark,
                docLayer,
                4);

            TextMeshPro hiddenTmp = CreateWorldTmp(
                "Redacted_HiddenText",
                redactedObj.transform,
                new Vector3(0f, 0f, -0.004f),
                barSize - new Vector2(0.25f, 0.15f),
                sampleCase != null && !string.IsNullOrWhiteSpace(sampleCase.hiddenRedactedText)
                    ? sampleCase.hiddenRedactedText
                    : "Trecho oculto revelado pela Tinta Reveladora.",
                4.4f,
                TextAlignmentOptions.MidlineLeft,
                ColorInkDark,
                docLayer,
                3,
                FontStyles.Bold | FontStyles.Italic,
                autoSize: true,
                minFontSize: 3.2f);
            hiddenTmp.gameObject.SetActive(false);

            RedactedArea redactedArea = redactedObj.AddComponent<RedactedArea>();
            SerializedObject redSo = new SerializedObject(redactedArea);
            redSo.FindProperty("soulCaseData").objectReferenceValue = sampleCase;
            redSo.FindProperty("redactionBarRenderer").objectReferenceValue = barSr;
            redSo.FindProperty("violetHaloRenderer").objectReferenceValue = haloSr;
            redSo.FindProperty("hiddenTextMesh").objectReferenceValue = hiddenTmp;
            redSo.ApplyModifiedPropertiesWithoutUndo();

            string prefabPath = $"{PrefabsFolderPath}/PF_DraggableDocument.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, prefabPath);
            UnityEngine.Object.DestroyImmediate(tempRoot);
            return savedPrefab;
        }

        /// <summary>
        /// Zona D — Ferramentas e Controles ("20_DeskProps_Fixed"):
        /// Tinteiro (#6C3483) + RevealingInkController, 3 Carimbos + StampController e Alavanca Pneumática + PneumaticLeverController.
        /// </summary>
        private static void BuildZoneD(PlaceholderAssets sprites, VerticalSliceGameManager gameManager, Camera mainCamera)
        {
            GameObject zoneD = new GameObject("ZoneD_ToolsAndControls");
            string deskLayer = ResolveSortingLayerOrDefault(LayerDeskSurface);
            string propsLayer = ResolveSortingLayerOrDefault(LayerDeskPropsFixed);

            // Base direita em Mogno Desgastado (#2E231E)
            CreateSpriteChild("Desk_RightToolsBase", zoneD.transform, new Vector3(15.1f, -4.5f, 0.5f), new Vector3(10.2f, 13.8f, 1f), sprites.Square, ColorWoodMahogany, deskLayer, 0);

            // 1. Frasco de Tinta Reveladora (#6C3483) com RevealingInkController
            GameObject inkObj = new GameObject("Tool_RevealingInk");
            inkObj.transform.SetParent(zoneD.transform, false);
            inkObj.transform.position = new Vector3(13.0f, -0.2f, 0f);

            BoxCollider2D inkCol = inkObj.AddComponent<BoxCollider2D>();
            inkCol.size = new Vector2(2.6f, 2.8f);

            CreateSpriteChild("Inkwell_Cap", inkObj.transform, new Vector3(0f, 1.15f, 0f), new Vector3(1.1f, 0.5f, 1f), sprites.Square, ColorBoothWall, propsLayer, 2);
            CreateSpriteChild("Inkwell_Glass", inkObj.transform, new Vector3(0f, -0.1f, 0f), new Vector3(2.2f, 2.2f, 1f), sprites.Square, ColorBoothWall, propsLayer, 1);
            SpriteRenderer inkLiquidSr = CreateSpriteChild("Inkwell_Liquid", inkObj.transform, new Vector3(0f, -0.2f, -0.01f), new Vector3(1.7f, 1.55f, 1f), sprites.Square, ColorRevealingInk, propsLayer, 2);

            TextMeshPro inkCounterTmp = CreateWorldTmp(
                "Inkwell_Counter",
                inkObj.transform,
                new Vector3(0f, -1.95f, -0.02f),
                new Vector2(4.4f, 1.4f),
                "TINTA\n3/3",
                4.4f,
                TextAlignmentOptions.Center,
                Color.white,
                propsLayer,
                4,
                FontStyles.Bold);

            RevealingInkController inkController = inkObj.AddComponent<RevealingInkController>();
            SerializedObject inkSo = new SerializedObject(inkController);
            inkSo.FindProperty("inkDrops").intValue = 3;
            inkSo.FindProperty("targetCamera").objectReferenceValue = mainCamera;
            inkSo.FindProperty("inkwellLiquidRenderer").objectReferenceValue = inkLiquidSr;
            inkSo.FindProperty("dropsCounterText").objectReferenceValue = inkCounterTmp;
            inkSo.FindProperty("autoCreateVisualPlaceholders").boolValue = false;
            inkSo.ApplyModifiedPropertiesWithoutUndo();

            // 2. Os 3 Carimbos (Repouso #D4AF37, Reencarnacao #2B5B84, Expurgo #9E2A2B) com StampController
            GameObject stampsObj = new GameObject("Tool_StampsRack");
            stampsObj.transform.SetParent(zoneD.transform, false);
            stampsObj.transform.position = new Vector3(13.0f, -6.2f, 0f);

            StampController stampController = stampsObj.AddComponent<StampController>();
            SerializedObject stampSo = new SerializedObject(stampController);
            stampSo.FindProperty("gameManager").objectReferenceValue = gameManager;
            stampSo.FindProperty("targetCamera").objectReferenceValue = mainCamera;
            stampSo.FindProperty("autoCreateVisualPlaceholders").boolValue = false;

            SerializedProperty slotsProp = stampSo.FindProperty("stampSlots");
            slotsProp.ClearArray();

            (VerdictType verdict, string label, Color color, float localY)[] stampDefs =
            {
                (VerdictType.Repouso, "REPOUSO", ColorStampRepouso, 2.2f),
                (VerdictType.Reencarnacao, "REENCARNAÇÃO", ColorStampReencarnacao, 0.0f),
                (VerdictType.Expurgo, "EXPURGO", ColorStampExpurgo, -2.2f)
            };

            for (int i = 0; i < stampDefs.Length; i++)
            {
                var def = stampDefs[i];
                GameObject sObj = new GameObject($"Stamp_{def.verdict}");
                sObj.transform.SetParent(stampsObj.transform, false);
                sObj.transform.localPosition = new Vector3(0f, def.localY, 0f);

                BoxCollider2D sCol = sObj.AddComponent<BoxCollider2D>();
                sCol.size = new Vector2(4.2f, 1.7f);

                CreateSpriteChild("Stamp_Pad", sObj.transform, new Vector3(0f, -0.22f, 0f), new Vector3(4.1f, 1.15f, 1f), sprites.Square, def.color, propsLayer, 2);
                CreateSpriteChild("Stamp_Handle", sObj.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.35f, 0.6f, 1f), sprites.Square, ColorWoodMahogany, propsLayer, 3);
                CreateWorldTmp(
                    "Stamp_Label",
                    sObj.transform,
                    new Vector3(0f, -0.22f, -0.02f),
                    new Vector2(3.9f, 1.0f),
                    def.label,
                    4.8f,
                    TextAlignmentOptions.Center,
                    Color.white,
                    propsLayer,
                    4,
                    FontStyles.Bold,
                    autoSize: true,
                    minFontSize: 3.4f);

                slotsProp.InsertArrayElementAtIndex(i);
                SerializedProperty elem = slotsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("verdict").enumValueIndex = (int)def.verdict;
                elem.FindPropertyRelative("displayLabel").stringValue = def.label;
                elem.FindPropertyRelative("stampColor").colorValue = def.color;
                elem.FindPropertyRelative("collider").objectReferenceValue = sCol;
                elem.FindPropertyRelative("visualTransform").objectReferenceValue = sObj.transform;
            }

            stampSo.ApplyModifiedPropertiesWithoutUndo();

            // 3. Alavanca Pneumática com PneumaticLeverController
            GameObject leverObj = new GameObject("Tool_PneumaticLever");
            leverObj.transform.SetParent(zoneD.transform, false);
            leverObj.transform.position = new Vector3(17.6f, -4.4f, 0f);

            BoxCollider2D leverCol = leverObj.AddComponent<BoxCollider2D>();
            leverCol.size = new Vector2(2.6f, 6.4f);

            CreateSpriteChild("Lever_Housing", leverObj.transform, Vector3.zero, new Vector3(1.9f, 6.0f, 1f), sprites.Square, ColorBoothWall, propsLayer, 1);
            CreateSpriteChild("Lever_Slot", leverObj.transform, new Vector3(0f, 0f, -0.01f), new Vector3(0.45f, 4.9f, 1f), sprites.Square, ColorInkDark, propsLayer, 2);

            GameObject leverHandle = new GameObject("Lever_Handle");
            leverHandle.transform.SetParent(leverObj.transform, false);
            leverHandle.transform.localPosition = new Vector3(0f, 2.1f, -0.02f);
            CreateSpriteChild("Handle_Knob", leverHandle.transform, Vector3.zero, new Vector3(2.4f, 0.85f, 1f), sprites.Square, ColorStampExpurgo, propsLayer, 4);

            CreateWorldTmp(
                "Lever_Label",
                leverObj.transform,
                new Vector3(0f, -3.85f, -0.02f),
                new Vector2(4.6f, 1.2f),
                "DESPACHO",
                4.5f,
                TextAlignmentOptions.Center,
                Color.white,
                propsLayer,
                5,
                FontStyles.Bold);

            PneumaticLeverController leverController = leverObj.AddComponent<PneumaticLeverController>();
            SerializedObject leverSo = new SerializedObject(leverController);
            leverSo.FindProperty("gameManager").objectReferenceValue = gameManager;
            leverSo.FindProperty("targetCamera").objectReferenceValue = mainCamera;
            leverSo.FindProperty("leverHandleTransform").objectReferenceValue = leverHandle.transform;
            leverSo.FindProperty("topLocalY").floatValue = 2.1f;
            leverSo.FindProperty("bottomLocalY").floatValue = -2.1f;
            leverSo.FindProperty("autoCreateVisualPlaceholders").boolValue = false;
            leverSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Fresta inferior de despacho (Zona E) para feedback visual de saída dos papéis.
        /// </summary>
        private static void BuildZoneEBottomSlot(PlaceholderAssets sprites)
        {
            GameObject zoneE = new GameObject("ZoneE_DispatchSlot");
            string propsLayer = ResolveSortingLayerOrDefault(LayerDeskPropsFixed);

            CreateSpriteChild("DispatchSlot_Trim", zoneE.transform, new Vector3(0f, -10.85f, 0.2f), new Vector3(12.6f, 0.85f, 1f), sprites.Square, ColorBoothWall, propsLayer, 2);
            CreateSpriteChild("DispatchSlot_Opening", zoneE.transform, new Vector3(0f, -10.85f, 0.15f), new Vector3(11.8f, 0.45f, 1f), sprites.Square, ColorInkDark, propsLayer, 3);
        }

        private static void ConfigureGameManager(
            VerticalSliceGameManager gameManager,
            List<SoulCaseData> loadedCases,
            TextMeshPro quotaHeaderText,
            List<GameObject> previewDocs)
        {
            SerializedObject gmSo = new SerializedObject(gameManager);

            SerializedProperty casesProp = gmSo.FindProperty("verticalSliceCases");
            casesProp.ClearArray();
            for (int i = 0; i < loadedCases.Count; i++)
            {
                casesProp.InsertArrayElementAtIndex(i);
                casesProp.GetArrayElementAtIndex(i).objectReferenceValue = loadedCases[i];
            }

            gmSo.FindProperty("autoStartOnPlay").boolValue = true;
            gmSo.FindProperty("autoBuildDeskPlaceholders").boolValue = false;
            gmSo.FindProperty("statusHeaderText").objectReferenceValue = quotaHeaderText;

            SerializedProperty previewProp = gmSo.FindProperty("scenePreviewObjectsToClear");
            previewProp.ClearArray();
            for (int i = 0; i < previewDocs.Count; i++)
            {
                previewProp.InsertArrayElementAtIndex(i);
                previewProp.GetArrayElementAtIndex(i).objectReferenceValue = previewDocs[i];
            }

            // Enquadramento calibrado para 640x360 @ 16 PPU (40 x 22.5 unidades)
            gmSo.FindProperty("deskBoundsMin").vector2Value = new Vector2(-8.2f, -8.8f);
            gmSo.FindProperty("deskBoundsMax").vector2Value = new Vector2(8.2f, 0.4f);
            gmSo.FindProperty("deskVisualCenter").vector2Value = new Vector2(0f, -4.5f);
            gmSo.FindProperty("deskVisualSize").vector2Value = new Vector2(19.6f, 12.6f);

            gmSo.FindProperty("certificateSize").vector2Value = new Vector2(6.8f, 8.4f);
            gmSo.FindProperty("certificateSpawnPos").vector2Value = new Vector2(-2.6f, -4.2f);
            gmSo.FindProperty("auxiliarySlipSize").vector2Value = new Vector2(5.6f, 4.6f);
            gmSo.FindProperty("auxiliarySlipSpawnPos").vector2Value = new Vector2(4.4f, -5.8f);
            gmSo.FindProperty("relicItemSize").vector2Value = new Vector2(4.6f, 2.4f);
            gmSo.FindProperty("documentFontSize").floatValue = 5.2f;

            gmSo.FindProperty("offscreenSlideOffsetX").floatValue = 16.0f;
            gmSo.FindProperty("bottomSlotExitY").floatValue = -14.5f;

            gmSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameManager);

            List<string> caseNames = new List<string>(loadedCases.Count);
            for (int i = 0; i < loadedCases.Count; i++)
            {
                caseNames.Add(loadedCases[i] != null ? loadedCases[i].name : "null");
            }
            Debug.Log($"{LogPrefix} Casos vinculados ao _GameManager.verticalSliceCases: {string.Join(", ", caseNames)}");
        }

        private static void ConfigureEndScreen(VerticalSliceEndScreen endScreen, VerticalSliceGameManager gameManager)
        {
            SerializedObject esSo = new SerializedObject(endScreen);
            esSo.FindProperty("gameManager").objectReferenceValue = gameManager;
            esSo.FindProperty("sandboxSceneName").stringValue = "Sandbox_VerticalSlice";
            esSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<SoulCaseData> LoadOrGenerateCases()
        {
            List<SoulCaseData> cases = LoadCases();
            if (cases.Count < CaseAssetPaths.Length)
            {
                Debug.Log($"{LogPrefix} Casos ausentes detectados; executando CaseDataGenerator automaticamente...");
                CaseDataGenerator.GenerateVerticalSliceCases();
                cases = LoadCases();
            }

            return cases;
        }

        private static List<SoulCaseData> LoadCases()
        {
            List<SoulCaseData> cases = new List<SoulCaseData>(CaseAssetPaths.Length);
            foreach (string path in CaseAssetPaths)
            {
                SoulCaseData data = AssetDatabase.LoadAssetAtPath<SoulCaseData>(path);
                if (data != null)
                {
                    cases.Add(data);
                }
            }

            return cases;
        }

        private static PlaceholderAssets EnsurePersistentPlaceholderSprites()
        {
            PlaceholderAssets assets = new PlaceholderAssets
            {
                Square = EnsureSquareSpriteAsset($"{SpritesFolderPath}/Placeholder_Square.png"),
                Circle = EnsureCircleSpriteAsset($"{SpritesFolderPath}/Placeholder_Circle.png"),
                PaperBordered = EnsurePaperBorderedSpriteAsset($"{SpritesFolderPath}/Placeholder_PaperBordered.png"),
                WiredGlass = EnsureWiredGlassSpriteAsset($"{SpritesFolderPath}/Placeholder_WiredGlass.png")
            };

            return assets;
        }

        private static Sprite EnsureSquareSpriteAsset(string assetPath)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            return SaveAndImportSprite(tex, assetPath, 16f, Vector4.zero, TextureWrapMode.Clamp);
        }

        private static Sprite EnsureCircleSpriteAsset(string assetPath)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            float sqrRadius = radius * radius;
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - radius;
                    float dy = (y + 0.5f) - radius;
                    byte a = (dx * dx + dy * dy) <= sqrRadius ? (byte)255 : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return SaveAndImportSprite(tex, assetPath, 32f, Vector4.zero, TextureWrapMode.Clamp);
        }

        private static Sprite EnsurePaperBorderedSpriteAsset(string assetPath)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32 fill = ColorPaperCream;
            Color32 border = ColorInkDark;
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    pixels[y * size + x] = isBorder ? border : fill;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return SaveAndImportSprite(tex, assetPath, 16f, new Vector4(1f, 1f, 1f, 1f), TextureWrapMode.Clamp);
        }

        private static Sprite EnsureWiredGlassSpriteAsset(string assetPath)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32 wire = new Color32(0x1C, 0x20, 0x26, 0x8C);
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isWire = x == 0 || y == 0 || x == y || x == (size - 1 - y);
                    pixels[y * size + x] = isWire ? wire : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return SaveAndImportSprite(tex, assetPath, 16f, Vector4.zero, TextureWrapMode.Repeat);
        }

        private static Sprite SaveAndImportSprite(
            Texture2D tex,
            string assetPath,
            float pixelsPerUnit,
            Vector4 border,
            TextureWrapMode wrapMode)
        {
            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string fullPath = Path.GetFullPath(assetPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(fullPath, pngBytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.spriteBorder = border;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = wrapMode;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static SpriteRenderer CreateSpriteChild(
            string name,
            Transform parent,
            Vector3 localPos,
            Vector3 localScale,
            Sprite sprite,
            Color color,
            string sortingLayer,
            int sortingOrder)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        private static TextMeshPro CreateWorldTmp(
            string name,
            Transform parent,
            Vector3 localPos,
            Vector2 rectSize,
            string content,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            string sortingLayer,
            int sortingOrder,
            FontStyles fontStyle = FontStyles.Normal,
            bool autoSize = false,
            float minFontSize = 2.0f)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;

            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = rectSize;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.color = color;

            if (autoSize)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = Mathf.Min(minFontSize, fontSize);
                tmp.fontSizeMax = fontSize;
            }

            if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            MeshRenderer mr = tmp.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = sortingLayer;
                mr.sortingOrder = sortingOrder;
            }

            return tmp;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;

            for (int i = 0; i < buildScenes.Count; i++)
            {
                if (string.Equals(buildScenes[i].path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    buildScenes[i].enabled = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        private static void EnsureFolderHierarchyExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }
                current = next;
            }
        }

        private static string ResolveSortingLayerOrDefault(string layerName)
        {
            return SortingLayer.NameToID(layerName) != 0 ? layerName : "Default";
        }
    }
}
