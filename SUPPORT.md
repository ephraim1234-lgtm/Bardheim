# Support

Use the GitHub repository for reproducible Bardheim issues:

https://github.com/ephraim1234-lgtm/Bardheim

For direct contact, release questions, permissions, or takedown requests, use:

```text
longhouselistings@gmail.com
```

## Before Reporting

- Confirm Bardheim, BepInExPack Valheim, and Jotunn are installed in the same
  profile.
- Launch through the same mod manager profile where Bardheim is installed.
- For MIDI issues, confirm the file is Standard MIDI format 0 or 1 and is in:

```text
BepInEx/plugins/Bardheim/songs/
```

- For multiplayer audio, confirm every participating player has Bardheim
  installed and `Multiplayer.EnableMultiplayerAudio` enabled. Multiplayer audio
  is experimental in `0.7.0`.

## Include in Bug Reports

- Bardheim version.
- Valheim version.
- BepInExPack Valheim version.
- Jotunn version.
- Whether the install came from Thunderstore/r2modman, Nexus, or manual copy.
- Relevant `BepInEx/LogOutput.log` lines.
- Steps to reproduce from a fresh launch.
- For MIDI issues, whether the problem affects lyre, drum, flute, or all
  instruments.

## Known 0.7.0 Limits

- Local play is the accepted release path.
- Multiplayer audio is disabled by default and needs more two-client testing.
- Bardheim does not synchronize MIDI files or song state between players.
- Bardheim does not add buffs, skills, attacks, build pieces, or durability.
