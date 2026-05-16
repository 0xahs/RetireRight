namespace RetireRight.Core.DTOs;

public record MacroDto(
    decimal? Kse100Level,
    decimal? Kse100ChangePct,
    decimal? SbpRate,
    decimal? PkrUsd,
    decimal? BrentCrude,
    decimal? Cpi,
    DateTime AsOf);
