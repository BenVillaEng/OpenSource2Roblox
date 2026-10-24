const { app, BrowserWindow, ipcMain, dialog, shell } = require('electron');
const path = require('path');
const fs = require('fs');
const { spawn } = require('child_process');
const https = require('https');

let mainWindow = null;
let activeEngineProcess = null;

// Paths
const LOCALAPPDATA = process.env.LOCALAPPDATA || path.join(process.env.USERPROFILE, 'AppData', 'Local');
const SETTINGS_PATH = path.join(LOCALAPPDATA, 'OpenSource2Roblox', 'settings.json');
const CACHE_PATH = path.join(LOCALAPPDATA, 'OpenSource2Roblox', 'upload_cache.json');
const DEFAULT_EXPORT_ROOT = path.join(process.env.USERPROFILE, 'Documents', 'Roblox Studio', 'Source2Roblox Exports');
function getEngineExecutablePath() {
    const candidates = [
        path.join(__dirname, '..', 'release', 'OpenSource2Roblox.exe'),
        path.join(path.dirname(process.execPath), 'engine', 'OpenSource2Roblox.exe'),
        path.join(path.dirname(process.execPath), 'OpenSource2Roblox-Engine.exe'),
        path.join(path.dirname(process.execPath), 'OpenSource2Roblox.exe'),
        path.join(process.resourcesPath || '', 'engine', 'OpenSource2Roblox.exe'),
        path.join(__dirname, 'engine', 'OpenSource2Roblox.exe')
    ];

    for (const p of candidates) {
        if (p && fs.existsSync(p)) return p;
    }
    return candidates[0];
}

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 1060,
        height: 740,
        minWidth: 920,
        minHeight: 640,
        frame: false,
        backgroundColor: '#4c5844',
        icon: path.join(__dirname, 'assets', 'lambda.ico'),
        webPreferences: {
            preload: path.join(__dirname, 'preload.js'),
            contextIsolation: true,
            nodeIntegration: false
        }
    });

    mainWindow.loadFile(path.join(__dirname, 'index.html'));

    mainWindow.on('closed', () => {
        if (activeEngineProcess) {
            try { activeEngineProcess.kill(); } catch (e) {}
            activeEngineProcess = null;
        }
        mainWindow = null;
    });
}

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit();
});

// Window controls
ipcMain.on('window-minimize', () => {
    if (mainWindow) mainWindow.minimize();
});

ipcMain.on('window-close', () => {
    if (mainWindow) mainWindow.close();
});

// Settings Management
ipcMain.handle('get-settings', async () => {
    try {
        if (fs.existsSync(SETTINGS_PATH)) {
            const raw = fs.readFileSync(SETTINGS_PATH, 'utf8');
            return JSON.parse(raw);
        }
    } catch (err) {
        console.error('Failed to load settings:', err);
    }
    return {
        GameDir: '',
        CustomTexturesDir: '',
        UploadAssets: true,
        UploadMeshes: true,
        RobloxApiKey: '',
        RobloxCreatorType: 'User',
        RobloxCreatorId: ''
    };
});

ipcMain.handle('save-settings', async (event, settings) => {
    try {
        const dir = path.dirname(SETTINGS_PATH);
        if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
        fs.writeFileSync(SETTINGS_PATH, JSON.stringify(settings, null, 2), 'utf8');
        return { success: true };
    } catch (err) {
        return { success: false, error: err.message };
    }
});

