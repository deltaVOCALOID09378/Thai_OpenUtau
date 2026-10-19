// Made And Checked By DELTA SYNTH & Gemini AI
// Original logic inspired by: Auto Pitch Writer by Canon
// Re-engineered for DELTA SYNTH OpenUtau Plugin compatibility

#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <sstream>

using namespace std;

// Function to process the UST temporary file
void ProcessUST(const string& filepath) {
    ifstream infile(filepath);
    if (!infile.is_open()) {
        cerr << "Error: Cannot open temporary file / เกิดข้อผิดพลาดในการเปิดไฟล์ชั่วคราว: " << filepath << endl;
        return;
    }

    vector<string> lines;
    string line;
    bool inNote = false;
    string currentLyric = "";

    while (getline(infile, line)) {
        if (line.rfind("[#", 0) == 0 && line.find("[#SETTING]") != 0 && line.find("[#VERSION]") != 0) {
            inNote = true;
            lines.push_back(line);
            continue;
        }

        if (inNote && line.empty()) {
            inNote = false;
        }

        if (inNote) {
            if (line.rfind("Lyric=", 0) == 0) {
                currentLyric = line.substr(6);
            }

            // Skip existing pitch parameters
            if (line.rfind("PBW=", 0) == 0 || line.rfind("PBY=", 0) == 0 || 
                line.rfind("PBS=", 0) == 0 || line.rfind("PBM=", 0) == 0 || 
                line.rfind("VBR=", 0) == 0) {
                continue;
            }

            lines.push_back(line);

            if (line.rfind("Length=", 0) == 0) {
                // Ignore Rest Notes
                if (currentLyric == "R" || currentLyric == "r") {
                    continue;
                }

                // Apply Canon-like Pitch Writing Logic
                // PBS, PBW, PBY etc
                lines.push_back("PBS=-30");
                lines.push_back("PBW=50,50");
                lines.push_back("PBY=0,-15,0");
                lines.push_back("PBM=");
                lines.push_back("VBR=65,30,35,20,20,0,0");
            }
        } else {
            lines.push_back(line);
        }
    }
    infile.close();

    ofstream outfile(filepath);
    if (!outfile.is_open()) {
        cerr << "Error: Cannot write to file / ไม่สามารถเขียนลงไฟล์ได้" << endl;
        return;
    }

    for (const auto& l : lines) {
        outfile << l << "\n";
    }
    outfile.close();

    cout << "Auto Pitch processed successfully (C++ Core) / ประมวลผลเส้นเสียงอัตโนมัติเรียบร้อย!" << endl;
}

int main(int argc, char* argv[]) {
    if (argc < 2) {
        cout << "Usage: AutoPitchWriterCore.exe <ust_temp_file.tmp>" << endl;
        return 1;
    }

    string tmpFile = argv[1];
    ProcessUST(tmpFile);
    return 0;
}
