using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 聊与AI
{
    public partial class ApiKeyInputBox : Form
    {
        public ApiKeyInputBox()
        {
            InitializeComponent();
            GlobalParams.CheckVersion();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" && textBox2.Text != "")
            {
                GlobalParams.BASE_URL = textBox2.Text;
                Config.Default.SERVER_ADRESS = textBox2.Text;

            }
            else if (textBox1.Text != "" && textBox2.Text == "") {
                GlobalParams.apiKey = textBox1.Text;
                Config.Default.API_KEY = textBox1.Text;
            } else if (textBox1.Text != "" && textBox2.Text != "") {
                GlobalParams.apiKey = textBox1.Text;
                Config.Default.API_KEY = textBox1.Text;
                GlobalParams.BASE_URL = textBox2.Text;
                Config.Default.SERVER_ADRESS = textBox2.Text;
            }

            
            Config.Default.Save();
            this.Close();
        }

        private void ApiKeyInputBox_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
