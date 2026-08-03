using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Caf.Midden.Wasm.Shared
{
    public partial class GeoJsonEditorMap : IAsyncDisposable
    {
        [Inject]
        public IJSRuntime JS { get; set; }

        [Inject]
        public IMessageService MessageService { get; set; }

        /// <summary>
        /// Bare geojson geometry, e.g. {"type":"Polygon","coordinates":[[[...]]]}
        /// </summary>
        [Parameter]
        public string Value { get; set; }

        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        [Parameter]
        public string Height { get; set; } = "300px";

        [Parameter]
        public bool ReadOnly { get; set; }

        private readonly string mapElementId = $"geojson-editor-map-{Guid.NewGuid():N}";

        private ElementReference mapElement;
        private IJSObjectReference mapModule;
        private IJSObjectReference mapState;
        private DotNetObjectReference<GeoJsonEditorMap> selfReference;

        private string appliedValue;
        private string rawGeoJson;
        private string validationMessage;
        private bool rawGeoJsonVisible;

        // The geometry in place before the most recent draw-replacement; null when there is nothing to undo
        private string previousGeometry;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                selfReference = DotNetObjectReference.Create(this);

                mapModule = await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./geojsonEditorMap.js");

                mapState = await mapModule.InvokeAsync<IJSObjectReference>(
                    "create", mapElement, Value, selfReference, ReadOnly);

                appliedValue = Value;

                return;
            }

            // Pushes externally changed values (saved geometry template, raw editor) to the map
            if (mapState != null && !string.Equals(appliedValue, Value, StringComparison.Ordinal))
            {
                appliedValue = Value;

                var isValid = await mapModule.InvokeAsync<bool>("setGeometry", mapState, Value);

                SetValidationMessage(isValid);

                StateHasChanged();
            }
        }

        /// <summary>
        /// Called by the map whenever a shape is drawn, edited, dragged or removed.
        /// </summary>
        [JSInvokable]
        public async Task OnGeometryChangedFromMap(string geometry)
        {
            appliedValue = geometry;
            Value = geometry;
            validationMessage = null;

            // An edit/drag/remove is a deliberate action on the current shape;
            // the undo opportunity only exists immediately after a replacement
            previousGeometry = null;

            await ValueChanged.InvokeAsync(geometry);

            StateHasChanged();
        }

        /// <summary>
        /// Called by the map when a newly drawn shape replaces an existing one.
        /// Stores the displaced geometry and shows a brief toast with an Undo action.
        /// </summary>
        [JSInvokable]
        public async Task OnShapeReplaced(string displaced)
        {
            previousGeometry = displaced;

            await MessageService.Info(new MessageConfig
            {
                Content = BuildUndoContent(),
                Duration = 5,
                Key = "geojson-shape-replaced"
            });
        }

        private RenderFragment BuildUndoContent() => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, "Previous shape replaced.\u00A0");
            builder.OpenElement(2, "a");
            builder.AddAttribute(3, "href", "javascript:void(0)");
            builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, UndoReplacement));
            builder.AddContent(5, "Undo");
            builder.CloseElement();
            builder.CloseElement();
        };

        private async Task UndoReplacement()
        {
            if (string.IsNullOrEmpty(previousGeometry))
            {
                return;
            }

            var geometryToRestore = previousGeometry;
            previousGeometry = null;

            appliedValue = geometryToRestore;
            Value = geometryToRestore;
            validationMessage = null;

            await mapModule.InvokeAsync<bool>("setGeometry", mapState, geometryToRestore);

            await ValueChanged.InvokeAsync(geometryToRestore);

            StateHasChanged();
        }

        /// <summary>
        /// Called by the raw GeoJSON control on the map's toolbar.
        /// </summary>
        [JSInvokable]
        public void ShowRawGeoJson()
        {
            rawGeoJson = Value;
            rawGeoJsonVisible = true;

            StateHasChanged();
        }

        private async Task OnRawGeoJsonOk(MouseEventArgs args)
        {
            rawGeoJsonVisible = false;

            // The raw text is kept even when it isn't valid geojson; the map keeps the last valid shape
            appliedValue = rawGeoJson;
            Value = rawGeoJson;

            // Deliberate manual edit — clear any pending undo
            previousGeometry = null;

            var isValid = await mapModule.InvokeAsync<bool>("setGeometry", mapState, rawGeoJson);

            SetValidationMessage(isValid);

            await ValueChanged.InvokeAsync(rawGeoJson);
        }

        private void OnRawGeoJsonCancel(MouseEventArgs args)
        {
            rawGeoJsonVisible = false;
        }

        private void SetValidationMessage(bool isValid)
        {
            validationMessage = isValid
                ? null
                : "The GeoJSON is not a valid geometry, so the map still shows the last valid shape.";
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (mapState != null)
                {
                    if (mapModule != null)
                    {
                        await mapModule.InvokeVoidAsync("dispose", mapState);
                    }

                    await mapState.DisposeAsync();
                }

                if (mapModule != null)
                {
                    await mapModule.DisposeAsync();
                }
            }
            catch (JSDisconnectedException)
            {
                // The circuit/page is already gone, nothing to clean up
            }

            selfReference?.Dispose();
        }
    }
}
