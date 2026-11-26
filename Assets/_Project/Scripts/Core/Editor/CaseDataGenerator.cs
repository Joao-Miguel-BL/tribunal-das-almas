using System;
using System.Collections.Generic;
using TribunalDasAlmas.Core;
using UnityEditor;
using UnityEngine;

namespace TribunalDasAlmas.Core.Editor
{
    /// <summary>
    /// Ferramenta Editor para gerar ou atualizar automaticamente os 3 casos da Vertical Slice.
    /// </summary>
    public static class CaseDataGenerator
    {
        private const string CasesFolderPath = "Assets/_Project/ScriptableObjects/Cases";
        private const string LogPrefix = "[CaseDataGenerator]";

        private readonly struct CaseDefinition
        {
            public readonly string FileName;
            public readonly string SoulName;
            public readonly string Occupation;
            public readonly string CertificateSummary;
            public readonly bool HasRedactedText;
            public readonly string HiddenRedactedText;
            public readonly string RelicName;
            public readonly KarmicWeightType KarmicWeight;
            public readonly VerdictType ExpectedVerdict;
            public readonly string IntercomQuestion1;
            public readonly string IntercomAnswer1;
            public readonly string IntercomQuestion2;
            public readonly string IntercomAnswer2;
            public readonly bool IsOnboardingNoTimer;

            public CaseDefinition(
                string fileName,
                string soulName,
                string occupation,
                string certificateSummary,
                bool hasRedactedText,
                string hiddenRedactedText,
                string relicName,
                KarmicWeightType karmicWeight,
                VerdictType expectedVerdict,
                string intercomQuestion1,
                string intercomAnswer1,
                string intercomQuestion2,
                string intercomAnswer2,
                bool isOnboardingNoTimer)
            {
                FileName = fileName;
                SoulName = soulName;
                Occupation = occupation;
                CertificateSummary = certificateSummary;
                HasRedactedText = hasRedactedText;
                HiddenRedactedText = hiddenRedactedText;
                RelicName = relicName;
                KarmicWeight = karmicWeight;
                ExpectedVerdict = expectedVerdict;
                IntercomQuestion1 = intercomQuestion1;
                IntercomAnswer1 = intercomAnswer1;
                IntercomQuestion2 = intercomQuestion2;
                IntercomAnswer2 = intercomAnswer2;
                IsOnboardingNoTimer = isOnboardingNoTimer;
            }
        }

        private static readonly CaseDefinition[] VerticalSliceCases =
        {
            new CaseDefinition(
                fileName: "Case01_Zulmira.asset",
                soulName: "Zulmira Tavares",
                occupation: "Costureira",
                certificateSummary: "Falecimento natural aos 84 anos em domicílio. Dossiê civil regular, sem ocorrências criminais, pendências burocráticas ou itens retidos na triagem.",
                hasRedactedText: false,
                hiddenRedactedText: string.Empty,
                relicName: string.Empty,
                karmicWeight: KarmicWeightType.Neutral,
                expectedVerdict: VerdictType.Reencarnacao,
                intercomQuestion1: "A senhora deixou algum assunto pendente em vida?",
                intercomAnswer1: "Só a barra do vestido da minha neta por terminar, meu filho. Mas vivi em paz e sinto que ainda tenho disposição pra recomeçar.",
                intercomQuestion2: "Trouxe algum objeto pessoal ou arrependimento consigo?",
                intercomAnswer2: "Nada, moço. Vim de mãos vazias, do mesmo jeitinho que cheguei lá.",
                isOnboardingNoTimer: true),

            new CaseDefinition(
                fileName: "Case02_Valdir.asset",
                soulName: "Valdir Nogueira",
                occupation: "Maquinista",
                certificateSummary: "Laudo de acidente ferroviário no ramal sul. Após falha no sistema principal de frenagem, o maquinista permaneceu na cabine acionando manualmente o freio de emergência até a colisão, impedindo o descarrilamento sobre o viaduto.",
                hasRedactedText: false,
                hiddenRedactedText: string.Empty,
                relicName: "Relógio de Bolso Derretido",
                karmicWeight: KarmicWeightType.AltruisticLevitation,
                expectedVerdict: VerdictType.Repouso,
                intercomQuestion1: "Por que o senhor não abandonou a cabine antes do impacto?",
                intercomAnswer1: "Tinha mais de duzentos passageiros nos vagões de trás. Se eu largasse a alavanca auxiliar, o trem não parava antes da ponte.",
                intercomQuestion2: "Esse Relógio de Bolso Derretido pertencia ao senhor?",
                intercomAnswer2: "Era do meu pai, também maquinista. Parou no minuto exato em que o último vagão travou nos trilhos. Pelo menos todo mundo chegou inteiro.",
                isOnboardingNoTimer: false),

            new CaseDefinition(
                fileName: "Case03_Artur.asset",
                soulName: "Artur Mendonça",
                occupation: "Operário",
                certificateSummary: "Laudo de óbito por descarga elétrica durante furto de cabos de cobre em subestação desativada. Consta ocorrência policial registrada por subtração de patrimônio.",
                hasRedactedText: true,
                hiddenRedactedText: "Insulina adquirida ilegalmente para a filha com a venda dos cabos de cobre.",
                relicName: string.Empty,
                karmicWeight: KarmicWeightType.HeavyGuilt,
                expectedVerdict: VerdictType.Repouso,
                intercomQuestion1: "O laudo aponta furto de cobre. O senhor confirma o crime?",
                intercomAnswer1: "Confirmo, senhor... Eu sei que errei e essa culpa me esmaga até aqui, mas eu não encontrei outra saída naquela noite.",
                intercomQuestion2: "Qual foi o destino do dinheiro obtido com os cabos?",
                intercomAnswer2: "Minha filha estava sem medicação e o posto continuava sem estoque. Eu só queria garantir que ela sobrevivesse.",
                isOnboardingNoTimer: false)
        };

