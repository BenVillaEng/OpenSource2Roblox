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

    // Moderation Alert Modal Elements
    const modalModerationAlert = document.getElementById('modal-moderation-alert');
    const btnCloseModerationModal = document.getElementById('btn-close-moderation-modal');
    const btnModerationDismiss = document.getElementById('btn-moderation-dismiss');
    const btnModerationOpenRoblox = document.getElementById('btn-moderation-open-roblox');
    const btnModerationResume = document.getElementById('btn-moderation-resume');
    const moderationFlaggedAsset = document.getElementById('moderation-flagged-asset');
    let isModeratedPaused = false;

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

    // Texture Selector & Viewer Elements
    const btnTexSelectAll = document.getElementById('btn-tex-select-all');
    const btnTexDeselectAll = document.getElementById('btn-tex-deselect-all');
    const btnTexDeselectRisky = document.getElementById('btn-tex-deselect-risky');
    const btnTexInvert = document.getElementById('btn-tex-invert');
    const texRiskyBtnCount = document.getElementById('tex-risky-btn-count');
    const texPillAllCount = document.getElementById('tex-pill-all-count');
    const texPillRiskyCount = document.getElementById('tex-pill-risky-count');
    const texPillExcludedCount = document.getElementById('tex-pill-excluded-count');
    const texSelectedCount = document.getElementById('tex-selected-count');
    const texTotalCount = document.getElementById('tex-total-count');
    const texExcludedCount = document.getElementById('tex-excluded-count');
    const texMasterCheckbox = document.getElementById('tex-master-checkbox');
    const texFilterPills = document.querySelectorAll('.tex-filter-pill');
    const texPreviewPane = document.getElementById('tex-preview-pane');
    const texPreviewCategory = document.getElementById('tex-preview-category');
    const texPreviewPlaceholder = document.getElementById('tex-preview-placeholder');
    const texPreviewLoading = document.getElementById('tex-preview-loading');
    const texPreviewImg = document.getElementById('tex-preview-img');
    const texPreviewResolution = document.getElementById('tex-preview-resolution');
    const texPreviewFormat = document.getElementById('tex-preview-format');
    const texPreviewPath = document.getElementById('tex-preview-path');
    const texPreviewRiskBadge = document.getElementById('tex-preview-risk-badge');
    const texPreviewRiskReason = document.getElementById('tex-preview-risk-reason');
    const btnTexToggleInclude = document.getElementById('btn-tex-toggle-include');

    const excludedTexturesSet = new Set();
    let activeTexPillFilter = 'all'; // 'all', 'risky', 'excluded'
    let selectedTextureForPreview = null;
    const texturePreviewCache = new Map();

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
    let isInspectingMap = false;

    async function openInspector(mapName) {
        if (!mapName || !mapName.trim()) {
            statusText.innerText = 'Please select or enter a map name to inspect.';
            logToConsole('Cannot inspect: No map selected.', 'error');
            return;
        }

        if (isInspectingMap) {
            logToConsole('Map inspection is already in progress.', 'info');
            return;
        }
        isInspectingMap = true;

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
            isInspectingMap = false;
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

        // Clear prior table content so old map data doesn't sit under the loader
        inspectModelsTbody.innerHTML = '';
        inspectTexturesTbody.innerHTML = '';
        inspectEntitiesTbody.innerHTML = '';

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

            // Evaluate risk algorithm on all textures if not already provided
            let totalRiskyCount = 0;
            currentInspection.textures.forEach(t => {
                if (!t.risk) {
                    t.risk = assessTextureRisk(t.path);
                }
                if (t.risk.isRisky) totalRiskyCount++;
            });

            // Populate Metrics Ribbon
            const s = currentInspection.summary;
            metricModels.innerText = s.uniqueModels.toLocaleString();
            metricPropInstances.innerText = `${s.totalPropInstances.toLocaleString()} total instances`;
            metricTextures.innerText = s.uniqueTextures.toLocaleString();
            metricTexSub.innerHTML = totalRiskyCount > 0
                ? `${s.uniqueTextures} textures <span style="color:#ffaaaa; font-weight:bold;">(${totalRiskyCount} risky)</span>`
                : `${s.uniqueTextures} textures (Clean)`;
            metricFaces.innerText = s.totalFaces.toLocaleString();
            metricDisplacements.innerText = `${s.totalDisplacements} Displacements`;
            metricBrushes.innerText = s.totalBrushes.toLocaleString();
            metricBspVersion.innerText = `BSP v${currentInspection.bspVersion}`;
            metricEntities.innerText = s.totalEntities.toLocaleString();
            metricSkyname.innerText = `Sky: ${currentInspection.skyName || 'default'}`;

            // Reset preview pane state
            selectedTextureForPreview = null;
            if (texPreviewPlaceholder) texPreviewPlaceholder.classList.remove('hidden');
            if (texPreviewImg) {
                texPreviewImg.classList.add('hidden');
                texPreviewImg.src = '';
            }
            if (texPreviewLoading) texPreviewLoading.classList.add('hidden');
            if (texPreviewResolution) texPreviewResolution.innerText = 'Res: -- x --';
            if (texPreviewFormat) texPreviewFormat.innerText = 'Fmt: --';
            if (texPreviewPath) texPreviewPath.innerText = 'materials/...';
            if (texPreviewCategory) texPreviewCategory.innerText = 'World Texture';
            if (texPreviewRiskBadge) {
                texPreviewRiskBadge.className = 'badge-risk-clean';
                texPreviewRiskBadge.innerText = 'CLEAN (LOW RISK)';
            }
            if (texPreviewRiskReason) texPreviewRiskReason.innerText = 'Select a texture to evaluate risk.';
            if (btnTexToggleInclude) {
                btnTexToggleInclude.innerText = 'EXCLUDE FROM UPLOAD';
                btnTexToggleInclude.classList.remove('btn-gold');
            }

            // Tab Badges
            tabModelCount.innerText = currentInspection.models.length;
            tabTextureCount.innerText = currentInspection.textures.length;
            tabEntityCount.innerText = currentInspection.entities.length;

            inspectFilterInput.value = '';
            renderFilteredInspectViews();
            inspectFooterInfo.innerText = `Map ${mapBaseName}.bsp ready. Click 'CONVERT THIS MAP NOW' to export to Roblox.`;
        } catch (err) {
            logToConsole(`Inspection exception: ${err.message}`, 'error');
            inspectFooterInfo.innerHTML = `<span style="color:var(--gs-red);">Exception: ${err.message}</span>`;
        } finally {
            isInspectingMap = false;
            inspectLoading.classList.add('hidden');
        }
    }

    function closeInspector() {
        modalMapInspect.classList.add('hidden');
        if (excludedTexturesSet.size > 0) {
            logToConsole(`Texture Exclusion Active: ${excludedTexturesSet.size} textures marked safe (will not upload to Roblox).`, 'info');
        }
    }

    function hasWord(text, word) {
        if (!text || !word) return false;
        const re = new RegExp(`(^|[^a-z0-9])${word}([^a-z0-9]|$)`, 'i');
        return re.test(text);
    }

    function assessTextureRisk(path) {
        if (!path) return { level: 'CLEAN', score: 0, tags: [], reason: 'Safe', isRisky: false };
        const lower = path.toLowerCase().replace(/\\/g, '/');

        const explicitNudity = [
            'pinup', 'nude', 'naked', 'playboy', 'hentai', 'anime_girl', 'sexy', 'boob',
            'bikini', 'lingerie', 'swimsuit', 'cleavage', 'vagina', 'pussy',
            'stripper', 'erotic', 'porn', 'xxx', 'waifu', 'poster_girl', 'girl_poster',
            'woman_poster', 'babes', 'panties', 'underwear', 'nsfw', 'fetish', 'lewd',
            'camgirl', 'sensual', 'nudity'
        ];
        const boundaryNudity = ['ass', 'butt', 'cock', 'dick', 'tit', 'tits', 'bra'];

        const goreKeywords = [
            'gore', 'blood', 'bloody', 'guts', 'flesh', 'gib', 'gibs', 'corpse', 'dead_body',
            'deadbody', 'mutilat', 'dismember', 'severed', 'wound', 'entrails', 'viscera',
            'slaughter', 'decapitat', 'splatter', 'murder', 'carnage', 'hanging_body',
            'charred_corpse', 'severed_limb'
        ];

        const drugKeywords = [
            'beer', 'liquor', 'wine', 'vodka', 'whiskey', 'whisky', 'rum', 'tequila', 'alcohol',
            'bar_sign', 'pub_sign', 'cigar', 'cigarette', 'tobacco', 'cannabis',
            'marijuana', 'joint', 'bong', 'pills', 'syringe', 'cocaine', 'meth',
            'heroin', 'budweiser', 'heineken', 'corona', 'jackdaniels', 'ashtray',
            'pack_smokes', 'hookah', 'vape'
        ];

        const hateKeywords = ['nazi', 'swastika', 'hitler', 'ss_', 'terrorist', 'kkk', 'fascist', 'aryan'];

        let score = 0;
        const tags = [];
        const reasons = [];

        for (const kw of explicitNudity) {
            if (lower.includes(kw)) {
                tags.push('Nudity/Pinup');
                reasons.push(`Pinup/Nudity keyword ('${kw}')`);
                score = Math.max(score, 95);
                break;
            }
        }

        if (score < 95) {
            for (const kw of boundaryNudity) {
                if (hasWord(lower, kw)) {
                    tags.push('Nudity/Pinup');
                    reasons.push(`Pinup/Adult keyword ('${kw}')`);
                    score = Math.max(score, 95);
                    break;
                }
            }
        }

        for (const kw of goreKeywords) {
            if (lower.includes(kw)) {
                tags.push('Gore/Blood');
                reasons.push(`Gore/Blood keyword ('${kw}')`);
                score = Math.max(score, 85);
                break;
            }
        }

        if (score < 85 && lower.includes('blood') && !lower.includes('bloodhound')) {
            tags.push('Gore/Blood');
            reasons.push('Blood keyword');
            score = Math.max(score, 85);
        }

        for (const kw of drugKeywords) {
            if (lower.includes(kw)) {
                tags.push('Alcohol/Drugs');
                reasons.push(`Alcohol/Drugs keyword ('${kw}')`);
                score = Math.max(score, 75);
                break;
            }
        }

        if (score < 75 && hasWord(lower, 'weed')) {
            const isGardenWeed = lower.includes('blend') || lower.includes('nature') ||
                                lower.includes('foliage') || lower.includes('ground') ||
                                lower.includes('path') || lower.includes('grass') ||
                                lower.includes('dirt') || lower.includes('garden') ||
                                lower.includes('flora');
            if (!isGardenWeed) {
                tags.push('Alcohol/Drugs');
                reasons.push('Substance/Weed keyword');
                score = Math.max(score, 75);
            }
        }

        for (const kw of hateKeywords) {
            if (lower.includes(kw)) {
                tags.push('Controversial');
                reasons.push(`Extremist/Hate keyword ('${kw}')`);
                score = Math.max(score, 99);
                break;
            }
        }

        if (lower.includes('poster') || lower.includes('spray') || lower.includes('graffiti') ||
            lower.startsWith('decals/') || lower.includes('/decals/')) {
            tags.push('Decal/Poster');
            if (score < 30) {
                score = Math.max(score, 30);
                reasons.push('Decal/Poster');
            }
        }

        if (lower.includes('custom/')) {
            tags.push('Custom Asset');
            if (score < 10) {
                score = Math.max(score, 10);
            }
        }

        let level = 'CLEAN';
        let reason = 'Standard environmental texture. Safe for Roblox cloud upload.';
        if (score >= 80) {
            level = 'HIGH';
            reason = `HIGH RISK of Roblox moderation: ${reasons.join(', ')}. Strongly recommend deselecting.`;
        } else if (score >= 70) {
            level = 'MEDIUM';
            reason = `Elevated risk: ${reasons.join(', ')}. Review image before uploading.`;
        }

        return {
            level,
            score,
            tags,
            reason,
            isRisky: score >= 70
        };
    }

    function updatePreviewPaneIncludeButton(t) {
        if (!btnTexToggleInclude || !t) return;
        const isExcluded = excludedTexturesSet.has(t.path);
        if (isExcluded) {
            btnTexToggleInclude.innerText = 'INCLUDE IN CLOUD UPLOAD';
            btnTexToggleInclude.classList.add('btn-gold');
        } else {
            btnTexToggleInclude.innerText = 'EXCLUDE FROM CLOUD UPLOAD';
            btnTexToggleInclude.classList.remove('btn-gold');
        }
    }

    let currentPreviewRequestId = 0;

    async function loadTexturePreview(t) {
        selectedTextureForPreview = t;
        if (!t) return;

        const requestId = ++currentPreviewRequestId;

        if (texPreviewCategory) texPreviewCategory.innerText = t.type || 'Material';
        if (texPreviewPath) {
            texPreviewPath.innerText = t.path;
            texPreviewPath.title = `Click to copy: ${t.path}`;
        }

        // Update risk card
        if (!t.risk) t.risk = assessTextureRisk(t.path);
        if (texPreviewRiskBadge) {
            if (t.risk.level === 'HIGH') {
                texPreviewRiskBadge.className = 'badge-risk-high';
                texPreviewRiskBadge.innerText = 'HIGH RISK (BAN HAZARD)';
            } else if (t.risk.level === 'MEDIUM') {
                texPreviewRiskBadge.className = 'badge-risk-medium';
                texPreviewRiskBadge.innerText = 'MEDIUM RISK (WARNING)';
            } else {
                texPreviewRiskBadge.className = 'badge-risk-clean';
                texPreviewRiskBadge.innerText = 'CLEAN (LOW RISK)';
            }
        }
        if (texPreviewRiskReason) {
            texPreviewRiskReason.innerText = t.risk.reason;
        }

        updatePreviewPaneIncludeButton(t);

        // Check cache
        if (texturePreviewCache.has(t.path)) {
            const cached = texturePreviewCache.get(t.path);
            if (texPreviewLoading) texPreviewLoading.classList.add('hidden');
            if (texPreviewPlaceholder) texPreviewPlaceholder.classList.add('hidden');
            if (texPreviewImg) {
                texPreviewImg.src = cached.dataUrl || `file://${cached.path.replace(/\\/g, '/')}`;
                texPreviewImg.classList.remove('hidden');
            }
            if (texPreviewResolution) texPreviewResolution.innerText = `Res: ${cached.width} x ${cached.height}`;
            if (texPreviewFormat) texPreviewFormat.innerText = `Fmt: ${cached.format}`;
            return;
        }

        // Decode via engine
        const gameDir = gameDirInput.value.trim();
        if (!gameDir) {
            if (texPreviewLoading) texPreviewLoading.classList.add('hidden');
            if (texPreviewImg) texPreviewImg.classList.add('hidden');
            if (texPreviewPlaceholder) {
                texPreviewPlaceholder.classList.remove('hidden');
                texPreviewPlaceholder.innerHTML = '<div style="font-size:11px; color:var(--gs-text-dim);">No game directory set</div>';
            }
            return;
        }

        if (texPreviewPlaceholder) texPreviewPlaceholder.classList.add('hidden');
        if (texPreviewImg) texPreviewImg.classList.add('hidden');
        if (texPreviewLoading) texPreviewLoading.classList.remove('hidden');
        if (texPreviewResolution) texPreviewResolution.innerText = 'Res: loading...';
        if (texPreviewFormat) texPreviewFormat.innerText = 'Fmt: loading...';

        const mapContext = currentInspection ? (currentInspection.map || selectedMapInput.value.trim()) : selectedMapInput.value.trim();

        try {
            const res = await window.api.previewTexture(gameDir, t.path, mapContext);
            if (requestId !== currentPreviewRequestId) {
                // Superseded by another selection
                return;
            }

            if (texPreviewLoading) texPreviewLoading.classList.add('hidden');

            if (res.success && res.data) {
                texturePreviewCache.set(t.path, res.data);
                if (texPreviewPlaceholder) texPreviewPlaceholder.classList.add('hidden');
                if (texPreviewImg) {
                    texPreviewImg.src = res.data.dataUrl || `file://${res.data.path.replace(/\\/g, '/')}`;
                    texPreviewImg.classList.remove('hidden');
                }
                if (texPreviewResolution) texPreviewResolution.innerText = `Res: ${res.data.width} x ${res.data.height}`;
                if (texPreviewFormat) texPreviewFormat.innerText = `Fmt: ${res.data.format}`;
            } else {
                if (texPreviewImg) texPreviewImg.classList.add('hidden');
                if (texPreviewPlaceholder) {
                    texPreviewPlaceholder.classList.remove('hidden');
                    texPreviewPlaceholder.innerHTML = `<div style="font-size:11px; color:var(--gs-text-dim); text-align:center;">Preview unavailable<br>(${res.error || 'VTF not in VPK'})</div>`;
                }
                if (texPreviewResolution) texPreviewResolution.innerText = 'Res: --';
                if (texPreviewFormat) texPreviewFormat.innerText = 'Fmt: --';
            }
        } catch (err) {
            if (requestId !== currentPreviewRequestId) return;
            if (texPreviewLoading) texPreviewLoading.classList.add('hidden');
            if (texPreviewImg) texPreviewImg.classList.add('hidden');
            if (texPreviewPlaceholder) {
                texPreviewPlaceholder.classList.remove('hidden');
                texPreviewPlaceholder.innerHTML = `<div style="font-size:11px; color:var(--gs-red);">Error: ${err.message}</div>`;
            }
        } finally {
            if (requestId === currentPreviewRequestId && texPreviewLoading) {
                texPreviewLoading.classList.add('hidden');
            }
        }
    }

    function updateTextureSelectionCounters() {
        if (!currentInspection || !currentInspection.textures) return;
        const total = currentInspection.textures.length;
        const excluded = currentInspection.textures.filter(t => excludedTexturesSet.has(t.path)).length;
        const selected = total - excluded;
        const risky = currentInspection.textures.filter(t => t.risk && t.risk.isRisky).length;

        if (texRiskyBtnCount) texRiskyBtnCount.innerText = risky;
        if (texPillAllCount) texPillAllCount.innerText = total;
        if (texPillRiskyCount) texPillRiskyCount.innerText = risky;
        if (texPillExcludedCount) texPillExcludedCount.innerText = excluded;
        if (texSelectedCount) texSelectedCount.innerText = selected;
        if (texTotalCount) texTotalCount.innerText = total;
        if (texExcludedCount) {
            texExcludedCount.innerText = `${excluded} excluded`;
            texExcludedCount.style.color = excluded > 0 ? 'var(--gs-yellow)' : 'var(--gs-text-dim)';
        }

        if (texMasterCheckbox) {
            if (excluded === 0) {
                texMasterCheckbox.checked = true;
                texMasterCheckbox.indeterminate = false;
            } else if (excluded === total) {
                texMasterCheckbox.checked = false;
                texMasterCheckbox.indeterminate = false;
            } else {
                texMasterCheckbox.checked = false;
                texMasterCheckbox.indeterminate = true;
            }
        }
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

        // 2. Textures (Selector & Inspector)
        currentInspection.textures.forEach(t => {
            if (!t.risk) t.risk = assessTextureRisk(t.path);
        });

        updateTextureSelectionCounters();

        const filteredTextures = currentInspection.textures.filter(t => {
            if (activeTexPillFilter === 'risky' && !t.risk.isRisky) return false;
            if (activeTexPillFilter === 'excluded' && !excludedTexturesSet.has(t.path)) return false;

            if (!filter) return true;
            return (t.path && t.path.toLowerCase().includes(filter)) ||
                   (t.type && t.type.toLowerCase().includes(filter)) ||
                   (t.risk && t.risk.level && t.risk.level.toLowerCase().includes(filter)) ||
                   (t.risk && t.risk.tags && t.risk.tags.some(tag => tag.toLowerCase().includes(filter)));
        });

        inspectTexturesTbody.innerHTML = '';
        if (filteredTextures.length === 0) {
            inspectTexturesTbody.innerHTML = '<tr><td colspan="4" style="text-align:center; padding:15px; color:var(--gs-text-dim);">No textures matching current filter.</td></tr>';
        } else {
            filteredTextures.forEach(t => {
                const isExcluded = excludedTexturesSet.has(t.path);
                const tr = document.createElement('tr');
                if (isExcluded) tr.classList.add('row-excluded');
                if (selectedTextureForPreview && selectedTextureForPreview.path === t.path) {
                    tr.classList.add('selected');
                }

                let riskBadge = '<span class="badge-risk-clean">CLEAN</span>';
                if (t.risk.level === 'HIGH') {
                    riskBadge = `<span class="badge-risk-high" title="${t.risk.reason}">HIGH RISK</span>`;
                } else if (t.risk.level === 'MEDIUM') {
                    riskBadge = `<span class="badge-risk-medium" title="${t.risk.reason}">MEDIUM</span>`;
                }

                tr.innerHTML = `
                    <td style="text-align:center;">
                        <input type="checkbox" class="tex-row-cb" ${!isExcluded ? 'checked' : ''} data-path="${t.path}" title="Include/exclude from cloud upload">
                    </td>
                    <td>${riskBadge}</td>
                    <td><span class="badge-tag">${t.type}</span></td>
                    <td><strong style="color:var(--gs-text-highlight);">${t.path}</strong></td>
                `;

                const cb = tr.querySelector('.tex-row-cb');
                cb.addEventListener('click', (e) => {
                    e.stopPropagation();
                    if (cb.checked) {
                        excludedTexturesSet.delete(t.path);
                        tr.classList.remove('row-excluded');
                    } else {
                        excludedTexturesSet.add(t.path);
                        tr.classList.add('row-excluded');
                    }
                    updateTextureSelectionCounters();
                    if (selectedTextureForPreview && selectedTextureForPreview.path === t.path) {
                        updatePreviewPaneIncludeButton(t);
                    }
                });

                tr.addEventListener('click', () => {
                    inspectTexturesTbody.querySelectorAll('tr').forEach(r => r.classList.remove('selected'));
                    tr.classList.add('selected');
                    loadTexturePreview(t);
                });

                inspectTexturesTbody.appendChild(tr);
            });

            // Auto-load preview of first texture if none active
            if (!selectedTextureForPreview && filteredTextures.length > 0) {
                loadTexturePreview(filteredTextures[0]);
            }
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

    // Texture Toolbar & Selector Listeners
    btnTexSelectAll?.addEventListener('click', () => {
        excludedTexturesSet.clear();
        renderFilteredInspectViews();
        logToConsole('Texture Selector: Selected all textures for cloud upload.', 'info');
    });

    btnTexDeselectAll?.addEventListener('click', () => {
        if (!currentInspection || !currentInspection.textures) return;
        currentInspection.textures.forEach(t => excludedTexturesSet.add(t.path));
        renderFilteredInspectViews();
        logToConsole(`Texture Selector: Deselected all ${currentInspection.textures.length} textures.`, 'warning');
    });

    btnTexDeselectRisky?.addEventListener('click', () => {
        if (!currentInspection || !currentInspection.textures) return;
        let count = 0;
        currentInspection.textures.forEach(t => {
            if (!t.risk) t.risk = assessTextureRisk(t.path);
            if (t.risk.isRisky) {
                excludedTexturesSet.add(t.path);
                count++;
            }
        });
        renderFilteredInspectViews();
        logToConsole(`Cloud Protection: Deselected ${count} risky textures (Nudity/Gore/Alcohol/Decals) to protect account.`, 'success');
        inspectFooterInfo.innerHTML = `<span style="color:#b4dfb4;">Deselected ${count} risky textures. Safe mode active!</span>`;
    });

    btnTexInvert?.addEventListener('click', () => {
        if (!currentInspection || !currentInspection.textures) return;
        currentInspection.textures.forEach(t => {
            if (excludedTexturesSet.has(t.path)) {
                excludedTexturesSet.delete(t.path);
            } else {
                excludedTexturesSet.add(t.path);
            }
        });
        renderFilteredInspectViews();
        logToConsole('Texture Selector: Inverted texture selection.', 'info');
    });

    btnTexToggleInclude?.addEventListener('click', () => {
        if (!selectedTextureForPreview) return;
        const p = selectedTextureForPreview.path;
        if (excludedTexturesSet.has(p)) {
            excludedTexturesSet.delete(p);
        } else {
            excludedTexturesSet.add(p);
        }
        renderFilteredInspectViews();
        updatePreviewPaneIncludeButton(selectedTextureForPreview);
    });

    texMasterCheckbox?.addEventListener('click', () => {
        if (!currentInspection || !currentInspection.textures) return;
        const checkAll = texMasterCheckbox.checked;
        const filter = (inspectFilterInput.value || '').trim().toLowerCase();
        const visible = currentInspection.textures.filter(t => {
            if (activeTexPillFilter === 'risky' && !t.risk.isRisky) return false;
            if (activeTexPillFilter === 'excluded' && !excludedTexturesSet.has(t.path)) return false;
            if (!filter) return true;
            return (t.path && t.path.toLowerCase().includes(filter)) ||
                   (t.type && t.type.toLowerCase().includes(filter));
        });

        visible.forEach(t => {
            if (checkAll) {
                excludedTexturesSet.delete(t.path);
            } else {
                excludedTexturesSet.add(t.path);
            }
        });
        renderFilteredInspectViews();
    });

    texFilterPills?.forEach(pill => {
        pill.addEventListener('click', () => {
            texFilterPills.forEach(p => p.classList.remove('active'));
            pill.classList.add('active');
            activeTexPillFilter = pill.getAttribute('data-tex-filter') || 'all';
            renderFilteredInspectViews();
        });
    });

    texPreviewPath?.addEventListener('click', () => {
        if (selectedTextureForPreview && selectedTextureForPreview.path) {
            navigator.clipboard.writeText(selectedTextureForPreview.path).then(() => {
                logToConsole(`Copied to clipboard: ${selectedTextureForPreview.path}`, 'info');
            });
        }
    });

    function showModerationModal(assetPath, reason) {
        if (moderationFlaggedAsset) {
            moderationFlaggedAsset.innerText = assetPath || 'Unknown asset';
        }
        if (modalModerationAlert) {
            modalModerationAlert.classList.remove('hidden');
        }
    }

    function closeModerationModal() {
        if (modalModerationAlert) {
            modalModerationAlert.classList.add('hidden');
        }
    }

    if (btnCloseModerationModal) btnCloseModerationModal.addEventListener('click', closeModerationModal);
    if (btnModerationDismiss) btnModerationDismiss.addEventListener('click', closeModerationModal);
    if (btnModerationOpenRoblox) {
        btnModerationOpenRoblox.addEventListener('click', () => {
            if (window.api && window.api.openUrl) {
                window.api.openUrl('https://www.roblox.com');
            } else {
                window.open('https://www.roblox.com', '_blank');
            }
        });
    }
    if (btnModerationResume) {
        btnModerationResume.addEventListener('click', () => {
            closeModerationModal();
            if (!isConverting) {
                btnConvertAction.click();
            }
        });
    }

    window.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            if (modalMapInspect && !modalMapInspect.classList.contains('hidden')) {
                closeInspector();
            } else if (modalModerationAlert && !modalModerationAlert.classList.contains('hidden')) {
                closeModerationModal();
            }
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
            customTexturesDir: customTexturesInput.value.trim(),
            excludedTextures: Array.from(excludedTexturesSet)
        };

        // Switch UI to converting state
        isConverting = true;
        isModeratedPaused = false;
        conversionHasErrors = false;
        btnConvertAction.disabled = true;
        btnConvertAction.classList.remove('btn-resume-glow');
        btnConvertAction.innerText = 'START CONVERSION';
        btnOpenStudio.disabled = true;
        btnCancel.style.display = 'inline-block';
        progressBar.style.width = '0%';
        statusText.innerText = `Starting conversion for ${mapName || modelName || vtfName}...`;

        logToConsole(`=======================================================`, 'cmd');
        logToConsole(`STARTING SOURCE2ROBLOX CONVERSION`, 'cmd');
        logToConsole(`Target: ${mapName ? 'Map ' + mapName : (modelName ? 'Model ' + modelName : 'VTF ' + vtfName)}`, 'info');
        logToConsole(`Game Folder: ${gameDir}`, 'info');
        logToConsole(`Server-Side Uploads: Textures=${job.uploadAssets}, Meshes=${job.uploadMeshes}`, 'roblox');
        if (job.excludedTextures && job.excludedTextures.length > 0) {
            logToConsole(`Cloud Protection: ${job.excludedTextures.length} textures excluded from cloud upload.`, 'warning');
        }
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

            case 'moderation_blocked': {
                isModeratedPaused = true;
                logToConsole('====================================================', 'error');
                logToConsole(' [!] ROBLOX ACCOUNT MODERATED - UPLOADS PAUSED', 'error');
                if (evt.data && evt.data.asset) {
                    logToConsole(` Flagged Asset: ${evt.data.asset}`, 'warning');
                }
                logToConsole(' Uploads stopped immediately to prevent asset loss.', 'warning');
                logToConsole(' Acknowledge warning at https://www.roblox.com, then click RESUME', 'warning');
                logToConsole('====================================================', 'error');
                showModerationModal(evt.data?.asset, evt.data?.reason || evt.message);
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

        if (isModeratedPaused || data.code === 2) {
            statusText.innerText = 'PAUSED: Account moderated. Review warning on Roblox, then click RESUME CONVERSION.';
            btnConvertAction.innerText = 'RESUME CONVERSION';
            btnConvertAction.classList.add('btn-resume-glow');
            logToConsole(`Engine paused cleanly (Exit code: ${data.code}). Upload cache is preserved; resume anytime.`, 'warning');
        } else if (data.code === 0 && !conversionHasErrors) {
            statusText.innerText = 'Conversion complete. Ready.';
            btnConvertAction.innerText = 'START CONVERSION';
            btnConvertAction.classList.remove('btn-resume-glow');
        } else {
            statusText.innerText = `Conversion failed (Exit code: ${data.code}). Check console log.`;
            logToConsole(`Engine process finished with code ${data.code}${conversionHasErrors ? ' (with errors)' : ''}`, 'error');
            btnConvertAction.innerText = 'START CONVERSION';
            btnConvertAction.classList.remove('btn-resume-glow');
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
                logToConsole('  resume       - Resumes paused conversion after moderation', 'info');
                logToConsole('  validate     - Tests Roblox Open Cloud API credentials', 'info');
                logToConsole('  steam        - Rescans local Steam libraries for games', 'info');
                logToConsole('  cache        - Displays asset upload deduplication statistics', 'info');
                logToConsole('  clearcache   - Clears upload cache file', 'info');
                logToConsole('  export       - Opens output folder in Windows Explorer', 'info');
                logToConsole('  clear        - Clears the console output log', 'info');
                break;

            case 'resume':
                if (isConverting) {
                    logToConsole('Conversion is already running.', 'warning');
                } else {
                    logToConsole('Resuming conversion process...', 'info');
                    btnConvertAction.click();
                }
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
