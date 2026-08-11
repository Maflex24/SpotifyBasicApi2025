window.albumsArtistNavigation = (() => {
    let dotNetRef = null;
    let frameId = null;

    const groupSelector = ".album-artist-group[data-artist-group-id]";

    function getGroups() {
        return Array.from(document.querySelectorAll(groupSelector));
    }

    function getTopOffset() {
        const controls = document.querySelector(".library-controls-dock");
        return Math.ceil(controls?.getBoundingClientRect().height ?? 0) + 12;
    }

    function getCurrentIndex(groups) {
        if (groups.length === 0) return -1;

        const threshold = getTopOffset() + 1;
        let current = 0;

        for (let index = 0; index < groups.length; index++) {
            if (groups[index].getBoundingClientRect().top <= threshold)
                current = index;
            else
                break;
        }

        return current;
    }

    function refresh() {
        frameId = null;
        if (!dotNetRef) return;

        const groups = getGroups();
        const current = getCurrentIndex(groups);
        dotNetRef.invokeMethodAsync(
            "SetArtistNavigationState",
            current > 0,
            current >= 0 && current < groups.length - 1);
    }

    function scheduleRefresh() {
        if (frameId !== null) return;
        frameId = window.requestAnimationFrame(refresh);
    }

    function observe(nextDotNetRef) {
        disconnect();
        dotNetRef = nextDotNetRef;
        window.addEventListener("scroll", scheduleRefresh, { passive: true });
        window.addEventListener("resize", scheduleRefresh);
        scheduleRefresh();
    }

    function move(direction) {
        const groups = getGroups();
        const current = getCurrentIndex(groups);
        const targetIndex = current + Math.sign(direction);
        if (targetIndex < 0 || targetIndex >= groups.length) {
            scheduleRefresh();
            return;
        }

        const target = groups[targetIndex];
        const top = Math.max(0, window.scrollY + target.getBoundingClientRect().top - getTopOffset());
        window.scrollTo({ top, behavior: "smooth" });
        window.setTimeout(scheduleRefresh, 350);
    }

    function disconnect() {
        window.removeEventListener("scroll", scheduleRefresh);
        window.removeEventListener("resize", scheduleRefresh);
        if (frameId !== null) {
            window.cancelAnimationFrame(frameId);
            frameId = null;
        }
        dotNetRef = null;
    }

    return { observe, refresh: scheduleRefresh, move, disconnect };
})();
