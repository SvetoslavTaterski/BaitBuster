namespace BaitBuster.DataPrep;

/// <summary>
/// Отчет за построяването на корпуса — записва се като JSON до TSV файла.
/// Служи за две неща: проследимост (какво точно е влязло в модела) и
/// материал за главата „Данни“ в дипломната работа.
/// </summary>
internal sealed record DatasetReport(
    DateTimeOffset BuiltAt,
    string NormalizationVersion,
    int TotalRowsRead,
    int TotalKept,
    int DroppedTooShort,
    int DroppedJunk,
    int DroppedDuplicate,
    int PhishingKept,
    int LegitimateKept,
    IReadOnlyList<SourceReport> Sources
);

/// <summary>Какво е допринесъл един корпус след почистването.</summary>
internal sealed record SourceReport(
    string Name,
    string PositiveClassMeaning,
    int RowsRead,
    int Kept,
    int Phishing,
    int Legitimate,
    int DroppedTooShort,
    int DroppedJunk,
    int DroppedDuplicate,
    int MedianLength
);
