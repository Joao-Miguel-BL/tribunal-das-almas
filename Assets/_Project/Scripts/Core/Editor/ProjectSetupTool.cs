using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TribunalDasAlmas.Core.Editor
{
    /// <summary>
    /// Ferramentas de configuração inicial do projeto "O Tribunal das Almas (Setor de Triagem 7)".
    /// </summary>
    public static class ProjectSetupTool
    {
        private const string TagManagerAssetPath = "ProjectSettings/TagManager.asset";
        private const string SortingLayersPropertyName = "m_SortingLayers";
        private const string LogPrefix = "[ProjectSetupTool]";

        private static readonly string[] RequiredSortingLayers =
        {
            "00_Background_Booth",
            "10_DeskSurface",
            "20_DeskProps_Fixed",
            "30_DraggableDocs",
            "40_ActiveTool_VFX"
        };

        private readonly struct SortingLayerEntry
        {
            public readonly string Name;
            public readonly long UniqueId;
            public readonly bool Locked;

            public SortingLayerEntry(string name, long uniqueId, bool locked)
            {
                Name = name;
                UniqueId = uniqueId;
                Locked = locked;
            }
        }

        [MenuItem("Tools/1. Configurar Sorting Layers")]
        public static void ConfigureSortingLayers()
        {
            UnityEngine.Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath(TagManagerAssetPath);
            if (tagManagerAssets == null || tagManagerAssets.Length == 0 || tagManagerAssets[0] == null)
            {
                Debug.LogError($"{LogPrefix} Não foi possível carregar '{TagManagerAssetPath}'.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            tagManager.Update();

            SerializedProperty sortingLayersProp = tagManager.FindProperty(SortingLayersPropertyName);
            if (sortingLayersProp == null || !sortingLayersProp.isArray)
            {
                Debug.LogError($"{LogPrefix} Propriedade '{SortingLayersPropertyName}' não encontrada em '{TagManagerAssetPath}'.");
                return;
            }

            HashSet<long> usedUniqueIds = new HashSet<long> { 0L };
            Dictionary<string, int> existingLayerIndices = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                SerializedProperty element = sortingLayersProp.GetArrayElementAtIndex(i);
                string layerName = GetLayerName(element);
                long uniqueId = GetLayerUniqueId(element);

                usedUniqueIds.Add(uniqueId);

                if (!string.IsNullOrEmpty(layerName) && !existingLayerIndices.ContainsKey(layerName))
                {
                    existingLayerIndices.Add(layerName, i);
                }
            }

            List<string> createdLayers = new List<string>();
            List<string> preservedLayers = new List<string>();

            foreach (string requiredLayer in RequiredSortingLayers)
            {
                if (existingLayerIndices.ContainsKey(requiredLayer))
                {
                    preservedLayers.Add(requiredLayer);
                    continue;
                }

                int newIndex = sortingLayersProp.arraySize;
                sortingLayersProp.InsertArrayElementAtIndex(newIndex);

                SerializedProperty newElement = sortingLayersProp.GetArrayElementAtIndex(newIndex);
                long newUniqueId = GenerateUniqueLayerId(usedUniqueIds);
                usedUniqueIds.Add(newUniqueId);

                SetLayerElement(newElement, requiredLayer, newUniqueId, false);
                existingLayerIndices[requiredLayer] = newIndex;
                createdLayers.Add(requiredLayer);
            }

            bool orderAdjusted = EnsureRequiredLayersOrder(sortingLayersProp);
            bool hasChanges = createdLayers.Count > 0 || orderAdjusted;

            if (hasChanges)
            {
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }

            LogResult(createdLayers, preservedLayers, orderAdjusted, sortingLayersProp);
        }

        /// <summary>
        /// Garante que as Sorting Layers obrigatórias fiquem na ordem exata definida em <see cref="RequiredSortingLayers"/>,
        /// preservando seus uniqueIDs originais e sem remover ou sobrescrever outras camadas existentes.
        /// </summary>
        private static bool EnsureRequiredLayersOrder(SerializedProperty sortingLayersProp)
        {
            List<int> targetSlots = new List<int>(RequiredSortingLayers.Length);
            Dictionary<string, SortingLayerEntry> requiredEntries = new Dictionary<string, SortingLayerEntry>(StringComparer.Ordinal);
            HashSet<string> requiredSet = new HashSet<string>(RequiredSortingLayers, StringComparer.Ordinal);

            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                SerializedProperty element = sortingLayersProp.GetArrayElementAtIndex(i);
                string name = GetLayerName(element);

                if (requiredSet.Contains(name) && !requiredEntries.ContainsKey(name))
                {
                    targetSlots.Add(i);
                    requiredEntries.Add(name, new SortingLayerEntry(
                        name,
                        GetLayerUniqueId(element),
                        GetLayerLocked(element)));
                }
            }

            if (targetSlots.Count != RequiredSortingLayers.Length)
            {
                return false;
            }

            bool needsReordering = false;
            for (int i = 0; i < RequiredSortingLayers.Length; i++)
            {
                string currentNameAtSlot = GetLayerName(sortingLayersProp.GetArrayElementAtIndex(targetSlots[i]));
                if (!string.Equals(currentNameAtSlot, RequiredSortingLayers[i], StringComparison.Ordinal))
                {
                    needsReordering = true;
                    break;
                }
            }

            if (!needsReordering)
            {
                return false;
            }

            for (int i = 0; i < RequiredSortingLayers.Length; i++)
            {
                SortingLayerEntry entry = requiredEntries[RequiredSortingLayers[i]];
                SerializedProperty slotElement = sortingLayersProp.GetArrayElementAtIndex(targetSlots[i]);
                SetLayerElement(slotElement, entry.Name, entry.UniqueId, entry.Locked);
            }

            return true;
        }

        private static long GenerateUniqueLayerId(HashSet<long> usedUniqueIds)
        {
            long candidate;
            do
            {
                // Gera um identificador positivo dentro de Int32 para total compatibilidade com a serialização da Unity.
                candidate = Guid.NewGuid().GetHashCode() & 0x7FFFFFFF;
            }
            while (candidate == 0L || usedUniqueIds.Contains(candidate));

            return candidate;
        }

        private static string GetLayerName(SerializedProperty element)
        {
            SerializedProperty nameProp = element.FindPropertyRelative("name");
            return nameProp != null ? nameProp.stringValue : string.Empty;
        }

        private static long GetLayerUniqueId(SerializedProperty element)
        {
            SerializedProperty idProp = element.FindPropertyRelative("uniqueID");
            return idProp != null ? idProp.longValue : 0L;
        }

        private static bool GetLayerLocked(SerializedProperty element)
        {
            SerializedProperty lockedProp = element.FindPropertyRelative("locked");
            if (lockedProp == null)
            {
                return false;
            }

            return lockedProp.propertyType == SerializedPropertyType.Boolean
                ? lockedProp.boolValue
                : lockedProp.intValue != 0;
        }

        private static void SetLayerElement(SerializedProperty element, string name, long uniqueId, bool locked)
        {
            SerializedProperty nameProp = element.FindPropertyRelative("name");
            if (nameProp != null)
            {
                nameProp.stringValue = name;
            }

            SerializedProperty idProp = element.FindPropertyRelative("uniqueID");
            if (idProp != null)
            {
                idProp.longValue = uniqueId;
            }

            SerializedProperty lockedProp = element.FindPropertyRelative("locked");
            if (lockedProp != null)
            {
                if (lockedProp.propertyType == SerializedPropertyType.Boolean)
                {
                    lockedProp.boolValue = locked;
                }
                else
                {
                    lockedProp.intValue = locked ? 1 : 0;
                }
            }
        }

        private static void LogResult(
            List<string> createdLayers,
            List<string> preservedLayers,
            bool orderAdjusted,
            SerializedProperty sortingLayersProp)
        {
            List<string> finalOrder = new List<string>(sortingLayersProp.arraySize);
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                finalOrder.Add(GetLayerName(sortingLayersProp.GetArrayElementAtIndex(i)));
            }

            if (createdLayers.Count == 0 && !orderAdjusted)
            {
                Debug.Log(
                    $"{LogPrefix} Todas as Sorting Layers obrigatórias já estavam configuradas na ordem correta. Nenhuma alteração necessária.\n" +
                    $"Ordem atual: {string.Join(" -> ", finalOrder)}");
                return;
            }

            string createdSummary = createdLayers.Count > 0 ? string.Join(", ", createdLayers) : "nenhuma";
            string preservedSummary = preservedLayers.Count > 0 ? string.Join(", ", preservedLayers) : "nenhuma";
            string orderSummary = orderAdjusted ? "Reordenada preservando uniqueIDs existentes" : "Mantida";

            Debug.Log(
                $"{LogPrefix} Sorting Layers configuradas com sucesso!\n" +
                $"- Adicionadas: {createdSummary}\n" +
                $"- Preservadas (já existentes): {preservedSummary}\n" +
                $"- Ordem relativa: {orderSummary}\n" +
                $"- Ordem final no TagManager: {string.Join(" -> ", finalOrder)}");
        }
    }
}
