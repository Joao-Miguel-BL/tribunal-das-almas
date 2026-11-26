using UnityEngine;

namespace TribunalDasAlmas.Core
{
    /// <summary>
    /// Classificação do peso cármico registrado na balança do Setor de Triagem 7.
    /// </summary>
    public enum KarmicWeightType
    {
        Neutral = 0,
        HeavyGuilt = 1,
        AltruisticLevitation = -1
    }

    /// <summary>
    /// Veredito oficial disponível na mesa de triagem.
    /// </summary>
    public enum VerdictType
    {
        Repouso,
        Reencarnacao,
        Expurgo
    }

    /// <summary>
    /// Estrutura de dados ScriptableObject que representa um caso completo de alma para julgamento.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Case00_NovaAlma",
        menuName = "Tribunal das Almas/Soul Case Data",
        order = 1)]
    public class SoulCaseData : ScriptableObject
    {
        [Header("Identificação da Alma")]
        public string soulName;
        public string occupation;

        [Header("Certidão de Óbito e Conteúdo Oculto")]
        [TextArea(3, 8)]
        public string certificateSummary;
        public bool hasRedactedText;
        [TextArea(2, 6)]
        public string hiddenRedactedText;

        [Header("Relíquia e Balança Cármica")]
        public string relicName;
        public KarmicWeightType karmicWeight = KarmicWeightType.Neutral;

        [Header("Veredito Esperado")]
        public VerdictType expectedVerdict = VerdictType.Reencarnacao;

        [Header("Diálogo do Interfone")]
        public string intercomQuestion1;
        [TextArea(2, 5)]
        public string intercomAnswer1;
        public string intercomQuestion2;
        [TextArea(2, 5)]
        public string intercomAnswer2;

        [Header("Configuração do Caso")]
        public bool isOnboardingNoTimer;
    }
}
