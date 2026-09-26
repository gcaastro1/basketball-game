using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Basket.EditorTools
{
    // The basketball mocap clips (Animations/Basquete) declare FBX TimeMode 7, "30 fps
    // drop-frame" (a video timecode). Unity does not support it -- "ImportFBX Errors:
    // Framerate was set to 0.00, it's been reset to 1.0" -- and a 1 fps clip breaks sampling.
    // Their keys are every 1/30 s, so the mode is rewritten to 6 (plain 30 fps): one byte in
    // GlobalSettings, animation data untouched.
    //
    // Runs once per editor load over the character animation folders and reimports what it
    // fixed; also on the menu Basket -> Fix FBX Frame Rates. The fixed files are real changes
    // to commit (Git LFS).
    public static class FbxFrameRateFixer
    {
        public const int Frames30Drop = 7;
        public const int Frames30 = 6;

        // Binary FBX property: "TimeMode" (S), "enum" (S), "" (S), "" (S), value (I).
        private static readonly byte[] TimeModeProperty = Build();

        [MenuItem("Basket/Fix FBX Frame Rates")]
        public static void FixMenu()
        {
            int n = FixFolder(TripoHumanoidImporter.TripoFolder);
            Debug.Log($"FBX frame rates: {n} file(s) changed from 30 fps drop-frame to 30 fps.");
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad() => EditorApplication.delayCall += () =>
        {
            int n = FixFolder(TripoHumanoidImporter.TripoFolder);
            if (n > 0) Debug.Log($"FBX frame rates: fixed {n} animation file(s) (30 fps drop-frame -> 30 fps). Commit them.");
        };

        public static int FixFolder(string folder)
        {
            if (!Directory.Exists(folder)) return 0;
            var fixedPaths = new List<string>();
            foreach (string file in Directory.GetFiles(folder, "*.fbx", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                if (!path.Contains(CharacterAnimationImporter.Marker)) continue;
                byte[] data = File.ReadAllBytes(path);
                if (!TryFix(data)) continue;
                File.WriteAllBytes(path, data);
                fixedPaths.Add(path);
            }
            foreach (string path in fixedPaths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return fixedPaths.Count;
        }

        // Rewrites a drop-frame TimeMode in place; false if the file has none.
        public static bool TryFix(byte[] data)
        {
            int at = IndexOf(data, TimeModeProperty);
            if (at < 0) return false;
            int value = at + TimeModeProperty.Length;
            if (value + 4 > data.Length || BitConverter.ToInt32(data, value) != Frames30Drop) return false;
            byte[] replacement = BitConverter.GetBytes(Frames30);
            Array.Copy(replacement, 0, data, value, 4);
            return true;
        }

        private static byte[] Build()
        {
            var bytes = new List<byte>();
            void Str(string s)
            {
                bytes.Add((byte)'S');
                bytes.AddRange(BitConverter.GetBytes(s.Length));
                foreach (char c in s) bytes.Add((byte)c);
            }
            Str("TimeMode");
            Str("enum");
            Str("");
            Str("");
            bytes.Add((byte)'I');
            return bytes.ToArray();
        }

        private static int IndexOf(byte[] data, byte[] pattern)
        {
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                int j = 0;
                while (j < pattern.Length && data[i + j] == pattern[j]) j++;
                if (j == pattern.Length) return i;
            }
            return -1;
        }
    }
}
