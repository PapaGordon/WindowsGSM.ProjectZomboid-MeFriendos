VERSION 0.1.0

WindowsGSM.ProjectZomboid - MeFriendos build
================================================

Installation
------------
1. Copy the complete ProjectZomboid.cs folder into the WindowsGSM plugins folder.
2. Reload plugins or restart WindowsGSM.
3. Add or import the Project Zomboid server.
4. Configure the required game and RCON ports manually in Windows Firewall.

Firewall change
---------------
WindowsGSM creates a broad inbound rule for the bundled java.exe before every server start.

This build removes broad inbound allow rules on any network profile that point to the exact bundled Java executable and allow every local port from every local and remote address. Port-specific or address-restricted manual rules are preserved.

Automatic port opening is intentionally disabled. It does not create or delete your manual port rules. It also does not touch Java rules belonging to another server.

This is safer because you can open only the required port, protocol, Windows profile and remote scope instead of allowing the complete Java runtime through the Public firewall profile.

If Windows cannot verify or remove the broad rule, the plugin refuses to start the server and shows an error. Run WindowsGSM as administrator.

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
