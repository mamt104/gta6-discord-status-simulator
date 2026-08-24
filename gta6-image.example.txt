# Security

This desktop application needs only a Discord **Application ID**, which is public.

Never commit or paste any of the following into this project:

- Bot Token
- Client Secret
- OAuth access or refresh token
- Discord webhook URL
- account password or session token

The application communicates locally with the Discord desktop IPC pipe. It does not need a bot account and does not need privileged bot permissions.

If a real secret is committed, remove it from Git history and rotate/revoke it immediately in the Discord Developer Portal.

