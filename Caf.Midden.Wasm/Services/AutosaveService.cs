using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Services;

/// <summary>
/// Coordinates autosaving in-progress editor drafts to browser localStorage so that
/// progress isn't lost on accidental reload/tab close, and restoring them on return.
///
/// Persistence is triggered by a combination of:
///  - A short debounce after a change is reported via <see cref="IAutosaveRegistration.NotifyChanged"/>.
///  - A periodic fallback timer, to catch mutations that don't route through NotifyChanged.
///  - A flush on tab close / visibility change (see <see cref="FlushAll"/>), registered once via JS interop.
/// </summary>
public sealed class AutosaveService : IAsyncDisposable
{
    private static readonly JsonSerializerOptions EnvelopeJsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IJSRuntime _js;
    private readonly IJSInProcessRuntime? _jsInProcess;
    private readonly Dictionary<string, AutosaveRegistration> _registrations = new();
    private readonly HashSet<string> _promptedKeys = new();
    private DotNetObjectReference<AutosaveService>? _selfRef;
    private bool _unloadHandlerRegistered;

    public event Action<string, DateTime>? Saved;

    public AutosaveService(IJSRuntime js)
    {
        _js = js;
        _jsInProcess = js as IJSInProcessRuntime;
    }

    /// <summary>
    /// Registers the global beforeunload/visibilitychange flush handler. Safe to call
    /// from multiple components; only registers once per app session.
    /// </summary>
    public async Task EnsureUnloadFlushRegisteredAsync()
    {
        if (_unloadHandlerRegistered)
        {
            return;
        }

        _selfRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("autosaveInterop.registerUnloadFlush", _selfRef);
        _unloadHandlerRegistered = true;
    }

    /// <summary>
    /// Registers a draft autosave for the given key. <paramref name="getSnapshotJson"/> is
    /// invoked to build the current draft envelope JSON whenever a save is triggered.
    /// </summary>
    public IAutosaveRegistration RegisterAutosave(
        string key,
        Func<string?> getSnapshotJson,
        TimeSpan debounce,
        TimeSpan periodic)
    {
        var registration = new AutosaveRegistration(this, key, getSnapshotJson, debounce, periodic);
        _registrations[key] = registration;
        return registration;
    }

    internal void Unregister(string key, AutosaveRegistration registration)
    {
        if (_registrations.TryGetValue(key, out var current) && ReferenceEquals(current, registration))
        {
            _registrations.Remove(key);
        }
    }

    internal void SaveDraft(string key, string? json)
    {
        if (_jsInProcess is null || string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            _jsInProcess.InvokeVoid("autosaveInterop.setItem", key, json);
            Saved?.Invoke(key, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            // Storage quota exceeded or another interop issue - fail silently
            // rather than surfacing a raw error to the user.
            try
            {
                _jsInProcess.InvokeVoid("console.warn", $"Autosave failed for key '{key}': {ex.Message}");
            }
            catch
            {
                // Ignore secondary failures logging the failure.
            }
        }
    }

    public DraftEnvelope<T>? TryGetDraft<T>(string key)
    {
        if (_jsInProcess is null)
        {
            return null;
        }

        try
        {
            var json = _jsInProcess.Invoke<string?>("autosaveInterop.getItem", key);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<DraftEnvelope<T>>(json, EnvelopeJsonOptions);
        }
        catch (Exception ex)
        {
            // Malformed/incompatible cached draft (e.g. from an older schema version) - discard,
            // but log so real bugs (vs. genuinely missing/stale drafts) are diagnosable.
            try
            {
                _jsInProcess.InvokeVoid("console.warn", $"Autosave TryGetDraft failed for key '{key}': {ex}");
            }
            catch
            {
                // Ignore secondary failures logging the failure.
            }

            return null;
        }
    }

    public void RemoveDraft(string key)
    {
        if (_jsInProcess is null)
        {
            return;
        }

        try
        {
            _jsInProcess.InvokeVoid("autosaveInterop.removeItem", key);
        }
        catch
        {
            // Ignore.
        }
    }

    public static string SerializeEnvelope<T>(DraftEnvelope<T> envelope, JsonSerializerOptions payloadOptions)
    {
        // Serialize the payload with the caller's options (e.g. enum converters) first,
        // then wrap it, so the envelope itself stays simple/stable across schema versions.
        var payloadJson = JsonSerializer.SerializeToElement(envelope.Payload, payloadOptions);

        var wrapper = new DraftEnvelope<JsonElement>
        {
            SchemaVersion = envelope.SchemaVersion,
            SavedAtUtc = envelope.SavedAtUtc,
            IdentityFingerprint = envelope.IdentityFingerprint,
            Payload = payloadJson
        };

        return JsonSerializer.Serialize(wrapper, EnvelopeJsonOptions);
    }

    /// <summary>
    /// Marks a key as having actually been prompted-about (i.e. a draft was found and offered
    /// for restore) in this app session, returning true the first time (caller should prompt),
    /// false thereafter (caller should skip prompting again).
    /// </summary>
    public bool TryMarkPrompted(string key) => _promptedKeys.Add(key);

    /// <summary>
    /// Returns true if a key has already been prompted-about in this app session, without
    /// marking it. Useful to check-before-committing when draft existence is unknown up front.
    /// </summary>
    public bool HasBeenPrompted(string key) => _promptedKeys.Contains(key);

    [JSInvokable]
    public Task FlushAll()
    {
        foreach (var registration in _registrations.Values.ToList())
        {
            registration.FlushNow();
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();

        foreach (var registration in _registrations.Values.ToList())
        {
            registration.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}

public interface IAutosaveRegistration : IDisposable
{
    /// <summary>Signals that a change occurred; schedules a debounced save.</summary>
    void NotifyChanged();

    /// <summary>Immediately persists the current snapshot.</summary>
    void FlushNow();
}

internal sealed class AutosaveRegistration : IAutosaveRegistration
{
    private readonly AutosaveService _owner;
    private readonly string _key;
    private readonly Func<string?> _getSnapshotJson;
    private readonly System.Threading.Timer _debounceTimer;
    private readonly System.Threading.Timer _periodicTimer;
    private readonly TimeSpan _debounceDueTime;
    private bool _disposed;

    public AutosaveRegistration(
        AutosaveService owner,
        string key,
        Func<string?> getSnapshotJson,
        TimeSpan debounce,
        TimeSpan periodic)
    {
        _owner = owner;
        _key = key;
        _getSnapshotJson = getSnapshotJson;
        _debounceDueTime = debounce;

        _debounceTimer = new System.Threading.Timer(_ => FlushNow(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _periodicTimer = new System.Threading.Timer(_ => FlushNow(), null, periodic, periodic);
    }

    public void NotifyChanged()
    {
        if (_disposed)
        {
            return;
        }

        _debounceTimer.Change(_debounceDueTime, Timeout.InfiniteTimeSpan);
    }

    public void FlushNow()
    {
        if (_disposed)
        {
            return;
        }

        var json = _getSnapshotJson();
        _owner.SaveDraft(_key, json);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _debounceTimer.Dispose();
        _periodicTimer.Dispose();
        _owner.Unregister(_key, this);
    }
}
