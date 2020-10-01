using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Diagnostics;

namespace XrayImageParser
{
    public partial class Form1 : Form
    {
        private int filledBoxes = 0; //counts how many serial numbers were entered
        private List<string> scannedSN = new List<string>(); //list of serials for future no-duplicate-check  method
        private string[] oldFileNames = new string[8]; //array of filenames in input folder
        private string outputFolder = string.Empty;
        StringBuilder sb = new StringBuilder();

        public Form1()
        {
            InitializeComponent();
            textBox1.Focus(); //set focus to 1st textbox
            textBox9.Text = ReadSetting("inputFolder"); //load input folder from app.config
            textBox10.Text = ReadSetting("outputFolder"); //write output folder to app.config
        }
        //LOAD CONFIGURATION -- APP CONFIG
        static string ReadSetting(string key)
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

        static void AddUpdateAppSetting(string key, string value)
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

        private bool CheckIfCorrectSerial(string serial)
        {
            if (serial.Length == 24)
            {
                scannedSN.Add(serial); //for future no-duplicate-check method, for now 2+ files can have same name - MAJOR BUG
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckIfFolderExists(string path)
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

        private string[] GrabFileNames(string folderPath)
        {
            //string[] sortedNames = Directory.GetFiles(folderPath, "*.jpg");
            try
            {
                string[] sortedNames = Directory.GetFiles(folderPath, "*.jpg"); //look only for images
                Debug.Assert(sortedNames.Length != 0);
                Array.Sort(sortedNames); //sort names so serial number is later assigned to correct image
                return sortedNames;
            }
            catch(Exception e)
            {
                MessageBox.Show("Nie mozna odczytac nazw plikow. Sprawdz folder ze zdjeciami. Blad: " + e.ToString());
                return null;
            }
        }

        private bool MoveFile(string inputFile, string outputFile, string boardStatus)
        {
            string outputFileName = outputFolder + "//" + outputFile;
            try
            {
                File.Move(inputFile, outputFileName + "_" + boardStatus + ".jpg");
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Nie mozna przeniesc pliku. Blad: " + e.ToString());
                return false;
            }
        }

        private string IncreaseFolderNumber(string folderName)
        {
            string number = string.Empty;
            int n = 0;
            string newFolderName = string.Empty;
            try
            {
                number = folderName.Substring(folderName.Length - 2); //get only last 2 chars from folder name
                n = Int32.Parse(number);
                n += 1;
                Debug.Assert(n != 1);
                number = n.ToString();
                //string newFName = folderName.TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }) + number;
                newFolderName = folderName.Substring(0, folderName.Length - 2) + number; //remove old number and add incremented one
                //MessageBox.Show("increase number name" + newFolderName);
                return newFolderName;
            }
            catch
            {
                MessageBox.Show("Niewlasciwa nazwa folderu");
                return folderName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AddUpdateAppSetting("inputFolder", textBox9.Text); //save in-out folder paths
            AddUpdateAppSetting("outputFolder", textBox10.Text);
            string boardStatus = string.Empty;
            string outputFileName = string.Empty;
            int boardNumber = 1;
            //ResetUI(this);
            if (CheckIfFolderExists(textBox9.Text) && CheckIfFolderExists(textBox10.Text))
            {
                //look for new files in folder
                oldFileNames = GrabFileNames(textBox9.Text); //get old filenames
                outputFolder = textBox10.Text; //set out folder
                if (filledBoxes == oldFileNames.Length)
                {
                    for (int i = 0; i < filledBoxes; i++) //actual file moving loop
                    {
                        if (oldFileNames[i].Contains("OK"))
                        {
                            boardStatus = "OK";
                        }
                        else if (oldFileNames[i].Contains("FAIL"))
                        {
                            boardStatus = "FAIL";
                            boardNumber += i;
                            sb.AppendLine("Produkt: " + boardNumber.ToString() + " ma status FAIL");
                        }
                        else
                        {
                            boardStatus = string.Empty;
                        }
                        if (checkBox1.Checked)
                        {
                            MessageBox.Show(oldFileNames[i] + " " + scannedSN[i]+ " " + boardStatus);
                        }
                        MoveFile(oldFileNames[i], scannedSN[i], boardStatus);
                    }
                    textBox9.Text = IncreaseFolderNumber(textBox9.Text); //each part stores images in new folder, increment number in output folder path
                    ResetUI(this);
                    if (sb.Length != 0)
                    {
                        MessageBox.Show(sb.ToString());
                    }
                }
                else
                {
                    MessageBox.Show("Ilosc zdjec w folderze nie odpowiada liczbie zeskanowanych numerow seryjnych");
                }
            }
            else
            {
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result = MessageBox.Show("Brak folderu na dysku Z. Czy chcesz go utworzyc?", "Brak folderu", buttons);
                if (result == DialogResult.Yes)
                {
                    Directory.CreateDirectory(outputFolder);
                }
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CheckIfCorrectSerial(textBox1.Text))
                {
                    textBox2.Focus();
                    filledBoxes += 1;
                    label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
                }
                else
                {
                    textBox1.Text = "";
                }
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CheckIfCorrectSerial(textBox2.Text))
                {
                    textBox3.Focus();
                    filledBoxes += 1;
                    label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
                }
                else
                {
                    textBox2.Text = "";
                }
            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CheckIfCorrectSerial(textBox3.Text))
                {
                    textBox4.Focus();
                    filledBoxes += 1;
                    label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
                }
                else
                {
                    textBox3.Text = "";
                }
            }
        }

