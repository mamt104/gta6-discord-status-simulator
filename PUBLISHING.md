# Publishing on GitHub

## Pre-publish checklist

Do not add any of the following to the repository:

- Bot Tokens or Client Secrets;
- Discord webhooks;
- `dist/gta6-presence.txt` containing your personal Application ID;
- personal profiles or screenshots containing account information;
- artwork or logos that you are not allowed to redistribute;
- old executables or test directories.

The included `.gitignore` excludes local builds, personal configuration, and user-provided artwork. The repository intentionally tracks only the sanitized files under `dist` that are part of the public ready-to-run package.

## Protect your commit identity

Git commits contain an author name and email address. Enable **Keep my email addresses private** in your GitHub email settings and use the GitHub-provided `noreply` address if you do not want to publish your personal email.

Check the repository-specific values before the first commit:

```powershell
git config user.name
git config user.email
```

## Create the local repository

From PowerShell in the project directory:

```powershell
git init
git add .
git status
git commit -m "Initial community release"
git branch -M main
```

Review the output of `git status` carefully before committing.

## Connect GitHub

Create an empty repository on GitHub, then use the URL shown by GitHub:

```powershell
git remote add origin https://github.com/YOUR-USERNAME/REPOSITORY-NAME.git
git push -u origin main
```

Choose **Private** if you do not want the source to be publicly visible. Anyone can technically read and copy files from a **Public** repository. A license defines permitted use but cannot prevent downloads.

## Publish a release

Attach the packaged download to a GitHub Release instead of storing every release binary in Git history.

Suggested release fields:

```text
Tag: v1.1.0
Title: GTA VI Discord Status Simulator v1.1.0
```

Calculate and publish the SHA-256 hash:

```powershell
Get-FileHash .\dist\GTA6.exe -Algorithm SHA256
```

Unsigned executables can trigger Windows SmartScreen or antivirus warnings. Publishing the source and reproducible build instructions allows users to inspect and verify the program.
