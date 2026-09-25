# typecheck

`check.sh` compiles all project C# (per-assembly, same references as the `.asmdef`s)
without Unity: Roslyn on Mono + Unity 2021.3 reference DLLs from NuGet + `stubs/`.

- Catches: syntax errors, type errors, missing/illegal cross-assembly references.
- Does **not** run tests or validate Unity-6-only APIs beyond the renames it maps.
- If a new Unity/Input System/Editor API is used, add it to `stubs/`.

Runs in CI (`.github/workflows/ci.yml`, job `typecheck`) on every push.
