# RWMM (Red Wizard Mod Manager)

RWMM is a lightweight mod framework for **Star Valor** (BepInEx) focused on **resource management via JSON “prototypes”**.

Right now RWMM’s scope is intentionally narrow:
- Dump the game’s “base” definitions to JSON (`base_prototypes/`) so you can use them as templates.
- Load JSON prototypes from mods and apply them to in-game databases at startup.
- Support **patching existing objects** (by `refName`) and **creating new objects by cloning** (via `cloneFrom`).
- Supports object types: `Item`, `Equipment`, `Quest`, `Perk`, `TWeapon`, `CrewMember`, `ShipModelData`.
- Allows importing PNG icons for items/equipment.
- Auto-manage numeric IDs behind the scenes (so mods should not fight over IDs).

RWMM includes **RW.Core**: a small utility library (namespace `RW`) that RWMM uses internally and that modders can reference in their own plugins.

---

## Install (for players)
(note: this mod does nothing on its own)
1. Install BepInEx.
2. Drop `RedWizardsModManager.plugin` into:
   `Star Valor\BepInEx\plugins\`
3. Drop `RedWizardsModManager.Patcher.dll` into:
   `Star Valor\BepInEx\patchers\`
4. Launch the game once in order to generate the `base_prototypes` folder.

---

## Install (for modders)

Recommended mod folder layout (Windows):

```
Star Valor\
  BepInEx\
    plugins\
      YourMod.plugin\
        prototypes\
          items\
            some_file.json
          equipment\
            other_file.json
        assets\
          items\
            your_icon.png
