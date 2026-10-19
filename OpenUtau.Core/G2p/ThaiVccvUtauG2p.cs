using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Api;

namespace OpenUtau.Core.G2p {
    public class ThaiVccvUtauG2p : G2pPack {
        private static readonly string[] graphemes = new string[] {
            "", "", "", "", "ก", "ข", "ฃ", "ค", "ฅ", "ฆ", "ง", "จ",
            "ฉ", "ช", "ซ", "ฌ", "ญ", "ฎ", "ฏ", "ฐ", "ฑ", "ฒ", "ณ",
            "ด", "ต", "ถ", "ท", "ธ", "น", "บ", "ป", "ผ", "ฝ", "พ", "ฟ",
            "ภ", "ม", "ย", "ร", "ฤ", "ล", "ฦ", "ว", "ศ", "ษ", "ส", "ห",
            "ฬ", "อ", "ฮ", "ฯ", "ะ", "ั", "า", "ำ", "ิ", "ี", "ึ", "ื",
            "ุ", "ู", "ฺ", "฿", "เ", "แ", "โ", "ใ", "ไ", "ๅ", "ๆ", "็", "่",
            "้", "๊", "๋", "์", "ํ", "๎",
        };

        private static readonly string[] phonemes = new string[] {
            "-", "a", "i", "u", "e", "o", "@", "Q", "3",
            "6", "1", "ia", "ua", "ay", "aw", "b", "ch", "d", "f", "h",
            "j", "kh", "k", "l", "m", "n", "ng", "ph", "p", "r", "s", "th",
            "t", "w", "y",
        };

        private static object lockObj = new object();
        private static Dictionary<string, int> graphemeIndexes;
        private static IG2p dict;
        private static InferenceSession session;
        private static Dictionary<string, string[]> predCache = new Dictionary<string, string[]>();

        public ThaiVccvUtauG2p() {
            lock (lockObj) {
                if (graphemeIndexes == null) {
                    graphemeIndexes = graphemes
                        .Skip(4)
                        .Select((g, i) => Tuple.Create(g, i))
                        .ToDictionary(t => t.Item1, t => t.Item2 + 4);
                    // Using the same dictionary/model for now.
                    dict = G2pDictionary.NewBuilder().Build();
                    session = null;
                }
            }
            GraphemeIndexes = graphemeIndexes;
            Phonemes = phonemes;
            Dict = dict;
            Session = session;
            PredCache = predCache;
        }
    }
}
