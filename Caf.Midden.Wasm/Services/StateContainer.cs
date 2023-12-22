using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;

namespace Caf.Midden.Wasm.Services
{
    public class StateContainer
    {
        public string AssemblyVersion { get; private set; }
        
        public DateTime LastUpdated { get; private set; } = 
            DateTime.UtcNow;
        public void UpdateLastUpdated(
            ComponentBase source,
            DateTime value)
        {
            this.LastUpdated = value;
            NotifyStateChanged(source, "UpdateLastUpdated");
        }


        public Metadata MetadataEdit { get; set; } = new Metadata();
        public void UpdateMetadataEdit(
            ComponentBase source,
            Metadata value)
        {
            this.MetadataEdit = value;
            NotifyStateChanged(source, "UpdateMetadataEdit");
        }

        public Project ProjectEdit { get; set; } = new Project();
        public void UpdateProjectEdit(
            ComponentBase source,
            Project value)
        {
            this.ProjectEdit = value;
            NotifyStateChanged(source, "UpdateProjectEdit");
        }

        public Configuration AppConfig { get; private set; }
        public void UpdateAppConfig(
            ComponentBase source, 
            Configuration value)
        {
            this.AppConfig = value;
            NotifyStateChanged(source, "UpdateAppConfig");
        }

        public Catalog Catalog { get; private set; } = new Catalog();
        public void UpdateCatalog(
            ComponentBase source,
            Catalog value)
        {
            this.Catalog = value;
            NotifyStateChanged(source, "UpdateCatalog");
        }

        public StateContainer()
        {
            var informationalVersion = 
                Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;

            // Added because a GUID was appended to the end for unknown reasons
            if (informationalVersion.Contains("+"))
                informationalVersion = informationalVersion.Split("+")[0];

            this.AssemblyVersion = informationalVersion;
        }

        public event Action<ComponentBase, string> StateChanged;

        public void NotifyStateChanged(
            ComponentBase source,
            string property) => StateChanged?.Invoke(source, property);
    }
}
