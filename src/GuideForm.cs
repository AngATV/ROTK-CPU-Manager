using System;
using System.Drawing;
using System.Windows.Forms;

namespace RotkCpu
{
    internal sealed class GuideForm : TacticalForm
    {
        internal GuideForm()
        {
            Text = Locale.T("ROTK CPU Manager — comprendre les réglages");
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            ClientSize = new Size(720, 680);
            MinimumSize = new Size(620, 500);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(17, 18, 19);
            ForeColor = Color.FromArgb(239, 231, 217);
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(0);
            var close = new Button
            {
                Text = Locale.T("FERMER"),
                Dock = DockStyle.Bottom,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(232, 65, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            close.FlatAppearance.BorderSize = 0;
            close.Click += delegate
            {
                Close();
            };
            AcceptButton = close;
            CancelButton = close;
            var content = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = BackColor,
                ForeColor = ForeColor,
                Font = Font,
                AccessibleName = Locale.T("Explications des profils CPU et méthode de comparaison")
            };
            var page = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22) };
            var scroll = new TacticalScrollView { Dock = DockStyle.Fill };
            page.Controls.Add(scroll);
            page.Controls.Add(close);
            Controls.Add(page);
            Controls.Add(new WindowChrome(this, false, null));
            AddSection(content, Locale.T("01 / POURQUOI ÇA PEUT CHANGER LA FLUIDITÉ"),
                Locale.T("Windows répartit le travail du jeu entre les cœurs disponibles. Sur certains processeurs, tous les cœurs n'ont pas les mêmes performances ou ne partagent pas le même cache. Choisir un ensemble cohérent peut réduire certains déplacements de travail et échanges entre groupes.\n\n")
                + Locale.T("Le résultat peut se voir sur la régularité des images, même si les FPS moyens changent peu. Exemple : 120 FPS correspondent à environ 8,3 ms par image ; une image qui prend 30 ms peut provoquer un à-coup. Ce n'est pas une mesure effectuée par cet outil."));
            AddSection(content, Locale.T("02 / INTEL : P-CORES ET E-CORES"),
                Locale.T("Les P-cores privilégient les performances ; les E-cores sont plus économes. Sur un Intel hybride reconnu, le profil conseillé propose les P-cores au jeu. Les autres cœurs restent disponibles pour Windows et vos applications. L'outil ne force pas les applications en arrière-plan à utiliser les E-cores."));
            AddSection(content, Locale.T("03 / AMD : CACHE L3, CCD ET X3D"),
                Locale.T("Le cache L3 est une mémoire rapide partagée par des cœurs. Certains Ryzen répartissent leurs cœurs sur plusieurs puces, les CCD. Garder le jeu dans un même groupe peut limiter les échanges entre groupes, mais lui donne aussi moins de cœurs.\n\n")
                + Locale.T("Certains Ryzen X3D ont un groupe disposant de beaucoup plus de cache. L'outil privilégie ce groupe seulement si les tailles détectées le montrent. Un groupe L3 ne correspond pas toujours à un CCD. Sur un CPU avec un seul groupe ou des groupes identiques, Windows reste le point de départ conseillé."));
            AddSection(content, Locale.T("04 / SANS SMT OU HYPER-THREADING"),
                Locale.T("Un cœur physique peut présenter deux threads logiques à Windows. Ces threads partagent les ressources du cœur. Le profil « un thread par cœur » n'en propose qu'un au jeu. Certains jeux y gagnent, d'autres perdent de la puissance disponible.\n\n")
                + Locale.T("C'est un essai, pas une optimisation universelle. Le SMT reste activé pour le reste du PC ; aucun réglage BIOS n'est modifié."));
            AddSection(content, Locale.T("05 / COMMENT CHOISIR VOTRE PROFIL"),
                Locale.T("1. Commencez par Gestion Windows, puis essayez le profil conseillé.\n")
                + Locale.T("2. Comparez une scène similaire : même zone, mêmes options graphiques et mêmes applications en arrière-plan. Laissez passer le chargement.\n")
                + Locale.T("3. Regardez les à-coups et, si votre overlay les affiche, le temps par image et les FPS des passages les plus lents (1 % low). La moyenne seule ne raconte pas tout.\n")
                + Locale.T("4. Répétez le test. Gardez le profil qui apporte une amélioration régulière sur votre machine. Si c'est moins bien, restaurez le réglage initial.\n\n")
                + Locale.T("ROTK CPU Manager ne mesure pas les FPS et ne prouve pas un gain. Il applique et vérifie une sélection de CPU Sets."));
            AddSection(content, Locale.T("06 / CE QUI CHANGE SUR VOTRE PC"),
                Locale.T("Seul H1Z1.exe dans le dossier ROTK choisi est ciblé. Les CPU Sets sont une préférence de répartition proposée à Windows : une affinité existante ou un réglage propre à un thread peut prendre le dessus. Aucun fichier du jeu n'est modifié.\n\n")
                + Locale.T("« Gestion Windows » retire la sélection de CPU Sets du jeu. « Restaurer et arrêter » remet la sélection présente avant l'intervention de l'outil. Minimiser garde la surveillance active ; fermer restaure. Si l'outil est tué de force, relancez le jeu pour repartir sur son état initial."));
            scroll.SetContent(content);
        }

        private static void AddSection(FlowLayoutPanel content, string heading, string body)
        {
            content.Controls.Add(new Label
            {
                Text = heading,
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(246, 112, 87),
                Margin = new Padding(0, 0, 0, 16)
            });
            content.Controls.Add(new Label
            {
                Text = body,
                AutoSize = true,
                Font = content.Font,
                ForeColor = content.ForeColor,
                Margin = new Padding(0, 0, 0, 32)
            });
        }

        internal static string ProfileReason(Profile profile)
        {
            if (profile.Key == "windows")
                return Locale.T("Windows choisit librement les cœurs disponibles. C'est votre référence pour comparer les essais et un bon choix lorsque le CPU ne présente pas de particularité utile à exploiter.");
            if (profile.Key.EndsWith("nosmt"))
                return Locale.T("Le jeu reçoit un seul thread logique par cœur physique. Cela peut réduire la concurrence entre deux threads d'un même cœur, mais aussi réduire la puissance disponible. Comparez avec la version qui conserve le SMT.");
            if (profile.Key == "pcores")
                return Locale.T("Votre Intel possède des cœurs de classes différentes. Ce profil propose les P-cores, les plus performants, à ROTK, avec tous leurs threads disponibles. Les autres cœurs restent disponibles pour Windows.");
            return profile.Recommended
                ? Locale.T("Un groupe de cœurs dispose de nettement plus de cache L3. Cette mémoire rapide peut aider le jeu à retrouver ses données. Le profil privilégie ce groupe sans retirer ses threads SMT.")
                : Locale.T("Le jeu est orienté vers un seul groupe de cache L3. Cela peut limiter les échanges entre groupes, mais réduit le nombre de cœurs disponibles. À comparer avec Gestion Windows.");
        }
    }
}
