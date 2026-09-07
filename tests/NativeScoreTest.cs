using System;
using System.Diagnostics;
using System.IO;

internal static class NativeScoreTest
{
    static int Main(string[] args)
    {
        Process child = null;
        try
        {
            child = Process.Start(new ProcessStartInfo(args[0]) { UseShellExecute=false, CreateNoWindow=true, RedirectStandardInput=true, RedirectStandardOutput=true });
            var ready = child.StandardOutput.ReadLineAsync();
            if (!ready.Wait(5000)) throw new Exception("Fixture startup timed out.");
            long image = long.Parse(ready.Result);
            using (ScoreMemory memory = new ScoreMemory(child.Id))
            {
                if (!ScoreBoost.Apply(memory,image)) throw new Exception("Fixture mission not detected.");
            }
            child.StandardInput.WriteLine("check"); child.StandardInput.Flush();
            if (!child.WaitForExit(5000) || child.ExitCode!=0 || child.StandardOutput.ReadToEnd().Trim()!="PASS") throw new Exception("Native score fixture failed.");
            File.WriteAllText(args[1], "PASS: native x64 trainer backend paused an owned x86 fixture process, resolved score pointers, wrote double/int values, resumed it and verified 200,000 scores. No game process touched.\n");
            return 0;
        }
        catch(Exception error) { File.WriteAllText(args[1],"FAIL: "+error); return 1; }
        finally { if(child!=null) { if(!child.HasExited) child.Kill(); child.Dispose(); } }
    }
}
