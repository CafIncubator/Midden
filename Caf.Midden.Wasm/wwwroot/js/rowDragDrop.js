// Allows dropping on table rows that opt in with the "midden-droppable-row" class.
// Blazor attribute splatting cannot use "@ondragover:preventDefault", so a single
// delegated listener enables the drop target for those rows.
(function () {
    document.addEventListener('dragover', function (e) {
        var target = e.target;

        if (target && target.nodeType !== 1) {
            target = target.parentElement;
        }

        if (target && target.closest && target.closest('.midden-droppable-row')) {
            e.preventDefault();
        }
    }, true);
})();
