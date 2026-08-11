using System;

namespace Caf.Midden.Wasm.Services;

/// <summary>
/// Envelope wrapping a cached draft written to browser localStorage, so that
/// restoring can validate schema compatibility and identify which item the
/// draft belongs to before offering to restore it.
/// </summary>
public sealed class DraftEnvelope<T>
{
    public int SchemaVersion { get; set; } = 1;

    public DateTime SavedAtUtc { get; set; }

    /// <summary>
    /// Optional fingerprint (e.g. "zone|name|project") identifying which item this
    /// draft belongs to, so a stale draft from a different item isn't offered for restore.
    /// Null/empty means the draft applies regardless of identity (e.g. app configuration).
    /// </summary>
    public string? IdentityFingerprint { get; set; }

    public T? Payload { get; set; }
}
