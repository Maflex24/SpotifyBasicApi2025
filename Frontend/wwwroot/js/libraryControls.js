window.libraryControls = (() => {
    let observer = null;

    function observe(sentinel, dotNetRef) {
        disconnect();

        observer = new IntersectionObserver(entries => {
            const entry = entries[0];
            dotNetRef.invokeMethodAsync("SetControlsPastAnchor", entry.boundingClientRect.top < 0);
        }, { threshold: 0 });

        observer.observe(sentinel);
    }

    function disconnect() {
        if (observer) {
            observer.disconnect();
            observer = null;
        }
    }

    return { observe, disconnect };
})();
