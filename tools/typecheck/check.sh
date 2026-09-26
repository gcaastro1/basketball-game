#!/usr/bin/env bash
# Compile-only check of the project's C# without a Unity install.
#
# Compiles every assembly (same boundaries and references as the .asmdef files) with
# Roslyn under Mono against Unity 2021.3 reference DLLs from NuGet plus small stubs for
# the Input System, Test Framework and UnityEditor APIs this project uses. Unity 6 API
# renames (linearVelocity, PhysicsMaterial, ...) are mapped back in a temp copy.
#
# It catches syntax, type and cross-assembly reference errors. It does NOT replace the
# real Unity test run (EditMode/PlayMode) in CI -- stubs have no behavior.
#
# Requires: mono (mono-devel), curl, unzip.   Usage: tools/typecheck/check.sh
set -euo pipefail

HERE=$(cd "$(dirname "$0")" && pwd)
REPO=$(cd "$HERE/../.." && pwd)
DEPS=${TYPECHECK_DEPS:-$HERE/.deps}
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

fetch() { # id version
    local dir="$DEPS/$1.$2"
    [ -d "$dir" ] && return
    mkdir -p "$dir"
    curl -fsSL -o "$dir.nupkg" "https://api.nuget.org/v3-flatcontainer/$1/$2/$1.$2.nupkg"
    unzip -qo "$dir.nupkg" -d "$dir"
    chmod -R u+rwX "$dir"   # some packages ship entries with mode 000
    rm "$dir.nupkg"
}
fetch unityengine.modules 2021.3.33
fetch microsoft.net.compilers 4.2.0
fetch nunit 3.13.3

MONO_LIB=$(dirname "$(dirname "$(command -v mono)")")/lib/mono/4.5
mkdir -p "$WORK/src" "$WORK/out"
cp -r "$REPO/Assets/_Project/Scripts" "$REPO/Assets/_Project/Editor" "$REPO/Assets/_Project/Tests" "$WORK/src/"
find "$WORK/src" -name '*.cs' -exec sed -i -e 's/linearVelocity/velocity/g; s/linearDamping/drag/g; s/angularDamping/angularDrag/g; s/PhysicsMaterialCombine/PhysicMaterialCombine/g; s/PhysicsMaterial\b/PhysicMaterial/g' {} +

CSC=(mono "$DEPS/microsoft.net.compilers.4.2.0/tools/csc.exe" -nologo -langversion:9 -nowarn:CS0649,CS0169,CS0414
     -t:library -nostdlib -r:"$MONO_LIB/mscorlib.dll" -r:"$MONO_LIB/System.dll" -r:"$MONO_LIB/System.Core.dll" -r:"$MONO_LIB/Facades/netstandard.dll")
mapfile -t UNITY_REFS < <(find "$DEPS/unityengine.modules.2021.3.33/lib/net45" -name 'UnityEngine*.dll' | sed 's/^/-r:/')
NUNIT=-r:"$DEPS/nunit.3.13.3/lib/net45/nunit.framework.dll"

failed=0
build() { # name, then csc args
    local name=$1; shift
    if "${CSC[@]}" "${UNITY_REFS[@]}" -out:"$WORK/out/$name.dll" "$@" | grep -v 'warning CS'; then :; fi
    if [ -f "$WORK/out/$name.dll" ]; then echo "ok      $name"; else echo "FAILED  $name"; failed=1; fi
}
refs() { for a in "$@"; do printf -- '-r:%s ' "$WORK/out/$a.dll"; done; }
src() { find "$WORK/src/$1" -name '*.cs'; }

build Stubs $(find "$HERE/stubs" -name '*.cs')
build Basket.Core $(src Scripts/Core)
build Basket.Characters $(refs Basket.Core) $(src Scripts/Characters)
build Basket.Gameplay $(refs Basket.Core Basket.Characters) $(src Scripts/Gameplay)
build Basket.AI $(refs Basket.Core) $(src Scripts/AI)
build Basket.Presentation $(refs Basket.Core Basket.Characters Basket.Gameplay) $(src Scripts/Presentation)
build Basket.Meta $(refs Basket.Core Basket.Characters) $(src Scripts/Meta)
build Basket.Input $(refs Basket.Core Stubs) $(src Scripts/Input)
build Basket.UI $(refs Basket.Core) $(src Scripts/UI)
build Basket.Bootstrap $(refs Basket.Core Basket.Characters Basket.Gameplay Basket.AI Basket.Input Basket.UI Basket.Presentation Basket.Meta) $(src Scripts/Bootstrap)
build Basket.EditorTools $(refs Stubs Basket.Core Basket.Characters Basket.Gameplay Basket.AI Basket.Input Basket.UI Basket.Presentation Basket.Bootstrap Basket.Meta) $(src Editor)
build Basket.Tests.EditMode "$NUNIT" $(refs Stubs Basket.Core Basket.Characters Basket.Gameplay Basket.AI Basket.Input Basket.Presentation Basket.Meta Basket.EditorTools) $(src Tests/EditMode)
build Basket.Tests.PlayMode "$NUNIT" $(refs Stubs Basket.Core Basket.Characters Basket.Gameplay Basket.AI Basket.Input Basket.Presentation Basket.Bootstrap Basket.Meta) $(src Tests/PlayMode)

# Unity binds a ScriptableObject/MonoBehaviour asset to its script file through the class
# named like the file; any other name breaks the asset at load time (only visible in Unity).
while IFS= read -r f; do
    if ! grep -qE "class $(basename "$f" .cs)\b" "$f"; then
        echo "FAILED  $f: ScriptableObject/MonoBehaviour file has no class named like the file"
        failed=1
    fi
done < <(grep -rlE ':\s*(ScriptableObject|MonoBehaviour)\b' --include='*.cs' "$HERE/../../Assets/_Project/Scripts")

exit $failed
