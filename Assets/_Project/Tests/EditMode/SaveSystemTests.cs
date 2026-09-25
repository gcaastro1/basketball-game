using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Basket.Characters;
using Basket.Meta;

// Etapa 8: save robustness (checksum, backup, versions, atomic file writes) and the
// profile round trip. Uses an in-memory serializer; JsonSaveTests covers JsonUtility.
public class SaveSystemTests
{
    // Stores each object under a key: exercises the save service without a JSON library.
    private sealed class KeySerializer : ISaveSerializer
    {
        private readonly Dictionary<string, object> store = new Dictionary<string, object>();
        private int next;

        private string Put(object o)
        {
            string key = "k" + next++;
            store[key] = o;
            return key;
        }

        public string Serialize(PlayerSave save) => Put(save);
        public PlayerSave Deserialize(string text) => store.TryGetValue(text, out object o) ? (PlayerSave)o : null;
        public string SerializeEnvelope(SaveEnvelope envelope) => Put(envelope);
        public SaveEnvelope DeserializeEnvelope(string text) => store.TryGetValue(text, out object o) ? (SaveEnvelope)o : null;
    }

    private static PlayerSave Sample(long gems)
    {
        var s = new PlayerSave();
        s.items.Add(new ItemStack { itemId = "gems", amount = gems });
        s.characters.Add(new CharacterInstance("ace", level: 12, limitBreak: 0, dupes: 2) { xp = 30 });
        return s;
    }

    [Test]
    public void SaveThenLoad_RoundTrips()
    {
        var service = new SaveService(new MemorySaveStorage(), new KeySerializer(), () => 5);
        service.Save("main", Sample(300));
        SaveLoadResult r = service.Load("main");
        Assert.AreEqual(SaveLoadStatus.Loaded, r.Status);
        Assert.AreEqual(300, r.Save.items[0].amount);
        Assert.AreEqual(5, r.Save.savedUtcTicks);
    }

    [Test]
    public void CorruptMain_FallsBackToTheBackup()
    {
        var storage = new MemorySaveStorage();
        var serializer = new KeySerializer();
        var service = new SaveService(storage, serializer);
        service.Save("main", Sample(100));
        service.Save("main", Sample(200));
        // Tamper with the newest save's payload: its checksum no longer matches.
        SaveEnvelope newest = serializer.DeserializeEnvelope(storage.Main);
        newest.checksum = "0000000000000000";

        SaveLoadResult r = service.Load("main");
        Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, r.Status);
        Assert.AreEqual(100, r.Save.items[0].amount, "the previous save");
    }

    [Test]
    public void NothingReadable_StartsANewPlayer()
    {
        var storage = new MemorySaveStorage { Main = "garbage", Backup = "more garbage" };
        SaveLoadResult r = new SaveService(storage, new KeySerializer()).Load("main");
        Assert.AreEqual(SaveLoadStatus.NewPlayer, r.Status);
        Assert.IsNotNull(r.Save.items);
    }

    [Test]
    public void SaveFromANewerBuild_IsReportedNotSilentlyReplaced()
    {
        var storage = new MemorySaveStorage();
        var serializer = new KeySerializer();
        PlayerSave future = Sample(1);
        string payload = serializer.Serialize(future);
        future.version = PlayerSave.CurrentVersion + 1;
        storage.Main = serializer.SerializeEnvelope(new SaveEnvelope { version = future.version, payload = payload, checksum = SaveService.Checksum(payload) });

        Assert.AreEqual(SaveLoadStatus.NewerVersion, new SaveService(storage, serializer).Load("main").Status);
    }

    [Test]
    public void Migration_FromUnversioned_FillsMissingData()
    {
        var old = new PlayerSave { version = 0, items = null, gacha = null, storyFlags = null };
        Assert.IsTrue(SaveMigrator.Migrate(old));
        Assert.AreEqual(PlayerSave.CurrentVersion, old.version);
        Assert.IsNotNull(old.items);
        Assert.IsNotNull(old.gacha.history);
        Assert.IsNotNull(old.storyFlags);
    }

    [Test]
    public void Checksum_ChangesWithTheContent()
    {
        Assert.AreEqual(SaveService.Checksum("abc"), SaveService.Checksum("abc"));
        Assert.AreNotEqual(SaveService.Checksum("abc"), SaveService.Checksum("abd"));
    }

    [Test]
    public void FileStorage_KeepsThePreviousSaveAsBackup_AndLeavesNoTempFile()
    {
        string dir = Path.Combine(Path.GetTempPath(), "basket_save_test_" + System.Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new FileSaveStorage(dir);
            storage.Write("slot", "first");
            storage.Write("slot", "second");
            Assert.IsTrue(storage.TryRead("slot", out string main));
            Assert.IsTrue(storage.TryReadBackup("slot", out string backup));
            Assert.AreEqual("second", main);
            Assert.AreEqual("first", backup);
            Assert.IsFalse(File.Exists(Path.Combine(dir, "slot.save.tmp")));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Test]
    public void Profile_RoundTripsInventoryCharactersGachaAndSettings()
    {
        var profile = new PlayerProfile();
        profile.Inventory.Add("gems", 480);
        profile.Inventory.AddCharacter(new CharacterInstance("ace", level: 9, limitBreak: 0, dupes: 1) { xp = 12 });
        profile.Gacha.PityFor("standard").pullsSinceTop = 33;
        profile.SetSetting("volume", "0.8");

        PlayerProfile back = PlayerProfile.FromSave(profile.ToSave());
        Assert.AreEqual(480, back.Inventory.Amount("gems"));
        CharacterInstance ace = back.Inventory.GetCharacter("ace");
        Assert.AreEqual(9, ace.level);
        Assert.AreEqual(12, ace.xp);
        Assert.AreEqual(1, ace.dupes);
        Assert.AreEqual(33, back.Gacha.PityFor("standard").pullsSinceTop);
        Assert.AreEqual("0.8", back.GetSetting("volume"));
    }
}
