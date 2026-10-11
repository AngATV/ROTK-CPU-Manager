using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RotkCpu
{
    internal static class Locale
    {
#if FORCE_EN
        internal const string ForcedLanguage = "en";
#elif FORCE_FR
        internal const string ForcedLanguage = "fr";
#else
        internal const string ForcedLanguage = null;
#endif
        private static string language = ForcedLanguage ?? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? "fr" : "en");
        internal static string Language
        {
            get
            {
                return language;
            }
            set
            {
                language = value == "en" ? "en" : "fr";
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);
            }
        }
        internal static void UsePreference(string preference)
        {
            Language = ForcedLanguage ?? (preference == "en" || preference == "fr" ? preference : language);
        }
        internal static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "Fermer et restaurer", "Close and restore" },
            { "Fermer la fenêtre", "Close window" },
            { "Défilement", "Scroll" },
            { "Agrandir / restaurer la fenêtre", "Maximize / restore window" },
            { "Réduire la fenêtre", "Minimize window" },
            { "Préférences illisibles, valeurs par défaut : ", "Could not read preferences, using defaults: " },
            { "Le fichier de préférences est vide.", "The preferences file is empty." },
            { " cœurs physiques   /   ", " physical cores   /   " },
            { " groupe(s) de cache", " cache group(s)" },
            { " P-cores identifiés · autres cœurs laissés disponibles pour Windows", " P-cores detected · other cores remain available to Windows" },
            { "Détection CPU indisponible", "CPU detection unavailable" },
            { "Aucun réglage ne sera appliqué : ", "No settings will be applied: " },
            { "Seul H1Z1.exe du dossier ROTK choisi peut être ciblé.", "Only H1Z1.exe in the selected ROTK folder can be targeted." },
            { "ROTK CPU Manager — détection dédiée", "ROTK CPU Manager — dedicated monitoring" },
            { "Ouvrir ROTK CPU Manager", "Open ROTK CPU Manager" },
            { "Quitter et restaurer", "Exit and restore" },
            { "En attente de ROTK · surveillance désactivée", "Waiting for ROTK · monitoring disabled" },
            { "01  /  INSTALLATION", "01  /  INSTALLATION" },
            { "Dossier d'installation ROTK", "ROTK installation folder" },
            { "Choisir…", "Browse…" },
            { "02  /  PROCESSEUR", "02  /  PROCESSOR" },
            { "03  /  PROFIL CPU", "03  /  CPU PROFILE" },
            { "Profil CPU ROTK", "ROTK CPU Manager profile" },
            { "APPLIQUER ET SURVEILLER", "APPLY AND MONITOR" },
            { "RESTAURER ET ARRÊTER", "RESTORE AND STOP" },
            { "À chaque lancement du jeu", "Every time the game starts" },
            { "EXPLICATION DU PROFIL", "PROFILE EXPLANATION" },
            { "COMPARER LES PROFILS", "COMPARE PROFILES" },
            { "1. Comparez avec Gestion Windows.\r\n2. Gardez une scène et des options similaires.\r\n3. Regardez surtout les à-coups et le temps par image, puis répétez le test.", "1. Compare with Windows management.\r\n2. Use a similar scene and settings.\r\n3. Focus on stutter and frame times, then repeat the test." },
            { "COMPRENDRE LES RÉGLAGES  ›", "UNDERSTAND THE SETTINGS  ›" },
            { "Journal de détection et d'application", "Detection and application log" },
            { "Aucun profil CPU disponible.", "No CPU profile is available." },
            { "Surveillance activée · ", "Monitoring enabled · " },
            { "Aucune restriction CPU Sets · gestion Windows", "No CPU Sets restriction · Windows management" },
            { " cœurs / ", " cores / " },
            { " threads sélectionnés\r\nCPU logiques : ", " selected threads\r\nLogical CPUs: " },
            { "Vérifiez le dossier ROTK · aucun nouveau profil appliqué", "Check the ROTK folder · no new profile applied" },
            { "Surveillance arrêtée · restauration à réessayer", "Monitoring stopped · retry restoration" },
            { "Restauration à réessayer : ", "Please retry restoration: " },
            { "Restauration incomplète · réessayez", "Restoration incomplete · please retry" },
            { "Sélectionnez le dossier contenant H1Z1.exe et rotkc.dll", "Select the folder containing H1Z1.exe and rotkc.dll" },
            { "Installation sélectionnée : ", "Selected installation: " },
            { "Préférences non enregistrées : ", "Could not save preferences: " },
            { "Gestion Windows", "Windows management" },
            { "  ·  conseillé", "  ·  recommended" },
            { "Laisse Windows répartir les threads. Point de départ conseillé lorsque la topologie ne justifie pas de restriction.", "Let Windows schedule the threads. Recommended starting point when the detected topology does not justify restricting cores." },
            { "Plusieurs groupes de processeurs détectés : gestion Windows conservée pour éviter une sélection partielle.", "Multiple processor groups detected: keep Windows management to avoid a partial selection." },
            { "Cœurs performance (P-cores)", "Performance cores (P-cores)" },
            { "Processeur Intel hybride : privilégie les cœurs de la classe la plus performante, en conservant tous leurs threads. Comparez la fluidité avec Gestion Windows.", "Hybrid Intel CPU: prefer the highest-performance core class, keeping all available threads. Compare smoothness with Windows management." },
            { "P-cores sans Hyper-Threading · essai", "P-cores without Hyper-Threading · trial" },
            { "Groupe à grand cache L3 (X3D)", "Large L3 cache group (X3D)" },
            { "Groupe L3 ", "L3 group " },
            { " · essai", " · trial" },
            { "Un groupe possède nettement plus de cache L3. Ce profil privilégie ce groupe, SMT conservé. Le gain reste à mesurer dans ROTK.", "One group has substantially more L3 cache. This profile favors that group and keeps SMT. Any benefit still needs to be measured in ROTK." },
            { "Limite le jeu à ce groupe de cache L3. À comparer avec Windows : moins de cœurs peut aussi réduire les performances.", "Restrict the game to this L3 cache group. Compare with Windows: fewer cores can also reduce performance." },
            { " sans SMT · essai", " without SMT · trial" },
            { "Un thread par cœur · essai", "One thread per core · trial" },
            { "Essai : un thread logique par cœur physique. Le SMT reste activé pour le reste du PC. Gardez ce profil uniquement si vos mesures dans ROTK sont meilleures.", "Trial: one logical thread per physical core. SMT stays enabled for the rest of the PC. Keep this profile only if your ROTK measurements improve." },
            { "ROTK CPU Manager — comprendre les réglages", "ROTK CPU Manager — understand the settings" },
            { "FERMER", "CLOSE" },
            { "Explications des profils CPU et méthode de comparaison", "CPU profile explanations and comparison method" },
            { "01 / POURQUOI ÇA PEUT CHANGER LA FLUIDITÉ", "01 / WHY IT MAY IMPROVE SMOOTHNESS" },
            { "Windows répartit le travail du jeu entre les cœurs disponibles. Sur certains processeurs, tous les cœurs n'ont pas les mêmes performances ou ne partagent pas le même cache. Choisir un ensemble cohérent peut réduire certains déplacements de travail et échanges entre groupes.\n\n", "Windows schedules the game's work across available cores. On some processors, cores differ in performance or do not share the same cache. Choosing a consistent set may reduce some work migrations and communication between groups.\n\n" },
            { "Le résultat peut se voir sur la régularité des images, même si les FPS moyens changent peu. Exemple : 120 FPS correspondent à environ 8,3 ms par image ; une image qui prend 30 ms peut provoquer un à-coup. Ce n'est pas une mesure effectuée par cet outil.", "The result may show up as more consistent frame delivery, even if average FPS barely changes. For example, 120 FPS means about 8.3 ms per frame; a frame taking 30 ms can cause a stutter. This example is not a measurement made by this tool." },
            { "02 / INTEL : P-CORES ET E-CORES", "02 / INTEL: P-CORES AND E-CORES" },
            { "Les P-cores privilégient les performances ; les E-cores sont plus économes. Sur un Intel hybride reconnu, le profil conseillé propose les P-cores au jeu. Les autres cœurs restent disponibles pour Windows et vos applications. L'outil ne force pas les applications en arrière-plan à utiliser les E-cores.", "P-cores prioritize performance; E-cores are more energy-efficient. On a detected hybrid Intel CPU, the recommended profile offers P-cores to the game. Other cores remain available to Windows and your applications. The tool does not force background applications onto E-cores." },
            { "03 / AMD : CACHE L3, CCD ET X3D", "03 / AMD: L3 CACHE, CCD AND X3D" },
            { "Le cache L3 est une mémoire rapide partagée par des cœurs. Certains Ryzen répartissent leurs cœurs sur plusieurs puces, les CCD. Garder le jeu dans un même groupe peut limiter les échanges entre groupes, mais lui donne aussi moins de cœurs.\n\n", "L3 cache is fast memory shared by a group of cores. Some Ryzen CPUs spread their cores over multiple chips called CCDs. Keeping the game in one group may reduce communication between groups, but also gives it fewer cores.\n\n" },
            { "Certains Ryzen X3D ont un groupe disposant de beaucoup plus de cache. L'outil privilégie ce groupe seulement si les tailles détectées le montrent. Un groupe L3 ne correspond pas toujours à un CCD. Sur un CPU avec un seul groupe ou des groupes identiques, Windows reste le point de départ conseillé.", "Some Ryzen X3D processors have a group with much more cache. The tool favors that group only when the detected cache sizes show it. An L3 group does not always match a CCD. For a CPU with one group or equal-sized groups, Windows remains the recommended starting point." },
            { "04 / SANS SMT OU HYPER-THREADING", "04 / WITHOUT SMT OR HYPER-THREADING" },
            { "Un cœur physique peut présenter deux threads logiques à Windows. Ces threads partagent les ressources du cœur. Le profil « un thread par cœur » n'en propose qu'un au jeu. Certains jeux y gagnent, d'autres perdent de la puissance disponible.\n\n", "A physical core may expose two logical threads to Windows. Those threads share the core's resources. The 'one thread per core' profile offers only one to the game. Some games benefit; others lose useful processing capacity.\n\n" },
            { "C'est un essai, pas une optimisation universelle. Le SMT reste activé pour le reste du PC ; aucun réglage BIOS n'est modifié.", "This is a trial, not a universal optimization. SMT stays enabled for the rest of the PC; no BIOS setting is changed." },
            { "05 / COMMENT CHOISIR VOTRE PROFIL", "05 / HOW TO CHOOSE YOUR PROFILE" },
            { "1. Commencez par Gestion Windows, puis essayez le profil conseillé.\n", "1. Start with Windows management, then try the recommended profile.\n" },
            { "2. Comparez une scène similaire : même zone, mêmes options graphiques et mêmes applications en arrière-plan. Laissez passer le chargement.\n", "2. Compare a similar scene: the same area, graphics settings and background applications. Let loading finish.\n" },
            { "3. Regardez les à-coups et, si votre overlay les affiche, le temps par image et les FPS des passages les plus lents (1 % low). La moyenne seule ne raconte pas tout.\n", "3. Watch for stutter and, if your overlay provides them, frame times and FPS during the slowest moments (1% low). The average alone does not tell the whole story.\n" },
            { "4. Répétez le test. Gardez le profil qui apporte une amélioration régulière sur votre machine. Si c'est moins bien, restaurez le réglage initial.\n\n", "4. Repeat the test. Keep the profile that consistently improves your own machine. If it gets worse, restore the original settings.\n\n" },
            { "ROTK CPU Manager ne mesure pas les FPS et ne prouve pas un gain. Il applique et vérifie une sélection de CPU Sets.", "ROTK CPU Manager does not measure FPS or prove a performance gain. It applies and verifies a selection of CPU Sets." },
            { "06 / CE QUI CHANGE SUR VOTRE PC", "06 / WHAT CHANGES ON YOUR PC" },
            { "Seul H1Z1.exe dans le dossier ROTK choisi est ciblé. Les CPU Sets sont une préférence de répartition proposée à Windows : une affinité existante ou un réglage propre à un thread peut prendre le dessus. Aucun fichier du jeu n'est modifié.\n\n", "Only H1Z1.exe in the selected ROTK folder is targeted. CPU Sets express a scheduling preference to Windows: an existing affinity or a thread-specific setting may take precedence. No game file is modified.\n\n" },
            { "« Gestion Windows » retire la sélection de CPU Sets du jeu. « Restaurer et arrêter » remet la sélection présente avant l'intervention de l'outil. Minimiser garde la surveillance active ; fermer restaure. Si l'outil est tué de force, relancez le jeu pour repartir sur son état initial.", "'Windows management' clears the game's CPU Sets selection. 'Restore and stop' puts back the selection that was present before this tool intervened. Minimizing keeps monitoring active; closing restores. If the tool is forcibly terminated, restart the game to return to its original state." },
            { "Windows choisit librement les cœurs disponibles. C'est votre référence pour comparer les essais et un bon choix lorsque le CPU ne présente pas de particularité utile à exploiter.", "Windows freely chooses among available cores. This is your comparison baseline and a good choice when the CPU has no detected feature that warrants restricting cores." },
            { "Le jeu reçoit un seul thread logique par cœur physique. Cela peut réduire la concurrence entre deux threads d'un même cœur, mais aussi réduire la puissance disponible. Comparez avec la version qui conserve le SMT.", "The game gets one logical thread per physical core. This may reduce contention between two threads on the same core, but also reduce available processing capacity. Compare with the profile that keeps SMT." },
            { "Votre Intel possède des cœurs de classes différentes. Ce profil propose les P-cores, les plus performants, à ROTK, avec tous leurs threads disponibles. Les autres cœurs restent disponibles pour Windows.", "Your Intel CPU has different classes of cores. This profile offers the highest-performance P-cores to ROTK, with all their available threads. Other cores remain available to Windows." },
            { "Un groupe de cœurs dispose de nettement plus de cache L3. Cette mémoire rapide peut aider le jeu à retrouver ses données. Le profil privilégie ce groupe sans retirer ses threads SMT.", "One core group has substantially more L3 cache. This fast memory may help the game access its data. The profile favors that group while keeping its SMT threads." },
            { "Le jeu est orienté vers un seul groupe de cache L3. Cela peut limiter les échanges entre groupes, mais réduit le nombre de cœurs disponibles. À comparer avec Gestion Windows.", "The game is directed towards one L3 cache group. This may limit communication between groups, but reduces the number of available cores. Compare with Windows management." },
            { "Windows n'a pas confirmé le profil demandé.", "Windows did not confirm the requested profile." },
            { "Restauration non confirmée par Windows.", "Windows did not confirm restoration." },
            { "Surveillance désactivée", "Monitoring disabled" },
            { "ROTK fermé (PID ", "ROTK closed (PID " },
            { "ROTK détecté (PID ", "ROTK detected (PID " },
            { "). Réglage initial mémorisé.", "). Original settings saved." },
            { " Fermez l'outil puis relancez-le comme administrateur si ROTK est élevé. Un processus protégé peut aussi refuser l'accès.", " Close the tool and run it as administrator if ROTK is elevated. A protected process may also deny access." },
            { "Accès au jeu ou application du profil non confirmé — voir le journal", "Game access or profile application not confirmed — see log" },
            { "En attente de ROTK · application automatique activée", "Waiting for ROTK · automatic application enabled" },
            { "ROTK détecté · profil actif : ", "ROTK detected · active profile: " },
            { "ROTK détecté · aucun réglage appliqué par l'outil", "ROTK detected · no settings applied by this tool" },
            { "Réglage initial restauré (PID ", "Original settings restored (PID " },
            { "Choisissez le dossier ROTK.", "Choose the ROTK folder." },
            { "Ce dossier doit contenir H1Z1.exe et rotkc.dll (client ROTK).", "This folder must contain H1Z1.exe and rotkc.dll (ROTK client)." },
            { "La liste des CPU Sets change trop vite. Réessayez.", "The CPU Sets list is changing too quickly. Try again." },
            { "Processeur inconnu", "Unknown processor" },
            { "Topologie CPU indisponible.", "CPU topology unavailable." },
            { "Aucun CPU Set disponible.", "No CPU Sets available." },
            { "Cache L3 non identifié : ", "L3 cache could not be identified: " },
            { "En-tête CPU tronqué.", "Truncated CPU header." },
            { "Taille CPU invalide.", "Invalid CPU record size." },
            { "CPU Set tronqué.", "Truncated CPU Set." },
            { "Identifiants CPU dupliqués.", "Duplicate CPU IDs." },
            { "Cache indisponible.", "Cache unavailable." },
            { "En-tête cache tronqué.", "Truncated cache header." },
            { "Taille cache invalide.", "Invalid cache record size." },
            { "Cache tronqué.", "Truncated cache record." },
            { "Masques cache tronqués.", "Truncated cache masks." },
            { "ROTK CPU Manager est déjà ouvert.", "ROTK CPU Manager is already running." },
            { "ROTK CPU Manager nécessite Windows 10 ou 11, 64 bits.", "ROTK CPU Manager requires 64-bit Windows 10 or 11." },
            { "ROTK CPU Manager — démarrage impossible", "ROTK CPU Manager — unable to start" }
        };
        internal static string T(string french)
        {
            string translated;
            return Language == "en" && English.TryGetValue(french, out translated) ? translated : french;
        }
        internal static string TranslateExisting(string text)
        {
            if (String.IsNullOrEmpty(text))
                return text;
            string french = text;
            foreach (var pair in English.OrderByDescending(x => x.Value.Length))
                french = french.Replace(pair.Value, pair.Key);
            if (Language != "en")
                return french;
            // A replacement can contain a shorter source fragment, so replace each original segment only once.
            var result = new StringBuilder();
            var keys = English.Keys.OrderByDescending(x => x.Length).ToArray();
            for (int i = 0; i < french.Length;)
            {
                string match = keys.FirstOrDefault(key => i + key.Length <= french.Length && String.CompareOrdinal(french, i, key, 0, key.Length) == 0);
                if (match == null)
                {
                    result.Append(french[i++]);
                    continue;
                }
                result.Append(English[match]);
                i += match.Length;
            }
            return result.ToString();
        }
        internal static void Refresh(Control control, Control pathBox)
        {
            if (control is TacticalHeader)
                control.Invalidate();
            if (control != pathBox && !(control is ComboBox))
                control.Text = TranslateExisting(control.Text);
            control.AccessibleName = TranslateExisting(control.AccessibleName);
            foreach (Control child in control.Controls)
                Refresh(child, pathBox);
        }
        internal static string Error(Exception exception)
        {
            var win32 = exception as Win32Exception;
            if (Language == "en" && win32 != null)
            {
                // English Windows message resources are not installed on every French system.
                switch (win32.NativeErrorCode)
                {
                    case 5:
                        return "Access is denied. (Windows error 5)";
                    case 6:
                        return "The process handle is invalid. (Windows error 6)";
                    case 87:
                        return "A parameter is invalid. (Windows error 87)";
                    case 122:
                        return "The buffer is too small. (Windows error 122)";
                }
                return "Windows error " + win32.NativeErrorCode;
            }
            return TranslateExisting(exception.Message);
        }
    }
}
