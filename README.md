# Dizzy Nudge

Sailwind BepInEx 5 plugin. Hold a **hammer**, look at **locked** (nailed) furniture, hold **Q / T / E**, and click to micro-move it. Hold **Alt** with those keys to rotate, **Shift** for smaller steps. Press **G** to flatten the item to the deck.

Vanilla hammer lock/unlock is unchanged when those keys are not held. Unlock physics items before you can pick them up as usual.

## Controls

Hold a hammer and look at a locked item. To lock one, hold right-click on it with the hammer until it locks.

### Move and rotate

Hold the keys, then click. Left click and right click go opposite ways.

| Hold | Left click | Right click | Each click | With `Shift` |
|------|------------|-------------|------------|--------------|
| `Q` | away | closer | 5 cm | 1 cm |
| `T` | up | down | 5 cm | 1 cm |
| `E` | left | right | 5 cm | 1 cm |
| `Alt`+`Q` | tilt away | tilt closer | 5° | 1° |
| `Alt`+`T` | roll left | roll right | 5° | 1° |
| `Alt`+`E` | turn left | turn right | 5° | 1° |

So fine rotation is `Shift`+`Alt`+`Q`/`T`/`E` and a click: 1° per click instead of 5°.

- Directions follow where you are looking, flattened to the deck when the item is on a boat.
- Left or right `Shift` both count, and so do left or right `Alt`.
- The game's keyboard pick-up and activate keys (`F` and `R` by default) work the same as left click and right click.

### Other keys

| Key | What it does |
|-----|--------------|
| `G` (no click) | Levels the item flat on the deck, keeping its heading. |
| Right click, no keys held | Vanilla hammer: hold to lock, click a locked item to unlock. Also unlocks items vanilla refuses, such as empty water barrels. |

### Good to know

- `T` is also the game's Throw key. While you hold the hammer, letting go of `Q`/`T`/`E` never drops or throws it. To drop the hammer, click at empty space with none of those keys held.
- On Windows, Left `Alt` + Left `Shift` switches keyboard language if you have more than one installed. Use Right `Shift` for fine rotation if that happens.

## Settings

`BepInEx\config\com.dizzy.sailwind.nudge.cfg`, created the first time the game runs with the mod.

| Setting | Default | What it does |
|---------|---------|--------------|
| `Enabled` | `true` | Turns nudging on or off. |
| `StepMeters` | `0.05` | Move per click, in meters (0.005 to 0.5). |
| `FineStepMeters` | `0.01` | Move per click with the fine modifier held (0.001 to 0.25). |
| `StepDegrees` | `5` | Rotation per click, in degrees (0.5 to 45). |
| `FineStepDegrees` | `1` | Rotation per click with the fine modifier held (0.1 to 15). |
| `FineModifier` | `LeftShift` | Hold for the fine steps. |
| `RotateModifier` | `LeftAlt` | Hold to rotate instead of move. |
| `AwayKey` | `Q` | Away / closer, or tilt with the rotate modifier. |
| `VerticalKey` | `T` | Up / down, or roll with the rotate modifier. |
| `StrafeKey` | `E` | Left / right, or turn with the rotate modifier. |
| `LevelKey` | `G` | Level to the deck. |

## Other mods

Works alongside [UnlimitedHammer](https://thunderstore.io/c/sailwind/p/DogEggz/UnlimitedHammer/) (nail-anything). Nudge only steals a click when Q/T/E is held on an already locked item; a fault in another hammer patch will not take down vanilla lock/unlock.

- **Furniture Fix** still uses Alt for empty-hand furniture pickup.
- **HooksHangMore:** a nudge click on a lamp hook never hangs the hammer there, so the hook stays free for lamps.
- **Deft Hands** also uses Alt. Alt+Q/T/E right-click still rotates; Deft Hands still freezes the camera and turns the hammer in hand while Alt is held.

## Install

Requires Sailwind + [BepInEx 5](https://thunderstore.io/c/sailwind/p/BepInEx/BepInExPack/) (Thunderstore BepInExPack recommended). Extract `Dizzy.Nudge` into `BepInEx\plugins\`. Restart Sailwind and check `BepInEx\LogOutput.log`.

## Build

```powershell
dotnet build src\Dizzy.Nudge\Dizzy.Nudge.csproj -c Release
```

Deploys to `BepInEx\plugins\Dizzy.Nudge\` when that folder exists next to `Sailwind.exe`.
