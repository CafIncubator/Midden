using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Caf.Midden.Wasm.Shared.Modals;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Validation;
using Caf.Midden.Wasm.Shared.Validation;

// Disambiguated from System.ComponentModel.DataAnnotations.ValidationResult, which this file also
// has in scope.
using ValidationResult = Caf.Midden.Core.Services.Validation.ValidationResult;

namespace Caf.Midden.Wasm.Shared
{
    public partial class ProjectEditor : ComponentBase, IDisposable
    {
        private const string DraftKeyPrefix = "midden.draft.project.v1";

        private static readonly JsonSerializerOptions DraftPayloadJsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly ProjectValidator Validator = new();

        private IDisposable? _stateSubscription;
        private IAutosaveRegistration? _autosaveRegistration;
        private DateTime? _lastSavedUtc;
        private string _draftKey = DraftKeyPrefix;
        private DraftEnvelope<Project>? _pendingDraft;
        private bool _draftRestorePromptVisible;
        private ValidationResult _validation = ValidationResult.Empty;
        private ValidationGate? _validationGate;

        public Project Project { get; set; } = new Project();

        [Parameter]
        public bool isLoading { get; set; } = false;

        string markdownHtml = "";

        private string AutosaveStatusText => _lastSavedUtc is null
            ? string.Empty
            : $"All changes saved \u00b7 {FormatRelativeTime(_lastSavedUtc.Value)}";

        private string DraftRestorePromptText => _pendingDraft is null
            ? string.Empty
            : $"A saved draft from {FormatRelativeTime(_pendingDraft.SavedAtUtc)} was found. Resume editing it?";

        private void RefreshValidation()
        {
            _validation = State.ProjectEdit is null
                ? ValidationResult.Empty
                : Validator.Validate(State.ProjectEdit, State.AppConfig);
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            RefreshValidation();
            await InvokeAsync(StateHasChanged);
            Console.WriteLine("LastUpdate_StateChanged");
        }

        protected override void OnInitialized()
        {
            markdownHtml = Markdig.Markdown.ToHtml(
                State.ProjectEdit.Description ?? string.Empty);

            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.ProjectEdit,
                AppStateChange.LastUpdated);

            RefreshValidation();
        }

        protected override async Task OnInitializedAsync()
        {
            await Autosave.EnsureUnloadFlushRegisteredAsync();

            _autosaveRegistration = Autosave.RegisterAutosave(
                _draftKey,
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
            if (key != _draftKey)
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

        private static string GetIdentityFingerprint(Project project)
            => project.Name ?? string.Empty;

        private string? BuildDraftSnapshotJson()
        {
            // While the restore prompt is open the current state is still the pre-restore
            // snapshot. Skip autosaving in this window - including the periodic fallback
            // timer - so an unanswered prompt can't clobber the real cached draft with it.
            if (_draftRestorePromptVisible)
            {
                return null;
            }

            if (State.ProjectEdit is null)
            {
                return null;
            }

            var envelope = new DraftEnvelope<Project>
            {
                SavedAtUtc = DateTime.UtcNow,
                IdentityFingerprint = GetIdentityFingerprint(State.ProjectEdit),
                Payload = State.ProjectEdit
            };

            return AutosaveService.SerializeEnvelope(envelope, DraftPayloadJsonOptions);
        }

        private void TryOfferDraftRestore()
        {
            if (Autosave.HasBeenPrompted(_draftKey))
            {
                return;
            }

            var draft = Autosave.TryGetDraft<Project>(_draftKey);
            if (draft?.Payload is null)
            {
                return;
            }

            // Only offer to restore if the draft matches the currently loaded project
            // (or the editor is still in its blank "new" state), so a stale draft from
            // editing a different project isn't offered while editing this one.
            var currentFingerprint = GetIdentityFingerprint(State.ProjectEdit);
            var isBlankCurrent = string.IsNullOrEmpty(currentFingerprint);
            if (!isBlankCurrent && draft.IdentityFingerprint != currentFingerprint)
            {
                return;
            }

            Autosave.TryMarkPrompted(_draftKey);

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

            State.UpdateProjectEdit(this, payload);
        }

        private void OnDraftRestoreDeclined()
        {
            _draftRestorePromptVisible = false;
            _pendingDraft = null;

            Autosave.RemoveDraft(_draftKey);
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

        Task OnMarkdownValueHTMLChanged(string value)
        {
            markdownHtml = value;
            return Task.CompletedTask;
        }

        private void NewProjectEdit()
        {
            //DateTime dt = DateTime.UtcNow;

            State.UpdateProjectEdit(this, new Project());
            Autosave.RemoveDraft(_draftKey);
        }

        // The Download button stays enabled; the gate decides whether the save actually happens so
        // the user always learns *why* nothing downloaded, and what to fix.
        private async Task RequestDownload()
        {
            if (State.ProjectEdit is null || _validationGate is null)
            {
                return;
            }

            RefreshValidation();

            await _validationGate.RequestAsync(_validation);
        }

        private async Task<string> SaveProject()
        {
            var now = DateTime.UtcNow;

            //State.MetadataEdit.ModifiedDate = now;

            string frontMatter = $"---\nproject: \"{State.ProjectEdit.Name}\"\nlastModified: \"{now.ToString("O")}\"\nstatus: \"{State.ProjectEdit.ProjectStatus}\"\n---";
            string fileString = frontMatter + "\n" + State.ProjectEdit.Description;

            var buffer = Encoding.UTF8.GetBytes(fileString);
            var stream = new MemoryStream(buffer);
            var fileBytes = stream.ToArray();

            await JS.InvokeAsync<string>(
                "saveAsFile", 
                $"DESCRIPTION.md",
                Convert.ToBase64String(fileBytes));

            Autosave.RemoveDraft(_draftKey);

            return fileString;
        }

        private async Task OnInputFileProjectChange(
            InputFileChangeEventArgs e)
        {
            isLoading = true;

            if (e.FileCount != 1)
            {
                return;
            }

            try
            {
                ProjectReader projectReader =
                    new ProjectReader(
                        new ProjectParser());

                // TODO: Figure out how to pass e.File.OpenReadStream() to projectReader without it failing
                string fileString;
                using (var sr = new StreamReader(e.File.OpenReadStream(), Encoding.UTF8))
                {
                    fileString = await sr.ReadToEndAsync();
                }

                var project = projectReader.Read(fileString);
                if (project is not null)
                {
                    State.UpdateProjectEdit(this, project);
                    Autosave.RemoveDraft(_draftKey);
                }
                //await ProjectChanged.InvokeAsync(this.Project);
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
