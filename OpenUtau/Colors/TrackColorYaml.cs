using System.IO;
using System.Text;
using OpenUtau.Core;

namespace OpenUtau.Colors;

public class TrackColorYaml {
    public string Name = "Custom";
    public string BaseColor = "#7266EE";
    public string BrightColor = "#B9B4F9";

    public static TrackColorYaml LoadFromFile(string path) {
        return Yaml.DefaultDeserializer.Deserialize<TrackColorYaml>(File.ReadAllText(path, Encoding.UTF8));
    }

    public void SaveToFile(string path) {
        File.WriteAllText(path, Yaml.DefaultSerializer.Serialize(this), Encoding.UTF8);
    }
}
