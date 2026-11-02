using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Classic {
    public static class ClassicSingerLoader {
        static USinger AdjustSingerType(Voicebank v) {
            switch (v.SingerType) {
                case USingerType.Enunu:
                    return new Core.Enunu.EnunuSinger(v) as USinger;
                case USingerType.DiffSinger:
                    return new Core.DiffSinger.DiffSingerSinger(v) as USinger;
                case USingerType.Voicevox:
                    return new Core.Voicevox.VoicevoxSinger(v) as USinger;
                default:
                    return new ClassicSinger(v) as USinger;
            }
        }
        public static IEnumerable<USinger> FindAllSingers() {
            return PathManager.Inst.SingersPaths
                .AsParallel()
                .AsOrdered()
                .SelectMany(path => {
                    var loader = new VoicebankLoader(path);
                    return loader.SearchAll()
                        .AsParallel()
                        .AsOrdered()
                        .Select(AdjustSingerType)
                        .ToList();
                })
                .ToList();
        }
    }
}
