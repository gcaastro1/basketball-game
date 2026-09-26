#!/usr/bin/env python3
"""Extracts a StadiumLayout asset from a stadium demo scene (MarpaStudio "Basket Ball Stadium").

The demo scene keeps every piece of the stadium as a prefab instance under one root ("Arena").
This writes their transforms, relative to the center of the court drawn on the floor (the
PlayField piece, at floor level), into a StadiumLayout ScriptableObject that the game builds
at runtime (Basket.Presentation.ArenaDresser). Pieces that the game provides itself are left
out (the hoops: gameplay needs its own rim; the demo ball).

Usage (from the repo root):
  python3 tools/stadium/extract_layout.py \
      Assets/MarpaStudio/DemoScene/DemoScene.unity Arena PlayField \
      Assets/_Project/Data/Arena/MarpaStadiumLayout.asset
"""
import glob
import os
import re
import sys

# Scene names (m_Name) or source assets left out of the layout.
EXCLUDE_SOURCES = {"Ring.fbx", "BasketBall.prefab", "Net.prefab", "FoamPoleFinal.prefab"}
FBX_ROOT_FILE_ID = "919132149155446097"  # a model's root GameObject, constant in Unity
SCRIPT_GUID_PLACEHOLDER = "{script_guid}"


def load_guids(root):
    guids = {}
    for meta in glob.glob(os.path.join(root, "Assets", "**", "*.meta"), recursive=True):
        with open(meta, errors="ignore") as f:
            m = re.search(r"guid: (\w+)", f.read(1000))
        if m:
            guids[m.group(1)] = meta[len(root) + 1:-5]
    return guids


def split_docs(text):
    parts = re.split(r"\n--- !u!(\d+) &(-?\d+)([^\n]*)\n", text)
    return {parts[i + 1]: (int(parts[i]), parts[i + 2], parts[i + 3]) for i in range(1, len(parts), 4)}


def vec(body, key, names, default):
    m = re.search(key + r": \{([^}]*)\}", body)
    if not m:
        return default
    vals = dict(re.findall(r"(\w+): ([-\d.eE]+)", m.group(1)))
    return tuple(float(vals.get(n, d)) for n, d in zip(names, default))


def prefab_root(path):
    """Root GameObject fileID and root transform of a .prefab file.

    A plain prefab has the root GameObject in the file. A prefab variant of a model (every
    MarpaStudio prefab) only holds a PrefabInstance of the model; Unity derives the fileID of
    the objects it brings in as (instance fileID ^ source fileID) & 0x7fffffffffffffff.
    """
    with open(path, errors="ignore") as f:
        docs = split_docs(f.read())
    for fid, (cls, _, body) in docs.items():
        if cls == 4 and "m_Father: {fileID: 0}" in body and "stripped" not in _:
            go = re.search(r"m_GameObject: \{fileID: (-?\d+)", body).group(1)
            return go, (vec(body, "m_LocalPosition", "xyz", (0, 0, 0)),
                        vec(body, "m_LocalRotation", "xyzw", (0, 0, 0, 1)),
                        vec(body, "m_LocalScale", "xyz", (1, 1, 1)))
    for fid, (cls, _, body) in docs.items():
        if cls == 1001 and "m_TransformParent: {fileID: 0}" in body:
            mods = dict(re.findall(r"propertyPath: ([\w.]+)\n\s+value: ([^\n]*)", body))
            go = (int(fid) ^ int(FBX_ROOT_FILE_ID)) & 0x7FFFFFFFFFFFFFFF
            return str(go), (tuple(float(mods.get("m_LocalPosition." + a, 0)) for a in "xyz"),
                             tuple(float(mods.get("m_LocalRotation." + a, 1 if a == "w" else 0)) for a in "xyzw"),
                             tuple(float(mods.get("m_LocalScale." + a, 1)) for a in "xyz"))
    raise ValueError("no root in " + path)


def qmul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def qrot(q, v):
    x, y, z, w = q
    vq = (v[0], v[1], v[2], 0.0)
    r = qmul(qmul(q, vq), (-x, -y, -z, w))
    return r[:3]


