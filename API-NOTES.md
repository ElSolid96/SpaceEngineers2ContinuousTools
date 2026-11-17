# API inspection notes

Inspected the installed assemblies in
`C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers2\Game2` with Mono.Cecil.

The hand drill is not just `DrillComponent`: the handheld specialization is `DrillToolComponent`.
Its public `RequestPrimaryAction(bool)` forwards to `TrySetPrimaryAction`, and the latter enters the
existing replicated drill state machine. The class also tracks `_primaryInputHeld`, confirming that
the request method is the native input edge rather than a direct mining bypass.

Welder and grinder are two modes of `IntegrityHandToolComponent`, not separate component types.
Its private `RequestActivatePrimaryActions(bool)` forwards directly to
`IntegrityToolComponent.TrySetActivation(bool, false)`. The current mode comes from
`IntegrityToolPerPlayerData.Mode`; enum values are `Component` and `Raw`.

The implementation hooks the request methods and retains their native active state after the
second release. It never calls `TryWeld`, `TryGrind`, voxel mining, inventory transfer, or damage
methods directly.
