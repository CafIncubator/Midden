using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class ConfigurationEditor : ComponentBase, IDisposable
    {
        private IDisposable? _stateSubscription;

        [Parameter]
        public bool isLoading { get; set; } = false;

        private Configuration? ConfigurationEdit { get; set; }

        private async Task OnStateChanged(Services.AppStateChangedEventArgs args)
        {
            ConfigurationEdit = State.AppConfig;
            await InvokeAsync(StateHasChanged);
        }

        protected override void OnInitialized()
        {
            ConfigurationEdit = State.AppConfig;

            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                Services.AppStateChange.AppConfig);
        }

        private void NewConfigurationEdit()
        {
            ConfigurationEdit = new Configuration();
            State.UpdateAppConfig(this, ConfigurationEdit);
        }

        private void AddGeometry()
        {
            ConfigurationEdit?.Geometries.Add(new Geometry());
        }

        private int? _dragSourceGeometryIndex;
        private int? _dragOverGeometryIndex;

        private void OnGeometryDragStart(int index)
        {
            _dragSourceGeometryIndex = index;
        }

        private void OnGeometryDragEnter(int index)
        {
            _dragOverGeometryIndex = index;
        }

        private void OnGeometryDrop(int index)
        {
            _dragOverGeometryIndex = null;

            if (ConfigurationEdit is null
                || _dragSourceGeometryIndex is null
                || _dragSourceGeometryIndex.Value == index)
            {
                _dragSourceGeometryIndex = null;
                return;
            }

            var geometries = ConfigurationEdit.Geometries;
            var item = geometries[_dragSourceGeometryIndex.Value];
            geometries.RemoveAt(_dragSourceGeometryIndex.Value);

            var insertAt = _dragSourceGeometryIndex.Value < index ? index - 1 : index;
            geometries.Insert(insertAt, item);

            _dragSourceGeometryIndex = null;
        }

        private void OnGeometryDragEnd()
        {
            _dragSourceGeometryIndex = null;
            _dragOverGeometryIndex = null;
        }

        private void RemoveGeometry(Geometry geometry)
        {
            ConfigurationEdit?.Geometries.Remove(geometry);
        }

        private async Task<string> SaveConfiguration()
        {
            if (ConfigurationEdit is null)
            {
                return string.Empty;
            }

            State.UpdateAppConfig(this, ConfigurationEdit);

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string jsonString = JsonSerializer.Serialize(ConfigurationEdit, options);

            var buffer = Encoding.UTF8.GetBytes(jsonString);
            var stream = new MemoryStream(buffer);
            var fileBytes = stream.ToArray();

            await JS.InvokeAsync<string>(
                "saveAsFile",
                "app-config.json",
                Convert.ToBase64String(fileBytes));

            return jsonString;
        }

        private async Task OnInputFileConfigurationChange(
            InputFileChangeEventArgs e)
        {
            isLoading = true;

            if (e.FileCount != 1)
            {
                isLoading = false;
                return;
            }

            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                };

                string fileString;
                using (var sr = new StreamReader(e.File.OpenReadStream(), Encoding.UTF8))
                {
                    fileString = await sr.ReadToEndAsync();
                }

                var configuration = JsonSerializer.Deserialize<Configuration>(fileString, options);
                if (configuration is not null)
                {
                    ConfigurationEdit = configuration;
                    State.UpdateAppConfig(this, configuration);
                }
            }
            catch
            {
                // TODO: Indicate error state
            }
            finally
            {
                isLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        public void Dispose()
        {
            _stateSubscription?.Dispose();
        }
    }
}
