using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services.Validation;
using Caf.Midden.Wasm.Shared.Validation;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string DraftKey = "midden.draft.configuration.v1";

        private static readonly JsonSerializerOptions DraftPayloadJsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly ConfigurationValidator Validator = new();

        private IDisposable? _stateSubscription;
        private IAutosaveRegistration? _autosaveRegistration;
        private DateTime? _lastSavedUtc;
        private DraftEnvelope<Configuration>? _pendingDraft;
        private bool _draftRestorePromptVisible;
        private ValidationResult _validation = ValidationResult.Empty;
        private ValidationGate? _validationGate;

        [Parameter]
        public bool isLoading { get; set; } = false;

        private Configuration? ConfigurationEdit { get; set; }

        private string AutosaveStatusText => _lastSavedUtc is null
            ? string.Empty
            : $"All changes saved \u00b7 {FormatRelativeTime(_lastSavedUtc.Value)}";

        private string DraftRestorePromptText => _pendingDraft is null
            ? string.Empty
            : $"A saved draft from {FormatRelativeTime(_pendingDraft.SavedAtUtc)} was found. Resume editing it?";

        private IEnumerable<ValidationIssue> GeometryIssues(int index) =>
            _validation.Issues.Where(i =>
                i.Path.StartsWith(
                    $"configuration.geometries[{index}]",
                    StringComparison.Ordinal));

        private void RefreshValidation()
        {
            _validation = ConfigurationEdit is null
                ? ValidationResult.Empty
                : Validator.Validate(ConfigurationEdit);
        }

        private async Task OnStateChanged(Services.AppStateChangedEventArgs args)
        {
            ConfigurationEdit = State.AppConfig;
            RefreshValidation();
            await InvokeAsync(StateHasChanged);
        }

        protected override void OnInitialized()
        {
            ConfigurationEdit = State.AppConfig;
            RefreshValidation();

            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                Services.AppStateChange.AppConfig);
        }

        protected override async Task OnInitializedAsync()
        {
            await Autosave.EnsureUnloadFlushRegisteredAsync();

            _autosaveRegistration = Autosave.RegisterAutosave(
                DraftKey,
                BuildDraftSnapshotJson,
                TimeSpan.FromMilliseconds(400),
                TimeSpan.FromSeconds(20));

            Autosave.Saved += OnAutosaveSaved;

            // The restore prompt is a declarative <Modal> in this component's own markup,
            // driven purely by component state, so it can be decided here without depending
            // on render/navigation timing.
            TryOfferDraftRestore();
        }

        private void OnAutosaveSaved(string key, DateTime savedAtUtc)
        {
            if (key != DraftKey)
            {
                return;
            }

            _lastSavedUtc = savedAtUtc;
            InvokeAsync(StateHasChanged);
        }

        private void OnFormFieldChanged(FieldChangedEventArgs e)
        {
            Autosave_NotifyChanged();
        }

        private void Autosave_NotifyChanged()
        {
            _autosaveRegistration?.NotifyChanged();

            // Validation is ambient and cheap here; it never gates autosave.
            RefreshValidation();
        }

        private string? BuildDraftSnapshotJson()
        {
            // While the restore prompt is open the current state is still the pre-restore
            // snapshot. Skip autosaving in this window - including the periodic fallback
            // timer - so an unanswered prompt can't clobber the real cached draft with it.
            if (_draftRestorePromptVisible)
            {
                return null;
            }

            if (ConfigurationEdit is null)
            {
                return null;
            }

            var envelope = new DraftEnvelope<Configuration>
            {
                SavedAtUtc = DateTime.UtcNow,
                IdentityFingerprint = null,
                Payload = ConfigurationEdit
            };

            return AutosaveService.SerializeEnvelope(envelope, DraftPayloadJsonOptions);
        }

        private void TryOfferDraftRestore()
        {
            if (Autosave.HasBeenPrompted(DraftKey))
            {
                return;
            }

            var draft = Autosave.TryGetDraft<Configuration>(DraftKey);
            if (draft?.Payload is null)
            {
                return;
            }

            Autosave.TryMarkPrompted(DraftKey);

            _pendingDraft = draft;
            _draftRestorePromptVisible = true;
        }

        private void OnDraftRestoreAccepted()
        {
            var payload = _pendingDraft?.Payload;

            _draftRestorePromptVisible = false;
            _pendingDraft = null;

            if (payload is null)
            {
                return;
            }

            ConfigurationEdit = payload;
            State.UpdateAppConfig(this, ConfigurationEdit);
            RefreshValidation();
        }

        private void OnDraftRestoreDeclined()
        {
            _draftRestorePromptVisible = false;
            _pendingDraft = null;

            Autosave.RemoveDraft(DraftKey);
        }

        private static string FormatRelativeTime(DateTime savedAtUtc)
        {
            var elapsed = DateTime.UtcNow - savedAtUtc;

            if (elapsed < TimeSpan.FromMinutes(1))
            {
                return "just now";
            }

            if (elapsed < TimeSpan.FromHours(1))
            {
                var minutes = (int)elapsed.TotalMinutes;
                return $"{minutes} minute{(minutes == 1 ? string.Empty : "s")} ago";
            }

            if (elapsed < TimeSpan.FromDays(1))
            {
                var hours = (int)elapsed.TotalHours;
                return $"{hours} hour{(hours == 1 ? string.Empty : "s")} ago";
            }

            var days = (int)elapsed.TotalDays;
            return $"{days} day{(days == 1 ? string.Empty : "s")} ago";
        }

        private void NewConfigurationEdit()
        {
            ConfigurationEdit = new Configuration();
            State.UpdateAppConfig(this, ConfigurationEdit);
            Autosave.RemoveDraft(DraftKey);
            RefreshValidation();
        }

        private void AddGeometry()
        {
            ConfigurationEdit?.Geometries.Add(new Geometry());
            Autosave_NotifyChanged();
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
            Autosave_NotifyChanged();
        }

        private void OnGeometryDragEnd()
        {
            _dragSourceGeometryIndex = null;
            _dragOverGeometryIndex = null;
        }

        private void RemoveGeometry(Geometry geometry)
        {
            ConfigurationEdit?.Geometries.Remove(geometry);
            Autosave_NotifyChanged();
        }

        // The Download button stays enabled; the gate decides whether the save actually happens so
        // the user always learns *why* nothing downloaded, and where the problem is.
        private async Task RequestDownload()
        {
            if (ConfigurationEdit is null || _validationGate is null)
            {
                return;
            }

            RefreshValidation();

            await _validationGate.RequestAsync(_validation);
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

            Autosave.RemoveDraft(DraftKey);

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
                    Autosave.RemoveDraft(DraftKey);
                    RefreshValidation();
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
            Autosave.Saved -= OnAutosaveSaved;
            _autosaveRegistration?.Dispose();
            _stateSubscription?.Dispose();
        }
    }
}