```

Notes:
- RWMM scans for **any folder named `prototypes` anywhere under `BepInEx\plugins\`**.
- **Folder structure and filenames under `prototypes\` are ignored** (they’re only for your organization).
- The only things that matter are the JSON contents (`type`, `refName`, etc.).

---

## First run: generate `base_prototypes`
Run the game once with RWMM installed.

RWMM will generate:

```
BepInEx\plugins\<RWMM folder>\base_prototypes\
```

This folder is **NOT loaded** as mod content. It exists only as **templates/reference** so you can copy a base object, then delete everything you’re not changing.

---

## Prototype JSON format

Every prototype file is a wrapper object:

```json
{
  "_comment": "Optional comment for humans.  This is not used in game",
  "type": "Item",
  "refName": "rwee_some_item",
  "cloneFrom": "some_existing_refName",
  "image": "assets/items/rwee_some_item.png",
  "obj": {
    "...": "fields for the underlying object type"
  }
}
```

### Required fields
- **`type`** (string): the object type being targeted. Must match what RWMM expects, e.g. `Item`, `Equipment`, `Quest`, `Perk`, `TWeapon`, `CrewMember`, `ShipModelData`.
- **`refName`** (string): your *universal key* for the object. RWMM uses this to find an existing object to update, or to assign identity to a cloned object.
- **`obj`** (object): the fields you want to set/change.

### Optional fields
- **`cloneFrom`** (string): if set, RWMM will **clone** the object with `cloneFrom` as its base, then assign your `refName` and apply your patch fields.
- **`image`** (string): for types that support sprites (currently items/equipment), RWMM can load a PNG and set the sprite field.
  - The path is resolved relative to the **parent folder of the `prototypes` folder** that contained the JSON file.
  - Recommended: keep `prototypes\` directly under your mod root so image paths are stable.
  - PNGs should be 256x256 for best results.

---

## How RWMM decides what to do

### 1) Update an existing object (patch)
If `refName` already exists in the target database, RWMM updates that object.

**Best practice:** only keep the fields you’re changing in `obj`. This reduces conflicts with other mods that patch the same thing.

### 2) Add a “new” object (clone)
RWMM does **not** currently support creating new objects from nothing.  It will let you clone an existing object, and modify fields as needed.

To add something “new”, you **clone** an existing object:
- Set `cloneFrom` to an existing `refName`.
- Set `refName` to a new unique value.
- Provide only the fields you want to differ.

### 3) Creating brand-new objects
Not implemented yet. (Planned.)

---

## Field optionality rules (important)

When **updating** or **cloning**:
- **All fields are optional**. If you don’t include it, RWMM leaves it as-is.
- This includes **nested objects**: you can specify only the nested fields you want to change.

**Collections (arrays/lists) are the exception:**
- If you touch an array/list field, you should currently treat it as **full replacement**.
- For arrays/lists of objects, you should assume **each element must be fully defined**.

(Translation: patching “just one element” of a list is not supported.)

---

## IDs: remove them

Do not set `id` in prototypes.

RWMM manages IDs based on `refName` and persists the mapping so saves remain stable. If you hardcode IDs you could potentially collide with other mods.  That said, RWMM will still assign your ids if you include them in your prototypes.

**Recommendation:** delete `id` from every template you copy from `base_prototypes`.

---

## `refName` conventions (recommended)

Use a mod prefix (slug), e.g.:
- `rwee_...`
- `mymod_...`

This prevents collisions and makes it obvious who “owns” a definition.

---

## Load order / conflicts
RWMM currently scans `prototypes` folders and JSON files without a formal dependency graph.
- `priority` exists in the wrapper but is **not implemented**.
- If two mods patch the same `refName`, you should assume the outcome is **not something to rely on**.

So:
- Prefer **minimal patches** (only changed fields).
- Avoid patching the same `refName` across multiple mods unless you control the stack.

---

## Types supported (current)
RWMM currently dumps/imports prototypes for:
- `Item`
- `Equipment`
- `Quest`
- `Perk`
- `TWeapon`
- `CrewMember`
- `ShipModelData`

---

## Workflow:
1. Run once to generate `base_prototypes`.
2. Copy the closest base object JSON into your mod’s `prototypes\...`.
3. Delete everything you’re not changing.
4. If you need something new, use `cloneFrom`.
5. Keep `refName` unique and prefixed.
6. Never ship IDs.

---

## RW.Core (namespace `RW`) API reference

RWMM includes a small utility library you can reference from your own BepInEx plugins that offers a bunch of utility to simplify sometimes complex tasks.

### `RW.Main`
- `Init(ManualLogSource log, int verbosity = 0)`  
  Initialize RW.Core logging.
- `log(string msg, int level = 1, ManualLogSource log = null)`  
  Log info (only if `verbosity >= level`).
- `warn(string msg, int level = 0)`  
  Log warning.
- `error(string msg, bool showPopup = false, int level = -1)`  
  Log error (optionally show a popup).
- `log_obj(object obj, int level = 1)`  
  Log an object as a JSON structure.
- `log_line_obj(object obj, int level = 1)`  
  Log a compact line for an object (type/id/ref).
- `log_line_list(List<object> obj, int level = 1)`  
  Log compact lines for a list of objects.

### `RW.DirUtils`
- `FindPluginDir()`  
  Walks upward from the executing assembly until it finds the `plugins` folder.
- `FindJsonFiles(string root_folder)`  
  Recursively returns `*.json` files under a folder.

### `RW.ListUtils`
- `GetByRef<T>(List<T> list, string value)`  
  Finds an object by its ref field (type-aware; `refName`, `nameRef`, `shipModelName`, etc.).
- `GetBy<T>(List<T> list, string field, int value)` / `GetBy<T>(..., string value)` / `GetBy<T, TField>(...)`  
  Generic reflection-based lookup by field/property name.
- `NextFreeId<T>(List<T> list)`  
  Returns the next available integer ID not used in the list.

### `RW.ObjUtils`
- `GetRef(object obj, bool silent = false)`  
  Returns the object’s ref string  (type-aware).
- `SetRef(object obj, string value)`  
  Sets the object’s ref field.
- `RefField(string typeName)`  
  Returns the ref field name for a type (type-aware).
- `SpriteField(string typeName)`  
  Returns the sprite field name for a type (currently items/equipment -> `sprite`).
- `GetIdReference(object obj, bool silent = false)` / `SetIdReference(object obj, int id)`  
  Get/set the numeric ID field (when present).
- `GetField<T>(object obj, string field, bool silent = false)`  
  Reflection field/property getter.
- `SetField<T>(object obj, string field, T value)`  
  Reflection field/property setter.
- `Clone<T>(T obj)`  
  Clones Unity objects (and falls back to a shallow memberwise clone where applicable).

### `RW.JsonUtils`
- `FromJson<T>(string json)`  
  Deserialize JSON.
- `Populate(object obj, string json)`  
  Populate an existing object from JSON.  JSON fields not present are left as-is.
- `PopulateClone<T>(T obj, string json)`  
  Clone an object and populate from JSON.
- `ToJson<T>(T obj)`  
  Serialize an object to JSON.
- `ToPrettyJson(object obj)`  
  Serialize to pretty, human readable JSON.
- `Pretty(string json)`  
  Pretty-format a JSON string.

### `RW.IconUtils`
- `MakeSprite(string png)`  
  Load a PNG into a cached `Sprite`.

### `RW.SimplePopup`
- `Show(string msg)` / `Hide()`  
  Basic popup UI helpers.

---

## Troubleshooting
- “Nothing is happening”:
  - Confirm your folder is named exactly `prototypes`.
  - Confirm each JSON has `"type": "Item"` (etc) and `"refName": "..."`.
- Icons not loading:
  - Confirm `image` path is relative to your mod root (parent of `prototypes`).
  - Confirm PNG exists and is readable.
- Conflicts with other mods:
  - Reduce your prototype to only the fields you truly need to change.
  - Avoid patching the same `refName` across multiple mods.

---

## Roadmap (high-level)
- Support creating brand-new objects without cloning.
- Deterministic load order + dependencies (Factorio-style manifest, eventual `package.json`).
- Better collection/array patching semantics.
