using System;
using System.IO;

public class log 
{
    string fileName;
    string fileName2;

    public log() {
        this.fileName = DateTime.Now.ToString("ss.mm.hh dd-MM-yyyy.txt");
        this.fileName2 = "PC " + this.fileName;
    }

    public void writeLog(string logLine) {
        using (StreamWriter w = File.AppendText(fileName)) {
            //w.WriteLine(logLine);
        }
    }

    public void writeLogPC(string logLine) {
        using (StreamWriter w = File.AppendText(fileName2)) {
            //w.WriteLine(logLine);
        }
    }
}