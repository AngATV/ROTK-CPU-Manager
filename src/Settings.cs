using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace RotkCpu
{
    public sealed class Settings
    {
        public string Folder = GameGuard.DefaultFolder;
        public string ProfileKey = "";
        public bool AutoEnabled;
        public string Language = "";
        private static string SettingsPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ROTK CPU", "settings.xml");
            }
        }

        internal static Settings Load()
        {
            return Load(SettingsPath);
        }

        internal static Settings Load(string path)
        {
            if (!File.Exists(path))
                return new Settings();
            using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                var settings = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(reader);
                if (settings == null)
                    throw new InvalidDataException(Locale.T("Le fichier de préférences est vide."));
                return settings;
            }
        }

        internal void Save()
        {
            Save(SettingsPath);
        }

        internal void Save(string path)
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            using (var stream = File.Create(temp))
                new XmlSerializer(typeof(Settings)).Serialize(stream, this);
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }
    }
}
