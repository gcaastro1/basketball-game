using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Basket.EditorTools
{
    public static class ListAnimationClips
    {
        [MenuItem("Basket/Debug/List Animation Clips")]
        public static void Run()
        {
            string[] roots =
            {
                "Assets/TripoModels/anime_character_3d_model/Animations",
            };

            var sb = new System.Text.StringBuilder();
            foreach (var root in roots)
            {
                var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { root });
                foreach (var guid in fbxGuids.Distinct())
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                        .Where(c => !c.name.StartsWith("__preview__"))
                        .ToArray();
                    if (clips.Length == 0) continue;
                    sb.AppendLine(path);
                    foreach (var c in clips)
                        sb.AppendLine($"  - {c.name} (len={c.length:F2}s, frames={c.length * c.frameRate:F0})");
                }
            }

            string outPath = Path.Combine(Application.dataPath, "..", "Logs", "clip-list.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"Wrote {outPath}");
        }
    }
}
