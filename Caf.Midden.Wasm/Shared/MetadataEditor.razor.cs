using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;
using AntDesign.TableModels;
using Caf.Midden.Wasm.Shared.Modals;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.JSInterop;
using Caf.Midden.Wasm.Services;
using Caf.Midden.Core.Services.Validation;
using Caf.Midden.Wasm.Shared.Validation;

// Disambiguated from System.ComponentModel.DataAnnotations.ValidationResult.
using ValidationResult = Caf.Midden.Core.Services.Validation.ValidationResult;

// Aliased because Caf.Midden.Wasm.Shared.MetadataSections is a namespace of components and wins
// simple-name resolution against the Core class of the same name.
using Sections = Caf.Midden.Core.Services.Validation.MetadataSections;

namespace Caf.Midden.Wasm.Shared
{
    public partial class MetadataEditor : ComponentBase
    {
        private const string DraftKeyPrefix = "midden.draft.metadata.v1";

        private static readonly JsonSerializerOptions DraftPayloadJsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private IDisposable? _stateSubscription;
        private IAutosaveRegistration? _autosaveRegistration;
        private DateTime? _lastSavedUtc;
        private string _draftKey = DraftKeyPrefix;
        private DraftEnvelope<Metadata>? _pendingDraft;
        private bool _draftRestorePromptVisible;

        private static readonly MetadataValidator Validator = new();

        private ValidationResult _validation = ValidationResult.Empty;
        private CompletenessResult? _completeness;
        private ValidationGate? _validationGate;

        [Inject]
        private IMessageService MessageService { get; set; } = default!;

        // Bound to the Tabs component so activating a validation issue can switch tabs.
        private string _activeTabKey = Sections.Basic;

        string markdownDescriptionHtml = "";

        private string ZoneTooltip = @"This is the ""data zone"" that the dataset belongs to. Items in the dropdown menu are populated by information specified in the app configuration.";
        private string DatasetNameTooltip = @"This is the name of the dataset and should correspond to the name of the file or folder that contains the actual data. This also determines the name of the .midden file that is created by the Editor.";
        private string ProjectTooltip = @"This is the name of the project that the dataset belongs to. Grouping datasets under the same project provides more context to the data through the associated Project pages.";
        private string DatasetDescriptionTooltip = @"This is a description of the dataset and should include enough information for a data user to understand the basic origin and purpose of the data.";
        private string ContactsTooltip = @"This is the contact information for the contributors to the dataset. Because Midden protects the data itself by not providing download links, providing contact information is important for potential data users to start a conversation about access.";
        
        private string DatasetTagsTooltip = @"These tags are used to make the dataset more discoverable. The ""Catalog"" supports browsing and searching datasets by tags. A dataset should contain at least a few tags and the use of those tags should be as consistent as possible. Items in the dropdown menu are populated by information specified in the app configuration.";
        private string VariablesTooltip = @"These represent the measurements, or other information, represented in the dataset; i.e. the ""data dictionary"". Specifying variables is not required but it is highly recommended if the data are to be used without close oversight.";
        private string SpatialRepeatsTooltip = @"This is the number of repeated measurements that are represented in the dataset. For example, a dataset that contains soil temperature measurements (a single variable described in the ""Variable"" section) at five locations buried at five different depths would have a value of 25.";
        private string SpatialExtentTooltip = @"This is the region at which the data were collected and/or represent. Values should be valid GeoJSON (point, line, or polygon). Items in the dropdown menu are populated by information specified in the app configuration.";
        private string TemporalResolutionTooltip = @"This is the frequency at which the variables in the dataset were measured. Air temperature measured every 15 minutes may have the value of ""15 min"". A dataset that contains plant community survey data taken annually may have a value of ""1 year"" or ""annually"".";
        private string TemporalExtentTooltip = @"This is the starting and ending dates (and optionally time) that contain the date and time the data were collected or that the data represent. Consider using the ISO 8601 format for time-intervals (e.g. ""1997-07-16/1997-07-17"" corresponds to a time-period starting on July 16, 1997 and ending on July 17, 1997).";
        private string FileFormatTooltip = @"This is the format that the data are stored in. This could be a file extension (e.g. "".json"", "".txt""), general category (e.g. ""tabular"", ""image""), or some standard (e.g. MIME types: ""text/csv"", ""application/java-archive"").";
        private string FilePathTemplateTooltip = @"This is a description of the directory and file structure within the dataset folder, if applicable. For example, this can be used to describe a dataset comprised of time-series files generated every hour and separated into monthly folders via: ""{YYYY-MM}/{DD}T{hh}:{mm}_{VariableName}.csv""";
        private string FilePathDescriptionTooltip = @"This is a description of the ""File Path Template"" where each variable is described. For example, ""{YYYY-MM} is the date, in ISO 8601 format, that the data were collected.""";
        private string DatasetStructureTooltip = @"This is a category tag that broadly indicates how the data are structured. For example, a dataset folder that has multiple files but a dataset structure of ""Single"" may indicate that the various files are different versions of a single dataset. A value of ""Multiple"" on the other hand may indicate these files are timeseries data that can be aggregated. Items in the dropdown menu are populated by information specified in the app configuration.";
        private string DatasetMethodsTooltip = @"These are methods used to generate the dataset and may include things like sample collection methods, data pipelines, and so on. Methods for specific measurements within the dataset can be described here but might be better to do so in the methods field of the associated variable. The intent for these fields is to provide multiple links (e.g. GitHub repository, protocols.io, standard operating procedures), but this currently also supports free text.";
        private string ParentDatasetsTooltip = @"This is used to specify datasets that this dataset was derived from. Values are expected to be linked resources (URL) but a citation/reference is fine. This field is important for documenting data lineage.";
        private string DerivedWorksTooltip = @"This is used to indicate related products that use the dataset (e.g. published papers, presentations, decision support tools). Values are expected to be linked resources (URL) but a citation/reference is fine. This field is not intended for derived datasets, see the field “Parent Datasets” for that.";

