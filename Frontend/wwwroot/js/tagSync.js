window.tagSync = (function () {
    let listeners = new Set();

    function notify(itemType, id, tags) {
        try {
            const detail = { itemType, id, tags };
            window.dispatchEvent(new CustomEvent('tagsChanged', { detail }));
        } catch (e) { }
    }

    function register(dotNetRef, methodName) {
        if (!dotNetRef || !methodName) return;
        const handler = (e) => {
            try {
                const d = e.detail || {};
                if (d.itemType && d.id && Array.isArray(d.tags)) {
                    dotNetRef.invokeMethodAsync(methodName, d.itemType, d.id, d.tags).catch(() => {});
                }
            } catch (err) { }
        };
        listeners.add({ dotNetRef, handler });
        window.addEventListener('tagsChanged', handler);
    }

    function unregister(dotNetRef) {
        for (const li of Array.from(listeners)) {
            if (li.dotNetRef === dotNetRef) {
                window.removeEventListener('tagsChanged', li.handler);
                listeners.delete(li);
            }
        }
    }

    return { notify, register, unregister };
})();


