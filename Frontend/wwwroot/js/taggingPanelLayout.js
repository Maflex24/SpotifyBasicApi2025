window.taggingPanelLayout = (() => {
    const layouts = new Map();
    const minimumHeight = 16 * 16;
    const topGap = 12;
    const bottomGap = 8;
    const fallbackTopGap = 12;

    function scheduleUpdate(panel) {
        const layout = layouts.get(panel);
        if (!layout || layout.frameId) return;

        layout.frameId = window.requestAnimationFrame(() => {
            layout.frameId = 0;
            update(panel);
        });
    }

    function update(panel) {
        const layout = layouts.get(panel);
        if (!layout) return;

        const viewportHeight = window.visualViewport?.height ?? window.innerHeight;
        const playerBar = document.querySelector(".player-bar");
        const controlsDock = document.querySelector(".library-controls-dock");
        const playerTop = playerBar ? playerBar.getBoundingClientRect().top : viewportHeight;
        const playerOffset = playerBar
            ? Math.max(bottomGap, Math.ceil(viewportHeight - playerTop + bottomGap))
            : 64;
        const panelBottom = viewportHeight - playerOffset;
        const controlsRect = controlsDock?.getBoundingClientRect();
        const controlsBottom = controlsRect && controlsRect.bottom > 0 && controlsRect.top < panelBottom
            ? Math.min(Math.max(controlsRect.bottom, fallbackTopGap), panelBottom)
            : fallbackTopGap;
        const availableHeight = Math.max(minimumHeight, Math.floor(panelBottom - controlsBottom - topGap));

        panel.style.setProperty("--tagging-panel-bottom-offset", `${playerOffset}px`);
        panel.style.setProperty("--tagging-panel-available-height", `${availableHeight}px`);
    }

    function observe(panel) {
        disconnect(panel);

        const layout = {
            frameId: 0,
            resizeObserver: null,
            onViewportChange: null,
            onScroll: null
        };

        layout.onViewportChange = () => scheduleUpdate(panel);
        layout.onScroll = () => scheduleUpdate(panel);
        layout.resizeObserver = new ResizeObserver(() => scheduleUpdate(panel));

        const playerBar = document.querySelector(".player-bar");
        const controlsDock = document.querySelector(".library-controls-dock");
        if (playerBar) layout.resizeObserver.observe(playerBar);
        if (controlsDock) layout.resizeObserver.observe(controlsDock);

        window.addEventListener("resize", layout.onViewportChange);
        window.visualViewport?.addEventListener("resize", layout.onViewportChange);
        window.visualViewport?.addEventListener("scroll", layout.onViewportChange);
        window.addEventListener("scroll", layout.onScroll, true);

        layouts.set(panel, layout);
        scheduleUpdate(panel);
    }

    function disconnect(panel) {
        const layout = layouts.get(panel);
        if (!layout) return;

        if (layout.frameId) window.cancelAnimationFrame(layout.frameId);
        layout.resizeObserver?.disconnect();
        window.removeEventListener("resize", layout.onViewportChange);
        window.visualViewport?.removeEventListener("resize", layout.onViewportChange);
        window.visualViewport?.removeEventListener("scroll", layout.onViewportChange);
        window.removeEventListener("scroll", layout.onScroll, true);
        layouts.delete(panel);
    }

    return { observe, disconnect };
})();
