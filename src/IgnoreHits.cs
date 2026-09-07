using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("1.0")]

internal sealed class PatchSite
{
    public readonly int Rva;
    public readonly string Name;
    public readonly byte[] Original;
    public PatchSite(int rva, string name, byte[] original) { Rva = rva; Name = name; Original = original; }
    public byte[] Expected(bool enabled) { byte[] b = (byte[])Original.Clone(); if (enabled) b[0] = 0xC3; return b; }
}

internal static class Native
{
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool ReadProcessMemory(IntPtr h, IntPtr address, byte[] data, UIntPtr size, out UIntPtr count);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool WriteProcessMemory(IntPtr h, IntPtr address, byte[] data, UIntPtr size, out UIntPtr count);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool VirtualProtectEx(IntPtr h, IntPtr address, UIntPtr size, uint protection, out uint previous);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool FlushInstructionCache(IntPtr h, IntPtr address, UIntPtr size);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr VirtualAlloc(IntPtr address, UIntPtr size, uint allocation, uint protection);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool VirtualFree(IntPtr address, UIntPtr size, uint freeType);
    [DllImport("user32.dll", SetLastError=true)] internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    internal static Exception Error(string operation) { return new Win32Exception(Marshal.GetLastWin32Error(), operation); }
}

internal sealed class MemoryPatches : IDisposable
{
    readonly IntPtr handle, image;
    readonly PatchSite[] sites;
    public bool Enabled { get; private set; }
    // Test seam: fail before a write to exercise rollback without affecting a game.
    internal int FailAtWrite = -1;
    public MemoryPatches(int pid, IntPtr image, PatchSite[] sites)
    {
        this.image = image; this.sites = sites;
        handle = Native.OpenProcess(0x438, false, pid); // query + VM operation/read/write only
        if (handle == IntPtr.Zero) throw Native.Error("Cannot access game memory. Run at the same privilege level as the game.");
        try
        {
            bool[] states = sites.Select(s => State(s)).ToArray();
            if (states.Any(s => s != states[0])) throw new InvalidOperationException("Partial or conflicting patch detected. Restart the game before attaching.");
            Enabled = states[0];
        }
        catch { Dispose(); throw; }
    }
    IntPtr Address(PatchSite site) { return new IntPtr(image.ToInt64() + site.Rva); }
    byte[] Read(PatchSite site)
    {
        byte[] bytes = new byte[site.Original.Length]; UIntPtr count;
        if (!Native.ReadProcessMemory(handle, Address(site), bytes, (UIntPtr)bytes.Length, out count) || count.ToUInt64() != (ulong)bytes.Length)
            throw Native.Error("Could not read " + site.Name);
        return bytes;
    }
    bool State(PatchSite site)
    {
        byte[] bytes = Read(site);
        if (bytes.SequenceEqual(site.Expected(false))) return false;
        if (bytes.SequenceEqual(site.Expected(true))) return true;
        throw new InvalidOperationException("Unexpected code at " + site.Name + ". No patch applied; restart without other trainers.");
    }
    void Write(PatchSite site, byte first)
    {
        uint previous, ignored; IntPtr address = Address(site); UIntPtr one = (UIntPtr)1;
        if (!Native.VirtualProtectEx(handle, address, one, 0x40, out previous)) throw Native.Error("Cannot make patch location writable.");
        Exception failure = null;
        try
        {
            UIntPtr count;
            if (!Native.WriteProcessMemory(handle, address, new byte[] { first }, one, out count) || count.ToUInt64() != 1)
                failure = Native.Error("Could not write " + site.Name);
            else if (!Native.FlushInstructionCache(handle, address, one)) failure = Native.Error("Could not flush instruction cache.");
        }
        finally
        {
            if (!Native.VirtualProtectEx(handle, address, one, previous, out ignored)) failure = Native.Error("Could not restore memory page protection. Restart the game.");
        }
        if (failure != null) throw failure;
    }
    public void Set(bool enabled)
    {
        // Validate every location before touching any location.
        foreach (PatchSite s in sites) if (State(s) != Enabled) throw new InvalidOperationException("Game code changed after attachment. Restart the game.");
        if (enabled == Enabled) return;
        List<PatchSite> touched = new List<PatchSite>();
        try
        {
            for (int i = 0; i < sites.Length; i++)
            {
                if (FailAtWrite == i) throw new IOException("Injected test write failure.");
                touched.Add(sites[i]); // include a write even if its cache/protection step fails
                Write(sites[i], enabled ? (byte)0xC3 : sites[i].Original[0]);
            }
            foreach (PatchSite s in sites) if (State(s) != enabled) throw new IOException("Patch verification failed.");
            Enabled = enabled;
        }
        catch (Exception error)
        {
            List<string> failures = new List<string>();
            touched.Reverse();
            foreach (PatchSite s in touched)
            {
                try { if (State(s) != Enabled) Write(s, Enabled ? (byte)0xC3 : s.Original[0]); }
                catch (Exception ex) { failures.Add(ex.Message); }
            }
            if (failures.Count > 0) throw new IOException(error.Message + " Recovery incomplete: restart the game. " + string.Join("; ", failures));
            throw new IOException(error.Message + " Earlier changes were rolled back.", error);
        }
    }
    public void Dispose() { if (handle != IntPtr.Zero) Native.CloseHandle(handle); }
}

