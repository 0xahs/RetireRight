namespace RetireRight.Core.DTOs;

/// <summary>
/// A per-stock investment memo. Deterministic — every line cites a metric key
/// or is flagged as opinion. Generated from computed metrics; no LLM involvement.
/// </summary>
public record StockMemoDto(
    string Symbol,
    string Verdict,
    DateTime GeneratedAt,
    DateTime? DataAsOf,
    string SourceConfidence,           // "high" / "partial" / "low"
    int MetricsPopulated,
    int MetricsTotal,
    MemoSection Snapshot,
    MemoSection Thesis,
    MemoSection Risks,
    MemoSection Valuation,
    MemoSection VerdictSection,
    ClaudeEnvelopeDto ClaudeEnvelope);

public record MemoSection(
    string Title,
    MemoLine[] Lines);

/// <summary>
/// A single memo sentence. <see cref="Citations"/> lists the metric keys that
/// back the claim — when empty, <see cref="IsOpinion"/> is true and the line
/// must be rendered with an "Opinion" marker.
/// </summary>
public record MemoLine(
    string Text,
    string[] Citations,
    bool IsOpinion);

/// <summary>
/// Compact JSON envelope optimised for pasting into a Claude conversation.
/// Contains the essential decision signal without the full metric dictionary.
/// </summary>
public record ClaudeEnvelopeDto(
    string Json,
    int EstimatedTokens,
    int SizeBytes);