// Steam Library & Source Game Discovery
ipcMain.handle('scan-steam-games', async () => {
    const discovered = [];
    const searchRoots = new Set();

    // Check common Steam library paths
    const programFilesX86 = process.env['ProgramFiles(x86)'] || 'C:\\Program Files (x86)';
    searchRoots.add(path.join(programFilesX86, 'Steam'));
    searchRoots.add('D:\\SteamLibrary');
    searchRoots.add('E:\\SteamLibrary');
    searchRoots.add('F:\\SteamLibrary');

    // Parse libraryfolders.vdf if present
    for (const root of Array.from(searchRoots)) {
        const vdfPath = path.join(root, 'steamapps', 'libraryfolders.vdf');
        if (fs.existsSync(vdfPath)) {
            try {
                const content = fs.readFileSync(vdfPath, 'utf8');
                const matches = content.match(/"path"\s+"([^"]+)"/g);
                if (matches) {
                    for (const m of matches) {
                        const libPath = m.replace(/"path"\s+"/, '').replace(/"$/, '').replace(/\\\\/g, '\\');
                        if (fs.existsSync(libPath)) searchRoots.add(libPath);
                    }
                }
            } catch (e) {}
        }
    }

    // Known Source games directory signatures
    const gameSignatures = [
        { name: "Garry's Mod", folder: 'GarrysMod', subDir: 'garrysmod' },
        { name: "Counter-Strike: Global Offensive", folder: 'Counter-Strike Global Offensive', subDir: 'csgo' },
        { name: "Counter-Strike: Source", folder: 'Counter-Strike Source', subDir: 'cstrike' },
        { name: "Half-Life 2", folder: 'Half-Life 2', subDir: 'hl2' },
        { name: "Team Fortress 2", folder: 'Team Fortress 2', subDir: 'tf' },
        { name: "Left 4 Dead 2", folder: 'Left 4 Dead 2', subDir: 'left4dead2' },
        { name: "Portal 2", folder: 'Portal 2', subDir: 'portal2' },
        { name: "Portal", folder: 'Portal', subDir: 'portal' }
    ];

    for (const libRoot of searchRoots) {
        const commonDir = path.join(libRoot, 'steamapps', 'common');
        if (!fs.existsSync(commonDir)) continue;

        for (const sig of gameSignatures) {
            const candidate = path.join(commonDir, sig.folder, sig.subDir);
            const gameInfo = path.join(candidate, 'gameinfo.txt');
            if (fs.existsSync(gameInfo)) {
                // Count available maps
                let mapCount = 0;
                const mapsDir = path.join(candidate, 'maps');
                if (fs.existsSync(mapsDir)) {
                    try {
                        mapCount = fs.readdirSync(mapsDir).filter(f => f.toLowerCase().endsWith('.bsp')).length;
                    } catch (e) {}
                }

                discovered.push({
                    name: sig.name,
                    gameDir: candidate,
                    mapCount
                });
            }
        }
    }

    return discovered;
});

// Scan Maps in Game Folder
ipcMain.handle('scan-maps', async (event, gameDir) => {
    if (!gameDir || !fs.existsSync(gameDir)) return [];

    const mapsDir = path.join(gameDir, 'maps');
    if (!fs.existsSync(mapsDir)) return [];

    try {
        const files = fs.readdirSync(mapsDir);
        const mapList = [];

        for (const file of files) {
            if (file.toLowerCase().endsWith('.bsp')) {
                const fullPath = path.join(mapsDir, file);
                try {
                    const stats = fs.statSync(fullPath);
                    mapList.push({
                        name: file.replace(/\.bsp$/i, ''),
                        fileName: file,
                        sizeBytes: stats.size,
                        sizeFormatted: (stats.size / (1024 * 1024)).toFixed(1) + ' MB',
                        modified: stats.mtime.toLocaleDateString()
                    });
                } catch (e) {
                    mapList.push({
                        name: file.replace(/\.bsp$/i, ''),
                        fileName: file,
                        sizeBytes: 0,
                        sizeFormatted: 'Unknown',
                        modified: ''
                    });
                }
            }
        }

        mapList.sort((a, b) => a.name.localeCompare(b.name));
        return mapList;
    } catch (err) {
        console.error('Error scanning maps:', err);
        return [];
    }
});

// Map Asset Inspection
ipcMain.handle('inspect-map', async (event, { gameDir, mapName }) => {
    const executablePath = getEngineExecutablePath();
    if (!fs.existsSync(executablePath)) {
        return { success: false, error: `Engine executable not found at: ${executablePath}` };
    }
    if (!gameDir || !mapName) {
        return { success: false, error: 'Both Source game directory and map name are required.' };
    }

    return new Promise((resolve) => {
        const args = ['-game', gameDir, '-inspect', mapName];
        const proc = spawn(executablePath, args, {
            cwd: path.dirname(executablePath),
            windowsHide: true
        });

        let inspectionData = null;
        let errorMessage = null;
        let stdoutRaw = '';

        proc.stdout.on('data', (data) => {
            const lines = data.toString().split(/\r?\n/);
            for (const line of lines) {
                if (!line.trim()) continue;
                stdoutRaw += line + '\n';
                if (line.startsWith('S2R_EVENT ')) {
                    try {
                        const json = JSON.parse(line.substring(10));
                        if (json.type === 'map_inspection') {
                            inspectionData = json.data;
                        } else if (json.type === 'error') {
                            errorMessage = json.message;
                        }
                    } catch (e) {}
                }
            }
        });

        proc.stderr.on('data', (data) => {
            const text = data.toString().trim();
            if (text) {
                errorMessage = (errorMessage ? errorMessage + ' | ' : '') + text;
            }
        });

        proc.on('close', (code) => {
            if (inspectionData) {
                resolve({ success: true, data: inspectionData });
            } else if (errorMessage) {
                resolve({ success: false, error: errorMessage });
            } else if (code !== 0) {
                resolve({ success: false, error: `Inspection exited with code ${code}.\n${stdoutRaw}` });
            } else {
                resolve({ success: false, error: 'No asset inspection data received from engine.' });
            }
        });

        proc.on('error', (err) => {
            resolve({ success: false, error: `Failed to spawn inspector: ${err.message}` });
        });
    });
});

