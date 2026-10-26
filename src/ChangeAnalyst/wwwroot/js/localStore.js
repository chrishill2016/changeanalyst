// Minimal IndexedDB wrapper. Values are JSON strings serialised on the .NET side.
const dbName = 'changeanalyst';
const dbVersion = 1;
const stores = ['entities', 'outbox', 'meta'];

let dbPromise;

function openDb() {
    dbPromise ??= new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, dbVersion);
        request.onupgradeneeded = () => {
            for (const store of stores) {
                if (!request.result.objectStoreNames.contains(store)) {
                    request.result.createObjectStore(store);
                }
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
    return dbPromise;
}

async function run(store, mode, action) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(store, mode);
        const request = action(tx.objectStore(store));
        tx.oncomplete = () => resolve(request.result);
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error);
    });
}

export const get = (store, key) => run(store, 'readonly', s => s.get(key)).then(v => v ?? null);
export const getAll = store => run(store, 'readonly', s => s.getAll());
export const put = (store, key, value) => run(store, 'readwrite', s => s.put(value, key)).then(() => { });
export const remove = (store, key) => run(store, 'readwrite', s => s.delete(key)).then(() => { });
