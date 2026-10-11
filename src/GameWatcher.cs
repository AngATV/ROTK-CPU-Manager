using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32.SafeHandles;

namespace RotkCpu
{
    internal sealed class GameSession : IDisposable
    {
        internal int Pid;
        internal SafeProcessHandle Handle;
        internal uint[] Original;
        internal bool Changed;
        internal bool Alive
        {
            get
            {
                uint result = Native.WaitForSingleObject(Handle, 0);
                if (result == Native.WaitFailed)
                    throw new Win32Exception();
                return result == Native.WaitTimeout;
            }
        }
        internal void Apply(Profile profile)
        {
            uint[] actual = Native.ReadSets(Handle);
            if (!actual.OrderBy(x => x).SequenceEqual(profile.Ids.OrderBy(x => x)))
            {
                // Mark before the write so even a readback failure leaves a restorable session.
                Changed = true;
                Native.WriteSets(Handle, profile.Ids);
                if (!Native.ReadSets(Handle).OrderBy(x => x).SequenceEqual(profile.Ids.OrderBy(x => x)))
                    throw new InvalidOperationException(Locale.T("Windows n'a pas confirmé le profil demandé."));
            }
        }
        internal void Restore()
        {
            if (!Changed || !Alive)
                return;
            Native.WriteSets(Handle, Original);
            if (!Native.ReadSets(Handle).OrderBy(x => x).SequenceEqual(Original.OrderBy(x => x)))
                throw new InvalidOperationException(Locale.T("Restauration non confirmée par Windows."));
            Changed = false;
        }
        public void Dispose()
        {
            Handle.Dispose();
        }
    }

    internal sealed class GameWatcher
    {
        private readonly Dictionary<int, GameSession> sessions = new Dictionary<int, GameSession>();
        internal Action<string> Log;
        internal string Status = Locale.T("Surveillance désactivée");

        internal void Tick(string folder, Profile profile, bool enabled)
        {
            foreach (var session in sessions.Values.Where(x => !x.Alive).ToList())
            {
                sessions.Remove(session.Pid);
                session.Dispose();
                Write(Locale.T("ROTK fermé (PID ") + session.Pid + ").");
            }
            folder = GameGuard.Validate(folder);
            int found = 0;
            bool error = false;
            foreach (var process in Process.GetProcessesByName("H1Z1"))
            {
                using (process)
                {
                    int pid = process.Id;
                    try
                    {
                        using (var query = Native.OpenProcess(Native.QueryLimitedInformation, false, pid))
                        {
                            if (query.IsInvalid)
                                throw new Win32Exception();
                            if (!GameGuard.Matches(folder, Native.ImagePath(query)))
                                continue;
                        }
                        found++;
                        if (!enabled)
                            continue;
                        GameSession session;
                        if (!sessions.TryGetValue(pid, out session))
                        {
                            var handle = Native.OpenProcess(Native.QueryLimitedInformation | Native.SetLimitedInformation | Native.Synchronize, false, pid);
                            try
                            {
                                if (handle.IsInvalid)
                                    throw new Win32Exception();
                                // Keep path validation and mutation on the same handle to guard against PID reuse.
                                if (!GameGuard.Matches(folder, Native.ImagePath(handle)))
                                    continue;
                                session = new GameSession { Pid = pid, Handle = handle, Original = Native.ReadSets(handle) };
                                sessions.Add(pid, session);
                                handle = null;
                                Write(Locale.T("ROTK détecté (PID ") + pid + Locale.T("). Réglage initial mémorisé."));
                            }
                            finally { if (handle != null) handle.Dispose(); }
                        }
                        session.Apply(profile);
                    }
                    catch (Win32Exception ex)
                    {
                        error = true;
                        Write("PID " + pid + " : " + Locale.Error(ex) + (ex.NativeErrorCode == 5
                            ? Locale.T(" Fermez l'outil puis relancez-le comme administrateur si ROTK est élevé. Un processus protégé peut aussi refuser l'accès.") : ""));
                    }
                    catch (InvalidOperationException ex) { error = true; Write("PID " + pid + " : " + Locale.Error(ex)); }
                }
            }
            Status = !enabled && sessions.Values.Any(x => x.Changed) ? Locale.T("Surveillance arrêtée · restauration à réessayer")
                : error ? Locale.T("Accès au jeu ou application du profil non confirmé — voir le journal")
                : found == 0 ? (enabled ? Locale.T("En attente de ROTK · application automatique activée") : Locale.T("En attente de ROTK · surveillance désactivée"))
                : enabled ? Locale.T("ROTK détecté · profil actif : ") + profile.Name : Locale.T("ROTK détecté · aucun réglage appliqué par l'outil");
        }

        internal void RestoreAll()
        {
            var failures = new List<string>();
            foreach (var session in sessions.Values.ToList())
            {
                try
                {
                    session.Restore();
                    Write(Locale.T("Réglage initial restauré (PID ") + session.Pid + ").");
                    sessions.Remove(session.Pid);
                    session.Dispose();
                }
                catch (Exception ex) { failures.Add("PID " + session.Pid + " : " + Locale.Error(ex)); }
            }
            if (failures.Count > 0)
                throw new InvalidOperationException(String.Join(Environment.NewLine, failures));
        }
        private void Write(string message)
        {
            if (Log != null)
                Log(message);
        }
    }
}