internal sealed class Trainer : Form
{
    readonly Label status = new Label(), detail = new Label(), keys = new Label(), scoreKeys = new Label();
    readonly Button toggle = new Button(), scoreToggle = new Button();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    Process game;
    MemoryPatches patches;
    ScoreMemory scoreMemory;
    long imageBase;
    bool scoreEnabled, scoreApplied;
    string lastError = "";
    bool hotkey, scoreHotkey;
    public Trainer()
    {
        Text = "Hotline Miami — Ignore Hits + Score · v1.0"; ClientSize = new Size(520, 406);
        FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(24, 25, 34);
        ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI", 10);
        Label title = new Label { Text = "HOTLINE MIAMI", Location = new Point(26, 21), Size = new Size(460, 32), Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.FromArgb(255, 99, 177) };
        status.Location = new Point(28, 66); status.Size = new Size(464, 25);
        detail.Location = new Point(28, 99); detail.Size = new Size(464, 53); detail.ForeColor = Color.Silver;
        toggle.Location = new Point(28, 166); toggle.Size = new Size(464, 55); toggle.FlatStyle = FlatStyle.Flat;
        toggle.Font = new Font("Segoe UI", 13, FontStyle.Bold); toggle.AccessibleName = "Toggle ignore hits";
        toggle.Click += delegate { Toggle(); };
        keys.Location = new Point(28, 231); keys.Size = new Size(464, 24);
        scoreToggle.Location = new Point(28, 269); scoreToggle.Size = new Size(464, 50); scoreToggle.FlatStyle = FlatStyle.Flat;
        scoreToggle.Font = new Font("Segoe UI", 12, FontStyle.Bold); scoreToggle.AccessibleName = "Toggle mission score boost";
        scoreToggle.Click += delegate { ToggleScore(); };
        scoreKeys.Location = new Point(28, 329); scoreKeys.Size = new Size(464, 25);
        Label note = new Label { Text = "F7 gameplay confirmed · F8 ready for gameplay testing", Location = new Point(28, 372), Size = new Size(464, 23), ForeColor = Color.Silver, Font = new Font("Segoe UI", 9) };
        Controls.AddRange(new Control[] { title, status, detail, toggle, keys, scoreToggle, scoreKeys, note });
        timer.Interval = 900; timer.Tick += delegate { Poll(); }; timer.Start();
        FormClosing += ClosingTrainer;
        PaintState();
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e); hotkey = Native.RegisterHotKey(Handle, 7, 0x4000, 0x76);
        keys.Text = hotkey ? "F7  ·  Toggle protection on / off" : "F7 is used by another app. Use the button to toggle.";
        scoreHotkey = Native.RegisterHotKey(Handle, 8, 0x4000, 0x77);
        scoreKeys.Text = scoreHotkey ? "F8  ·  Keep mission score at 200,000 or higher" : "F8 is used by another app. Use the score button.";
    }
    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (hotkey) Native.UnregisterHotKey(Handle, 7);
        if (scoreHotkey) Native.UnregisterHotKey(Handle, 8);
        base.OnHandleDestroyed(e);
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x312 && m.WParam.ToInt32() == 7) Toggle();
        if (m.Msg == 0x312 && m.WParam.ToInt32() == 8) ToggleScore();
        base.WndProc(ref m);
    }
    void Detach()
    {
        scoreEnabled = false; scoreApplied = false;
        if (scoreMemory != null) { scoreMemory.Dispose(); scoreMemory = null; }
        if (patches != null) { patches.Dispose(); patches = null; }
        if (game != null) { game.Dispose(); game = null; }
    }
    void Poll()
    {
        try
        {
            if (game != null && game.HasExited) { Detach(); lastError = ""; }
            if (game == null)
            {
                Process[] found = Process.GetProcessesByName("HotlineGL");
                if (found.Length > 1) { foreach (Process p in found) p.Dispose(); throw new InvalidOperationException("More than one Updated game is running. Close the extra instance."); }
                if (found.Length == 1)
                {
                    Process candidate = found[0];
                    try
                    {
                        ProcessModule module = candidate.MainModule;
                        Program.CheckFile(module.FileName);
                        patches = new MemoryPatches(candidate.Id, module.BaseAddress, Definitions.Sites);
                        imageBase = module.BaseAddress.ToInt64();
                        game = candidate; candidate = null; lastError = "";
                        Program.Log("Attached to HotlineGL PID " + game.Id + "; protection " + (patches.Enabled ? "ON (recovered)" : "OFF"));
                    }
                    finally { if (candidate != null) candidate.Dispose(); }
                }
                else lastError = "";
            }
        }
        catch (Exception ex) { if (lastError != ex.Message) Program.Log(ex.Message); lastError = ex.Message; }
        if (game != null && scoreEnabled)
        {
            try { scoreApplied = ScoreBoost.Apply(scoreMemory, imageBase); }
            catch (Exception ex) { scoreEnabled = false; lastError = "F8 stopped: " + ex.Message; Program.Log(lastError); }
        }
        PaintState();
    }
    void ToggleScore()
    {
        // Disable first so pressing F8 off never applies one final boost.
        if (scoreEnabled) { scoreEnabled = false; Program.Log("F8 score boost OFF; previously added points retained."); PaintState(); return; }
        Poll(); if (patches == null) return;
        try
        {
            if (scoreMemory == null) scoreMemory = new ScoreMemory(game.Id);
            scoreApplied = ScoreBoost.Apply(scoreMemory, imageBase);
            scoreEnabled = true; lastError = ""; Program.Log("F8 score boost ON");
        }
        catch (Exception ex) { lastError = "F8: " + ex.Message; Program.Log(lastError); }
        PaintState();
    }
    void Toggle()
    {
        Poll(); if (patches == null) return;
        try { patches.Set(!patches.Enabled); lastError = ""; Program.Log("Protection " + (patches.Enabled ? "ON" : "OFF")); }
        catch (Exception ex) { lastError = ex.Message; Program.Log(lastError); MessageBox.Show(this, lastError, "Protection could not be changed", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        PaintState();
    }
    void PaintState()
    {
        bool attached = patches != null, on = attached && patches.Enabled;
        status.Text = attached ? "Updated game connected · protection " + (on ? "ON" : "OFF") : "Waiting for Hotline Miami — Updated";
        detail.Text = lastError.Length > 0 ? lastError : attached ? "F7 switches ignore-hits protection. Turn it off for scripted story events if needed." : "Start Hotline Miami in Steam and choose Updated. Then press F7 to enable protection.";
        toggle.Enabled = attached;
        toggle.Text = on ? "PROTECTION ON  ·  F7 to turn off" : "PROTECTION OFF  ·  F7 to turn on";
        toggle.BackColor = on ? Color.FromArgb(27, 100, 86) : Color.FromArgb(48, 49, 66);
        scoreToggle.Enabled = attached;
        scoreToggle.Text = scoreEnabled ? (scoreApplied ? "SCORE BOOST ON  ·  F8 to turn off" : "SCORE BOOST ON  ·  Waiting for mission") : "SCORE BOOST OFF  ·  F8 to turn on";
        scoreToggle.BackColor = scoreEnabled ? Color.FromArgb(107, 69, 130) : Color.FromArgb(48, 49, 66);
    }
    void ClosingTrainer(object sender, FormClosingEventArgs e)
    {
        try { if (patches != null && !game.HasExited) patches.Set(false); }
        catch (Exception ex)
        {
            Program.Log("Close restore failed: " + ex.Message);
            e.Cancel = true;
            MessageBox.Show(this, "Could not restore all patches. Close the game, then close this utility.\n\n" + ex.Message, "Restore incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        timer.Stop(); Detach(); Program.Log("Closed; protection restored or game already exited.");
    }
}

internal static class Program
{
    internal static void CheckFile(string path)
    {
        if (!string.Equals(Path.GetFileName(path), "HotlineGL.exe", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only the Updated HotlineGL.exe is supported.");
        string hash;
        using (FileStream f = File.OpenRead(path)) using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "");
        if (hash != Definitions.Hash) throw new InvalidOperationException("Unsupported game build. This utility will not patch a different executable.");
    }
    internal static void Log(string message)
    {
        try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IgnoreHits.log"), DateTime.Now.ToString("s") + " " + message + Environment.NewLine); } catch { }
    }
    [STAThread] static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--self-test")
        {
            try { File.WriteAllText(args[1], Tests.Run() + ScoreTests.Run()); return 0; }
            catch (Exception ex) { File.WriteAllText(args[1], "FAIL\n" + ex); return 1; }
        }
        bool created;
        using (Mutex mutex = new Mutex(true, "Local\\HotlineMiamiIgnoreHits-v1", out created))
        {
            if (!created) { MessageBox.Show("Ignore Hits is already running."); return 0; }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Trainer());
        }
        return 0;
    }
}

internal static class Tests
{
    static void Assert(bool condition, string name) { if (!condition) throw new Exception(name); }
    internal static string Run()
    {
        StringBuilder report = new StringBuilder();
        int size = Definitions.Sites.Max(s => s.Rva) + 0x1000;
        IntPtr memory = Native.VirtualAlloc(IntPtr.Zero, (UIntPtr)size, 0x3000, 0x40);
        if (memory == IntPtr.Zero) throw Native.Error("Test allocation failed");
        try
        {
            foreach (PatchSite s in Definitions.Sites) Marshal.Copy(s.Original, 0, new IntPtr(memory.ToInt64()+s.Rva), s.Original.Length);
            uint previous, ignored;
            Assert(Native.VirtualProtectEx(Process.GetCurrentProcess().Handle, memory, (UIntPtr)size, 0x20, out previous), "Make fixture executable/read-only");
            using (MemoryPatches p = new MemoryPatches(Process.GetCurrentProcess().Id, memory, Definitions.Sites))
            {
                Assert(!p.Enabled, "Initial state");
                for (int i=0; i<10; i++) { p.Set(true); Assert(p.Enabled, "Enable"); p.Set(false); Assert(!p.Enabled, "Disable"); }
                report.AppendLine("PASS: 10 enable/disable cycles over all 13 real-memory fixture sites.");
                foreach (PatchSite site in Definitions.Sites)
                {
                    IntPtr address = new IntPtr(memory.ToInt64()+site.Rva);
                    Assert(Native.VirtualProtectEx(Process.GetCurrentProcess().Handle, address, (UIntPtr)1, 0x40, out previous), "Query fixture protection");
                    Assert(previous == 0x20, "Original execute/read page protection restored");
                    Assert(Native.VirtualProtectEx(Process.GetCurrentProcess().Handle, address, (UIntPtr)1, previous, out ignored), "Restore queried protection");
                }
                report.AppendLine("PASS: original execute/read protection restored at all patch sites.");
                p.FailAtWrite = 6; bool failed = false;
                try { p.Set(true); } catch (IOException) { failed = true; }
                Assert(failed && !p.Enabled, "Failed enable rolls back"); p.FailAtWrite = -1;
                p.Set(true); p.FailAtWrite = 6; failed = false;
                try { p.Set(false); } catch (IOException) { failed = true; }
                Assert(failed && p.Enabled, "Failed disable rolls back"); p.FailAtWrite = -1; p.Set(false);
                report.AppendLine("PASS: mid-transaction failures roll back in both directions.");
                Assert(Native.VirtualProtectEx(Process.GetCurrentProcess().Handle, memory, (UIntPtr)size, 0x40, out previous), "Allow deliberate fixture corruption");
                PatchSite first = Definitions.Sites[0]; IntPtr at = new IntPtr(memory.ToInt64()+first.Rva);
                Marshal.WriteByte(at, 0x90); failed = false;
                try { p.Set(true); } catch (InvalidOperationException) { failed = true; }
                Assert(failed && !p.Enabled, "Unexpected bytes refused"); Marshal.WriteByte(at, first.Original[0]);
                report.AppendLine("PASS: unexpected code is rejected before writes.");
                p.Set(true);
                using (MemoryPatches recovered = new MemoryPatches(Process.GetCurrentProcess().Id, memory, Definitions.Sites))
                { Assert(recovered.Enabled, "Recover prior session"); recovered.Set(false); }
                // Original object is now deliberately stale; verify it refuses a foreign state change.
                failed = false; try { p.Set(false); } catch (InvalidOperationException) { failed = true; }
                Assert(failed, "Stale state must be rejected");
                report.AppendLine("PASS: fully patched session recovery; stale ownership rejected.");
            }
            foreach (PatchSite s in Definitions.Sites)
            {
                byte[] result = new byte[s.Original.Length]; Marshal.Copy(new IntPtr(memory.ToInt64()+s.Rva), result, 0, result.Length);
                Assert(result.SequenceEqual(s.Original), "Final byte restoration");
            }
            report.AppendLine("PASS: all fixture bytes restored exactly. No game process touched.");
            return report.ToString();
        }
        finally { Native.VirtualFree(memory, UIntPtr.Zero, 0x8000); }
    }
}
