using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace OpenUtau.Core.DiffSinger {
    internal static class LunaiDsUnvoicedDefaults {
        const string ResourceName = "OpenUtau.Core.DiffSinger.Data.lunai-dsunvoiced.yaml";

        internal static bool TryLoadPhonemes(HashSet<string> target) {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream == null) {
                return false;
            }
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var config = Core.Yaml.DefaultDeserializer.Deserialize<DsUnvoicedConfig>(reader.ReadToEnd());
            if (config?.phonemes == null) {
                return false;
            }
            foreach (var phoneme in config.phonemes) {
                if (string.IsNullOrWhiteSpace(phoneme)) {
                    continue;
                }
                target.Add(phoneme.Trim());
            }
            return target.Count > 0;
        }
    }
}
