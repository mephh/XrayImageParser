using System;
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

        internal bool GenerateReport(string path, string logfile)
        {
            string filePath = path + "\\Raport.txt";
            string boxName = path.Substring(path.LastIndexOf('\\')+1);
            string shortedLogName = logfile.Substring(logfile.LastIndexOf('/')+1);
            string[] lines = { "Raport dla boxu nr " + boxName,
                "Raport został wygenerowany dnia: " + DateTime.Now.ToShortDateString() + " o godzinie: " + DateTime.Now.ToShortTimeString(),
                "LOG \t GODZINA TESTU"};
            if (!File.Exists(filePath))
            {
                try
                {
                    //File.Create(filePath);
                    File.WriteAllLines(filePath, lines);
                    //using (StreamWriter sw = new StreamWriter(filePath, true))
                    //{
                    //    sw.WriteLine("Raport dla boxu nr " + boxName);
                    //    sw.WriteLine("Raport został wygenerowany dnia: " + DateTime.Now.ToShortDateString() + " o godzinie: " + DateTime.Now.ToShortTimeString());
                    //    sw.WriteLine();
                    //    sw.WriteLine();
                    //    sw.WriteLine("LOG \t GODZINA TESTU");
                    //}
                }
                catch (Exception e)
                {
                    MessageBox.Show("Nie mozna utworzyc pliku z raportem. Kod bledu: " + e.ToString());
                    return false;
                }
            }
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                //if (logfiles.Length != 0)
                //{
                //    for (int i = 0; i < logfiles.Length; i++)
                //    {
                //        sw.WriteLine(logfiles[i] + "\t \t " + DateTime.Now.ToShortTimeString());
                //    }
                //}
                if (shortedLogName.Length != 0)
                {
                    sw.WriteLine(shortedLogName + " \t " + DateTime.Now.ToShortTimeString());
                }
            }
            return true;

        }
    }
}
