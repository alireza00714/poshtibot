using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    [Range(1, 50)]
    public int TopK { get; set; } = 5;

    [Range(0d, 1d)]
    public double SimilarityThreshold { get; set; } = 0.72;

    public bool UseDirectFaqFallback { get; set; } = true;

    [Range(0d, 1d)]
    public double DirectFaqExactMatchThreshold { get; set; } = 0.92;

    [Range(0d, 1d)]
    public double VectorWeight { get; set; } = 0.55;

    [Range(0d, 1d)]
    public double KeywordWeight { get; set; } = 0.20;

    [Range(0d, 1d)]
    public double IntentWeight { get; set; } = 0.15;

    [Range(0d, 1d)]
    public double CategoryWeight { get; set; } = 0.05;

    [Range(0d, 1d)]
    public double PriorityWeight { get; set; } = 0.05;
}