        AntDesign.Form<Metadata> form;

        private Person? _dragContactSource;
        private int? _dragContactSourceIndex;
        private int? _dragContactOverIndex;
        private Variable? _dragVariableSource;
        private int? _dragVariableSourceIndex;
        private int? _dragVariableOverIndex;
        private DatasetStringListKind? _dragStringListKind;
        private int? _dragStringSourceIndex;
        private int? _dragStringOverIndex;

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            RefreshValidation();
            await InvokeAsync(StateHasChanged);
            Console.WriteLine("LastUpdate_StateChanged");
        }

        private string AutosaveStatusText => _lastSavedUtc is null
            ? string.Empty
            : $"All changes saved \u00b7 {FormatRelativeTime(_lastSavedUtc.Value)}";

        private string DraftRestorePromptText => _pendingDraft is null
            ? string.Empty
            : $"A saved draft from {FormatRelativeTime(_pendingDraft.SavedAtUtc)} was found. Resume editing it?";

        #region Validation

        /// <summary>
        /// Re-runs the Core validator and completeness calculator against current state.
        /// </summary>
        /// <remarks>
        /// The Core validator, rather than <c>form.Validate()</c>, is the gate: AntDesign skips
        /// fields on <c>TabPane</c>s the user never opened, so the form cannot see them.
        /// </remarks>
        private void RefreshValidation()
        {
            if (State?.MetadataEdit is null)
            {
                _validation = ValidationResult.Empty;
                _completeness = null;
                return;
            }

            _validation = Validator.Validate(State.MetadataEdit, State.AppConfig);
            _completeness = MetadataCompletenessCalculator.Calculate(State.MetadataEdit);
        }

        /// <summary>
        /// Error counts keyed by tab. <see cref="MetadataSections"/> values are the TabPane keys,
        /// so no translation table is needed.
        /// </summary>
        private IReadOnlyDictionary<string, int> ErrorCountsByTab =>
            _validation.CountsBySection();

        private int TabErrorCount(string tabKey) =>
            ErrorCountsByTab.TryGetValue(tabKey, out var count) ? count : 0;

        /// <summary>
        /// Issues owned by a single variable row, matched on the indexed path the validator emits.
        /// </summary>
        private IEnumerable<ValidationIssue> VariableIssues(Variable variable)
        {
            var index = State.MetadataEdit.Dataset.Variables.IndexOf(variable);

            if (index < 0)
            {
                return [];
            }

            var prefix = $"dataset.variables[{index}]";

            return _validation.Issues.Where(
                i => i.Path.StartsWith(prefix, StringComparison.Ordinal));
        }

        private bool VariableHasErrors(Variable variable) =>
            VariableIssues(variable).Any(i => i.Severity == ValidationSeverity.Error);

        private string VariableIssueTitle(Variable variable) =>
            string.Join(" ", VariableIssues(variable).Select(i => i.Message));

        /// <summary>
        /// Issues owned by a single contact row.
        /// </summary>
        private IEnumerable<ValidationIssue> ContactIssues(Person contact)
        {
            var index = State.MetadataEdit.Dataset.Contacts.IndexOf(contact);

            if (index < 0)
            {
                return [];
            }

            var prefix = $"dataset.contacts[{index}]";

            return _validation.Issues.Where(
                i => i.Path.StartsWith(prefix, StringComparison.Ordinal));
        }

        private bool ContactHasErrors(Person contact) =>
            ContactIssues(contact).Any(i => i.Severity == ValidationSeverity.Error);

        private string ContactIssueTitle(Person contact) =>
            string.Join(" ", ContactIssues(contact).Select(i => i.Message));

