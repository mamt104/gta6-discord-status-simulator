# GTA VI Discord Rich Presence 🌴

A polished and configurable Windows presence simulator with Discord game detection, realistic activity rotation, custom statuses, character selection, and saved profiles.

> [!IMPORTANT]
> This is an unofficial fan project. It is not affiliated with, endorsed by, or sponsored by Rockstar Games, Take-Two Interactive, or Discord. It does not include GTA VI and is not evidence of access to a game build.

## Required first-time setup

**You must run `GTA6.exe` and add that running executable to Discord's Registered Games list. This is the only supported way to make Discord detection mode work correctly.**

1. Build or download the project and launch the fixed-name file `GTA6.exe`.
2. Keep the program running.
3. Open **Discord → User Settings → Registered Games**.
4. Select **Add it!** and choose the running `GTA6.exe`.
5. Keep **Discord detection only** enabled in the app.
6. Do not rename or move `GTA6.exe` after registering it. If you move it, remove the old Registered Games entry and add the new path again.

Without this step, Discord may display the wrong name or icon, fail to create recent activity, or treat each renamed build as a different game.

Discord ultimately controls detected Game Profiles, icons, links, and recent activity. Detection can vary by client version, account, cache, executable path, and Discord's server-side game database; the application cannot guarantee an official or verified profile.

## Two separate modes

### Discord detection only — recommended

- starts automatically every time the app opens;
- sends no custom Rich Presence payload;
- lets Discord control the detected game card and hover behavior;
- avoids artificial party text such as `1 of 1`;
- gives the most natural result available through executable detection.

### Custom Rich Presence — optional

Disable **Discord detection only**, enter your own Discord Application ID, and select **START**. This mode enables custom scenes, rotating descriptions, timers, and the optional `Join` URL button.

Custom Rich Presence replaces the detected game card. Discord may make the complete `name + details + state` block clickable or underline the entire block on hover. Rich Presence does not provide a setting that makes only the game name clickable, and it cannot claim another publisher's official Game Profile.

## Features

- pure Discord detection mode enabled on every startup;
- realistic automatic activity sequence or manual status control;
- variable scene timing or fixed 1, 3, 5, 10, 15, or 30-minute intervals;
- Jason, Lucia, or combined character selection;
- continuous session timer;
- optional five-minute startup period with no description;
- optional `Join` URL button;
- automatic profile saving;
- no artificial party size in single-player;
- no Bot Token, Client Secret, or Discord login required.

## Requirements

- Windows 10 or Windows 11;
- the Discord desktop app running on the same computer;
- Microsoft .NET Framework 4.x;
- your own Discord Developer application only if you want custom Rich Presence.

This is not technically a Discord bot. It is a desktop program that communicates with the local Discord client through Rich Presence IPC.

## Build from source

Open PowerShell in the project directory and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The fixed-name executable is created at:

```text
dist\GTA6.exe
```

To remove previous build output and rebuild:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Clean
```

If you add `assets\app-icon.png` before building, the build script generates and embeds a Windows icon. Without that optional file, Windows uses the default executable icon.

## Configure custom Rich Presence

You can skip this section when using **Discord detection only**.

### 1. Create a Discord application

1. Open <https://discord.com/developers/applications>.
2. Select **New Application**.
3. Choose a name.
4. Under **General Information**, copy the **Application ID**.
5. Do not use your Discord user ID; it is a different identifier.
6. You do not need to create a bot.
7. Never share a Bot Token or Client Secret. This project does not use either one.

### 2. Upload a Rich Presence image

1. In the Developer Portal, open **Rich Presence → Art Assets**.
2. Upload a square PNG or JPG, preferably 1024 × 1024.
3. Before saving it, assign the asset key `gtavi_cover`.
4. Discord does not allow saved asset keys to be renamed. Delete and upload the asset again if you need a different key.
5. Only use artwork that you are allowed to use or distribute.

Rockstar artwork is intentionally not included. See [assets/README.md](assets/README.md) for optional local filenames.

### 3. Save your Application ID

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\configure.ps1
```

Paste your Application ID when prompted. To use a different image asset key:

```powershell
.\configure.ps1 -AssetKey "my_cover"
```

The script creates these local files under `dist`:

- `gta6-presence.txt`: your public Application ID;
- `gta6-image.txt`: the uploaded image asset key;
- `gta6-profile.ini`: settings saved by the app.

These local configuration files are excluded from Git.

## Run

```powershell
.\run.ps1
```

You can also launch `dist\GTA6.exe` directly. Always keep the filename and registered path unchanged when using Discord detection.

## Troubleshooting

### Discord does not show the game

- confirm that the Discord desktop app is open;
- run `GTA6.exe` before opening Registered Games;
- add the exact running executable through **Add it!**;
- keep **Discord detection only** enabled;
- remove stale entries for old names such as `GTA6-v1.0-READY.exe`;
- restart Discord after changing the registered path.

### The whole text block is underlined on hover

You are using custom Rich Presence. Discord controls that hover style, and the RPC payload cannot change its clickable area. Re-enable **Discord detection only** for pure executable detection.

### The Rich Presence image does not appear

- make sure `dist\gta6-image.txt` exactly matches the Developer Portal asset key;
- allow a few minutes for Discord's cache to update;
- quit and reopen Discord;
- delete and upload the asset again if you tried to rename a saved key.

### Buttons do not appear on my own profile

Discord may hide an activity's URL buttons from its owner. Check the Presence from a second account or ask a friend.

## Security and privacy

Read [SECURITY.md](SECURITY.md). Application IDs and Public Keys are public; Bot Tokens and Client Secrets are private. The program does not collect passwords and does not require administrator privileges.

## Publishing on GitHub

See [PUBLISHING.md](PUBLISHING.md). Before publishing, confirm that `git status` does not contain personal configuration, unlicensed artwork, tokens, webhooks, or secrets.

## License

The source code is available under the [MIT License](LICENSE). Third-party names, trademarks, and artwork remain the property of their respective owners.
