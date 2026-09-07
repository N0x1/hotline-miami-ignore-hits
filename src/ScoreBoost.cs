using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

internal interface IScoreMemory
{
    byte[] Read(long address, int count);
    void Write(long address, byte[] data);
    void Suspend();
    void Resume();
}

internal sealed class ScoreMemory : IScoreMemory, IDisposable
{
    readonly IntPtr handle;
    [DllImport("ntdll.dll")] static extern int NtSuspendProcess(IntPtr process);
    [DllImport("ntdll.dll")] static extern int NtResumeProcess(IntPtr process);
    public ScoreMemory(int pid)
    {
        handle = Native.OpenProcess(0xC38, false, pid); // VM access + suspend/resume
        if (handle == IntPtr.Zero) throw Native.Error("Cannot access score memory.");
    }
    public byte[] Read(long address, int count)
    {
        byte[] data = new byte[count]; UIntPtr read;
        if (!Native.ReadProcessMemory(handle, new IntPtr(address), data, (UIntPtr)count, out read) || read.ToUInt64() != (ulong)count)
            throw Native.Error("Cannot read score state.");
        return data;
    }
    public void Write(long address, byte[] data)
    {
        UIntPtr written;
        if (!Native.WriteProcessMemory(handle, new IntPtr(address), data, (UIntPtr)data.Length, out written) || written.ToUInt64() != (ulong)data.Length)
            throw Native.Error("Cannot update score state.");
    }
    public void Suspend() { if (NtSuspendProcess(handle) < 0) throw new IOException("Cannot pause score updates safely."); }
    public void Resume() { if (NtResumeProcess(handle) < 0) throw new IOException("Could not resume the game after updating score. Close and restart the game."); }
    public void Dispose() { Native.CloseHandle(handle); }
}

internal static class ScoreBoost
{
    public const double Floor = 200000;
    public const int GlobalsRva = 0xBFFCD8, ValueVtableRva = 0x6B8BF4;
    public const int KillScore = 0x24A, MissionScore = 0x69, DisplayScore = 0x254;
    public const int MissionTime = 0x131, CurrentLevel = 0x271;
    sealed class Number
    {
        public long Address;
        public int Kind;
        public double Value;
        public byte[] Before;
    }
    static uint U32(IScoreMemory mem, long address) { return BitConverter.ToUInt32(mem.Read(address, 4), 0); }
    static Number ReadNumber(IScoreMemory mem, long image, uint table, int index)
    {
        uint pointer = U32(mem, table + index * 8L + 4);
        if (pointer < 0x10000) throw new InvalidOperationException("Score values are not ready yet.");
        byte[] header = mem.Read(pointer, 16);
        if (BitConverter.ToUInt32(header, 0) != image + ValueVtableRva)
            throw new InvalidOperationException("Unrecognized score value layout. Score boost was not applied.");
        int kind = BitConverter.ToInt32(header, 4);
        if (kind != 1 && kind != 2) throw new InvalidOperationException("Score value is not a supported number.");
        double value = kind == 1 ? BitConverter.ToInt32(header, 8) : BitConverter.ToDouble(header, 8);
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1000000000)
            throw new InvalidOperationException("Score value is outside the expected range.");
        return new Number { Address = pointer + 8L, Kind = kind, Value = value, Before = header.Skip(8).Take(kind == 1 ? 4 : 8).ToArray() };
    }
    // Returns false while the game has not initialized a timed mission.
    public static bool Apply(IScoreMemory mem, long image)
    {
        mem.Suspend();
        try
        {
            uint table = U32(mem, image + GlobalsRva);
            if (table < 0x10000) return false;
            Number time = ReadNumber(mem, image, table, MissionTime);
            Number level = ReadNumber(mem, image, table, CurrentLevel);
            if (time.Value <= 0 || level.Value > 20 || level.Value != Math.Floor(level.Value)) return false;
            // Validate all fields first. Kill score feeds the end-of-mission recomputation.
            Number[] fields = new[] { KillScore, MissionScore, DisplayScore }.Select(i => ReadNumber(mem, image, table, i)).ToArray();
            List<Number> changed = new List<Number>();
            try
            {
                foreach (Number field in fields)
                {
                    if (field.Value >= Floor || changed.Any(n => n.Address == field.Address)) continue;
                    changed.Add(field);
                    byte[] value = field.Kind == 1 ? BitConverter.GetBytes((int)Floor) : BitConverter.GetBytes(Floor);
                    mem.Write(field.Address, value);
                    if (!mem.Read(field.Address, value.Length).SequenceEqual(value)) throw new IOException("Score write verification failed.");
                }
            }
            catch (Exception error)
            {
                List<string> errors = new List<string>();
                changed.Reverse();
                foreach (Number field in changed)
                {
                    try { mem.Write(field.Address, field.Before); }
                    catch (Exception restoreError) { errors.Add(restoreError.Message); }
                }
                if (errors.Count != 0) throw new IOException("Score update failed and recovery was incomplete. Restart the mission. " + string.Join("; ", errors), error);
                throw;
            }
            return true;
        }
        finally { mem.Resume(); }
    }
}

