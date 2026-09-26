using Xunit;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using OpenUtau.App;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder {
    // ตั้งค่าแอปพลิเคชันให้รันในโหมด Headless (ไม่มีหน้าต่าง UI โผล่ขึ้นมาตอนรันเทสต์)
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

namespace OpenUtau.App {
    public class AppTest {
        
        [Fact]
        public void BuildTest() {
            // ตรวจสอบว่าคลาส App และ Program สามารถสร้าง Instance ได้จริง (ไม่เป็น Abstract)
            Assert.False(typeof(App).IsAbstract);
            Assert.False(typeof(Program).IsAbstract);
        }

        [Fact]
        public void StringsTest() {
            // เตรียมแอปพลิเคชันโดยไม่เปิดหน้าต่าง UI
            var appBuilder = TestAppBuilder.BuildAvaloniaApp()
                .SetupWithoutStarting();
            var app = appBuilder.Instance as App;
            
            // ตรวจสอบว่าแอปพลิเคชันถูกสร้างขึ้นมาอย่างถูกต้อง ไม่เป็นค่าว่าง
            Assert.NotNull(app);

            // ดึงข้อมูลภาษาทั้งหมดที่มีในระบบ
            var languages = App.GetLanguages();
            
            // ตรวจสอบว่ามีภาษาในระบบมากกว่า 1 ภาษา
            Assert.True(languages.Count > 1);
            
            // ตรวจสอบการโหลดภาษาหลักที่จำเป็นในระบบ (รวมถึงภาษาไทยที่เพิ่มเข้ามาใหม่)
            Assert.Contains("en-US", languages.Keys); // ภาษาอังกฤษ (สหรัฐอเมริกา)
            Assert.Contains("zh-CN", languages.Keys); // ภาษาจีน (ตัวย่อ)
            Assert.Contains("ja-JP", languages.Keys); // ภาษาญี่ปุ่น
            Assert.Contains("th-TH", languages.Keys); // ภาษาไทย (ตรวจสอบเพิ่มเติมเพื่อให้ครอบคลุมการใช้งาน)

            // ตรวจสอบว่าข้อมูลพจนานุกรมของแต่ละภาษาต้องไม่เป็นค่าว่างเปล่า (โหลดสำเร็จ)
            foreach (var pair in languages) {
                Assert.NotNull(pair.Value);
            }
        }
    }
}