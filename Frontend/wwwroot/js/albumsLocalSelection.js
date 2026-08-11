window.albumsLocalSelection = (() => {
    function getTopOffset() {
        const controls = document.querySelector(".library-controls-dock");
        return Math.ceil(controls?.getBoundingClientRect().height ?? 0) + 12;
    }

    function scrollToAlbum(albumId) {
        if (!albumId) return false;

        const target = Array.from(document.querySelectorAll("[data-album-card-id]"))
            .find(element => element.dataset.albumCardId === albumId);
        if (!target) return false;

        const top = Math.max(0, window.scrollY + target.getBoundingClientRect().top - getTopOffset());
        window.scrollTo({ top, behavior: "smooth" });
        return true;
    }

    return { scrollToAlbum };
})();
