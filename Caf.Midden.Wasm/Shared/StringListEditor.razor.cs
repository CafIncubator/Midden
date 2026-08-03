using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class StringListEditor : ComponentBase
    {
        [Parameter]
        public List<string> Items { get; set; } = new List<string>();

        [Parameter]
        public EventCallback<List<string>> ItemsChanged { get; set; }

        [Parameter]
        public string? Placeholder { get; set; }

        [Parameter]
        public string? ItemLabel { get; set; }

        private async Task AddItem()
        {
            Items.Add(string.Empty);
            await ItemsChanged.InvokeAsync(Items);
        }

        private async Task RemoveItem(int index)
        {
            Items.RemoveAt(index);
            await ItemsChanged.InvokeAsync(Items);
        }

        private async Task UpdateItem(int index, string value)
        {
            Items[index] = value;
            await ItemsChanged.InvokeAsync(Items);
        }

        private int? _dragSourceIndex;
        private int? _dragOverIndex;
        private bool _suppressRender;

        private void OnDragStart(int index)
        {
            _dragSourceIndex = index;
        }

        private void OnDragEnter(int index)
        {
            if (_dragOverIndex == index) return;
            _dragOverIndex = index;
        }

        protected override bool ShouldRender()
        {
            if (_suppressRender)
            {
                _suppressRender = false;
                return false;
            }
            return true;
        }

        private async Task OnDrop(int index)
        {
            _dragOverIndex = null;

            if (_dragSourceIndex is null || _dragSourceIndex.Value == index)
            {
                _dragSourceIndex = null;
                return;
            }

            var item = Items[_dragSourceIndex.Value];
            Items.RemoveAt(_dragSourceIndex.Value);

            var insertAt = _dragSourceIndex.Value < index ? index - 1 : index;
            Items.Insert(insertAt, item);

            _dragSourceIndex = null;

            await ItemsChanged.InvokeAsync(Items);
        }

        private void OnDragEnd()
        {
            _dragSourceIndex = null;
            _dragOverIndex = null;
        }
    }
}
