// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : BatchEdit.cs
// Version      : 1.0
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/Editing/BatchEdit.cs
// Description  : Interface for batch lyric/note editing operations. 
//                Optimized with Expression-Bodied Members for zero-overhead.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Editing {
    /// <summary>
    /// อินเทอร์เฟซหลักสำหรับการประมวลผลแก้ไขตัวโน้ตและเนื้อร้องแบบกลุ่ม (Batch Edit Interface)
    /// </summary>
    public interface BatchEdit {
        string Name { get; }
        
        bool IsAsync => false;

        void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager);

        // [DELTA SYNTH] Optimized using Expression-Bodied Method for a cleaner standard fallback execution
        void RunAsync(
            UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager,
            Action<int, int> setProgressCallback, CancellationToken cancellationToken) 
            => Run(project, part, selectedNotes, docManager);
    }
}