// Dialogs
ipcMain.handle('browse-folder', async (event, title) => {
    if (!mainWindow) return null;
    const res = await dialog.showOpenDialog(mainWindow, {
        title: title || 'Select Folder',
        properties: ['openDirectory']
    });
    if (!res.canceled && res.filePaths.length > 0) {
        return res.filePaths[0];
    }
    return null;
});

ipcMain.handle('browse-file', async (event, filters) => {
    if (!mainWindow) return null;
    const res = await dialog.showOpenDialog(mainWindow, {
        title: 'Select File',
        properties: ['openFile'],
        filters: filters || [{ name: 'All Files', extensions: ['*'] }]
    });
    if (!res.canceled && res.filePaths.length > 0) {
        return res.filePaths[0];
    }
    return null;
});

// Roblox Credentials Validation
ipcMain.handle('verify-roblox-credentials', async (event, creds) => {
    const { apiKey, creatorType, creatorId } = creds;

    if (!apiKey || !apiKey.trim()) {
        return { success: false, message: 'API Key cannot be empty.' };
    }
    if (!creatorId || !creatorId.trim()) {
        return { success: false, message: 'Creator ID cannot be empty.' };
    }

    return new Promise((resolve) => {
        const req = https.request('https://apis.roblox.com/assets/v1/operations/test-probe', {
            method: 'GET',
            headers: {
                'x-api-key': apiKey.trim()
            },
            timeout: 8000
        }, (res) => {
            const statusCode = res.statusCode;

            if (statusCode === 401) {
                resolve({ success: false, message: 'HTTP 401: Unauthorized. The provided Roblox API key is invalid.' });
            } else if (statusCode === 403) {
                resolve({ success: false, message: 'HTTP 403: Forbidden. API key lacks permissions or has IP restrictions.' });
            } else if (statusCode === 404 || statusCode === 400 || (statusCode >= 200 && statusCode < 300)) {
                // The gateway authenticated the key! 404 is expected because 'test-probe' does not exist.
                resolve({
                    success: true,
                    message: `Roblox Open Cloud credentials authenticated successfully (${creatorType} ID: ${creatorId}).`
                });
            } else {
                resolve({ success: false, message: `Roblox API returned HTTP ${statusCode}: ${res.statusMessage}` });
            }
        });

        req.on('error', (e) => {
            resolve({ success: false, message: `Network error connecting to Roblox Open Cloud: ${e.message}` });
        });

        req.on('timeout', () => {
            req.destroy();
            resolve({ success: false, message: 'Connection to Roblox Open Cloud timed out.' });
        });

        req.end();
    });
});

// Cache Stats & Management
ipcMain.handle('get-cache-stats', async () => {
    try {
        if (fs.existsSync(CACHE_PATH)) {
            const raw = fs.readFileSync(CACHE_PATH, 'utf8');
            const data = JSON.parse(raw);
            const count = Object.keys(data).length;
            const stats = fs.statSync(CACHE_PATH);
            return {
                exists: true,
                count,
                sizeFormatted: (stats.size / 1024).toFixed(1) + ' KB'
            };
        }
    } catch (e) {}
    return { exists: false, count: 0, sizeFormatted: '0 KB' };
});

ipcMain.handle('clear-upload-cache', async () => {
    try {
        if (fs.existsSync(CACHE_PATH)) {
            fs.unlinkSync(CACHE_PATH);
        }
        return { success: true, message: 'Upload cache cleared successfully.' };
    } catch (e) {
        return { success: false, message: e.message };
    }
});

