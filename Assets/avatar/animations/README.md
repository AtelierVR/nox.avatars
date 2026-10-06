# Avatar animations

This folder contains the default animation assets used by the Nox avatar playable layers.

## `UniversalAnimationLibrary.fbx`

- **Source (download):** <https://raw.githubusercontent.com/IAFahim/quaternius.universalAnimationLibrary.standard/main/UAL1_Standard.fbx>
- **Repository:** <https://github.com/IAFahim/quaternius.universalAnimationLibrary.standard> (mirror)
- **Original author / pack:** **Quaternius — Universal Animation Library** — <https://quaternius.com/packs/universalanimationlibrary.html>
- **License:** **CC0 1.0 Universal (public domain)** — see the `LICENSE` file of the source repository
  (<https://github.com/IAFahim/quaternius.universalAnimationLibrary.standard/blob/main/LICENSE>).
- **File:** downloaded as `UAL1_Standard.fbx` and renamed to `UniversalAnimationLibrary.fbx`.
- **Format:** binary FBX (`Kaydara FBX Binary`), tracked with **Git LFS** (`*.fbx filter=lfs` in `Packages/nox.avatars/.gitattributes`).
- The rig's rest pose is a **T-Pose**, and the file ships **45 humanoid animation clips**.

> Note: Mixamo / Ready Player Me T-Pose FBX files were considered but **rejected**: the Ready Player Me
> Animation Library license forbids using the animations with non-RPM characters and forbids
> redistribution, so it cannot be shipped in this project.

## `standards/`

The `standards/` folder contains the **45 clips exported from `UniversalAnimationLibrary.fbx`**,
flattened into individual `.anim` assets.

- **Produced with:** the in-project tool `Nox ▸ Tools ▸ FBX Animation Exporter`
  (`Packages/nox.editor/Runtime/FBXAnimationExporter.cs`), using *Create separate files*.
- **Naming:** the FBX clip names contain the root object and a `|` separator (e.g. `Armature|Idle_Loop`).
  Since `|` is illegal in file paths, the exporter sanitizes the name to `Armature_Idle_Loop.anim`
  (all illegal path characters are replaced with `_`).
- **Origin / license:** same as the source FBX above — **Quaternius Universal Animation Library, CC0 1.0**.
  These are public-domain derivatives; do not add any extra licensing restrictions to them.

### Full list of exported clips

`A_TPose`, `Crouch_Fwd_Loop`, `Crouch_Idle_Loop`, `Dance_Loop`, `Death01`, `Driving_Loop`,
`Fixing_Kneeling`, `Hit_Chest`, `Hit_Head`, `Idle_Loop`, `Idle_Talking_Loop`, `Idle_Torch_Loop`,
`Interact`, `Jog_Fwd_Loop`, `Jump_Land`, `Jump_Loop`, `Jump_Start`, `PickUp_Table`,
`Pistol_Aim_Down`, `Pistol_Aim_Neutral`, `Pistol_Aim_Up`, `Pistol_Idle_Loop`, `Pistol_Reload`,
`Pistol_Shoot`, `Punch_Cross`, `Punch_Jab`, `Push_Loop`, `Roll`, `Roll_RM`, `Sitting_Enter`,
`Sitting_Exit`, `Sitting_Idle_Loop`, `Sitting_Talking_Loop`, `Spell_Simple_Enter`,
`Spell_Simple_Exit`, `Spell_Simple_Idle_Loop`, `Spell_Simple_Shoot`, `Sprint_Loop`,
`Swim_Fwd_Loop`, `Swim_Idle_Loop`, `Sword_Attack`, `Sword_Attack_RM`, `Sword_Idle`,
`Walk_Formal_Loop`, `Walk_Loop`.

## Other files in this folder

- `Pose.controller` — the standard `Pose` playable layer: an integer parameter `Pose` selects a
  whole-body pose (`0` normal, `1` presentation, `2` calibration, `3` sitting — see
  `Nox.Avatars.StateMachines.AvatarPose`) and each state declares what it does to the avatar with the
  state behaviours `TrackingControl` (cut/restore the IK per limb) and `PlayableLayerControl`
  (stop/resume another layer):

  | Value | State | Motion | Behaviour |
  |---|---|---|---|
  | 0 | `Normal` | none, *Write Defaults off* (the layer writes nothing, the avatar is fully driven by the player) | `TrackingControl` → Head/hands/feet in **Tracking** |
  | 1 | `Presentation` | `standards/Armature_Idle_Talking_Loop.anim` | `TrackingControl` → all in **Animation** |
  | 2 | `Calibration` | `standards/Armature_A_TPose.anim` | `TrackingControl` → all in **Animation**, `PlayableLayerControl` → stops `Locomotion` on enter, starts it on exit |
  | 3 | `Sitting` | `standards/Armature_Sitting_Idle_Loop.anim` | `TrackingControl` → hips/feet in **Animation** |

  Because the `Normal` state writes nothing, the layer can stay enabled at full weight and the game
  only has to write the integer: this is how a full-body calibration puts the avatar in its reference
  pose (`Nox.XR.Runtime.FullBody.FullBodyCalibration` asks for value `2`). Add it to the avatar's
  `PlayableAvatarModule` (as a `Pose`-named layer) and nothing else is needed on the avatar side.
- `Core.anim`, `Pulse.anim`, `Default.controller`, `Locomotion.controller`, `ErrorFX.controller` —
  the original Nox avatar playable-layer assets.
