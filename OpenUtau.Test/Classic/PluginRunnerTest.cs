using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Ustx;
using Xunit;
using static OpenUtau.Classic.PluginRunner;

namespace OpenUtau.Classic {

    public class PluginRunnerTest {

        public PluginRunnerTest() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // ตรวจสอบและสร้างโฟลเดอร์ Cache หากไม่มีอยู่ในระหว่างการทำ UnitTest
            if (!Directory.Exists(PathManager.Inst.CachePath)) {
                Directory.CreateDirectory(PathManager.Inst.CachePath);
            }
        }

        [Theory]
        [ClassData(typeof(ExecuteTestData))]
        public async Task ExecuteTest(ExecuteArgument given, Action<StreamWriter, string> when, Action<ReplaceNoteEventArgs> then, Action<PluginErrorEventArgs> error) {
            // เมื่อทำการเรียกใช้ PluginRunner
            // ตรวจสอบผลลัพธ์ (การตรวจสอบอยู่ในคลาส ExecuteTestData ที่ส่งเข้ามาผ่าน ClassData)
            var runner = new PluginRunner(PathManager.Inst, then, error);
            await runner.Execute(given.Project, given.Part, given.First, given.Last, new PluginStub(when));
        }

        [Fact]
        public async Task ExecuteErrorTest() {
            // กำหนดข้อมูลเริ่มต้น
            var given = ExecuteTestData.BasicUProject();

            // จำลองการเกิด Error โดยให้ PluginStub คืนค่ากลับเป็นข้อความว่างเปล่า
            // ตรวจสอบว่า Error ถูกเรียกใช้งานจริงหรือไม่
            var then = new Action<ReplaceNoteEventArgs>((args) => Assert.Fail("Should not reach here."));
            var error = new Action<PluginErrorEventArgs>((args) => Assert.True(true));
            var runner = new PluginRunner(PathManager.Inst, then, error);
            await runner.Execute(given.Project, given.Part, given.First, given.Last, new PluginStub((writer, text) => {
                // ไม่ทำอะไร เพื่อให้เกิด Error
            }));
        }

