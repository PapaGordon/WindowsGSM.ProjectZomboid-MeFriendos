VERSION 0.1.1

WindowsGSM.ProjectZomboid - MeFriendos build
================================================

Installation
------------
1. Copy the complete ProjectZomboid.cs folder into the WindowsGSM plugins folder.
2. Reload plugins or restart WindowsGSM.
3. Add or import the Project Zomboid server.
4. Configure the required game and RCON ports manually in Windows Firewall.

Firewall behavior
-----------------
WindowsGSM creates an automatic application exception for the bundled java.exe before every server start.

This build removes that exact application exception through the same Windows Firewall COM API family used by WindowsGSM itself. It then checks the authorized-application list again before allowing Java to start.

The cleanup does not depend on the PowerShell NetSecurity cmdlets. Manual port rules are not created or removed, and Java rules belonging to another server are not selected because the exact executable path must match.

Automatic port opening is intentionally disabled. Configure only the required port, protocol, Windows profile and remote scope manually.

If Windows cannot verify or remove the matching automatic exception, the plugin refuses to start the server and shows an error. Run WindowsGSM as administrator.

MeFriendos setup
----------------
- Game UDP ports: Public profile
- RCON TCP port: Private profile, VPN/local subnet only
- RCON TCP port: blocked on Public

Use the ports configured for your own server.

Credits
-------
Based on WindowsGSM.ProjectZomboid by Richard Beard.
MeFriendos build by PapaGordon / MeFriendos.

https://github.com/PapaGordon/WindowsGSM.ProjectZomboid-MeFriendos
https://mefriendos.de
