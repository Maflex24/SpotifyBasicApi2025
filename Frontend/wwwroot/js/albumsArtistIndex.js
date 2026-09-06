window.albumsArtistIndex = (() => {
    let root = null;
    let dotNetRef = null;

    function handlePointerDown(event) {
        if (!root || root.contains(event.target)) return;
        dotNetRef?.invokeMethodAsync("CloseArtistNavigationPanel");
    }

    function observeOutsideClick(rootElement, nextDotNetRef) {
        disconnectOutsideClick();
        root = rootElement;
        dotNetRef = nextDotNetRef;
        document.addEventListener("pointerdown", handlePointerDown);
    }

    function disconnectOutsideClick() {
        document.removeEventListener("pointerdown", handlePointerDown);
        root = null;
        dotNetRef = null;
    }

    return { observeOutsideClick, disconnectOutsideClick };
})();