        // Only Download is gated. Preview produces nothing shareable, and viewing the rendered
        // output is a legitimate way to diagnose the problems the gate reports, so blocking it
        // would make the error state harder to fix.
        private async Task RequestDownload()
        {
            if (State?.MetadataEdit is null || _validationGate is null)
            {
                return;
            }

            RefreshValidation();

            await _validationGate.RequestAsync(_validation);
        }

        /// <summary>
        /// Switches to the tab owning an issue so the user can act on it.
        /// </summary>
        private void OnValidationIssueSelected(ValidationIssue issue)
        {
            _activeTabKey = issue.Section;
        }

        /// <summary>
        /// Appends an error count to a tab label. Text rather than color alone, so the badge is
        /// still readable without color perception.
        /// </summary>
        private string TabLabel(string label, string sectionKey)
        {
            var count = TabErrorCount(sectionKey);

            return count == 0 ? label : $"{label} ({count})";
        }

        private string BasicTabLabel => TabLabel("Basic", Sections.Basic);

        private string VariablesTabLabel => TabLabel("Variables", Sections.Variables);

        private string CoverageTabLabel => TabLabel("Coverage", Sections.Coverage);

        private string StructureTabLabel => TabLabel("Structure", Sections.Structure);

        private string ProcessingTabLabel => TabLabel("Processing", Sections.Processing);

        private string CompletenessText =>
            _completeness is null ? string.Empty : $"{_completeness.Percent}% complete";

        private string CompletenessTooltip
        {
            get
            {
                if (_completeness is null)
                {
                    return string.Empty;
                }

                var suggestions = _completeness.TopSuggestions.Take(3).ToList();

                return suggestions.Count == 0
                    ? "This dataset is fully documented."
                    : "To improve: " + string.Join(" ", suggestions.Select(s => s.Suggestion));
            }
        }

        #endregion

        protected override void OnInitialized()
        {
            //this.EditContext = new EditContext(State.MetadataEdit);
            //this.EditContext.OnFieldChanged +=
            //    EditContext_OnFieldChange;

            markdownDescriptionHtml = Markdig.Markdown.ToHtml(
                State.MetadataEdit.Dataset.Description ?? string.Empty);

            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.MetadataEdit,
                AppStateChange.LastUpdated,
                AppStateChange.AppConfig);

