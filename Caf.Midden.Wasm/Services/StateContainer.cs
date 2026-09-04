using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Services;

public sealed class StateContainer
{
    public string AssemblyVersion { get; }

    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    public Metadata MetadataEdit { get; private set; } = CreateEmptyMetadata();

    public Project ProjectEdit { get; private set; } = new();

    public Configuration? AppConfig { get; private set; }

    public Catalog Catalog { get; private set; } = new();

    public StateContainer()
    {
        string informationalVersion = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
            ?? "0.0.0";

        int metadataSeparatorIndex = informationalVersion.IndexOf('+');
        AssemblyVersion = metadataSeparatorIndex >= 0
            ? informationalVersion[..metadataSeparatorIndex]
            : informationalVersion;
    }

    public event EventHandler<AppStateChangedEventArgs>? StateChanged;

    public IDisposable Subscribe(
        object subscriber,
        Func<AppStateChangedEventArgs, Task> onChanged,
        params AppStateChange[] interestedChanges)
    {
        void Handler(object? _, AppStateChangedEventArgs args)
        {
            if (args.IsFrom(subscriber))
            {
                return;
            }

            if (interestedChanges.Length > 0 && Array.IndexOf(interestedChanges, args.Change) < 0)
            {
                return;
            }

            _ = onChanged(args);
        }

        StateChanged += Handler;
        return new Subscription(() => StateChanged -= Handler);
    }

    public void SetLastUpdated(DateTime value, object? source = null)
    {
        LastUpdated = value;
        NotifyStateChanged(AppStateChange.LastUpdated, source);
    }

    public void SetMetadataEdit(Metadata value, object? source = null)
    {
        MetadataEdit = value ?? CreateEmptyMetadata();
        NotifyStateChanged(AppStateChange.MetadataEdit, source);
    }

    public void SetProjectEdit(Project value, object? source = null)
    {
        ProjectEdit = value ?? new Project();
        NotifyStateChanged(AppStateChange.ProjectEdit, source);
    }

    public void SetAppConfig(Configuration value, object? source = null)
    {
        AppConfig = value;
        NotifyStateChanged(AppStateChange.AppConfig, source);
    }

    public void SetCatalog(Catalog value, object? source = null)
    {
        Catalog = value ?? new Catalog();
        NotifyStateChanged(AppStateChange.Catalog, source);
    }

    public void UpdateLastUpdated(ComponentBase source, DateTime value) => SetLastUpdated(value, source);

    public void UpdateMetadataEdit(ComponentBase source, Metadata value) => SetMetadataEdit(value, source);

    public void UpdateProjectEdit(ComponentBase source, Project value) => SetProjectEdit(value, source);

    public void UpdateAppConfig(ComponentBase source, Configuration value) => SetAppConfig(value, source);

    public void UpdateCatalog(ComponentBase source, Catalog value) => SetCatalog(value, source);

    public void NotifyStateChanged(AppStateChange change, object? source = null)
        => StateChanged?.Invoke(this, new AppStateChangedEventArgs(change, source));

    private static Metadata CreateEmptyMetadata()
    {
        DateTime now = DateTime.UtcNow;
        return new Metadata
        {
            CreationDate = now,
            ModifiedDate = now,
            Dataset = new Dataset()
        };
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _isDisposed;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _dispose();
            _isDisposed = true;
        }
    }
}
