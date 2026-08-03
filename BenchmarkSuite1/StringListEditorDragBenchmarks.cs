using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using Microsoft.VSDiagnostics;

namespace Caf.Midden.Wasm.Benchmarks
{
    [CPUUsageDiagnoser]
    public class StringListEditorDragBenchmarks
    {
        private List<string> _items = null!;
        private int? _dragSourceIndex;
        private int? _dragOverIndex;
        [Params(10, 40, 100)]
        public int ItemCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _items = new List<string>(ItemCount);
            for (int i = 0; i < ItemCount; i++)
                _items.Add($"Item {i}");
        }

        /// <summary>
        /// Simulates moving the mouse over every row during a single drag gesture.
        /// Each call to OnDragEnter triggers StateHasChanged in the real component.
        /// Total re-renders = ItemCount.
        /// </summary>
        [Benchmark(Description = "DragEnter_FullSweep – re-renders per drag gesture")]
        public int DragEnter_FullSweep()
        {
            _dragSourceIndex = 0;
            int changes = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                // OnDragEnter body
                _dragOverIndex = i;
                changes++;
            }

            _dragSourceIndex = null;
            _dragOverIndex = null;
            return changes; // == ItemCount; shows re-render count to the profiler
        }

        /// <summary>
        /// Simulates the list mutation in OnDrop: remove source, re-insert at target.
        /// Called once per drop, but mutates a list that is fully re-rendered afterward.
        /// </summary>
        [Benchmark(Description = "OnDrop_ListMutation – cost of reorder")]
        public List<string> OnDrop_ListMutation()
        {
            // Reset list each iteration via a local copy to avoid state bleed.
            var items = new List<string>(_items);
            int source = 0;
            int target = items.Count - 1;
            var item = items[source];
            items.RemoveAt(source);
            int insertAt = source < target ? target - 1 : target;
            items.Insert(insertAt, item);
            return items;
        }

        /// <summary>
        /// Simulates the optimized path where OnDragEnter skips the assignment
        /// (and therefore StateHasChanged) when the index hasn't changed.
        /// Shows the reduction in re-renders from a guard check.
        /// </summary>
        [Benchmark(Description = "DragEnter_WithGuard – re-renders with ShouldRender guard")]
        public int DragEnter_WithGuard()
        {
            _dragSourceIndex = 0;
            int changes = 0;
            int previousOver = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                // Simulated rapid hover: each index visited twice (common in browser events)
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    if (_dragOverIndex != i)
                    {
                        _dragOverIndex = i;
                        changes++; // only this path triggers StateHasChanged
                    }
                }

                previousOver = i;
            }

            _dragSourceIndex = null;
            _dragOverIndex = null;
            return changes;
        }
    }
}