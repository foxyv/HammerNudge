# Dizzy Nudge

Sailwind BepInEx 5 plugin. Hold a **hammer**, look at **locked** (nailed) furniture, hold **Q / T / E**, and click to micro-move it.

Vanilla hammer lock/unlock is unchanged when those keys are not held. Unlock physics items before you can pick them up as usual.

```powershell
dotnet build src\Dizzy.Nudge\Dizzy.Nudge.csproj -c Release
```

Deploys to `BepInEx\plugins\Dizzy.Nudge\` when that folder exists next to `Sailwind.exe`.

## In-game

Lock the item with the hammer first, then:

| Hold | Left click | Right click |
|------|------------|-------------|
| `Q` | away | closer |
| `T` | up | down |
| `E` | left | right |

Hold **Shift** for a finer step. Axes follow your look, flattened to the deck when the item is on a boat.

Config: `BepInEx\config\com.dizzy.sailwind.nudge.cfg`

Works alongside [UnlimitedHammer](https://thunderstore.io/c/sailwind/p/DogEggz/UnlimitedHammer/) (nail-anything). Nudge only steals a click when Q/T/E is held on an already locked item; a fault in another hammer patch will not take down vanilla lock/unlock.

## Install

Requires Sailwind + [BepInEx 5](https://thunderstore.io/c/sailwind/p/BepInEx/BepInExPack/) (Thunderstore BepInExPack recommended). Extract `Dizzy.Nudge` into `BepInEx\plugins\`. Restart Sailwind and check `BepInEx\LogOutput.log`.
