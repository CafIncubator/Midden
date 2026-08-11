using System;

namespace Caf.Midden.Wasm.Services;

public enum AppStateChange
{
    LastUpdated,
    MetadataEdit,
    ProjectEdit,
    AppConfig,
    Catalog
}

public sealed class AppStateChangedEventArgs : EventArgs
{
    public AppStateChangedEventArgs(AppStateChange change, object? source)
    {
        Change = change;
        Source = source;
    }

    public AppStateChange Change { get; }

    public object? Source { get; }

    public bool IsFrom(object? instance) => ReferenceEquals(Source, instance);
}