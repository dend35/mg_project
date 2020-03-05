using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace AccountSwicher
{
    public partial class Form1 : Form
    {
        string clientPath = "C:\\MinesAccountSwitcher\\MinesClient\\Mines3.exe";
        string playersPath = "C:\\MinesAccountSwitcher\\Players";
        public Form1()
        {
            InitializeComponent();
            UpdateCombo();
        }

        private void UpdateCombo()
        {
            var playersName = Directory.GetDirectories(playersPath).ToList();
            for (var index = 0; index < playersName.Count; index++)
            {
                playersName[index] = playersName[index].Split('\\')[3];
            }

            comboBox1.DataSource = playersName;
        }

        public void Save()
        {
            var reg = Registry.CurrentUser.OpenSubKey(Path);
            var valueNames = reg.GetValueNames();
            var hashName = valueNames.First(i => i.Contains("user_hash"));
            var idName = valueNames.First(i => i.Contains("user_id"));
            var player = new PlayerAccModel
            {
                Name = textBox2.Text,
                HashName = hashName,
                IdName = idName,
                HashValue = (byte[])reg.GetValue(hashName),
                IdValue = (byte[])reg.GetValue(idName),
                Note = textBox3.Text
            };
            var playerJson = JsonConvert.SerializeObject(player);
            var playerDirectory = Directory.CreateDirectory(playersPath + "\\" + player.Name);
            var stream = new StreamWriter(playersPath + "\\" + player.Name + ".json");
            stream.Write(playerJson);
            stream.Close();
            UpdateCombo();
        }
        public void Load()
        {
           
            var reg = Registry.CurrentUser.OpenSubKey(Path,true);
            var valueNames = reg.GetValueNames();
            var playerName = comboBox1.Text;
            var stream = new StreamReader(playersPath + "\\" + playerName +"\\"+ playerName + ".json");
            var playerJson = stream.ReadToEnd();
            var player = JsonConvert.DeserializeObject<PlayerAccModel>(playerJson);
            reg.SetValue(player.HashName,player.HashValue);
            reg.SetValue(player.IdName,player.IdValue);
            Log("Аккаунт загружен");

        }
        private void Delete()
        {
            var reg = Registry.CurrentUser.OpenSubKey(Path,true);
            var valueNames = reg.GetValueNames();
            var hashName = valueNames.First(i => i.Contains("user_hash"));
            var idName = valueNames.First(i => i.Contains("user_id"));
            reg.DeleteValue(hashName);
            reg.DeleteValue(idName);
            Log("Данные удалены");
        }

        public void Log(string text)
        {
            textBox1.Text += text + "\n";
        }
        private string Path = "Software\\MyachinInc\\Mines3";

        private void button1_Click(object sender, EventArgs e)
        {
          Save();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Load();
            Process.Start(clientPath);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Delete();
        }
    }
}