def main(scene_path, root_name, center_name, out_path):
    repo = os.getcwd()
    guids = load_guids(repo)
    with open(scene_path) as f:
        docs = split_docs(f.read())

    names = {fid: re.search(r"m_Name: ([^\n]*)", b).group(1) for fid, (c, _, b) in docs.items() if c == 1}
    root_tf = None
    for fid, (cls, _, body) in docs.items():
        if cls == 4 and "stripped" not in _:
            go = re.search(r"m_GameObject: \{fileID: (-?\d+)", body)
            if go and names.get(go.group(1)) == root_name:
                root_tf = (fid, body)
    if root_tf is None:
        sys.exit("root %s not found" % root_name)
    root_pos = vec(root_tf[1], "m_LocalPosition", "xyz", (0, 0, 0))
    root_rot = vec(root_tf[1], "m_LocalRotation", "xyzw", (0, 0, 0, 1))

    pieces, center = [], None
    for fid, (cls, _, body) in docs.items():
        if cls != 1001 or ("m_TransformParent: {fileID: %s}" % root_tf[0]) not in body:
            continue
        src = re.search(r"m_SourcePrefab: \{fileID: -?\d+, guid: (\w+)", body).group(1)
        src_path = guids.get(src)
        if src_path is None:
            print("skipped: no asset for guid", src)
            continue
        mods = dict(re.findall(r"propertyPath: ([\w.]+)\n\s+value: ([^\n]*)", body))
        if mods.get("m_IsActive") == "0":
            continue
        if src_path.endswith(".prefab"):
            go_id, (dp, dr, ds) = prefab_root(os.path.join(repo, src_path))
        else:
            go_id, (dp, dr, ds) = FBX_ROOT_FILE_ID, ((0, 0, 0), (0, 0, 0, 1), (1, 1, 1))
        pos = tuple(float(mods.get("m_LocalPosition." + a, d)) for a, d in zip("xyz", dp))
        rot = tuple(float(mods.get("m_LocalRotation." + a, d)) for a, d in zip("xyzw", dr))
        scale = tuple(float(mods.get("m_LocalScale." + a, d)) for a, d in zip("xyz", ds))
        # Into the scene's world space (the root has unit scale in the demo).
        wpos = tuple(p + o for p, o in zip(qrot(root_rot, pos), root_pos))
        wrot = qmul(root_rot, rot)
        name = mods.get("m_Name", os.path.basename(src_path))
        if name == center_name or os.path.basename(src_path).startswith(center_name + "."):
            center = wpos
        if os.path.basename(src_path) in EXCLUDE_SOURCES:
            continue
        # Material overrides on the instance (the model's renderer), by slot.
        mats = sorted((int(i), g) for i, g in re.findall(
            r"propertyPath: m_Materials\.Array\.data\[(\d+)\]\n\s+value: [^\n]*\n\s+objectReference: \{fileID: 2100000, guid: (\w+)",
            body))
        pieces.append((name, go_id, src, wpos, wrot, scale, [g for _, g in mats]))

    if center is None:
        sys.exit("center piece %s not found" % center_name)
    pieces.sort(key=lambda p: (p[0], p[3]))

    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
             "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
             "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
             "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % SCRIPT_GUID_PLACEHOLDER,
             "  m_Name: " + os.path.splitext(os.path.basename(out_path))[0], "  m_EditorClassIdentifier: ",
             "  courtSize: {x: 15.24, y: 28.65}", "  pieces:"]
    def f(v):
        text = ("%.5f" % v).rstrip("0").rstrip(".")
        return "0" if text in ("", "-0") else text

    for name, go_id, guid, p, r, s, mats in pieces:
        local = tuple(a - c for a, c in zip(p, center))
        lines += ["  - prefab: {fileID: %s, guid: %s, type: 3}" % (go_id, guid),
                  "    position: {x: %s, y: %s, z: %s}" % tuple(f(v) for v in local),
                  "    rotation: {x: %s, y: %s, z: %s, w: %s}" % tuple(f(v) for v in r),
                  "    scale: {x: %s, y: %s, z: %s}" % tuple(f(v) for v in s)]
        if mats:
            lines.append("    materials:")
            lines += ["    - {fileID: 2100000, guid: %s, type: 2}" % g for g in mats]
        else:
            lines.append("    materials: []")
    text = "\n".join(lines) + "\n"
    script_guid = os.environ.get("STADIUM_LAYOUT_SCRIPT_GUID")
    if script_guid:
        text = text.replace(SCRIPT_GUID_PLACEHOLDER, script_guid)
    with open(out_path, "w") as out:
        out.write(text)
    print("%d pieces, court center at %s" % (len(pieces), tuple(round(c, 3) for c in center)))


if __name__ == "__main__":
    if len(sys.argv) != 5:
        sys.exit(__doc__)
    main(*sys.argv[1:])
