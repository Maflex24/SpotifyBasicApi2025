window.infiniteScroll = (function () {
	let observer = null;

	function observe(containerEl, sentinelEl, dotNetRef) {
		if (!containerEl || !sentinelEl || !dotNetRef)
			return;

		if (observer)
			observer.disconnect();

		observer = new IntersectionObserver((entries) => {
			for (const entry of entries) {
				if (entry.isIntersecting) {
					dotNetRef.invokeMethodAsync('OnBottomReached');
				}
			}
		}, { root: containerEl, rootMargin: '0px', threshold: 0.1 });

		observer.observe(sentinelEl);
	}

	function observeWindow(sentinelEl, dotNetRef) {
		if (!sentinelEl || !dotNetRef)
			return;

		if (observer)
			observer.disconnect();

		observer = new IntersectionObserver((entries) => {
			for (const entry of entries) {
				if (entry.isIntersecting) {
					dotNetRef.invokeMethodAsync('OnBottomReached');
				}
			}
		}, { root: null, rootMargin: '0px', threshold: 0.1 });

		observer.observe(sentinelEl);
	}

	function disconnect() {
		if (observer) {
			observer.disconnect();
			observer = null;
		}
	}

	return { observe, observeWindow, disconnect };
})();


