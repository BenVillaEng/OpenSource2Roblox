const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('api', {
    // Window control
    minimizeWindow: () => ipcRenderer.send('window-minimize'),
    closeWindow: () => ipcRenderer.send('window-close'),

    // Steam & Game Discovery
    scanSteamGames: () => ipcRenderer.invoke('scan-steam-games'),
    scanMaps: (gameDir) => ipcRenderer.invoke('scan-maps', gameDir),
    inspectMap: (gameDir, mapName) => ipcRenderer.invoke('inspect-map', { gameDir, mapName }),
    browseFolder: (title) => ipcRenderer.invoke('browse-folder', title),
    browseFile: (filters) => ipcRenderer.invoke('browse-file', filters),

    // Settings
    getSettings: () => ipcRenderer.invoke('get-settings'),
    saveSettings: (settings) => ipcRenderer.invoke('save-settings', settings),

    // Roblox Cloud API
    verifyRobloxCredentials: (credentials) => ipcRenderer.invoke('verify-roblox-credentials', credentials),
    getCacheStats: () => ipcRenderer.invoke('get-cache-stats'),
    clearUploadCache: () => ipcRenderer.invoke('clear-upload-cache'),

    // Conversion Engine Control
    startConversion: (jobConfig) => ipcRenderer.invoke('start-conversion', jobConfig),
    cancelConversion: () => ipcRenderer.invoke('cancel-conversion'),
    openOutputFolder: (customPath) => ipcRenderer.invoke('open-output-folder', customPath),
    openInStudio: (filePath) => ipcRenderer.invoke('open-in-studio', filePath),

    // Realtime Event Stream
    onEngineEvent: (callback) => {
        const handler = (event, data) => callback(data);
        ipcRenderer.on('engine-event', handler);
        return () => ipcRenderer.removeListener('engine-event', handler);
    },
    onEngineExit: (callback) => {
        const handler = (event, data) => callback(data);
        ipcRenderer.on('engine-exit', handler);
        return () => ipcRenderer.removeListener('engine-exit', handler);
    }
});
