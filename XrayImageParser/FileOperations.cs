using System.Configuration;
using System.IO;
using System.Windows.Forms;

namespace XrayImageParser
{
    class FileOperations
    {
        //LOAD CONFIGURATION -- APP CONFIG
        internal static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found"; //check if called setting name exists
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                MessageBox.Show("brak zapisanej sciezki");
                return "Brak takiej sciezki";
            }
        }

        internal static void AddUpdateAppSetting(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings; //load settings from app.config
                if (settings[key] == null)
                {
                    settings.Add(key, value); //create new setting
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified); //save changes
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name); //update app.config
            }
            catch (ConfigurationErrorsException)
            {
                MessageBox.Show("Error writing app settings");
            }
        }
        //END OF CONFIGURATION METHODS
        internal static bool CheckIfFolderExists(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            else
            {
                MessageBox.Show("Sprawdz czy folder isnieje: " + path);
                return false;
            }
        }

    }
}
