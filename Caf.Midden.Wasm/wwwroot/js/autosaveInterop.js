window.autosaveInterop = {
    getItem: function (key) {
        try {
            return window.localStorage.getItem(key);
        } catch (e) {
            console.warn("autosaveInterop.getItem failed for key '" + key + "'", e);
            return null;
        }
    },
    setItem: function (key, value) {
        try {
            window.localStorage.setItem(key, value);
            return true;
        } catch (e) {
            console.warn("autosaveInterop.setItem failed for key '" + key + "'", e);
            return false;
        }
    },
    removeItem: function (key) {
        try {
            window.localStorage.removeItem(key);
        } catch (e) {
            console.warn("autosaveInterop.removeItem failed for key '" + key + "'", e);
        }
    },
    // Registers a single set of listeners (safe to call multiple times) that flush
    // all active autosave registrations right before the tab is closed/reloaded or
    // hidden. Blazor WebAssembly supports synchronous JS interop, so this callback
    // completes before the page actually unloads.
    registerUnloadFlush: function (dotnetRef) {
        if (window.autosaveInterop._unloadRegistered) {
            return;
        }
        window.autosaveInterop._unloadRegistered = true;

        var flush = function () {
            try {
                dotnetRef.invokeMethod("FlushAll");
            } catch (e) {
                console.warn("autosaveInterop flush failed", e);
            }
        };

        window.addEventListener("beforeunload", flush);
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "hidden") {
                flush();
            }
        });
    }
};
