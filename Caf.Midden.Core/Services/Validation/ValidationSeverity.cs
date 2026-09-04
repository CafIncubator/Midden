namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// How much a <see cref="ValidationIssue"/> should interfere with the user.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// A suggestion. Never blocks anything; feeds the completeness meter and advisory copy.
    /// </summary>
    Info = 0,

    /// <summary>
    /// The artifact is structurally valid but the metadata quality is poor. Editors should
    /// let the user proceed after an explicit confirmation.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// The artifact would break the catalog or the file itself. Editors must block the
    /// explicit save/download action.
    /// </summary>
    Error = 2
}
