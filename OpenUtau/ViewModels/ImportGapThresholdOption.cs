// Version: 0.1
// Made and Checked By DELTA SYNTH And Gemini Claude and ChatGPT
// Original By OpenUtau Contributors

namespace OpenUtau.App.ViewModels
{
    // [DELTA SYNTH] โมเดลเก็บค่าขอบเขตการนำเข้าข้อมูล
    public sealed class ImportGapThresholdOption
    {
        public int Denominator { get; init; }
        public string Label { get; init; } = "";

        public override string ToString() => Label;
    }
}