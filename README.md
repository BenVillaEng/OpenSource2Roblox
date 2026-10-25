# OpenSource2Roblox - GoldSource Edition v2.0

**OpenSource2Roblox** is a high-performance reverse-engineering tool for converting Valve Source 1 engine assets (`.bsp` maps, `.mdl` models, `.vtf` textures, `.vmt` materials) into native Roblox Studio formats (`.rbxl`, `.rbxm`, `.mesh`, and PBR `SurfaceAppearance`).

This release features a classic **GoldSource VGUI (Counter-Strike 1.6 / Half-Life)** desktop user interface, complete Roblox Open Cloud server-side asset integration, real-time developer console, and automated Steam library discovery.

---

## Key Features

* **Authentic GoldSource VGUI Design**: Faithful 1:1 recreation of the classic Valve VGUI Options dialog and Developer Console (`~`).
* **Pre-Import Map Asset Inspector**: Inspect any `.bsp` map before converting to see an exact preview of all 3D models (`.mdl`), textures & materials (`.vtf`/`.vmt`), world geometry (faces, displacements, brushes), and entities, with instant search filtering and VPK presence verification.
* **Server-Side Cloud Import (`rbxassetid://`)**: Automatically uploads converted textures (`Image`) and meshes (`Mesh`) directly to Roblox Open Cloud Assets API. The exported place is 100% cloud-ready for multiplayer servers and Team Create without missing textures.
* **Automatic Steam Game Discovery**: Scans Windows Registry and `libraryfolders.vdf` across all drives to auto-detect Garry's Mod, CS:GO, CS:Source, Half-Life 2, Team Fortress 2, and Left 4 Dead 2.
* **Interactive Developer Console**: Real-time conversion event stream with color-coded logging and interactive command input (`help`, `status`, `validate`, `inspect`, `cache`, `clearcache`, `steam`, `export`).
* **Roblox API Key Validator**: Built-in credential test tool that queries Roblox Open Cloud gateways before starting lengthy conversions.
* **Intelligent Asset Deduplication**: MD5 content hashing and local cache (`upload_cache.json`) to eliminate duplicate asset uploads across different maps and runs.

---

## Quick Start

### 1. Launching the App
* **Standalone Release**: Unzip `OpenSource2Roblox-v2.0-GoldSource-win-x64.zip` and run `OpenSource2Roblox.exe`.
* **Launcher**: Double-click `OpenSource2Roblox-GoldSource.bat` in the project root.
* **From Source**: Run `cd ui && npm start`.

### 2. Converting a Map
1. In the **Maps** tab, select your game from the **Detected Steam Game** dropdown (or click *Browse...* to select the game folder with `gameinfo.txt`).
2. Pick any detected `.bsp` map from the list (e.g. `office_summer`, `gm_construct`, `de_dust2`).
3. Ensure **Upload Textures** and **Upload Meshes** are checked if you want server-side assets.
4. Click **CONVERT MAP NOW** and monitor live progress in the **Console** tab.
5. When complete, click **OPEN STUDIO PLACE** to load the map in Roblox Studio.

---

## Roblox Cloud Credentials Setup (Exact Order)

To make textures and meshes visible to all players server-side:
1. Go to the [Roblox Creator Dashboard](https://create.roblox.com/dashboard/credentials).
2. Navigate to **API Keys** and click **Create API Key**.
3. Under **Access Permissions**, add and configure both of the following:
   * **Assets**: enable **Write** (Asset: Create) and **Read**
   * **Assets Permission**: enable **Write** and **Read**
4. Under **Creator**, select whether you are publishing under your personal User Account or a Roblox Group.
5. Copy the generated API Key and your numeric **Creator ID** into the **Roblox Cloud** tab in the app.
6. Click **Test Credentials** to verify authorization.

---

## Bug Fixes & Improvements in this Release

1. **Fixed Workshop Addon World Assembly Crash (`Parameter 'key'`)**: Resolved an `ArgumentNullException: Value cannot be null. (Parameter 'key')` that aborted map conversion during `Assembling world...` when props had null models or empty keys, ensuring the `.rbxl` place file is generated and saved reliably.
2. **Automatic Cross-Game Dependency Mounting**: Workshop and community maps depending on **Counter-Strike: Source (CS:S)**, **Team Fortress 2 (TF2)**, **Half-Life 2 (HL2)**, **Episode 1/2**, **Portal**, and **Left 4 Dead 2** are now automatically detected and mounted across all Steam libraries and `mount.cfg`, resolving missing models and textures.
3. **Workshop Addons & Navigation Mesh (.nav) Support**:
   * Scans Garry's Mod `addons/`, `download/maps/`, and Steam workshop directories (`workshop/content/4000/`).
   * Automatically pairs `.nav` navigation mesh files with their corresponding `.bsp` map.
   * Note: Roblox Studio automatically computes native 3D navigation meshes via `PathfindingService`.
4. **Accurate Error Reporting**: Prevented false positive `CONVERSION FINISHED SUCCESSFULLY [OK]` messages when map conversions encounter errors.
5. **Fixed Mesh Cloud Upload Dependency**: Meshes can be uploaded to Roblox Cloud independently of textures.
6. **Roblox Gateway Credential Validator**: Added `-verifyCredentials` CLI verification providing instant feedback on key validity.

---

## Compiling from Source

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Node.js v18+ and npm

### Build the Backend Executable
```bash
dotnet publish vendor/Source2Roblox/Source2Roblox.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output release
```

### Run the Desktop Frontend
```bash
cd ui
npm install
npm start
```