using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Markdig;
using Caf.Midden.Wasm.Shared.MetadataLineage;

namespace Caf.Midden.Wasm.Shared
{
    public partial class MetadataDetails : ComponentBase
    {
        [Parameter]
        public Metadata Metadata { get; set; }

        public bool VarsHaveMethods { get; set; }

        public bool VarsHaveQCApplied { get; set; }

        public bool VarsHaveProcessingLevel { get; set; }

        public bool VarsHaveVariableType { get; set; }

        public bool VarsHaveTags { get; set; }

// Depricated
//        public bool VarsHaveHeight { get; set; }

        public int TableWidth { get; set; }

        public TableFilter<string>[] FilterProcessing;
        public TableFilter<string>[] FilterVariableType;

        private MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .Build();

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        protected override void OnInitialized()
        {
            if(Metadata?.Dataset?.Variables != null)
            {
                this.TableWidth = 900;

                int numMethods = Metadata.Dataset.Variables.SelectMany(v => v.Methods).Count();
                if (numMethods > 0)
                {
                    this.VarsHaveMethods = true;
                    this.TableWidth += 400;
                }

                int numQCApplied = Metadata.Dataset.Variables.SelectMany(v => v.QCApplied).Count();
                if (numQCApplied > 0)
                {
                    this.VarsHaveQCApplied = true;
                    this.TableWidth += 100;
                }

                int numProcessingLevel = Metadata.Dataset.Variables.Where(v => !string.IsNullOrEmpty(v.ProcessingLevel)).Count();
                if (numProcessingLevel > 0)
                {
                    this.VarsHaveProcessingLevel = true;
                    this.TableWidth += 100;
                }

                int numVariableType = Metadata.Dataset.Variables.Where(v => !string.IsNullOrEmpty(v.VariableType)).Count();
                if (numVariableType > 0)
                {
                    this.VarsHaveVariableType = true;
                    this.TableWidth += 100;
                }

                int numTags = Metadata.Dataset.Variables.SelectMany(v => v.Tags).Count();
                if (numTags > 0)
                {
                    this.VarsHaveTags = true;
                    this.TableWidth += 100;
                }
// Depricated
//                int numHeight = Metadata.Dataset.Variables.Where(v => v.Height != null).Count();
//                if (numHeight > 0)
//                {
//                    this.VarsHaveHeight = true;
//                    this.TableWidth += 50;
//                }

                StateHasChanged();
            }

            if(State?.AppConfig != null)
                SetFilters(State?.AppConfig);
        }

        private void SetFilters(Configuration appConfig)
        {
            if (appConfig == null)
                return;

            List<TableFilter<string>> processings = new List<TableFilter<string>>();
            foreach (var processing in appConfig.ProcessingLevels)
            {
                processings.Add(new TableFilter<string> { Text = processing, Value = processing });
            }
            this.FilterProcessing = processings.ToArray();

            List<TableFilter<string>> variableTypes = new List<TableFilter<string>>();
            foreach (var variableType in appConfig.VariableTypes)
            {
                variableTypes.Add(new TableFilter<string> { Text = variableType, Value = variableType });
            }
            this.FilterVariableType = variableTypes.ToArray();
        }
    }
}
