window.homeSearchInterop = {
    registerShortcut: function (inputId) {
        this._handler = function (e) {
            var target = e.target;
            var isTyping = target && (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable);

            if ((e.key === 'k' && (e.ctrlKey || e.metaKey)) || (e.key === '/' && !isTyping)) {
                var el = document.getElementById(inputId);
                if (el) {
                    e.preventDefault();
                    el.focus();
                }
            }
            else if (e.key === 'Escape' && target && target.id === inputId) {
                target.blur();
            }
        };
        document.addEventListener('keydown', this._handler);
    },
    unregisterShortcut: function () {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
    }
};
