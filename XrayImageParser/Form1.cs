using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XrayImageParser
{
    public partial class Form1 : Form
    {
        private int filledBoxes = 0; //counts how many serial numbers were entered
        private List<string> scannedSN = new List<string>(); //list of serials for future no-duplicate-check  method
        private string[] oldFileNames = new string[8]; //array of filenames in input folder
        private string outputFolder = string.Empty;
        private StringBuilder sb = new StringBuilder();
        private readonly StringBuilder errMsgBox = new StringBuilder();
        public string CurrentFile { get; set; }

        public Form1()
        {
            InitializeComponent();
            textBox1.Focus(); //set focus to 1st textbox
            textBox9.Text = FileOperations.ReadSetting("inputFolder"); //load input folder from app.config
            textBox10.Text = FileOperations.ReadSetting("outputFolder"); //write output folder to app.config
        }

        private bool CheckIfCorrectSerial(string serial)
        {
            if (serial.Length == 24)
            {
                if (!scannedSN.Contains(serial))
                {
                    scannedSN.Add(serial); //for future no-duplicate-check method, for now 2+ files can have same name - MAJOR FLAW, files can be overwritten
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private string IncreaseFolderNumber(string folderName)
        {
            try
            {   //maybe linq..
                var number = string.Concat(folderName.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse());   //stack overflow:)  reverse folder name so you can read from end, if char is a number add it to array, reverse again: Part134 -> 431traP 431|traP -> 134
                //string number = folderName.Substring(folderName.Length - 2); //will fail at Part100 etc
                int n = Int32.Parse(number);
                n += 1;
                Debug.Assert(n != 1);
                number = n.ToString();
                string newFolderName = folderName.Substring(0, folderName.Length - number.Length) + number;
                return newFolderName;
            }
            catch
            {
                MessageBox.Show("Program nie jest w stanie automatycznie zmienic nazwy folderu.\n Sprawdz czy sciezka do folderu Part jest prawidlowa.");
                return folderName;
            }
        }
        private string[] GrabFileNames(string folderPath)
        {
            try
            {
                string[] sortedNames = Directory.GetFiles(folderPath, "*.jpg"); //look only for images
                Debug.Assert(sortedNames.Length != 0);
                Array.Sort(sortedNames); //sort names so serial number is later assigned to correct image
                return sortedNames;
            }
            catch (Exception e)
            {
                MessageBox.Show("Nie mozna odczytac nazw plikow. Sprawdz folder ze zdjeciami. Blad: " + e.ToString());
                return null;
            }
        }

        private bool MoveFile(string inputFile, string outputFile, string boardStatus)
        {
            //string copy = string.Empty;
            string outputFileName = outputFolder + "//" + outputFile + "_" + boardStatus + ".jpg";
            //string copyFileName = outputFileName + "_copy";

            int chosenOption = 0;
            if (checkBox1.Checked) //debug mode
            {
                MessageBox.Show("Stara nazwa zdjęcia: " + inputFile + " Nowa nazwa: " + outputFileName + "_" + boardStatus + ".jpg");
            }
            if (File.Exists(outputFileName))
            {
                //call dialogbox
                CurrentFile = outputFileName;
                chosenOption = MoveOptions();
            }
            try
            {
                if (chosenOption != 0)
                {
                    if (chosenOption == 1)
                    {
                        string copyFileName = outputFileName.Insert(outputFileName.IndexOf('.'), "_Copy");
                        File.Replace(inputFile, outputFileName, copyFileName);
                    }
                    else if (chosenOption == 2)
                    {
                        File.Delete(outputFileName);
                        File.Move(inputFile, outputFileName);
                    }
                }
                else
                {
                    File.Move(inputFile, outputFileName);// + "_" + boardStatus + ".jpg");
                }

                return true;
            }
            catch (IOException e)
            {
                MessageBox.Show("Nie mozna przeniesc pliku. Blad: " + e.ToString());
                return false;
            }
        }

        public int MoveOptions()
        {
            int status = 0;
            Form2 moveDialog = new Form2(CurrentFile);
            moveDialog.StartPosition = FormStartPosition.CenterParent;
            DialogResult result = moveDialog.ShowDialog();
            if (result == DialogResult.Yes)
            {
                //overwrite
                status = 2;
            }
            else if (result == DialogResult.Ignore)
            {
                //skip
            }
            else if (result == DialogResult.No)
            {
                //save copy
                status = 1;
            }
            moveDialog.Dispose();
            return status;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            errMsgBox.AppendLine("Ilosc zdjec w folderze nie odpowiada liczbie zeskanowanych numerow seryjnych.");
            errMsgBox.AppendLine("Zeskanowanych numerów: " + filledBoxes.ToString());
            errMsgBox.AppendLine("Wygenerowanych zdjęć: " + oldFileNames.Length);
            FileOperations.AddUpdateAppSetting("inputFolder", textBox9.Text); //save in-out folder paths
            FileOperations.AddUpdateAppSetting("outputFolder", textBox10.Text);
            string boardStatus = string.Empty;
            string outputFileName = string.Empty;
            int boardNumber = 1; //needed to bypass counting from 0
            if (FileOperations.CheckIfFolderExists(textBox9.Text) && FileOperations.CheckIfFolderExists(textBox10.Text))
            {   //look for new files in folder
                oldFileNames = GrabFileNames(textBox9.Text); //get old filenames
                outputFolder = textBox10.Text; //set out folder
                if (filledBoxes == oldFileNames.Length) //check if there is same amount of serials and images
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
                            boardNumber += i; //mark which board is failing
                            sb.AppendLine("Produkt nr: " + boardNumber.ToString() + " ma status FAIL");
                        }
                        Debug.Assert(boardStatus != string.Empty); //check that images have correct name format
                        MoveFile(oldFileNames[i], scannedSN[i], boardStatus);
                    }
                    textBox9.Text = IncreaseFolderNumber(textBox9.Text); //each part stores images in new folder, increment number in output folder path
                    if (sb.Length != 0)
                    {
                        MessageBox.Show(sb.ToString()); //show which boards failed
                    }
                    ResetUI(this); //reset all textboxes except folder paths
                }
                else
                {
                    MessageBox.Show(errMsgBox.ToString());
                }
            }
            else
            {
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result = MessageBox.Show("Brak folderu do którego mają być przeniesione zdjęcia. Czy chcesz go utworzyc?", "Brak folderu", buttons);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(textBox10.Text);
                    }
                    catch
                    {
                        MessageBox.Show("Nie można utworzyć folderu.");
                    }
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
            Array.Clear(oldFileNames, 0, oldFileNames.Length);
            //Debug.Assert(oldFileNames.Length == 8);
            if (checkBox1.Checked)
            {
                MessageBox.Show(oldFileNames.ToString());
            }
            label14.Text = "Zeskanowanych numerów:" + filledBoxes.ToString();
            errMsgBox.Clear();
            sb.Clear();
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
    }
}
