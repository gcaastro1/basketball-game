using System;
using System.IO;
using System.Text;

namespace Basket.Meta
{
    // What is written to disk: the save as text plus what it takes to trust it.
    [Serializable]
    public class SaveEnvelope
    {
        public int format = 1;
        public int version;
        public string checksum;
        public string payload;
    }

    public interface ISaveSerializer
    {
        string Serialize(PlayerSave save);
        PlayerSave Deserialize(string text);
        string SerializeEnvelope(SaveEnvelope envelope);
        SaveEnvelope DeserializeEnvelope(string text);
    }

    public interface ISaveStorage
    {
        bool TryRead(string slot, out string text);
        bool TryReadBackup(string slot, out string text);
        // Keeps the previous save as the backup, then replaces it.
        void Write(string slot, string text);
    }

    public enum SaveLoadStatus { Loaded, LoadedFromBackup, NewPlayer, NewerVersion }

    public readonly struct SaveLoadResult
    {
        public readonly PlayerSave Save;
        public readonly SaveLoadStatus Status;

        public SaveLoadResult(PlayerSave save, SaveLoadStatus status)
        {
            Save = save;
            Status = status;
        }
    }

    public sealed class SaveService
    {
        private readonly ISaveStorage storage;
        private readonly ISaveSerializer serializer;
        private readonly Func<long> clock;

        public SaveService(ISaveStorage storage, ISaveSerializer serializer, Func<long> utcTicks = null)
        {
            this.storage = storage;
            this.serializer = serializer;
            clock = utcTicks ?? (() => DateTime.UtcNow.Ticks);
        }

        public void Save(string slot, PlayerSave save)
        {
            save.version = PlayerSave.CurrentVersion;
            save.savedUtcTicks = clock();
            string payload = serializer.Serialize(save);
            var envelope = new SaveEnvelope { version = save.version, checksum = Checksum(payload), payload = payload };
            storage.Write(slot, serializer.SerializeEnvelope(envelope));
        }

        // The main save, else the backup (corrupt or missing main), else a fresh profile. A
        // save from a newer build is not overwritten by accident: it comes back as NewerVersion.
        public SaveLoadResult Load(string slot)
        {
            SaveLoadStatus? newer = null;
            if (storage.TryRead(slot, out string text))
            {
                PlayerSave s = TryOpen(text, ref newer);
                if (s != null) return new SaveLoadResult(s, SaveLoadStatus.Loaded);
            }
            if (storage.TryReadBackup(slot, out string backup))
            {
                PlayerSave s = TryOpen(backup, ref newer);
                if (s != null) return new SaveLoadResult(s, SaveLoadStatus.LoadedFromBackup);
            }
            return new SaveLoadResult(new PlayerSave(), newer ?? SaveLoadStatus.NewPlayer);
        }

        private PlayerSave TryOpen(string text, ref SaveLoadStatus? newer)
        {
            try
            {
                SaveEnvelope envelope = serializer.DeserializeEnvelope(text);
                if (envelope == null || envelope.payload == null || envelope.checksum != Checksum(envelope.payload)) return null;
                PlayerSave save = serializer.Deserialize(envelope.payload);
                if (save == null) return null;
                if (!SaveMigrator.Migrate(save))
                {
                    newer = SaveLoadStatus.NewerVersion;
                    return null;
                }
                return save;
            }
            catch (Exception)
            {
                return null; // unreadable: fall back
            }
        }

        // FNV-1a 64-bit over the UTF-8 payload: detects truncated or edited files.
        public static string Checksum(string payload)
        {
            const ulong offset = 14695981039346656037UL, prime = 1099511628211UL;
            ulong hash = offset;
            foreach (byte b in Encoding.UTF8.GetBytes(payload ?? ""))
            {
                hash ^= b;
                hash *= prime;
            }
            return hash.ToString("x16");
        }
    }

    // One file per slot; writes go to a temp file first and the previous save becomes the
    // backup, so a crash mid-write never leaves the player without a readable save.
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string directory;

        public FileSaveStorage(string directory)
        {
            this.directory = directory;
        }

        private string PathOf(string slot) => Path.Combine(directory, slot + ".save");
        private string BackupOf(string slot) => PathOf(slot) + ".bak";

        public bool TryRead(string slot, out string text) => TryReadFile(PathOf(slot), out text);
        public bool TryReadBackup(string slot, out string text) => TryReadFile(BackupOf(slot), out text);

        private static bool TryReadFile(string path, out string text)
        {
            text = null;
            if (!File.Exists(path)) return false;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public void Write(string slot, string text)
        {
            Directory.CreateDirectory(directory);
            string path = PathOf(slot), temp = path + ".tmp", backup = BackupOf(slot);
            File.WriteAllText(temp, text, Encoding.UTF8);
            if (File.Exists(path))
            {
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(path, backup);
            }
            File.Move(temp, path);
        }
    }

    public sealed class MemorySaveStorage : ISaveStorage
    {
        public string Main, Backup;

        public bool TryRead(string slot, out string text) => (text = Main) != null;
        public bool TryReadBackup(string slot, out string text) => (text = Backup) != null;

        public void Write(string slot, string text)
        {
            if (Main != null) Backup = Main;
            Main = text;
        }
    }
}
