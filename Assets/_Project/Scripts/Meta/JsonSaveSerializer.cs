using UnityEngine;

namespace Basket.Meta
{
    // Unity's JsonUtility: human-readable saves, no extra packages.
    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        private readonly bool pretty;

        public JsonSaveSerializer(bool pretty = false)
        {
            this.pretty = pretty;
        }

        public string Serialize(PlayerSave save) => JsonUtility.ToJson(save, pretty);
        public PlayerSave Deserialize(string text) => JsonUtility.FromJson<PlayerSave>(text);
        public string SerializeEnvelope(SaveEnvelope envelope) => JsonUtility.ToJson(envelope, pretty);
        public SaveEnvelope DeserializeEnvelope(string text) => JsonUtility.FromJson<SaveEnvelope>(text);
    }
}