        private void textBox4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CheckIfCorrectSerial(textBox4.Text))
                {
                    textBox5.Focus();
                    filledBoxes += 1;
                    label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
                }
                else
                {
                    textBox4.Text = "";
                }
            }
        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CheckIfCorrectSerial(textBox5.Text))
                {
                    textBox6.Focus();
                    filledBoxes += 1;
                    label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
                }
                else
                {
                    textBox5.Text = "";
                }
            }
        }

        private void textBox6_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CheckIfCorrectSerial(textBox6.Text))
                {
                    textBox7.Focus();
                    filledBoxes += 1;
                    label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
                }
                else
                {
                    textBox6.Text = "";
                }
            }
        }

        private void textBox7_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CheckIfCorrectSerial(textBox7.Text))
                {
                    textBox8.Focus();
                    filledBoxes += 1;
                    label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
                }
                else
                {
                    textBox7.Text = "";
                }
            }
        }

        private void textBox8_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CheckIfCorrectSerial(textBox8.Text))
                {
                    filledBoxes += 1;
                    label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
                }
                else
                {
                    textBox8.Text = "";
                }
            }
        }

        private void ResetUI(Control control)
        {
            //foreach (var c in control.Controls)
            //{
            //    if (c is TextBox)
            //    {
            //        ((TextBox)c).Text = string.Empty;
            //    }
            //}
            textBox1.Text = string.Empty;
            textBox2.Text = string.Empty;
            textBox3.Text = string.Empty;
            textBox4.Text = string.Empty;
            textBox5.Text = string.Empty;
            textBox6.Text = string.Empty;
            textBox7.Text = string.Empty;
            textBox8.Text = string.Empty;
            filledBoxes = 0;
            scannedSN.Clear();
            Array.Clear(oldFileNames,0,filledBoxes);
            if (checkBox1.Checked)
            {
                MessageBox.Show(oldFileNames.ToString());
            }
            label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ResetUI(this);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        //private void handleTextbox(int txtboxNumber, KeyEventArgs e)
        //{
        //    Control ctrl;
        //    if (e.KeyCode == Keys.Enter)
        //    {
        //        if (CheckIfCorrectSerial(textBox8.Text))
        //        {
        //            filledBoxes += 1;
        //        }
        //        else
        //        {
        //            textBox8.Text = "";
        //        }
        //    }
        //}
    }
}
