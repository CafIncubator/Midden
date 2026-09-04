using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Wasm.Shared.MetadataSections;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class MetadataDetails : ComponentBase
    {
        private const int DescriptionPreviewCharacterThreshold = 240;
        private const int DescriptionPreviewLineThreshold = 3;
        private const int VariablePageSize = 25;

        private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .Build();

        private string _metadataIdentity = string.Empty;

        [Parameter]
        public Metadata? Metadata { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        public bool VarsHaveMethods { get; set; }

        public bool VarsHaveQCApplied { get; set; }

        public bool VarsHaveProcessingLevel { get; set; }

        public bool VarsHaveVariableType { get; set; }

        public bool VarsHaveTags { get; set; }

        public int TableWidth { get; set; }

        public string DescriptionHtml { get; set; } = string.Empty;

        public bool CanExpandDescription { get; set; }

        public bool IsDescriptionExpanded { get; set; }

        public bool ShowDescription { get; set; }

        public bool ShowDatasetMethods { get; set; }

        public bool ShowLineage { get; set; }

        public bool ShowDerivedWorks { get; set; }

        public bool ShowFileInformation { get; set; }

        public bool ShowSpatialInformation { get; set; }

        public bool ShowTemporalInformation { get; set; }

        public bool ShowContacts { get; set; }

        public bool ShowTags { get; set; }

        public bool ShowVariables { get; set; }

        public bool HasDescription { get; set; }
        public bool HasMethods { get; set; }
        public bool HasLineage { get; set; }
        public bool HasDerivedWorks { get; set; }
        public bool HasFileInformation { get; set; }
        public bool HasVariables { get; set; }
        public bool HasSpatialInformation { get; set; }
        public bool HasTemporalInformation { get; set; }
        public bool HasContacts { get; set; }
        public bool HasTags { get; set; }

        public List<VariableRowItem> VariableRows { get; set; } = new();

        public List<ExpandableTextItem> DatasetMethodItems { get; set; } = new();

        public List<ExpandableTextItem> DerivedWorkItems { get; set; } = new();

        public TableFilter<string>[] FilterProcessing { get; set; } = Array.Empty<TableFilter<string>>();
        public TableFilter<string>[] FilterVariableType { get; set; } = Array.Empty<TableFilter<string>>();

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        private void CloseDescription() => ShowDescription = false;
        private void CloseDatasetMethods() => ShowDatasetMethods = false;
        private void CloseLineage() => ShowLineage = false;
        private void CloseDerivedWorks() => ShowDerivedWorks = false;
        private void CloseFileInformation() => ShowFileInformation = false;
        private void CloseVariables() => ShowVariables = false;
        private void CloseSpatialInformation() => ShowSpatialInformation = false;
        private void CloseTemporalInformation() => ShowTemporalInformation = false;
        private void CloseContacts() => ShowContacts = false;
        private void CloseTags() => ShowTags = false;

        private DatasetLineageSection? _lineageSection;

        // AntDesign's Modal raises AfterOpen once it has fully finished opening
        // (animation complete, focus applied). Re-fitting here is a reliable,
        // event-driven fallback to the CSS animation-disable fix, in case any
        // measurement still lands early.
        private void OnLineageModalAfterOpen()
        {
            _lineageSection?.RefitDiagram();
        }


        protected override void OnInitialized()
        {
            if (State?.AppConfig != null)
            {
                SetFilters(State.AppConfig);
            }
        }

        protected override void OnParametersSet()
        {
            string nextIdentity = GetMetadataIdentity(Metadata);
            if (string.Equals(_metadataIdentity, nextIdentity, StringComparison.Ordinal))
            {
                return;
            }

            _metadataIdentity = nextIdentity;
            BuildDatasetViewModel();
            ResetCollapsedSections();
        }

        private void BuildDatasetViewModel()
        {
            DescriptionHtml = string.Empty;
            CanExpandDescription = false;
            IsDescriptionExpanded = false;
            VariableRows = new List<VariableRowItem>();
            DatasetMethodItems = new List<ExpandableTextItem>();
            DerivedWorkItems = new List<ExpandableTextItem>();

            HasDescription = false;
            HasMethods = false;
            HasLineage = false;
            HasDerivedWorks = false;
            HasFileInformation = false;
            HasVariables = false;
            HasSpatialInformation = false;
            HasTemporalInformation = false;
            HasContacts = false;
            HasTags = false;

            VarsHaveMethods = false;
            VarsHaveQCApplied = false;
            VarsHaveProcessingLevel = false;
            VarsHaveVariableType = false;
            VarsHaveTags = false;
            TableWidth = 900;

            if (Metadata?.Dataset == null)
            {
                return;
            }

            DescriptionHtml = GetMarkdown(Metadata.Dataset.Description);
            CanExpandDescription = ShouldAllowExpand(Metadata.Dataset.Description);
            HasDescription = !string.IsNullOrWhiteSpace(Metadata.Dataset.Description);

            List<Variable> datasetVariables = Metadata.Dataset.Variables ?? new List<Variable>();
            VariableRows = datasetVariables
                .Select((variable, index) => new VariableRowItem(variable, index, DescriptionPreviewCharacterThreshold, DescriptionPreviewLineThreshold))
                .ToList();

            if (VariableRows.Any(row => row.HasMethods))
            {
                VarsHaveMethods = true;
                TableWidth += 400;
            }

            if (VariableRows.Any(row => row.HasQualityControls))
            {
                VarsHaveQCApplied = true;
                TableWidth += 100;
            }

            if (VariableRows.Any(row => !string.IsNullOrWhiteSpace(row.Variable.ProcessingLevel)))
            {
                VarsHaveProcessingLevel = true;
                TableWidth += 100;
            }

            if (VariableRows.Any(row => !string.IsNullOrWhiteSpace(row.Variable.VariableType)))
            {
                VarsHaveVariableType = true;
                TableWidth += 100;
            }

            if (VariableRows.Any(row => row.HasTags))
            {
                VarsHaveTags = true;
                TableWidth += 100;
            }

            DatasetMethodItems = (Metadata.Dataset.Methods ?? new List<string>())
                .Where(method => !string.IsNullOrWhiteSpace(method))
                .Select(method => new ExpandableTextItem(method!, DescriptionPreviewCharacterThreshold, DescriptionPreviewLineThreshold))
                .ToList();
            HasMethods = DatasetMethodItems.Count > 0;

            DerivedWorkItems = (Metadata.Dataset.DerivedWorks ?? new List<string>())
                .Where(derived => !string.IsNullOrWhiteSpace(derived))
                .Select(derived => new ExpandableTextItem(derived!, DescriptionPreviewCharacterThreshold, DescriptionPreviewLineThreshold))
                .ToList();
            HasDerivedWorks = DerivedWorkItems.Count > 0;

            HasLineage = Metadata.Dataset.ParentDatasets?.Any() == true;
            HasVariables = VariableRows.Count > 0;
            HasFileInformation =
                !string.IsNullOrWhiteSpace(Metadata.Dataset.DatasetPath) ||
                !string.IsNullOrWhiteSpace(Metadata.Dataset.Format) ||
                !string.IsNullOrWhiteSpace(Metadata.Dataset.FilePathTemplate) ||
                !string.IsNullOrWhiteSpace(Metadata.Dataset.FilePathDescriptor) ||
                !string.IsNullOrWhiteSpace(Metadata.Dataset.Structure);
            HasSpatialInformation =
                !string.IsNullOrWhiteSpace(Metadata.Dataset.Geometry) ||
                Metadata.Dataset.SpatialRepeats.HasValue;
            HasTemporalInformation =
                !string.IsNullOrWhiteSpace(Metadata.Dataset.TemporalExtent) ||
                !string.IsNullOrWhiteSpace(Metadata.Dataset.TemporalResolution);
            HasContacts = (Metadata.Dataset.Contacts?.Count ?? 0) > 0;
            HasTags = (Metadata.Dataset.Tags?.Count ?? 0) > 0;
        }

        private void ResetCollapsedSections()
        {
            ShowDescription = false;
            ShowDatasetMethods = false;
            ShowLineage = false;
            ShowDerivedWorks = false;
            ShowFileInformation = false;
            ShowVariables = false;
            ShowSpatialInformation = false;
            ShowTemporalInformation = false;
            ShowContacts = false;
            ShowTags = false;
        }

        private static string GetMetadataIdentity(Metadata? metadata)
        {
            if (metadata?.Dataset == null)
            {
                return string.Empty;
            }

            return string.Join("|",
                metadata.Dataset.Zone ?? string.Empty,
                metadata.Dataset.Project ?? string.Empty,
                metadata.Dataset.Name ?? string.Empty,
                metadata.ModifiedDate.ToString("O"));
        }

        private static string GetMarkdown(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            return Markdown.ToHtml(content, MarkdownPipeline);
        }

        private static bool ShouldAllowExpand(string? content)
        {
            string normalizedContent = content ?? string.Empty;
            int lineCount = normalizedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
            return normalizedContent.Length > DescriptionPreviewCharacterThreshold || lineCount > DescriptionPreviewLineThreshold;
        }

        private void SetFilters(Configuration appConfig)
        {
            List<TableFilter<string>> processings = appConfig.ProcessingLevels
                .Select(processing => new TableFilter<string> { Text = processing, Value = processing })
                .ToList();
            FilterProcessing = processings.ToArray();

            List<TableFilter<string>> variableTypes = appConfig.VariableTypes
                .Select(variableType => new TableFilter<string> { Text = variableType, Value = variableType })
                .ToList();
            FilterVariableType = variableTypes.ToArray();
        }

        private static bool FilterByPrefix(string expectedPrefix, string? actualValue)
            => !string.IsNullOrWhiteSpace(actualValue) && actualValue.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);

        private void ToggleDescription()
        {
            IsDescriptionExpanded = !IsDescriptionExpanded;
        }

        private static void ToggleExpandableText(ExpandableTextItem item)
        {
            item.IsExpanded = !item.IsExpanded;
        }

        private static void ToggleVariableDescription(VariableRowItem item)
        {
            item.IsDescriptionExpanded = !item.IsDescriptionExpanded;
        }

        private static void ToggleVariableMethods(VariableRowItem item)
        {
            item.IsMethodsExpanded = !item.IsMethodsExpanded;
        }

        private async Task DownloadVariables()
        {
            if (Metadata?.Dataset?.Variables == null || string.IsNullOrWhiteSpace(Metadata.Dataset.Name))
            {
                return;
            }

            string datasetName = Metadata.Dataset.Name.Replace(" ", "_");
            string filename = $"{datasetName}_DataDictionary.csv";
            string csvData = DataDictionaryWriterCafCsv.Write(Metadata.Dataset);

            await JSRuntime.InvokeVoidAsync("downloadCSV", filename, csvData);
        }

        public sealed class ExpandableTextItem
        {
            public ExpandableTextItem(string content, int characterThreshold, int lineThreshold)
            {
                Content = content;
                int lineCount = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
                CanExpand = content.Length > characterThreshold || lineCount > lineThreshold;
            }

            public string Content { get; }
            public bool CanExpand { get; }
            public bool IsExpanded { get; set; }
        }

        public sealed class VariableRowItem
        {
            public VariableRowItem(Variable variable, int index, int characterThreshold, int lineThreshold)
            {
                Variable = variable;
                Key = $"{index}-{variable.Name}";

                DescriptionText = variable.Description ?? string.Empty;
                MethodsText = string.Join("\n", (variable.Methods ?? new List<string>()).Where(method => !string.IsNullOrWhiteSpace(method)));

                int descriptionLineCount = DescriptionText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
                int methodsLineCount = MethodsText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;

                CanExpandDescription = DescriptionText.Length > characterThreshold || descriptionLineCount > lineThreshold;
                CanExpandMethods = MethodsText.Length > characterThreshold || methodsLineCount > lineThreshold;
            }

            public string Key { get; }
            public Variable Variable { get; }
            public string DescriptionText { get; }
            public string MethodsText { get; }
            public bool CanExpandDescription { get; }
            public bool CanExpandMethods { get; }
            public bool IsDescriptionExpanded { get; set; }
            public bool IsMethodsExpanded { get; set; }

            public bool HasMethods => !string.IsNullOrWhiteSpace(MethodsText);
            public bool HasQualityControls => (Variable.QCApplied?.Count ?? 0) > 0;
            public bool HasTags => (Variable.Tags?.Count ?? 0) > 0;
        }
    }
}