            RefreshValidation();
        }

        Task OnMarkdownDescriptionValueHTMLChanged(string value)
        {
            markdownDescriptionHtml = value;
            Autosave_NotifyChanged();
            return Task.CompletedTask;
        }

        private Task OnMetadataLoaded(Metadata metadata)
        {
            State.SetMetadataEdit(metadata, this);
            Autosave.RemoveDraft(_draftKey);
            RefreshValidation();
            return Task.CompletedTask;
        }

        private void EditContext_OnFieldChange(
            object sender, 
            FieldChangedEventArgs e)
        {
            State.UpdateLastUpdated(this, DateTime.UtcNow);
        }

        private void OnFormFieldChanged(FieldChangedEventArgs e)
        {
            Autosave_NotifyChanged();
        }

        private void Autosave_NotifyChanged()
        {
            _autosaveRegistration?.NotifyChanged();

            // Validation is ambient here; it never gates autosave.
            RefreshValidation();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            // Many mutations (contact/variable/tag add-delete-drag, etc.) happen via
            // button/drag handlers rather than AntDesign FormItem field-changed events.
            // Blazor re-renders after any such event, so treating a re-render as a proxy
            // for "something may have changed" reliably captures all of them without
            // needing to instrument every individual handler.
            //
            // While the restore prompt is open the state shown is still the pre-restore
            // snapshot, so renders caused by the prompt itself must not be treated as edits -
            // otherwise they'd autosave blank state over the cached draft before the user answers.
            if (!firstRender && !_draftRestorePromptVisible)
            {
                Autosave_NotifyChanged();
            }
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

        private static string GetIdentityFingerprint(Metadata metadata)
            => $"{metadata?.Dataset?.Zone}|{metadata?.Dataset?.Name}|{metadata?.Dataset?.Project}";

        private string? BuildDraftSnapshotJson()
        {
            // While the restore prompt is open the current state is still the pre-restore
            // snapshot. Skip autosaving in this window - including the periodic fallback
            // timer - so an unanswered prompt can't clobber the real cached draft with it.
            if (_draftRestorePromptVisible)
            {
                return null;
            }

            if (State.MetadataEdit is null)
            {
                return null;
            }

            var envelope = new DraftEnvelope<Metadata>
            {
                SavedAtUtc = DateTime.UtcNow,
                IdentityFingerprint = GetIdentityFingerprint(State.MetadataEdit),
                Payload = State.MetadataEdit
            };

            return AutosaveService.SerializeEnvelope(envelope, DraftPayloadJsonOptions);
        }

        private void TryOfferDraftRestore()
        {
            if (Autosave.HasBeenPrompted(_draftKey))
            {
                return;
            }

            var draft = Autosave.TryGetDraft<Metadata>(_draftKey);
            if (draft?.Payload is null)
            {
                return;
            }

            // Only offer to restore if the draft matches the currently loaded dataset
            // (or the editor is still in its blank "new" state), so a stale draft from
            // editing a different dataset isn't offered while editing this one.
            var currentFingerprint = GetIdentityFingerprint(State.MetadataEdit);
            var isBlankCurrent = string.IsNullOrEmpty(State.MetadataEdit?.Dataset?.Name)
                && string.IsNullOrEmpty(State.MetadataEdit?.Dataset?.Zone)
                && string.IsNullOrEmpty(State.MetadataEdit?.Dataset?.Project);
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

            State.SetMetadataEdit(payload, this);
            markdownDescriptionHtml = Markdig.Markdown.ToHtml(
                State.MetadataEdit.Dataset.Description ?? string.Empty);
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

        private void NewMetadata()
        {
            DateTime dt = DateTime.UtcNow;

            State.UpdateMetadataEdit(this,new Metadata()
            {
                Dataset = new Dataset(),
                CreationDate = dt,
                ModifiedDate = dt
            });

            State.UpdateLastUpdated(this, DateTime.UtcNow);

            Autosave.RemoveDraft(_draftKey);
        }

        #region Contact Functions
        private ModalRef personModalRef;
        private Task OpenPersonModalTemplate(Person contact)
        {
            var templateOptions = new ViewModels.PersonModalViewModel
            {
                Person = new Person()
                {
                    Name = contact.Name,
                    Email = contact.Email,
                    Role = contact.Role
                },
                Roles = State.AppConfig.Roles
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Contact";
            modalConfig.OnCancel = async (e) =>
            {
                await personModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                contact.Name = templateOptions.Person.Name;
                contact.Email = templateOptions.Person.Email;
                contact.Role = templateOptions.Person.Role;

                await personModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                RemoveBlankContacts();

                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            personModalRef = ModalService
                .CreateModal<PersonModal, ViewModels.PersonModalViewModel>(
                    modalConfig,
                    templateOptions);

            return Task.CompletedTask;
        }

        private void RemoveBlankContacts()
        {
            List<Person> contactsToRemove = new List<Person>();
            foreach(Person contact in State.MetadataEdit.Dataset.Contacts)
            {
                if(string.IsNullOrWhiteSpace(contact.Name) &&
                    string.IsNullOrWhiteSpace(contact.Email) &&
                    string.IsNullOrWhiteSpace(contact.Role))
                {
                    contactsToRemove.Add(contact);
                }
            }
            foreach(Person remove in contactsToRemove)
            {
                State.MetadataEdit.Dataset.Contacts.Remove(remove);
            }
        }

        private async Task AddContactHandler()
        {
            if (State.MetadataEdit.Dataset.Contacts == null)
                State.MetadataEdit.Dataset.Contacts = new List<Person>();

            var contact = new Person();

            await OpenPersonModalTemplate(contact);

            State.MetadataEdit.Dataset.Contacts.Add(contact);
        }

        private void DeleteContactHandler(Person person)
        {
            State.MetadataEdit.Dataset.Contacts.Remove(person);
        }

        private void OnContactDragStart(Person contact)
        {
            _dragContactSource = contact;
            _dragContactSourceIndex = State.MetadataEdit.Dataset.Contacts.IndexOf(contact);
            _dragContactOverIndex = _dragContactSourceIndex;
        }

        private void OnContactDragEnter(Person targetContact)
        {
            if (_dragContactSourceIndex is null) return;

            var targetIndex = State.MetadataEdit.Dataset.Contacts.IndexOf(targetContact);
            if (targetIndex < 0 || _dragContactOverIndex == targetIndex) return;

            _dragContactOverIndex = targetIndex;
            StateHasChanged();
        }

        private Dictionary<string, object> GetContactRowAttributes(RowData<Person> row)
        {
            var contact = row.Data;

            var attributes = new Dictionary<string, object>
            {
                ["ondragenter"] = EventCallback.Factory.Create<DragEventArgs>(
                    this, () => OnContactDragEnter(contact)),
                ["ondrop"] = EventCallback.Factory.Create<DragEventArgs>(
                    this, () => OnContactDrop(contact))
            };

            var issueTitle = ContactIssueTitle(contact);

            if (!string.IsNullOrEmpty(issueTitle))
            {
                attributes["title"] = issueTitle;
            }

            return attributes;
        }

        private string GetContactRowClassName(RowData<Person> row)
        {
            var className = DropRowClassName(
                State.MetadataEdit.Dataset.Contacts.IndexOf(row.Data),
                _dragContactSourceIndex,
                _dragContactOverIndex);

            // Contacts are edited in a modal, so the row is the only place the problem can show.
            if (ContactHasErrors(row.Data))
            {
                className = string.IsNullOrEmpty(className)
                    ? "validation-row-error"
                    : $"{className} validation-row-error";
            }

            return className;
        }

        private void OnContactDrop(Person targetContact)
        {
            if (_dragContactSource == null || _dragContactSourceIndex is null)
            {
                ClearContactDragState();
                return;
            }

            var contacts = State.MetadataEdit.Dataset.Contacts;
            var sourceIndex = _dragContactSourceIndex.Value;
            var targetIndex = contacts.IndexOf(targetContact);

            if (targetIndex < 0 ||
                sourceIndex < 0 ||
                sourceIndex >= contacts.Count ||
                sourceIndex == targetIndex)
            {
                ClearContactDragState();
                return;
            }

            var item = _dragContactSource;
            contacts.RemoveAt(sourceIndex);
            contacts.Insert(targetIndex, item);

            ClearContactDragState();
        }

        private void OnContactDragEnd()
        {
            ClearContactDragState();
        }

        private void ClearContactDragState()
        {
            _dragContactSource = null;
            _dragContactSourceIndex = null;
            _dragContactOverIndex = null;
        }
        #endregion

        #region DatasetTag
        private string NewDatasetTag { get; set; }
        private string SavedDatasetTag { get; set; }

        private void AddDatasetTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) &&
                !IsDuplicateDatasetTag(tag))
            {
                State.MetadataEdit.Dataset.Tags.Add(tag);
            }
        }
        private void AddDatasetTagHandler()
        {
            AddDatasetTag(NewDatasetTag);
            NewDatasetTag = "";
        }

        private void DatasetTagSelectedItemChangedHandler(string value)
        {
            AddDatasetTag(value);
            SavedDatasetTag = "";
        }

        private void DeleteDatasetTagHandler(string tag)
        {
            State.MetadataEdit.Dataset.Tags.Remove(tag);
        }

        private bool IsDuplicateDatasetTag(string tag)
        {
            var dup = State.MetadataEdit.Dataset.Tags.Find(s => s == tag);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }
        #endregion

        #region DatasetMethods
        private string NewDatasetMethod { get; set; }

        
        private void AddDatasetMethod(string method)
        {
            if(!string.IsNullOrWhiteSpace(method) &&
                !IsDuplicateDatasetMethod(method))
            {
                State.MetadataEdit.Dataset.Methods.Add(method);
                NewDatasetMethod = "";
            }
        }
        private bool IsDuplicateDatasetMethod(string method)
        {
            var dup = State.MetadataEdit.Dataset.Methods.Find(s => s == method);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }

        private void AddDatasetMethodHandler()
        {
            AddDatasetMethod(NewDatasetMethod);
        }
        private void DeleteDatasetMethodHandler(string method)
        {
            State.MetadataEdit.Dataset.Methods.Remove(method);
        }
        #endregion

        #region Parent Datasets
        private string NewParentDataset { get; set; }

        private void AddParentDataset(string parentDataset)
        {
            if (!string.IsNullOrWhiteSpace(parentDataset) &&
                !IsDuplicateParentDataset(parentDataset))
            {
                State.MetadataEdit.Dataset.ParentDatasets.Add(parentDataset);
                NewParentDataset = "";
            }
        }
        private bool IsDuplicateParentDataset(string parentDataset)
        {
            var dup = State.MetadataEdit.Dataset.ParentDatasets.Find(p => p == parentDataset);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }

        private void AddParentDatasetHandler()
        {
            AddParentDataset(NewParentDataset);
        }
        private void DeleteParentDatasetHandler(string parentDataset)
        {
            State.MetadataEdit.Dataset.ParentDatasets.Remove(parentDataset);
        }
        #endregion

        #region Derived Works
        private string NewDerivedWork { get; set; }

        private void AddDerivedWork(string derived)
        {
            if (!string.IsNullOrWhiteSpace(derived) &&
                !IsDuplicateDerivedWork(derived))
            {
                State.MetadataEdit.Dataset.DerivedWorks.Add(derived);
                NewDerivedWork = "";
            }
        }
        private bool IsDuplicateDerivedWork(string derived)
        {
            var dup = State.MetadataEdit.Dataset.DerivedWorks.Find(s => s == derived);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }

        private void AddDerivedWorkHandler()
        {
            AddDerivedWork(NewDerivedWork);
        }
        private void DeleteDerivedWorkHandler(string derived)
        {
            State.MetadataEdit.Dataset.DerivedWorks.Remove(derived);
        }

        private enum DatasetStringListKind
        {
            Tags,
            Methods,
            ParentDatasets,
            DerivedWorks
        }

        private void OnStringDragStart(DatasetStringListKind listKind, int index)
        {
            _dragStringListKind = listKind;
            _dragStringSourceIndex = index;
            _dragStringOverIndex = index;
        }

        private void OnStringDragEnter(DatasetStringListKind listKind, int index)
        {
            if (_dragStringListKind != listKind || _dragStringOverIndex == index) return;
            _dragStringOverIndex = index;
        }

        private bool IsStringDragOver(DatasetStringListKind listKind, int index)
        {
            return _dragStringListKind == listKind && _dragStringOverIndex == index;
        }

        private void OnStringDrop(DatasetStringListKind listKind, int index)
        {
            if (_dragStringListKind != listKind || _dragStringSourceIndex is null)
            {
                ClearStringDragState();
                return;
            }

            var list = GetStringList(listKind);
            var sourceIndex = _dragStringSourceIndex.Value;

            if (sourceIndex < 0 || sourceIndex >= list.Count || index < 0 || index > list.Count)
            {
                ClearStringDragState();
                return;
            }

            if (sourceIndex == index)
            {
                ClearStringDragState();
                return;
            }

            var item = list[sourceIndex];
            list.RemoveAt(sourceIndex);

            var insertAt = sourceIndex < index ? index - 1 : index;
            list.Insert(insertAt, item);

            ClearStringDragState();
        }

        private void OnStringDragEnd()
        {
            ClearStringDragState();
        }

        private void ClearStringDragState()
        {
            _dragStringListKind = null;
            _dragStringSourceIndex = null;
            _dragStringOverIndex = null;
        }

        private List<string> GetStringList(DatasetStringListKind listKind)
        {
            return listKind switch
            {
                DatasetStringListKind.Tags => State.MetadataEdit.Dataset.Tags,
                DatasetStringListKind.Methods => State.MetadataEdit.Dataset.Methods,
                DatasetStringListKind.ParentDatasets => State.MetadataEdit.Dataset.ParentDatasets,
                DatasetStringListKind.DerivedWorks => State.MetadataEdit.Dataset.DerivedWorks,
                _ => throw new ArgumentOutOfRangeException(nameof(listKind), listKind, null)
            };
        }
        #endregion

        #region Variable Functions

        private Variable VariableQuickEditRef;
        private ViewModels.VariableModalViewModel QuickEditViewModel;

        private Task StartQuickEdit(Variable variable)
        {
            VariableQuickEditRef = variable;
            if(QuickEditViewModel == null)
            {
                QuickEditViewModel = new ViewModels.VariableModalViewModel
                {
                    Variable = new Variable()
                    {
                        Name = variable.Name,
                        Description = variable.Description,
                        Units = variable.Units,
                        Height = variable.Height,
                        Tags = variable.Tags,
                        Methods = variable.Methods,
                        QCApplied = variable.QCApplied,
                        ProcessingLevel = variable.ProcessingLevel,
                        VariableType = variable.VariableType
                    },
                    ProcessingLevels = State.AppConfig.ProcessingLevels,
                    QCFlags = State.AppConfig.QCTags,
                    VariableTypes = State.AppConfig.VariableTypes,
                    Tags = State.AppConfig.Tags,
                    SelectedTags = variable.Tags ??= new List<string>(),
                    SelectedQCApplied = variable.QCApplied ??= new List<string>()
                };
            }
            else
            {
                QuickEditViewModel.Variable = new Variable()
                {
                    Name = variable.Name,
                    Description = variable.Description,
                    Units = variable.Units,
                    Height = variable.Height,
                    Tags = variable.Tags,
                    Methods = variable.Methods,
                    QCApplied = variable.QCApplied,
                    ProcessingLevel = variable.ProcessingLevel,
                    VariableType = variable.VariableType
                };
                QuickEditViewModel.SelectedTags = variable.Tags ??= new List<string>();
                QuickEditViewModel.SelectedQCApplied = variable.QCApplied ??= new List<string>();
            }
            return Task.CompletedTask;
        }

        private Task EndQuickEdit()
        {
            var editedVariable = VariableQuickEditRef.DeepCopy();
            editedVariable.Name = QuickEditViewModel.Variable.Name;
            editedVariable.Description = QuickEditViewModel.Variable.Description;
            editedVariable.Units = QuickEditViewModel.Variable.Units;
            editedVariable.Height = QuickEditViewModel.Variable.Height;
            editedVariable.Tags = QuickEditViewModel.SelectedTags.ToList();
            editedVariable.Methods = QuickEditViewModel.Variable.Methods;
            editedVariable.QCApplied = QuickEditViewModel.SelectedQCApplied.ToList();
            editedVariable.ProcessingLevel = QuickEditViewModel.Variable.ProcessingLevel;
            editedVariable.VariableType = QuickEditViewModel.Variable.VariableType;

            var variableIndex = State.MetadataEdit.Dataset.Variables.IndexOf(VariableQuickEditRef);
            if (variableIndex < 0)
            {
                VariableQuickEditRef = null;
                return Task.CompletedTask;
            }

            var candidate = JsonSerializer.Deserialize<Metadata>(
                JsonSerializer.Serialize(State.MetadataEdit, DraftPayloadJsonOptions),
                DraftPayloadJsonOptions)!;
            candidate.Dataset.Variables[variableIndex] = editedVariable;

            var variablePath = $"dataset.variables[{variableIndex}]";
            var errors = Validator.Validate(candidate, State.AppConfig).Errors
                .Where(issue => issue.Path.StartsWith(variablePath, StringComparison.Ordinal))
                .ToList();

            if (errors.Count > 0)
            {
                MessageService.Error(new MessageConfig
                {
                    Content = string.Join(" ", errors.Select(issue => issue.Message)),
                    Duration = 8
                });

                return Task.CompletedTask;
            }

            VariableQuickEditRef.Name = editedVariable.Name;
            VariableQuickEditRef.Description = editedVariable.Description;
            VariableQuickEditRef.Units = editedVariable.Units;
            VariableQuickEditRef.Height = editedVariable.Height;
            VariableQuickEditRef.Tags = editedVariable.Tags;
            VariableQuickEditRef.Methods = editedVariable.Methods;
            VariableQuickEditRef.QCApplied = editedVariable.QCApplied;
            VariableQuickEditRef.ProcessingLevel = editedVariable.ProcessingLevel;
            VariableQuickEditRef.VariableType = editedVariable.VariableType;

            VariableQuickEditRef = null;
            return Task.CompletedTask;
        }

        private ModalRef variableModalRef;
        private Task OpenVariableModalTemplate(Variable variable)
        {
            var templateOptions = new ViewModels.VariableModalViewModel
            {
                Variable = new Variable()
                {
                    Name = variable.Name,
                    Description = variable.Description,
                    Units = variable.Units,
                    Height = variable.Height,
                    Tags = variable.Tags,
                    Methods = variable.Methods,
                    QCApplied = variable.QCApplied,
                    ProcessingLevel = variable.ProcessingLevel,
                    VariableType = variable.VariableType
                },
                ProcessingLevels = State.AppConfig.ProcessingLevels,
                QCFlags = State.AppConfig.QCTags,
                VariableTypes = State.AppConfig.VariableTypes,
                Tags = State.AppConfig.Tags,
                SelectedTags = variable.Tags ??= new List<string>(),
                SelectedQCApplied = variable.QCApplied ??= new List<string>()
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Variable";
            modalConfig.Width = "70%";
            modalConfig.OnCancel = async (e) =>
            {
                await variableModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                variable.Name = templateOptions.Variable.Name;
                variable.Description = templateOptions.Variable.Description;
                variable.Units = templateOptions.Variable.Units;
                variable.Height = templateOptions.Variable.Height;
                variable.Tags = templateOptions.SelectedTags.ToList();
                variable.Methods = templateOptions.Variable.Methods;
                variable.QCApplied = templateOptions.SelectedQCApplied.ToList();
                variable.ProcessingLevel = templateOptions.Variable.ProcessingLevel;
                variable.VariableType = templateOptions.Variable.VariableType;
                await variableModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                RemoveBlankVariables();

                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            variableModalRef = ModalService
                .CreateModal<VariableModal, ViewModels.VariableModalViewModel>(
                    modalConfig,
                    templateOptions);

            return Task.CompletedTask;
        }

        private void RemoveBlankVariables()
        {
            List<Variable> variablesToRemove = new List<Variable>();
            foreach (Variable variable in State.MetadataEdit.Dataset.Variables)
            {
                if (string.IsNullOrWhiteSpace(variable.Name) &&
                    string.IsNullOrWhiteSpace(variable.Description) &&
                    string.IsNullOrWhiteSpace(variable.Units))
                {
                    variablesToRemove.Add(variable);
                }
            }
            foreach (Variable remove in variablesToRemove)
            {
                State.MetadataEdit.Dataset.Variables.Remove(remove);
            }
        }

        private async Task AddVariableHandler()
        {
            var variable = new Variable();

            await OpenVariableModalTemplate(variable);

            State.MetadataEdit.Dataset.Variables.Add(variable);
        }

        private void DeleteVariableHandler(Variable variable)
        {
            State.MetadataEdit.Dataset.Variables.Remove(variable);
        }

        private void OnVariableDragStart(Variable variable)
        {
            _dragVariableSource = variable;
            _dragVariableSourceIndex = State.MetadataEdit.Dataset.Variables.IndexOf(variable);
            _dragVariableOverIndex = _dragVariableSourceIndex;
        }

        private void OnVariableDragEnter(Variable targetVariable)
        {
            if (_dragVariableSourceIndex is null) return;

            var targetIndex = State.MetadataEdit.Dataset.Variables.IndexOf(targetVariable);
            if (targetIndex < 0 || _dragVariableOverIndex == targetIndex) return;

            _dragVariableOverIndex = targetIndex;
            StateHasChanged();
        }

        private Dictionary<string, object> GetVariableRowAttributes(RowData<Variable> row)
        {
            var variable = row.Data;

            var attributes = new Dictionary<string, object>
            {
                ["ondragenter"] = EventCallback.Factory.Create<DragEventArgs>(
                    this, () => OnVariableDragEnter(variable)),
                ["ondrop"] = EventCallback.Factory.Create<DragEventArgs>(
                    this, () => OnVariableDrop(variable))
            };

            var issueTitle = VariableIssueTitle(variable);

            if (!string.IsNullOrEmpty(issueTitle))
            {
                attributes["title"] = issueTitle;
            }

            return attributes;
        }

        private string GetVariableRowClassName(RowData<Variable> row)
        {
            var className = DropRowClassName(
                State.MetadataEdit.Dataset.Variables.IndexOf(row.Data),
                _dragVariableSourceIndex,
                _dragVariableOverIndex);

            // Bulk CSV import can introduce dozens of invalid rows at once, so the row itself has
            // to carry the signal - a summary at the bottom of the page would not locate them.
            if (VariableHasErrors(row.Data))
            {
                className = string.IsNullOrEmpty(className)
                    ? "validation-row-error"
                    : $"{className} validation-row-error";
            }

            return className;
        }

        private void OnVariableDrop(Variable targetVariable)
        {
            if (_dragVariableSource == null || _dragVariableSourceIndex is null)
            {
                ClearVariableDragState();
                return;
            }

            var variables = State.MetadataEdit.Dataset.Variables;
            var sourceIndex = _dragVariableSourceIndex.Value;
            var targetIndex = variables.IndexOf(targetVariable);

            if (targetIndex < 0 ||
                sourceIndex < 0 ||
                sourceIndex >= variables.Count ||
                sourceIndex == targetIndex)
            {
                ClearVariableDragState();
                return;
            }

            var item = _dragVariableSource;
            variables.RemoveAt(sourceIndex);
            variables.Insert(targetIndex, item);

            ClearVariableDragState();
        }

        private void OnVariableDragEnd()
        {
            ClearVariableDragState();
        }

        private void ClearVariableDragState()
        {
            _dragVariableSource = null;
            _dragVariableSourceIndex = null;
            _dragVariableOverIndex = null;
        }

        private static string DropRowClassName(int rowIndex, int? sourceIndex, int? overIndex)
        {
            const string droppable = "midden-droppable-row";

            if (rowIndex < 0 || sourceIndex is null || overIndex is null)
                return droppable;

            if (rowIndex != overIndex.Value || overIndex.Value == sourceIndex.Value)
                return droppable;

            return sourceIndex.Value > overIndex.Value
                ? $"{droppable} midden-drop-above"
                : $"{droppable} midden-drop-below";
        }
        #endregion

        #region Geometry
        private string GeometryTemplate { get; set; }
        private void OnGeometryItemChangedHandler(string value)
        {
            State.MetadataEdit.Dataset.Geometry = value;
        }

        // Drawing/editing on the map no longer matches the selected saved geometry
        private void OnMapGeometryChanged(Dataset dataset, string value)
        {
            dataset.Geometry = value;

            if (!string.Equals(GeometryTemplate, value, StringComparison.Ordinal))
            {
                GeometryTemplate = null;
            }
        }

        public void Dispose()
        {
            //this.EditContext.OnFieldChanged -=
            //     EditContext_OnFieldChange;
            Autosave.Saved -= OnAutosaveSaved;
            _autosaveRegistration?.Dispose();
            _stateSubscription?.Dispose();
        }
        #endregion

        // Callers reach this through RequestDownload, which runs the Core validator gate first.
        private async Task<string> SaveDataset()
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string jsonString;

            var now = DateTime.UtcNow;

            State.MetadataEdit.ModifiedDate = now;
            if (State.MetadataEdit.CreationDate == DateTime.MinValue)
                State.MetadataEdit.CreationDate = now;

            jsonString = JsonSerializer.Serialize(State.MetadataEdit, options);

            var buffer = Encoding.UTF8.GetBytes(jsonString);
            var stream = new MemoryStream(buffer);
            var fileBytes = stream.ToArray();
            
            await JS.InvokeAsync<string>(
                "saveAsFile", 
                $"{State.MetadataEdit.Dataset.Name}.midden",
                Convert.ToBase64String(fileBytes));

            Autosave.RemoveDraft(_draftKey);

            return jsonString;
        }

        private ModalRef metadataDetailsModalRef;
        private Task OpenMetadataDetailsModalTemplate(Metadata metadata)
        {
            var templateOptions = new ViewModels.MetadataDetailsViewModel
            {
                Metadata = metadata
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Metadata Preview";
            modalConfig.Width = "90%";
            modalConfig.DestroyOnClose = true;
            modalConfig.OnCancel = async (e) =>
            {
                await metadataDetailsModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                await metadataDetailsModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            metadataDetailsModalRef = ModalService
                .CreateModal<MetadataDetailsModal, ViewModels.MetadataDetailsViewModel>(
                    modalConfig,
                    templateOptions);

            return Task.CompletedTask;
        }
    }
}
