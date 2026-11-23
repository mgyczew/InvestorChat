const storageKey = "investorchat.watchlist.guest";
const legacyStorageKey = "investorchat.watchlist";

export function load() {
    const value = localStorage.getItem(storageKey);
    if (!value) {
        return null;
    }

    try {
        let symbols = JSON.parse(value);
        if (typeof symbols === "string") {
            symbols = JSON.parse(symbols);
        }

        if (!Array.isArray(symbols)) {
            return null;
        }

        return symbols
            .map(item => typeof item === "string" ? item : item?.symbol ?? item?.Symbol ?? item?.ticker ?? item?.Ticker)
            .filter(symbol => typeof symbol === "string" && symbol.trim().length > 0);
    } catch {
        return null;
    }
}

export function loadLegacy() {
    const value = localStorage.getItem(legacyStorageKey);
    if (!value) {
        return null;
    }

    try {
        let symbols = JSON.parse(value);
        if (typeof symbols === "string") {
            symbols = JSON.parse(symbols);
        }

        return Array.isArray(symbols)
            ? symbols.map(item => typeof item === "string" ? item : item?.symbol ?? item?.Symbol ?? item?.ticker ?? item?.Ticker)
                .filter(symbol => typeof symbol === "string" && symbol.trim().length > 0)
            : null;
    } catch {
        return null;
    }
}

export function save(symbols) {
    localStorage.setItem(storageKey, JSON.stringify(symbols));
}
