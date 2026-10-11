using System;
using System.Collections.Generic;
using System.Linq;

namespace RotkCpu
{
    internal sealed class Profile
    {
        internal string Key, Name, Explanation;
        internal uint[] Ids;
        internal bool Recommended;
        public override string ToString()
        {
            return Name + (Recommended ? Locale.T("  ·  conseillé") : "");
        }
    }

    internal static class Profiles
    {
        internal static List<Profile> Build(CpuTopology cpu)
        {
            var result = new List<Profile>();
            result.Add(new Profile
            {
                Key = "windows",
                Name = Locale.T("Gestion Windows"),
                Ids = new uint[0],
                Recommended = true,
                Explanation = Locale.T("Laisse Windows répartir les threads. Point de départ conseillé lorsque la topologie ne justifie pas de restriction.")
            });
            var available = cpu.Sets.Where(x => (x.Flags & 2) == 0).ToList();
            if (cpu.Sets.Select(x => x.Group).Distinct().Count() != 1)
            {
                result[0].Explanation = Locale.T("Plusieurs groupes de processeurs détectés : gestion Windows conservée pour éviter une sélection partielle.");
                return result;
            }
            if (available.Count == 0)
                return result;
            bool intel = cpu.Vendor == "GenuineIntel";
            bool amd = cpu.Vendor == "AuthenticAMD";
            if (intel && available.Select(x => x.Efficiency).Distinct().Count() > 1)
            {
                byte fastest = available.Max(x => x.Efficiency);
                var performance = available.Where(x => x.Efficiency == fastest).ToList();
                if (performance.Select(x => x.CoreKey).Distinct().Count() >= 4)
                {
                    Add(result, "pcores", Locale.T("Cœurs performance (P-cores)"), performance,
                        Locale.T("Processeur Intel hybride : privilégie les cœurs de la classe la plus performante, en conservant tous leurs threads. Comparez la fluidité avec Gestion Windows."), true);
                    AddNoSmt(result, "pcores-nosmt", Locale.T("P-cores sans Hyper-Threading · essai"), performance);
                }
            }
            if (amd)
            {
                var clusters = available.GroupBy(x => x.CacheKey).OrderByDescending(g => g.Max(x => x.L3Bytes)).ThenBy(g => g.Key).ToList();
                bool asymmetric = clusters.Count > 1 && clusters.All(g => g.All(x => x.L3Bytes > 0))
                    && clusters[0].Max(x => x.L3Bytes) > clusters.Min(g => g.Max(x => x.L3Bytes)) * 2UL;
                if (clusters.Count > 1)
                {
                    int number = 0;
                    foreach (var cluster in clusters)
                    {
                        number++;
                        if (cluster.Select(x => x.CoreKey).Distinct().Count() < 4)
                            continue;
                        bool preferred = asymmetric && number == 1;
                        string name = preferred ? Locale.T("Groupe à grand cache L3 (X3D)") : Locale.T("Groupe L3 ") + number + Locale.T(" · essai");
                        Add(result, "cache-" + cluster.Key, name, cluster.ToList(),
                            preferred ? Locale.T("Un groupe possède nettement plus de cache L3. Ce profil privilégie ce groupe, SMT conservé. Le gain reste à mesurer dans ROTK.")
                                : Locale.T("Limite le jeu à ce groupe de cache L3. À comparer avec Windows : moins de cœurs peut aussi réduire les performances."), preferred);
                        AddNoSmt(result, "cache-" + cluster.Key + "-nosmt", name + Locale.T(" sans SMT · essai"), cluster.ToList());
                    }
                }
            }
            AddNoSmt(result, "all-nosmt", Locale.T("Un thread par cœur · essai"), available);
            return result;
        }

        private static void Add(List<Profile> result, string key, string name, List<CpuSet> sets, string explanation, bool recommended)
        {
            if (recommended)
                foreach (var profile in result)
                    profile.Recommended = false;
            result.Add(new Profile
            {
                Key = key,
                Name = name,
                Ids = sets.Select(x => x.Id).OrderBy(x => x).ToArray(),
                Explanation = explanation,
                Recommended = recommended
            });
        }

        private static void AddNoSmt(List<Profile> result, string key, string name, List<CpuSet> sets)
        {
            var physical = sets.GroupBy(x => x.CoreKey).Select(g => g.OrderBy(x => x.Logical).First()).ToList();
            if (physical.Count >= 4 && physical.Count < sets.Count)
                Add(result, key, name, physical, Locale.T("Essai : un thread logique par cœur physique. Le SMT reste activé pour le reste du PC. Gardez ce profil uniquement si vos mesures dans ROTK sont meilleures."), false);
        }
    }
}
