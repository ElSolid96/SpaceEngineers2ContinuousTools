# Changelog

## Unreleased

- Ignore SE2's transient `BlockIntegrityScreen` and `InteractionHintScreen` overlays so ordinary
  welder/grinder use and double-click latching are not interrupted.
- Keep real screens, focus loss, tool removal, and world teardown as forced-release boundaries.
- Add tests covering transient tool overlays and real screen interruptions.

## 1.0.0

- Initial double-click latch for the hand drill, welder, and grinder.
- Configurable double-click window and per-tool enable switches.
- Native release on tool removal, screen changes, focus loss, input teardown, or plugin unload.
