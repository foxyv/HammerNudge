# Dizzy Nudge

Sailwind BepInEx 5 plugin. Hold a **hammer**, look at **locked** (nailed) furniture, hold **Q / T / E**, and click to micro-move it. Hold **Alt** with those keys to rotate. Press **G** to flatten the item to the deck.

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
| `Alt`+`Q` | tilt away | tilt closer |
| `Alt`+`T` | roll left | roll right |
| `Alt`+`E` | turn left | turn right |

Press **G** (no click) to level the item flat on the deck, keeping its heading. Hold **Shift** for a finer step. Axes follow your look, flattened to the deck when the item is on a boat.

Config: `BepInEx\config\com.dizzy.sailwind.nudge.cfg`

Works alongside [UnlimitedHammer](https://thunderstore.io/c/sailwind/p/DogEggz/UnlimitedHammer/) (nail-anything). Nudge only steals a click when Q/T/E is held on an already locked item; a fault in another hammer patch will not take down vanilla lock/unlock. Furniture Fix still uses Alt for empty-hand furniture pickup. With HooksHangMore, a nudge click on a lamp hook never hangs the hammer there, so the hook stays free for lamps. With Deft Hands, which also uses Alt, Alt+Q/T/E right-click still rotates; Deft Hands still freezes the camera and turns the hammer in hand while Alt is held.

## Install

Requires Sailwind + [BepInEx 5](https://thunderstore.io/c/sailwind/p/BepInEx/BepInExPack/) (Thunderstore BepInExPack recommended). Extract `Dizzy.Nudge` into `BepInEx\plugins\`. Restart Sailwind and check `BepInEx\LogOutput.log`.
