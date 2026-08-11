// Aliased with a distinct name: the sibling namespaces Caf.Midden.Core.Services.Configuration and
// Caf.Midden.Core.Services.Metadata win simple-name resolution against the model types, and an
// alias of the same name would not.
using AppConfiguration = Caf.Midden.Core.Models.v0_2.Configuration;

namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// Validates a single model instance. Implementations must be pure and side-effect free so the
/// Blazor editors can call them freely during rendering.
/// </summary>
/// <typeparam name="T">The model being validated.</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Produces every finding for <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The instance to validate.</param>
    /// <param name="configuration">
    /// The active app configuration, used for vocabulary checks (zones, roles, tags, and so on).
    /// Optional: when null, or when a given vocabulary list is empty, those checks are skipped
    /// rather than reported as failures.
    /// </param>
    ValidationResult Validate(T model, AppConfiguration? configuration = null);
}