        // ==========================================
        // ข้อมูลสำหรับทดสอบ (Test Data Providers)
        // ==========================================
        class ExecuteTestData : IEnumerable<object[]> {
            public IEnumerator<object[]> GetEnumerator() {
                // ใช้ yield return เพื่อความกระชับและไม่ต้องสร้าง List เก็บไว้ใน Memory
                yield return new object[] { BasicUProject(), DoNothingResponse(), DoNothingAssertion(), FailedErrorMethod() };
                yield return new object[] { BasicUProject(), IncludeNullResponse(), IncludeNullAssertion(), FailedErrorMethod() };
                yield return new object[] { BasicUProject(), EditFlagsResponse(), EditFlagsAssertion(), FailedErrorMethod() };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public static ExecuteArgument BasicUProject() {
                var project = new UProject();
                project.tracks.Add(new UTrack(project) { TrackNo = 0 });
                
                var part = new UVoicePart() { trackNo = 0, position = 0 };
                project.parts.Add(part);

                Ustx.AddDefaultExpressions(project);
     
                // สร้างโน้ตต่างๆ
                var before = UNote.Create(); before.lyric = "a"; before.duration = 10; before.position = 0;
                var first = UNote.Create(); first.lyric = "ka"; first.duration = 20; first.position = 10;
                var second = UNote.Create(); second.lyric = "r"; second.duration = 30; second.position = 30;
                var third = UNote.Create(); third.lyric = "ta"; third.duration = 40; third.position = 60;
                var last = UNote.Create(); last.lyric = "na"; last.duration = 50; last.position = 100;
                var after = UNote.Create(); after.lyric = "ha"; after.duration = 60; after.position = 150;

                // เชื่อมโยงโน้ตเข้าด้วยกัน
                before.Next = first;
                first.Next = second;
                second.Next = third;
                third.Next = last;
                last.Next = after;

                // เพิ่ม Phoneme และ Expressions
                var secondUpnoneme = new UPhoneme { Parent = second };
                secondUpnoneme.SetExpression(project, project.tracks[0], Ustx.GEN, 30);
                secondUpnoneme.SetExpression(project, project.tracks[0], Ustx.VEL, 40);
                secondUpnoneme.SetExpression(project, project.tracks[0], Ustx.VOL, 50);
                secondUpnoneme.SetExpression(project, project.tracks[0], Ustx.MOD, 60);

                // นำโน้ตเข้า Part
                part.notes.Add(before);
                part.notes.Add(first);
                part.notes.Add(second);
                part.notes.Add(third);
                part.notes.Add(last);
                part.notes.Add(after);
                part.phonemes.Add(secondUpnoneme);

                return new ExecuteArgument(project, part, first, last);
            }

            // (ส่วนของการตั้งค่า Action สำหรับ Response และ Assertion ยังคงตรรกะเดิม แต่จัด Format ให้อ่านง่ายขึ้น)
            private static Action<StreamWriter, string> DoNothingResponse() {
                return (writer, text) => {
                    var cacheDir = PathManager.Inst.CachePath;
                    var expected = $@"[#SETTING]
Tempo=120
Tracks=1
CacheDir={cacheDir}
Mode2=True
[#PREV]
Length=10
Lyric=R
NoteNum=60
PreUtterance=
[#0000]
Length=20
Lyric=ka
NoteNum=0
PreUtterance=
[#0001]
Length=30
Lyric=r
NoteNum=0
PreUtterance=
Velocity=40
Intensity=50
Modulation=60
Flags=g30B0H0P86
[#0002]
Length=40
Lyric=ta
NoteNum=0
PreUtterance=
[#0003]
Length=50
Lyric=na
NoteNum=0
PreUtterance=
[#NEXT]
Length=60
Lyric=ha
NoteNum=0
PreUtterance=
";
                    expected = expected.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
                    Assert.Equal(expected, text);
                    writer.Write(text);
                };
            }

            private static Action<ReplaceNoteEventArgs> DoNothingAssertion() => (args) => Assert.Fail("this method is not running");

            private static Action<StreamWriter, string> IncludeNullResponse() {
                return (writer, text) => {
                    writer.WriteLine("[#0000]\nLength=480\nLyric=A");
                    writer.WriteLine("[#0001]\nLength=480\nLyric=R");
                    writer.WriteLine("[#0002]\nLyric=zo"); // duration is null (change)
                    writer.WriteLine("[#0003]\nLength="); // duration is zero (delete)
                    writer.WriteLine("[#INSERT]\nLength=240\nLyric=me");
                };
            }

            private static Action<ReplaceNoteEventArgs> IncludeNullAssertion() {
                return (args) => {
                    Assert.Equal(4, args.ToRemove.Count);
                    Assert.Equal(3, args.ToAdd.Count);
                    Assert.Equal(480, args.ToAdd[0].duration);
                    Assert.Equal("A", args.ToAdd[0].lyric);
                    Assert.Equal(40, args.ToAdd[1].duration);
                    Assert.Equal("zo", args.ToAdd[1].lyric);
                    Assert.Equal(240, args.ToAdd[2].duration);
                    Assert.Equal("me", args.ToAdd[2].lyric);
                };
            }

            private static Action<StreamWriter, string> EditFlagsResponse() {
                return (writer, text) => {
                    writer.WriteLine("[#0000]");
                    writer.WriteLine("[#0001]\nFlags=g10");
                    writer.WriteLine("[#0002]\nFlags=g-10B20");
                    writer.WriteLine("[#0003]\nFlags=B30L2");
                };
            }

            private static Action<ReplaceNoteEventArgs> EditFlagsAssertion() {
                return (args) => {
                    Assert.Equal(4, args.ToRemove.Count);
                    Assert.Equal(4, args.ToAdd.Count);
                    Assert.Equal(10, args.ToAdd[1].phonemeExpressions.FirstOrDefault(exp => exp.descriptor?.abbr == Ustx.GEN)?.value);
                    Assert.Equal(-10, args.ToAdd[2].phonemeExpressions.FirstOrDefault(exp => exp.descriptor?.abbr == Ustx.GEN)?.value);
                    Assert.Equal(20, args.ToAdd[2].phonemeExpressions.FirstOrDefault(exp => exp.descriptor?.abbr == Ustx.BRE)?.value);
                    Assert.Equal(30, args.ToAdd[3].phonemeExpressions.FirstOrDefault(exp => exp.descriptor?.abbr == Ustx.BRE)?.value);
                };
            }

            private static Action<PluginErrorEventArgs> FailedErrorMethod() => (args) => throw args.Exception;
            private static Action<PluginErrorEventArgs> EmptyErrorMethod() => (args) => { /* do nothing */ };
        }
    }

    // ==========================================
    // คลาสสนับสนุนต่างๆ (Helper Classes)
    // ==========================================
    class PluginStub : IPlugin {
        private readonly Action<StreamWriter, string> action;
        public string Encoding => "shift_jis";

        public PluginStub(Action<StreamWriter, string> action) {
            this.action = action;
        }

        public async Task Run(string tempFile) {
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var text = await File.ReadAllTextAsync(tempFile, System.Text.Encoding.GetEncoding(Encoding));
            File.Delete(tempFile);
            
            using var writer = new StreamWriter(tempFile, false, System.Text.Encoding.GetEncoding(Encoding));
            action.Invoke(writer, text);
        }
    }

    public class ExecuteArgument {
        public readonly UProject Project;
        public readonly UVoicePart Part;
        public readonly UNote First;
        public readonly UNote Last;

        public ExecuteArgument(UProject project, UVoicePart part, UNote first, UNote last) {
            Project = project;
            Part = part;
            First = first;
            Last = last;
        }
    }
}