        [MenuItem("Tools/2. Gerar Casos Vertical Slice")]
        public static void GenerateVerticalSliceCases()
        {
            if (!EnsureFolderHierarchyExists(CasesFolderPath))
            {
                Debug.LogError($"{LogPrefix} Não foi possível criar ou validar a pasta de destino '{CasesFolderPath}'.");
                return;
            }

            List<string> createdAssets = new List<string>();
            List<string> updatedAssets = new List<string>();
            List<string> failedAssets = new List<string>();

            try
            {
                foreach (CaseDefinition definition in VerticalSliceCases)
                {
                    string assetPath = $"{CasesFolderPath}/{definition.FileName}";

                    try
                    {
                        UnityEngine.Object existingObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
                        SoulCaseData caseAsset;
                        bool isNewAsset;

                        if (existingObject == null)
                        {
                            caseAsset = ScriptableObject.CreateInstance<SoulCaseData>();
                            if (caseAsset == null)
                            {
                                throw new InvalidOperationException("ScriptableObject.CreateInstance<SoulCaseData>() retornou nulo.");
                            }

                            ApplyDefinition(caseAsset, definition);
                            AssetDatabase.CreateAsset(caseAsset, assetPath);
                            isNewAsset = true;
                        }
                        else if (existingObject is SoulCaseData existingCase)
                        {
                            caseAsset = existingCase;
                            ApplyDefinition(caseAsset, definition);
                            EditorUtility.SetDirty(caseAsset);
                            isNewAsset = false;
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Já existe um asset de tipo incompatível ({existingObject.GetType().Name}) em '{assetPath}'.");
                        }

                        if (isNewAsset)
                        {
                            createdAssets.Add(definition.FileName);
                        }
                        else
                        {
                            updatedAssets.Add(definition.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedAssets.Add(definition.FileName);
                        Debug.LogError($"{LogPrefix} Erro ao gerar '{assetPath}': {ex.Message}");
                    }
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            LogGenerationSummary(createdAssets, updatedAssets, failedAssets);

            UnityEngine.Object casesFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CasesFolderPath);
            if (casesFolder != null)
            {
                EditorGUIUtility.PingObject(casesFolder);
            }
        }

        private static void ApplyDefinition(SoulCaseData target, CaseDefinition source)
        {
            target.soulName = source.SoulName;
            target.occupation = source.Occupation;
            target.certificateSummary = source.CertificateSummary;
            target.hasRedactedText = source.HasRedactedText;
            target.hiddenRedactedText = source.HiddenRedactedText;
            target.relicName = source.RelicName;
            target.karmicWeight = source.KarmicWeight;
            target.expectedVerdict = source.ExpectedVerdict;
            target.intercomQuestion1 = source.IntercomQuestion1;
            target.intercomAnswer1 = source.IntercomAnswer1;
            target.intercomQuestion2 = source.IntercomQuestion2;
            target.intercomAnswer2 = source.IntercomAnswer2;
            target.isOnboardingNoTimer = source.IsOnboardingNoTimer;
        }

        private static bool EnsureFolderHierarchyExists(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            string[] segments = folderPath.Split('/');
            if (segments.Length == 0 || !string.Equals(segments[0], "Assets", StringComparison.Ordinal))
            {
                return false;
            }

            string currentPath = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string nextSegment = segments[i];
                if (string.IsNullOrWhiteSpace(nextSegment))
                {
                    continue;
                }

                string nextPath = $"{currentPath}/{nextSegment}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string createdGuid = AssetDatabase.CreateFolder(currentPath, nextSegment);
                    if (string.IsNullOrEmpty(createdGuid))
                    {
                        return false;
                    }
                }

                currentPath = nextPath;
            }

            return AssetDatabase.IsValidFolder(folderPath);
        }

        private static void LogGenerationSummary(
            List<string> createdAssets,
            List<string> updatedAssets,
            List<string> failedAssets)
        {
            string createdSummary = createdAssets.Count > 0 ? string.Join(", ", createdAssets) : "nenhum";
            string updatedSummary = updatedAssets.Count > 0 ? string.Join(", ", updatedAssets) : "nenhum";

            if (failedAssets.Count > 0)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Geração concluída com avisos.\n" +
                    $"- Criados: {createdSummary}\n" +
                    $"- Atualizados (GUID preservado): {updatedSummary}\n" +
                    $"- Falhas: {string.Join(", ", failedAssets)}");
                return;
            }

            Debug.Log(
                $"{LogPrefix} Casos da Vertical Slice gerados com sucesso em '{CasesFolderPath}'!\n" +
                $"- Criados: {createdSummary}\n" +
                $"- Atualizados (GUID preservado): {updatedSummary}");
        }
    }
}
