window.folderSync = (function () {
    let dotNetRef = null;

    function register(ref, method) {
        dotNetRef = { ref, method };
    }

    function notify(folderId) {
        if (!dotNetRef) return;
        dotNetRef.ref.invokeMethodAsync(dotNetRef.method, folderId ?? null);
    }

    return { register, notify };
})();


