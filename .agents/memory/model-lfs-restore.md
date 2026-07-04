---
name: Model.cs LFS restore
description: Model.cs is Git LFS tracked and silently goes empty after checkpoints/restores; how to recover it from the local LFS cache.
---

## Rule
`ConwaysWorld.Simulation/Model.cs` is tracked by Git LFS. After certain checkpoint restores or task-agent merges, the file on disk becomes 0 bytes (an unresolved pointer). The build then fails with cascade errors across `Model.EditMode.cs` and `Model.WorldEvents.cs` (partial class members can't find `_columns`, `CellGrid`, `_settings`, etc.).

**Why:** The Replit environment cannot `git lfs pull` interactively (SSH GitHub host key prompt blocks it), so LFS files aren't auto-fetched after restores.

**How to apply:** Check `wc -c ConwaysWorld.Simulation/Model.cs`. If it's 0, read the LFS pointer SHA256 from `git show HEAD:ConwaysWorld.Simulation/Model.cs`, then restore:

```bash
cp .git/lfs/objects/<aa>/<bb>/<full-sha256> ConwaysWorld.Simulation/Model.cs
```

The object lives at `.git/lfs/objects/<first2>/<next2>/<full-sha256>`. The local cache always has it because previous sessions fetched it.

After restoring, re-apply any in-flight edits (the restored file is the checkpoint state, not the latest working copy).

## Additional hazard: Python read-modify-write
If Model.cs is temporarily swapped to an LFS pointer and a Python `open(..., 'r').read()` runs on it, Python returns an empty string (the raw pointer bytes confuse UTF-8 decode). A subsequent `open(..., 'w').write(content)` then writes 0 bytes. Use the `edit` tool for targeted changes to Model.cs — never a Python read-modify-write script.

## Finding the right LFS version
If the HEAD pointer SHA is the pre-change version, check all local LFS objects by size to find the one with your changes already applied:

```bash
find .git/lfs/objects -type f | xargs -I{} sh -c 'echo "$(wc -c < {}) {}"' | sort -rn | head -10
```

Inspect candidates with Python: `text.find('BirthNeighborCount')` (or whatever was recently added). Copy the right one.
