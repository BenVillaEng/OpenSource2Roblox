// ==========================================================================
// OpenSource2Roblox - GoldSource Edition (renderer.js)
// ==========================================================================

document.addEventListener('DOMContentLoaded', async () => {
    // Elements
    const btnMinimize = document.getElementById('btn-minimize');
    const btnClose = document.getElementById('btn-close');
    const tabButtons = document.querySelectorAll('.gs-tab');
    const tabPanels = document.querySelectorAll('.gs-tab-panel');
    const consoleBadge = document.getElementById('console-badge');

    // Maps Tab Elements
    const detectedGamesSelect = document.getElementById('detected-games-select');
    const btnRescanSteam = document.getElementById('btn-rescan-steam');
    const gameDirInput = document.getElementById('game-dir-input');
    const btnBrowseGameDir = document.getElementById('btn-browse-game-dir');
    const mapSearchInput = document.getElementById('map-search-input');
    const mapCountLabel = document.getElementById('map-count-label');
    const mapsTableBody = document.getElementById('maps-table-body');
    const selectedMapInput = document.getElementById('selected-map-input');
    const btnBrowseBsp = document.getElementById('btn-browse-bsp');
    const btnInspectMap = document.getElementById('btn-inspect-map');

    // Inspector Modal Elements
    const modalMapInspect = document.getElementById('modal-map-inspect');
    const btnCloseInspectModal = document.getElementById('btn-close-inspect-modal');
    const btnInspectClose = document.getElementById('btn-inspect-close');
    const btnInspectConvertNow = document.getElementById('btn-inspect-convert-now');
    const inspectModalTitle = document.getElementById('inspect-modal-title');
    const inspectLoading = document.getElementById('inspect-loading');
    const inspectFilterInput = document.getElementById('inspect-filter-input');
    const btnInspectClearFilter = document.getElementById('btn-inspect-clear-filter');
    const inspectFooterInfo = document.getElementById('inspect-footer-info');

    // Metrics elements
    const metricModels = document.getElementById('metric-models');
    const metricPropInstances = document.getElementById('metric-prop-instances');
    const metricTextures = document.getElementById('metric-textures');
    const metricTexSub = document.getElementById('metric-tex-sub');
    const metricFaces = document.getElementById('metric-faces');
    const metricDisplacements = document.getElementById('metric-displacements');
    const metricBrushes = document.getElementById('metric-brushes');
    const metricBspVersion = document.getElementById('metric-bsp-version');
    const metricEntities = document.getElementById('metric-entities');
    const metricSkyname = document.getElementById('metric-skyname');

    // Tab counts & subtabs
    const tabModelCount = document.getElementById('tab-model-count');
    const tabTextureCount = document.getElementById('tab-texture-count');
    const tabEntityCount = document.getElementById('tab-entity-count');
    const inspectSubtabButtons = document.querySelectorAll('.gs-inspect-tab');
    const inspectPanels = document.querySelectorAll('.gs-inspect-panel');
    const inspectModelsTbody = document.getElementById('inspect-models-tbody');
    const inspectTexturesTbody = document.getElementById('inspect-textures-tbody');
    const inspectEntitiesTbody = document.getElementById('inspect-entities-tbody');

    let currentInspection = null;
    let activeInspectSubtab = 'subtab-models';

    // Quick Options
    const chkUploadTextures = document.getElementById('chk-upload-textures');
    const chkUploadMeshes = document.getElementById('chk-upload-meshes');
    const chkOpenStudio = document.getElementById('chk-open-studio');

    // Models & Textures Tab Elements
    const modelPathInput = document.getElementById('model-path-input');
    const modelSkinInput = document.getElementById('model-skin-input');
    const btnBrowseModel = document.getElementById('btn-browse-model');
    const vtfPathInput = document.getElementById('vtf-path-input');
    const btnBrowseVtf = document.getElementById('btn-browse-vtf');
    const customTexturesInput = document.getElementById('custom-textures-input');
    const btnBrowseCustomTex = document.getElementById('btn-browse-custom-tex');

    // Roblox Cloud Elements
    const apiKeyInput = document.getElementById('api-key-input');
    const btnToggleKey = document.getElementById('btn-toggle-key-visibility');
    const creatorTypeSelect = document.getElementById('creator-type-select');
    const creatorIdInput = document.getElementById('creator-id-input');
    const btnVerifyCreds = document.getElementById('btn-verify-credentials');
    const credsStatusBox = document.getElementById('credentials-status-box');
    const credsStatusText = document.getElementById('credentials-status-text');
    const cacheStatsLabel = document.getElementById('cache-stats-label');
    const btnClearCache = document.getElementById('btn-clear-cache');

    // Console Elements
    const consoleOutput = document.getElementById('console-output');
    const consoleInput = document.getElementById('console-input');
    const btnConsoleSubmit = document.getElementById('btn-console-submit');
    const btnConsoleClear = document.getElementById('btn-console-clear');

    // Status & Action Bar
    const statusText = document.getElementById('status-text');
    const progressBar = document.getElementById('progress-bar');
    const btnConvertAction = document.getElementById('btn-convert-action');
    const btnOpenStudio = document.getElementById('btn-open-studio');
    const btnOpenOutput = document.getElementById('btn-open-output');
    const btnCancel = document.getElementById('btn-cancel');

    // State Variables
    let currentMaps = [];
    let isConverting = false;
    let lastExportedPlacePath = null;
    let unreadErrors = 0;
    const commandHistory = [];
    let historyIndex = -1;

    // --- Window Controls ---
    btnMinimize.addEventListener('click', () => window.api.minimizeWindow());
    btnClose.addEventListener('click', () => window.api.closeWindow());

    // --- Tab Switching ---
    tabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetTab = btn.getAttribute('data-tab');

            tabButtons.forEach(b => b.classList.remove('active'));
            tabPanels.forEach(p => p.classList.remove('active'));

            btn.classList.add('active');
            const panel = document.getElementById(targetTab);
            if (panel) panel.classList.add('active');

            if (targetTab === 'tab-console') {
                unreadErrors = 0;
                consoleBadge.style.display = 'none';
                consoleBadge.innerText = '0';
                consoleOutput.scrollTop = consoleOutput.scrollHeight;
            }
        });
    });

    // --- Logging Helper ---
    function logToConsole(message, type = 'info') {
        const line = document.createElement('div');
        line.className = `gs-log-line gs-log-${type}`;
        const time = new Date().toLocaleTimeString();
        line.innerText = `[${time}] ${message}`;
        consoleOutput.appendChild(line);
        consoleOutput.scrollTop = consoleOutput.scrollHeight;

        if (type === 'error' && !document.getElementById('tab-console').classList.contains('active')) {
            unreadErrors++;
            consoleBadge.style.display = 'inline-block';
            consoleBadge.innerText = String(unreadErrors);
        }
    }

    // --- Settings Persistence ---
    async function loadSettings() {
        const settings = await window.api.getSettings();
        if (settings) {
            if (settings.GameDir) gameDirInput.value = settings.GameDir;
            if (settings.CustomTexturesDir) customTexturesInput.value = settings.CustomTexturesDir;
            if (settings.RobloxApiKey) apiKeyInput.value = settings.RobloxApiKey;
            if (settings.RobloxCreatorType) creatorTypeSelect.value = settings.RobloxCreatorType;
            if (settings.RobloxCreatorId) creatorIdInput.value = settings.RobloxCreatorId;
            chkUploadTextures.checked = settings.UploadAssets !== false;
            chkUploadMeshes.checked = settings.UploadMeshes !== false;
        }
    }

    async function saveCurrentSettings() {
        const current = {
            GameDir: gameDirInput.value.trim(),
            CustomTexturesDir: customTexturesInput.value.trim(),
            UploadAssets: chkUploadTextures.checked,
            UploadMeshes: chkUploadMeshes.checked,
            RobloxApiKey: apiKeyInput.value.trim(),
            RobloxCreatorType: creatorTypeSelect.value,
            RobloxCreatorId: creatorIdInput.value.trim()
        };
        await window.api.saveSettings(current);
    }

    // --- Steam Game Discovery ---
    async function discoverSteamGames() {
        detectedGamesSelect.innerHTML = '<option value="">Searching Steam libraries...</option>';
        logToConsole('Scanning Steam libraries for Source 1 titles...', 'info');

        const games = await window.api.scanSteamGames();
        detectedGamesSelect.innerHTML = '';

        if (games.length === 0) {
            detectedGamesSelect.innerHTML = '<option value="">No Steam Source games found automatically</option>';
            logToConsole('No Source games found in standard Steam paths. You can manually browse to your game folder.', 'warning');
            return;
        }

        detectedGamesSelect.innerHTML = '<option value="">-- Select Installed Game --</option>';
        let selectedIndex = 0;

        games.forEach((game, idx) => {
            const opt = document.createElement('option');
            opt.value = game.gameDir;
            opt.innerText = `${game.name} (${game.mapCount} maps) - ${game.gameDir}`;
            detectedGamesSelect.appendChild(opt);

            // Auto-select Garry's Mod if found
            if (game.name.includes("Garry's Mod") || (gameDirInput.value && gameDirInput.value === game.gameDir)) {
                selectedIndex = idx + 1;
            }
        });

        if (selectedIndex > 0) {
            detectedGamesSelect.selectedIndex = selectedIndex;
            gameDirInput.value = games[selectedIndex - 1].gameDir;
            loadMapsForGame(games[selectedIndex - 1].gameDir);
        } else if (gameDirInput.value) {
            loadMapsForGame(gameDirInput.value);
        }

        logToConsole(`Discovered ${games.length} Source game installations across Steam libraries.`, 'success');
    }

    btnRescanSteam.addEventListener('click', discoverSteamGames);

    detectedGamesSelect.addEventListener('change', () => {
        const dir = detectedGamesSelect.value;
        if (dir) {
            gameDirInput.value = dir;
            saveCurrentSettings();
            loadMapsForGame(dir);
        }
    });

    // --- Browse Buttons ---
    btnBrowseGameDir.addEventListener('click', async () => {
        const folder = await window.api.browseFolder('Select Source Game Directory (containing gameinfo.txt)');
        if (folder) {
            gameDirInput.value = folder;
            saveCurrentSettings();
            loadMapsForGame(folder);
        }
    });

    btnBrowseBsp.addEventListener('click', async () => {
        const file = await window.api.browseFile([
            { name: 'Source Map & Nav Files (*.bsp, *.nav)', extensions: ['bsp', 'nav'] },
            { name: 'Source BSP Maps (*.bsp)', extensions: ['bsp'] },
            { name: 'Source Navigation Mesh (*.nav)', extensions: ['nav'] },
            { name: 'All Files', extensions: ['*'] }
        ]);
        if (file) {
            let targetFile = file;
            if (file.toLowerCase().endsWith('.nav')) {
                const bspCandidate = file.replace(/\.nav$/i, '.bsp');
                targetFile = bspCandidate;
                logToConsole(`Selected navigation mesh (.nav). Paired with map: ${bspCandidate.replace(/.*[\\\/]/, '')}`, 'info');
            }
            selectedMapInput.value = targetFile;
            logToConsole(`Selected custom map file: ${targetFile}`, 'info');
        }
    });

    btnBrowseModel.addEventListener('click', async () => {
        const file = await window.api.browseFile([
            { name: 'Source Model Files', extensions: ['mdl'] },
            { name: 'All Files', extensions: ['*'] }
        ]);
        if (file) {
            modelPathInput.value = file;
            logToConsole(`Selected model: ${file}`, 'info');
        }
    });

    btnBrowseVtf.addEventListener('click', async () => {
        const file = await window.api.browseFile([
            { name: 'Valve Texture Files', extensions: ['vtf'] },
            { name: 'All Files', extensions: ['*'] }
        ]);
        if (file) {
            vtfPathInput.value = file;
            logToConsole(`Selected VTF texture: ${file}`, 'info');
        }
    });

    btnBrowseCustomTex.addEventListener('click', async () => {
        const folder = await window.api.browseFolder('Select Custom Textures / Materials Directory');
        if (folder) {
            customTexturesInput.value = folder;
            saveCurrentSettings();
        }
    });

    // --- Maps Table Population & Filtering ---
    async function loadMapsForGame(gameDir) {
        if (!gameDir) return;
        mapsTableBody.innerHTML = '<tr><td colspan="3" style="text-align:center; padding:15px;">Scanning maps, addons, and workshop directories...</td></tr>';

        currentMaps = await window.api.scanMaps(gameDir);
        renderMapsTable(currentMaps);
    }

    function renderMapsTable(maps) {
        mapsTableBody.innerHTML = '';
        mapCountLabel.innerText = `${maps.length} maps`;

        if (maps.length === 0) {
            mapsTableBody.innerHTML = '<tr><td colspan="3" style="text-align:center; padding:20px; color:var(--gs-text-dim);">No .bsp map files found in maps/, download/, or addons folders.</td></tr>';
            return;
        }

        maps.forEach(map => {
            const tr = document.createElement('tr');
            const navBadge = map.hasNav ? '<span title="Accompanying .nav navigation mesh found" style="color:var(--gs-yellow); font-size:10px; margin-left:6px; border:1px solid var(--gs-yellow); padding:1px 3px; border-radius:2px; font-weight:bold;">NAV</span>' : '';
            const categoryBadge = map.category && map.category !== 'Stock Game' ? `<span style="color:var(--gs-text-dim); font-size:10px; margin-left:4px;">[${map.category}]</span>` : '';

            tr.innerHTML = `
                <td><strong>${map.name}</strong>${navBadge}${categoryBadge}</td>
                <td>${map.sizeFormatted}</td>
                <td style="color:var(--gs-text-dim);">${map.modified}</td>
            `;

            if (selectedMapInput.value && (selectedMapInput.value.toLowerCase() === map.name.toLowerCase() || (map.fullPath && selectedMapInput.value.toLowerCase() === map.fullPath.toLowerCase()))) {
                tr.classList.add('selected');
            }

            tr.addEventListener('click', () => {
                document.querySelectorAll('#maps-table-body tr').forEach(r => r.classList.remove('selected'));
                tr.classList.add('selected');
                selectedMapInput.value = map.fullPath || map.name;
                statusText.innerText = `Selected map: ${map.name} (${map.sizeFormatted})${map.hasNav ? ' [with .nav]' : ''}. Ready to convert or inspect.`;
            });

            tr.addEventListener('dblclick', () => {
                openInspector(map.fullPath || map.name);
            });

            mapsTableBody.appendChild(tr);
        });
    }

    mapSearchInput.addEventListener('input', () => {
        const query = mapSearchInput.value.trim().toLowerCase();
        const filtered = currentMaps.filter(m => m.name.toLowerCase().includes(query));
        renderMapsTable(filtered);
    });

    // --- Map Asset Inspector Implementation ---
    async function openInspector(mapName) {
        if (!mapName || !mapName.trim()) {
            statusText.innerText = 'Please select or enter a map name to inspect.';
            logToConsole('Cannot inspect: No map selected.', 'error');
            return;
        }

        let gameDir = gameDirInput.value.trim();
        // If gameDir is not set, try to infer it from the map path
        if (!gameDir) {
            const rawMap = mapName.trim();
            const gmodMatch = rawMap.match(/(.*[\\\/]garrysmod)(?:[\\\/]|$)/i);
            if (gmodMatch) {
                gameDir = gmodMatch[1];
                gameDirInput.value = gameDir;
                logToConsole(`Auto-detected game directory from map path: ${gameDir}`, 'info');
            }
        }

        if (!gameDir) {
            statusText.innerText = 'Please select a valid Source game directory first.';
            logToConsole('Cannot inspect: Missing Source game folder.', 'error');
            return;
        }

        let targetMap = mapName.trim();
        if (targetMap.toLowerCase().endsWith('.nav')) {
            targetMap = targetMap.replace(/\.nav$/i, '.bsp');
        }

        const mapBaseName = targetMap.replace(/^.*[\\\/]/, '').replace(/\.bsp$/i, '');
        inspectModalTitle.innerText = `Map Asset Inspector - ${mapBaseName}.bsp`;
        inspectFooterInfo.innerText = `Inspecting ${mapBaseName}.bsp in ${gameDir}...`;
        modalMapInspect.classList.remove('hidden');
        inspectLoading.classList.remove('hidden');

        try {
            const res = await window.api.inspectMap(gameDir, targetMap);
            inspectLoading.classList.add('hidden');

            if (!res.success) {
                logToConsole(`Failed to inspect map '${mapBaseName}': ${res.error}`, 'error');
                inspectFooterInfo.innerHTML = `<span style="color:var(--gs-red);">Error: ${res.error}</span>`;
                inspectModelsTbody.innerHTML = `<tr><td colspan="5" style="text-align:center; padding:20px; color:var(--gs-red);">Failed to inspect map: ${res.error}</td></tr>`;
                inspectTexturesTbody.innerHTML = `<tr><td colspan="2" style="text-align:center; padding:20px; color:var(--gs-red);">Failed to inspect map: ${res.error}</td></tr>`;
                inspectEntitiesTbody.innerHTML = `<tr><td colspan="2" style="text-align:center; padding:20px; color:var(--gs-red);">Failed to inspect map: ${res.error}</td></tr>`;
                return;
            }

            currentInspection = res.data;
            logToConsole(`Successfully inspected map '${mapBaseName}': ${currentInspection.summary.uniqueModels} models, ${currentInspection.summary.uniqueTextures} textures, ${currentInspection.summary.totalFaces} faces.`, 'success');

            // Populate Metrics Ribbon
            const s = currentInspection.summary;
            metricModels.innerText = s.uniqueModels.toLocaleString();
            metricPropInstances.innerText = `${s.totalPropInstances.toLocaleString()} total instances`;
            metricTextures.innerText = s.uniqueTextures.toLocaleString();
            metricTexSub.innerText = `${s.uniqueTextures} materials/textures`;
            metricFaces.innerText = s.totalFaces.toLocaleString();
            metricDisplacements.innerText = `${s.totalDisplacements} Displacements`;
            metricBrushes.innerText = s.totalBrushes.toLocaleString();
            metricBspVersion.innerText = `BSP v${currentInspection.bspVersion}`;
            metricEntities.innerText = s.totalEntities.toLocaleString();
            metricSkyname.innerText = `Sky: ${currentInspection.skyName || 'default'}`;

            // Tab Badges
            tabModelCount.innerText = currentInspection.models.length;
            tabTextureCount.innerText = currentInspection.textures.length;
            tabEntityCount.innerText = currentInspection.entities.length;

            inspectFilterInput.value = '';
            renderFilteredInspectViews();
            inspectFooterInfo.innerText = `Map ${mapBaseName}.bsp ready. Click 'CONVERT THIS MAP NOW' to export to Roblox.`;
        } catch (err) {
            inspectLoading.classList.add('hidden');
            logToConsole(`Inspection exception: ${err.message}`, 'error');
            inspectFooterInfo.innerHTML = `<span style="color:var(--gs-red);">Exception: ${err.message}</span>`;
        }
    }

    function closeInspector() {
        modalMapInspect.classList.add('hidden');
    }

    function renderFilteredInspectViews() {
        if (!currentInspection) return;
        const filter = (inspectFilterInput.value || '').trim().toLowerCase();

        // 1. Models
        const filteredModels = currentInspection.models.filter(m => {
            if (!filter) return true;
            return (m.Path && m.Path.toLowerCase().includes(filter)) ||
                   (m.Type && m.Type.toLowerCase().includes(filter)) ||
                   (m.Skins && m.Skins.some(sk => sk.toString().includes(filter)));
        });

        inspectModelsTbody.innerHTML = '';

        // Easter Egg: Classified Black Mesa Asset
        if (filter === 'crowbar' || filter === 'gman' || filter === 'borealis' || filter === 'lambda' || filter === 'snark') {
            const eggTr = document.createElement('tr');
            eggTr.style.background = '#3e371f';
            eggTr.innerHTML = `
                <td style="text-align:center;"><span class="badge-status-ok">FOUND</span></td>
                <td><strong style="color:var(--gs-yellow);">models/weapons/v_crowbar.mdl [CLASSIFIED]</strong></td>
                <td><span class="badge-tag">HEV Equipment</span></td>
                <td style="text-align:right; font-family:var(--font-mono); font-weight:bold; color:var(--gs-yellow);">3</td>
                <td style="text-align:center; color:var(--gs-text-dim);">42</td>
            `;
            eggTr.addEventListener('click', () => {
                inspectFooterInfo.innerText = "Black Mesa Research Facility: Theoretical Physics Department - Standard Issue Steel Crowbar. Resonance Cascade containment active.";
            });
            inspectModelsTbody.appendChild(eggTr);
        }

        if (filteredModels.length === 0 && !inspectModelsTbody.children.length) {
            inspectModelsTbody.innerHTML = '<tr><td colspan="5" style="text-align:center; padding:15px; color:var(--gs-text-dim);">No models matching filter.</td></tr>';
        } else {
            filteredModels.forEach(m => {
                const tr = document.createElement('tr');
                const statusBadge = m.ExistsInGame
                    ? '<span class="badge-status-ok">FOUND</span>'
                    : '<span class="badge-status-missing">MISSING</span>';
                const skinsStr = m.Skins && m.Skins.length > 0 ? m.Skins.join(', ') : '0';

                tr.innerHTML = `
                    <td style="text-align:center;">${statusBadge}</td>
                    <td><strong style="color:var(--gs-text-highlight);">${m.Path}</strong></td>
                    <td><span class="badge-tag">${m.Type}</span></td>
                    <td style="text-align:right; font-family:var(--font-mono); font-weight:bold; color:var(--gs-yellow);">${m.InstanceCount}</td>
                    <td style="text-align:center; color:var(--gs-text-dim);">${skinsStr}</td>
                `;
                tr.addEventListener('click', () => {
                    inspectModelsTbody.querySelectorAll('tr').forEach(r => r.classList.remove('selected'));
                    tr.classList.add('selected');
                    inspectFooterInfo.innerText = `${m.Path} | ${m.Type} | Instances: ${m.InstanceCount} | Skins: [${skinsStr}] | Status: ${m.ExistsInGame ? 'Available in Game VPK' : 'Custom / Missing'}`;
                });
                inspectModelsTbody.appendChild(tr);
            });
        }

        // 2. Textures
        const filteredTextures = currentInspection.textures.filter(t => {
            if (!filter) return true;
            return (t.path && t.path.toLowerCase().includes(filter)) ||
                   (t.type && t.type.toLowerCase().includes(filter));
        });

        inspectTexturesTbody.innerHTML = '';
        if (filteredTextures.length === 0) {
            inspectTexturesTbody.innerHTML = '<tr><td colspan="2" style="text-align:center; padding:15px; color:var(--gs-text-dim);">No textures matching filter.</td></tr>';
        } else {
            filteredTextures.forEach(t => {
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td><span class="badge-tag">${t.type}</span></td>
                    <td><strong>${t.path}</strong></td>
                `;
                tr.addEventListener('click', () => {
                    inspectTexturesTbody.querySelectorAll('tr').forEach(r => r.classList.remove('selected'));
                    tr.classList.add('selected');
                    inspectFooterInfo.innerText = `Material: ${t.path} | Category: ${t.type}`;
                });
                inspectTexturesTbody.appendChild(tr);
            });
        }

        // 3. Entities
        const filteredEntities = currentInspection.entities.filter(e => {
            if (!filter) return true;
            return e.className && e.className.toLowerCase().includes(filter);
        });

        inspectEntitiesTbody.innerHTML = '';
        if (filteredEntities.length === 0) {
            inspectEntitiesTbody.innerHTML = '<tr><td colspan="2" style="text-align:center; padding:15px; color:var(--gs-text-dim);">No entities matching filter.</td></tr>';
        } else {
            filteredEntities.forEach(e => {
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td><strong style="color:var(--gs-text-highlight);">${e.className}</strong></td>
                    <td style="text-align:right; font-family:var(--font-mono); font-weight:bold; color:var(--gs-yellow);">${e.count}</td>
                `;
                tr.addEventListener('click', () => {
                    inspectEntitiesTbody.querySelectorAll('tr').forEach(r => r.classList.remove('selected'));
                    tr.classList.add('selected');
                    inspectFooterInfo.innerText = `Entity Class: ${e.className} | Count: ${e.count} instances`;
                });
                inspectEntitiesTbody.appendChild(tr);
            });
        }
    }

    // Inspect Event Handlers
    btnInspectMap.addEventListener('click', () => {
        const targetMap = selectedMapInput.value;
        if (!targetMap) {
            logToConsole('Please select a map from the table first.', 'error');
            statusText.innerText = 'Select a map to inspect first.';
            return;
        }
        openInspector(targetMap);
    });

    btnCloseInspectModal.addEventListener('click', closeInspector);
    btnInspectClose.addEventListener('click', closeInspector);

    btnInspectConvertNow.addEventListener('click', () => {
        closeInspector();
        btnConvertAction.click();
    });

    inspectFilterInput.addEventListener('input', renderFilteredInspectViews);
    btnInspectClearFilter.addEventListener('click', () => {
        inspectFilterInput.value = '';
        renderFilteredInspectViews();
    });

    inspectSubtabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const subtabId = btn.getAttribute('data-subtab');
            inspectSubtabButtons.forEach(b => b.classList.remove('active'));
            inspectPanels.forEach(p => p.classList.remove('active'));

            btn.classList.add('active');
            const panel = document.getElementById(subtabId);
            if (panel) panel.classList.add('active');
            activeInspectSubtab = subtabId;
        });
    });

    window.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && !modalMapInspect.classList.contains('hidden')) {
            closeInspector();
        }
    });

    // --- Roblox Credentials Testing ---
    btnToggleKey.addEventListener('click', () => {
        if (apiKeyInput.type === 'password') {
            apiKeyInput.type = 'text';
            btnToggleKey.innerText = 'Hide';
        } else {
            apiKeyInput.type = 'password';
            btnToggleKey.innerText = 'Show';
        }
    });

    btnVerifyCreds.addEventListener('click', async () => {
        const apiKey = apiKeyInput.value.trim();
        const creatorType = creatorTypeSelect.value;
        const creatorId = creatorIdInput.value.trim();

        credsStatusBox.style.display = 'block';
        credsStatusBox.className = 'gs-callout';
        credsStatusText.innerText = 'Testing connection with Roblox Open Cloud Gateway...';
        logToConsole(`Validating Roblox Open Cloud credentials (${creatorType} ID: ${creatorId})...`, 'roblox');

        const res = await window.api.verifyRobloxCredentials({ apiKey, creatorType, creatorId });

        if (res.success) {
            credsStatusBox.className = 'gs-callout';
            credsStatusText.innerHTML = `<strong style="color:var(--gs-green);">[OK] Authenticated:</strong> ${res.message}`;
            logToConsole(`Roblox Open Cloud verification succeeded: ${res.message}`, 'success');
        } else {
            credsStatusBox.className = 'gs-callout alert';
            credsStatusText.innerHTML = `<strong style="color:var(--gs-red);">[FAIL] Verification Failed:</strong> ${res.message}`;
            logToConsole(`Roblox Open Cloud verification failed: ${res.message}`, 'error');
        }

        saveCurrentSettings();
    });

    // --- Cache Statistics ---
    async function updateCacheStats() {
        const stats = await window.api.getCacheStats();
        if (stats.exists) {
            cacheStatsLabel.innerText = `Cached Assets: ${stats.count.toLocaleString()} (${stats.sizeFormatted})`;
        } else {
            cacheStatsLabel.innerText = 'Cached Assets: 0 (No cache yet)';
        }
    }

    btnClearCache.addEventListener('click', async () => {
        if (confirm('Clear the local upload cache? Previously uploaded assets will need to be re-uploaded on the next conversion.')) {
            const res = await window.api.clearUploadCache();
            logToConsole(res.message, 'info');
            updateCacheStats();
        }
    });

    // --- Conversion Engine Streaming & Execution ---
    btnConvertAction.addEventListener('click', async () => {
        const gameDir = gameDirInput.value.trim();
        const mapName = selectedMapInput.value.trim();
        const modelName = modelPathInput.value.trim();
        const vtfName = vtfPathInput.value.trim();

        if (!gameDir) {
            alert('Please select or enter a valid Source game directory first.');
            return;
        }

        if (!mapName && !modelName && !vtfName) {
            alert('Please select or specify a Map, Model, or Texture to convert.');
            return;
        }

        await saveCurrentSettings();

        // Prepare job
        const job = {
            gameDir,
            mapName: mapName || undefined,
            modelName: modelName || undefined,
            vtfName: vtfName || undefined,
            uploadAssets: chkUploadTextures.checked,
            uploadMeshes: chkUploadMeshes.checked,
            robloxApiKey: apiKeyInput.value.trim(),
            robloxCreatorType: creatorTypeSelect.value,
            robloxCreatorId: creatorIdInput.value.trim(),
            customTexturesDir: customTexturesInput.value.trim()
        };

        // Switch UI to converting state
        isConverting = true;
        conversionHasErrors = false;
        btnConvertAction.disabled = true;
        btnOpenStudio.disabled = true;
        btnCancel.style.display = 'inline-block';
        progressBar.style.width = '0%';
        statusText.innerText = `Starting conversion for ${mapName || modelName || vtfName}...`;

        logToConsole(`=======================================================`, 'cmd');
        logToConsole(`STARTING SOURCE2ROBLOX CONVERSION`, 'cmd');
        logToConsole(`Target: ${mapName ? 'Map ' + mapName : (modelName ? 'Model ' + modelName : 'VTF ' + vtfName)}`, 'info');
        logToConsole(`Game Folder: ${gameDir}`, 'info');
        logToConsole(`Server-Side Uploads: Textures=${job.uploadAssets}, Meshes=${job.uploadMeshes}`, 'roblox');
        logToConsole(`=======================================================`, 'cmd');

        const launchResult = await window.api.startConversion(job);
        if (!launchResult.success) {
            isConverting = false;
            btnConvertAction.disabled = false;
            btnCancel.style.display = 'none';
            statusText.innerText = `Failed to launch conversion: ${launchResult.error}`;
            logToConsole(`Launch error: ${launchResult.error}`, 'error');
        }
    });

    btnCancel.addEventListener('click', async () => {
        if (confirm('Are you sure you want to cancel the active conversion process?')) {
            await window.api.cancelConversion();
            logToConsole('Conversion aborted by user.', 'warning');
        }
    });

    // Handle Engine Events
    let conversionHasErrors = false;

    window.api.onEngineEvent((evt) => {
        if (!evt) return;

        switch (evt.type) {
            case 'progress': {
                statusText.innerText = evt.message || 'Processing...';
                if (evt.data && evt.data.total > 0) {
                    const percent = Math.min(100, Math.round((evt.data.current / evt.data.total) * 100));
                    progressBar.style.width = `${percent}%`;
                    statusText.innerText = `${evt.message} (${evt.data.current} / ${evt.data.total} - ${percent}%)`;
                } else {
                    progressBar.style.width = '45%';
                }
                logToConsole(evt.message, 'info');
                break;
            }

            case 'output': {
                logToConsole(evt.message, 'success');
                if (evt.message.includes('.rbxl')) {
                    const match = evt.message.match(/([A-Za-z]:\\[^ \n\r\t]+\.rbxl)/);
                    if (match) lastExportedPlacePath = match[1];
                }
                break;
            }

            case 'warning': {
                logToConsole(evt.message, 'warning');
                break;
            }

            case 'error': {
                conversionHasErrors = true;
                logToConsole(evt.message, 'error');
                break;
            }

            case 'done': {
                progressBar.style.width = '100%';
                if (conversionHasErrors) {
                    statusText.innerText = 'Conversion encountered errors. Place file was not generated.';
                    logToConsole('CONVERSION ENCOUNTERED ERRORS [FAILED]', 'error');
                } else {
                    statusText.innerText = 'Conversion completed successfully!';
                    logToConsole('CONVERSION FINISHED SUCCESSFULLY [OK]', 'success');
                    btnOpenStudio.disabled = false;
                    updateCacheStats();

                    if (chkOpenStudio.checked && lastExportedPlacePath) {
                        window.api.openInStudio(lastExportedPlacePath);
                    }
                }
                break;
            }

            case 'raw':
            default: {
                if (evt.message) {
                    if (evt.message.includes('rbxassetid://') || evt.message.includes('Queued')) {
                        logToConsole(evt.message, 'roblox');
                    } else if (evt.message.includes('Wrote') || evt.message.includes('complete')) {
                        logToConsole(evt.message, 'success');
                    } else {
                        logToConsole(evt.message, 'info');
                    }
                }
                break;
            }
        }
    });

    window.api.onEngineExit((data) => {
        isConverting = false;
        btnConvertAction.disabled = false;
        btnCancel.style.display = 'none';
        if (data.code === 0 && !conversionHasErrors) {
            statusText.innerText = 'Conversion complete. Ready.';
        } else {
            statusText.innerText = `Conversion failed (Exit code: ${data.code}). Check console log.`;
            logToConsole(`Engine process finished with code ${data.code}${conversionHasErrors ? ' (with errors)' : ''}`, 'error');
        }
    });

    // --- Action Buttons ---
    btnOpenOutput.addEventListener('click', () => {
        window.api.openOutputFolder();
    });

    btnOpenStudio.addEventListener('click', () => {
        if (lastExportedPlacePath) {
            window.api.openInStudio(lastExportedPlacePath);
        } else {
            // Find most recent rbxl in default folder
            window.api.openOutputFolder();
        }
    });

    // --- Console Commands ---
    function executeConsoleCommand(cmd) {
        const raw = cmd.trim();
        if (!raw) return;

        commandHistory.push(raw);
        historyIndex = commandHistory.length;

        logToConsole(`] ${raw}`, 'cmd');

        const parts = raw.split(/\s+/);
        const action = parts[0].toLowerCase();

        switch (action) {
            case 'help':
                logToConsole('Available GoldSource Console Commands:', 'cmd');
                logToConsole('  help         - Displays this list of commands', 'info');
                logToConsole('  status       - Shows engine status and current targets', 'info');
                logToConsole('  validate     - Tests Roblox Open Cloud API credentials', 'info');
                logToConsole('  steam        - Rescans local Steam libraries for games', 'info');
                logToConsole('  cache        - Displays asset upload deduplication statistics', 'info');
                logToConsole('  clearcache   - Clears upload cache file', 'info');
                logToConsole('  export       - Opens output folder in Windows Explorer', 'info');
                logToConsole('  clear        - Clears the console output log', 'info');
                break;

            case 'clear':
            case 'cls':
                consoleOutput.innerHTML = '';
                break;

            case 'status':
                logToConsole(`Engine Status: ${isConverting ? 'CONVERTING' : 'IDLE'}`, isConverting ? 'warning' : 'success');
                logToConsole(`Current Game: ${gameDirInput.value || 'None selected'}`, 'info');
                logToConsole(`Target Map: ${selectedMapInput.value || 'None'}`, 'info');
                logToConsole(`Cloud Textures: ${chkUploadTextures.checked}, Cloud Meshes: ${chkUploadMeshes.checked}`, 'info');
                break;

            case 'validate':
                btnVerifyCreds.click();
                break;

            case 'steam':
                discoverSteamGames();
                break;

            case 'cache':
                updateCacheStats().then(() => {
                    logToConsole(cacheStatsLabel.innerText, 'info');
                });
                break;

            case 'clearcache':
                btnClearCache.click();
                break;

            case 'export':
                window.api.openOutputFolder();
                break;

            case 'inspect':
                const targetMap = param1 || selectedMapInput.value;
                if (!targetMap) {
                    logToConsole("Usage: inspect <map_name> (or select a map from the table first)", 'error');
                } else {
                    openInspector(targetMap);
                }
                break;

            // --- Easter Egg: Classic Valve Console Cheats ---
            case 'sv_cheats':
            case 'sv_cheats 1':
                logToConsole("Server cvar 'sv_cheats' set to 1. Black Mesa clearance level: OMNI. Welcome, Dr. Freeman.", 'warning');
                break;

            case 'god':
                logToConsole("godmode ON. Warning: HEV Mark IV hazard suit blast armor fully energized.", 'warning');
                break;

            case 'noclip':
                logToConsole("noclip ON. Clipping disabled. You can now pass through BSP brush solids and displacement meshes.", 'warning');
                break;

            case 'impulse':
                if (param1 === '101') {
                    logToConsole("impulse 101: Acquired Crowbar, 9mm Glock, .357 Magnum, MP5 Submachine Gun, SPAS-12, Crossbow, RPG, Gauss Gun, Gluon Cannon, and 10 Snarks.", 'success');
                } else {
                    logToConsole(`impulse ${param1} processed.`, 'info');
                }
                break;

            case 'crowbar':
                logToConsole("*CLANG! CLANG! CLANG!* Standard-issue Black Mesa solid steel crowbar swung at wall.", 'warning');
                break;

            case 'gman':
                logToConsole("'The right man in the wrong place can make all the difference in the world. So, wake up, Mister Freeman. Wake up and smell the ashes.'", 'roblox');
                break;

            case 'gaben':
                logToConsole("Gabe Newell: 'These things, they take time.' (Half-Life 3 release status: Valve Time).", 'info');
                break;

            case 'lambda':
                logToConsole("        /\\", 'warning');
                logToConsole("       /  \\", 'warning');
                logToConsole("      / /\\ \\", 'warning');
                logToConsole("     / /  \\ \\", 'warning');
                logToConsole("    / /    \\ \\", 'warning');
                logToConsole("   /_/      \\_\\", 'warning');
                logToConsole("   [BLACK MESA RESEARCH FACILITY - SECTOR C]", 'success');
                break;

            case 'borealis':
                logToConsole("Aperture Science Research Vessel 'Borealis' located at coordinates [REDACTED].", 'error');
                break;

            default:
                logToConsole(`Unknown command '${action}'. Type 'help' for available commands.`, 'error');
                break;
        }
    }

    btnConsoleSubmit.addEventListener('click', () => {
        executeConsoleCommand(consoleInput.value);
        consoleInput.value = '';
    });

    btnConsoleClear.addEventListener('click', () => {
        consoleOutput.innerHTML = '';
    });

    consoleInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            executeConsoleCommand(consoleInput.value);
            consoleInput.value = '';
        } else if (e.key === 'ArrowUp') {
            if (historyIndex > 0) {
                historyIndex--;
                consoleInput.value = commandHistory[historyIndex] || '';
            }
        } else if (e.key === 'ArrowDown') {
            if (historyIndex < commandHistory.length - 1) {
                historyIndex++;
                consoleInput.value = commandHistory[historyIndex] || '';
            } else {
                historyIndex = commandHistory.length;
                consoleInput.value = '';
            }
        }
    });

    // Documentation Link
    document.getElementById('link-creator-dashboard')?.addEventListener('click', (e) => {
        e.preventDefault();
        window.api.browseFile();
    });

    // --- Easter Egg: Title Lambda 3-Click (Half-Life 3) ---
    const titleLambda = document.getElementById('title-lambda');
    const windowTitleText = document.getElementById('window-title-text');
    let lambdaClicks = 0;
    let lambdaTimer = null;

    if (titleLambda) {
        titleLambda.addEventListener('click', () => {
            lambdaClicks++;
            clearTimeout(lambdaTimer);

            if (lambdaClicks === 3) {
                lambdaClicks = 0;
                titleLambda.style.color = '#ffdd44';
                titleLambda.style.textShadow = '0 0 8px #ffc107';
                if (windowTitleText) windowTitleText.innerText = '[HALF-LIFE 3 CONFIRMED] Black Mesa Security Clearance Granted';
                logToConsole('*BEEP* *BEEP* HEV Suit Voice: Welcome to the HEV Mark IV protective system. Vital signs monitored.', 'warning');
                logToConsole('Black Mesa Sector C test chamber online. Anti-mass spectrometer initialized to 105%.', 'roblox');

                setTimeout(() => {
                    titleLambda.style.color = '';
                    titleLambda.style.textShadow = '';
                    if (windowTitleText) windowTitleText.innerText = 'OPTIONS: OpenSource2Roblox v2.0 - GoldSource Edition';
                }, 5000);
            } else {
                lambdaTimer = setTimeout(() => {
                    lambdaClicks = 0;
                }, 1200);
            }
        });
    }

    // --- Easter Egg: Konami Code (Black Mesa 1998 CRT Phosphor HUD) ---
    const konamiSequence = ['ArrowUp', 'ArrowUp', 'ArrowDown', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'ArrowLeft', 'ArrowRight', 'b', 'a'];
    let konamiIndex = 0;

    window.addEventListener('keydown', (e) => {
        if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA')) {
            return;
        }

        if (e.key === konamiSequence[konamiIndex] || e.key.toLowerCase() === konamiSequence[konamiIndex]) {
            konamiIndex++;
            if (konamiIndex === konamiSequence.length) {
                konamiIndex = 0;
                const isPhosphor = document.body.classList.toggle('crt-phosphor-mode');
                logToConsole(
                    isPhosphor
                        ? '[CHEATS] Konami Code accepted! Black Mesa 1998 CRT Phosphor HUD mode ACTIVATED.'
                        : '[CHEATS] Konami Code accepted! Standard GoldSource display mode RESTORED.',
                    'warning'
                );
            }
        } else {
            konamiIndex = 0;
        }
    });

    // --- Initialization ---
    await loadSettings();
    await updateCacheStats();
    await discoverSteamGames();
});