internal static class ScoreTests
{
    sealed class Fixture : IScoreMemory
    {
        public readonly byte[] Data = new byte[0xC10000];
        public int Pauses, Resumes, Writes, FailWrite = -1;
        public bool Paused;
        public const long Image = 0x400000;
        const int Table = 0x10000;
        public Fixture()
        {
            Put(ScoreBoost.GlobalsRva, BitConverter.GetBytes((uint)(Image + Table)));
            // Independent addresses from the native timer increment, not the production constant.
            Field(0x131, 2, 60);
            Field(0x24F, 1, 0);
            Field(ScoreBoost.CurrentLevel, 1, 1);
            Field(ScoreBoost.KillScore, 2, 500);
            Field(ScoreBoost.MissionScore, 2, 1000);
            Field(ScoreBoost.DisplayScore, 1, 995);
        }
        void Put(int offset, byte[] bytes) { Array.Copy(bytes, 0, Data, offset, bytes.Length); }
        int Object(int index) { return 0x20000 + index * 32; }
        public void Field(int index, int kind, double value)
        {
            int obj = Object(index);
            Put(Table + index * 8 + 4, BitConverter.GetBytes((uint)(Image + obj)));
            Put(obj, BitConverter.GetBytes((uint)(Image + ScoreBoost.ValueVtableRva)));
            Put(obj + 4, BitConverter.GetBytes(kind));
            Put(obj + 8, kind == 1 ? BitConverter.GetBytes((int)value) : BitConverter.GetBytes(value));
        }
        public double Value(int index) { int o = Object(index); return BitConverter.ToInt32(Data, o+4) == 1 ? BitConverter.ToInt32(Data, o+8) : BitConverter.ToDouble(Data, o+8); }
        public byte[] Read(long address, int count) { if (!Paused) throw new Exception("Read without pause"); return Data.Skip(checked((int)(address-Image))).Take(count).ToArray(); }
        public void Write(long address, byte[] bytes)
        {
            if (!Paused) throw new Exception("Write without pause");
            if (Writes++ == FailWrite) throw new IOException("Injected score write failure");
            Put(checked((int)(address-Image)), bytes);
        }
        public void Suspend() { if (Paused) throw new Exception("Double pause"); Pauses++; Paused = true; }
        public void Resume() { Resumes++; Paused = false; }
    }
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("Score test: " + reason); }
    public static string Run()
    {
        Fixture f = new Fixture();
        Check(ScoreBoost.Apply(f, Fixture.Image), "mission detected");
        foreach (int i in new[] { ScoreBoost.KillScore, ScoreBoost.MissionScore, ScoreBoost.DisplayScore }) Check(f.Value(i)==200000, "numeric score floor");
        Check(!f.Paused && f.Pauses==f.Resumes, "resume on success");
        int count = f.Writes; ScoreBoost.Apply(f, Fixture.Image); Check(f.Writes==count, "no repeated writes above floor");
        f.Field(ScoreBoost.KillScore, 2, 350000); ScoreBoost.Apply(f, Fixture.Image); Check(f.Value(ScoreBoost.KillScore)==350000, "higher score preserved");
        f = new Fixture(); f.Field(0x131, 2, 0); Check(!ScoreBoost.Apply(f, Fixture.Image) && f.Writes==0 && !f.Paused, "wait before mission");
        f.Field(0x131, 1, 1); f.Field(ScoreBoost.CurrentLevel, 1, 0);
        Check(ScoreBoost.Apply(f, Fixture.Image) && f.Value(ScoreBoost.KillScore)==200000, "armed before mission activates on next poll, including level zero");
        Check(f.Value(0x24F)==0 && f.Value(0x131)==1, "unrelated counter and mission timer preserved");
        f.Field(0x131, 1, 0); f.Field(ScoreBoost.KillScore, 1, 0); f.Field(ScoreBoost.MissionScore, 1, 0); f.Field(ScoreBoost.DisplayScore, 1, 0);
        count = f.Writes; Check(!ScoreBoost.Apply(f, Fixture.Image) && f.Writes==count, "wait during mission restart");
        f.Field(0x131, 1, 60); Check(ScoreBoost.Apply(f, Fixture.Image) && f.Value(ScoreBoost.KillScore)==200000, "reapply after mission restart");
        f = new Fixture(); f.Field(ScoreBoost.MissionScore, 4, 1000); bool failed = false;
        try { ScoreBoost.Apply(f, Fixture.Image); } catch (InvalidOperationException) { failed = true; }
        Check(failed && f.Writes==0 && !f.Paused, "reject unsupported type before writes");
        f = new Fixture(); f.Field(ScoreBoost.MissionScore, 2, double.NaN); failed = false;
        try { ScoreBoost.Apply(f, Fixture.Image); } catch (InvalidOperationException) { failed = true; }
        Check(failed && f.Writes==0 && !f.Paused, "reject invalid number");
        f = new Fixture(); byte[] before = (byte[])f.Data.Clone(); f.FailWrite = 1; failed = false;
        try { ScoreBoost.Apply(f, Fixture.Image); } catch (IOException) { failed = true; }
        Check(failed && before.SequenceEqual(f.Data) && !f.Paused, "rollback and resume on write failure");
        return "PASS: F8 actual timer with unrelated zero counter, pre-mission activation, level zero, mission restart, score floor, int/double fields, higher scores, type/value guards, rollback and balanced pause/resume.\n";
    }
}