// Launch Conversion Process
ipcMain.handle('start-conversion', async (event, config) => {
    if (activeEngineProcess) {
        return { success: false, error: 'A conversion job is already running.' };
    }

    const executablePath = getEngineExecutablePath();
    if (!fs.existsSync(executablePath)) {
        return { success: false, error: `Engine executable not found at: ${executablePath}` };
    }

    const args = [];

    if (config.gameDir) {
        args.push('-game', config.gameDir);
    }
    if (config.mapName) {
        args.push('-map', config.mapName);
    }
    if (config.modelName) {
        args.push('-model', config.modelName);
    }
    if (config.vtfName) {
        args.push('-vtf', config.vtfName);
    }
    if (config.outputDir) {
        args.push('-output', config.outputDir);
    }
    if (config.uploadAssets) {
        args.push('-upload', 'true');
    }
    if (config.uploadMeshes) {
        args.push('-uploadMeshes', 'true');
    }
    if (config.robloxApiKey) {
        args.push('-robloxApiKey', config.robloxApiKey);
    }
    if (config.robloxCreatorType) {
        args.push('-robloxCreatorType', config.robloxCreatorType.toLowerCase());
    }
    if (config.robloxCreatorId) {
        args.push('-robloxCreatorId', config.robloxCreatorId);
    }
    if (config.customTexturesDir) {
        args.push('-customTexturesDir', config.customTexturesDir);
    }

    try {
        activeEngineProcess = spawn(executablePath, args, {
            cwd: path.dirname(executablePath),
            windowsHide: true
        });

        activeEngineProcess.stdout.on('data', (data) => {
            const lines = data.toString().split(/\r?\n/);
            for (const line of lines) {
                if (!line.trim()) continue;

                if (line.startsWith('S2R_EVENT ')) {
                    try {
                        const json = JSON.parse(line.substring(10));
                        mainWindow?.webContents.send('engine-event', json);
                    } catch (e) {
                        mainWindow?.webContents.send('engine-event', { type: 'raw', message: line });
                    }
                } else {
                    mainWindow?.webContents.send('engine-event', { type: 'raw', message: line });
                }
            }
        });

        activeEngineProcess.stderr.on('data', (data) => {
            const text = data.toString().trim();
            if (text) {
                mainWindow?.webContents.send('engine-event', { type: 'error', message: text });
            }
        });

        activeEngineProcess.on('close', (code) => {
            activeEngineProcess = null;
            mainWindow?.webContents.send('engine-exit', { code });
        });

        return { success: true };
    } catch (err) {
        activeEngineProcess = null;
        return { success: false, error: err.message };
    }
});

// Cancel Conversion
ipcMain.handle('cancel-conversion', async () => {
    if (activeEngineProcess) {
        try {
            activeEngineProcess.kill();
            activeEngineProcess = null;
            return { success: true };
        } catch (e) {
            return { success: false, error: e.message };
        }
    }
    return { success: true };
});

// Open Output Directory
ipcMain.handle('open-output-folder', async (event, customPath) => {
    const target = customPath && fs.existsSync(customPath) ? customPath : DEFAULT_EXPORT_ROOT;
    if (!fs.existsSync(target)) {
        fs.mkdirSync(target, { recursive: true });
    }
    shell.openPath(target);
    return { success: true };
});

// Open in Roblox Studio
ipcMain.handle('open-in-studio', async (event, targetFile) => {
    if (!targetFile || !fs.existsSync(targetFile)) {
        return { success: false, error: 'Target file not found.' };
    }

    // Attempt to locate RobloxStudioBeta.exe in LocalAppData
    const versionsDir = path.join(LOCALAPPDATA, 'Roblox', 'Versions');
    let studioExe = null;

    if (fs.existsSync(versionsDir)) {
        try {
            const subdirs = fs.readdirSync(versionsDir);
            for (const dir of subdirs) {
                const candidate = path.join(versionsDir, dir, 'RobloxStudioBeta.exe');
                if (fs.existsSync(candidate)) {
                    studioExe = candidate;
                    break;
                }
            }
        } catch (e) {}
    }

    if (studioExe) {
        spawn(studioExe, [targetFile], { detached: true, stdio: 'ignore' }).unref();
        return { success: true };
    } else {
        // Fallback to default shell association
        shell.openPath(targetFile);
        return { success: true, fallback: true };
    }
});
