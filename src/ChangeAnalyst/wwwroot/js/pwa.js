// Service worker registration, update detection and connectivity events.
// Loaded as a module by index.html and imported again by Blazor (same module instance).
let waitingWorker = null;
let updateListener = null;

function markUpdateReady(worker) {
    waitingWorker = worker;
    updateListener?.invokeMethodAsync('OnUpdateAvailable');
}

if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).then(registration => {
        if (registration.waiting && navigator.serviceWorker.controller) {
            markUpdateReady(registration.waiting);
        }
        registration.addEventListener('updatefound', () => {
            const worker = registration.installing;
            worker?.addEventListener('statechange', () => {
                if (worker.state === 'installed' && navigator.serviceWorker.controller) {
                    markUpdateReady(worker);
                }
            });
        });
        // Long-lived tabs: check for a new version every hour.
        setInterval(() => registration.update(), 60 * 60 * 1000);
    });
}

export function watchUpdates(dotnetRef) {
    updateListener = dotnetRef;
    if (waitingWorker) {
        dotnetRef.invokeMethodAsync('OnUpdateAvailable');
    }
}

export function applyUpdate() {
    if (!waitingWorker) {
        location.reload();
        return;
    }
    navigator.serviceWorker.addEventListener('controllerchange', () => location.reload(), { once: true });
    waitingWorker.postMessage({ type: 'SKIP_WAITING' });
}

export function watchConnectivity(dotnetRef) {
    const notify = () => dotnetRef.invokeMethodAsync('OnConnectivityChanged', navigator.onLine);
    window.addEventListener('online', notify);
    window.addEventListener('offline', notify);
    return navigator.onLine;
}
