using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 聊与AI
{
    public partial class Update : Form
    {
        public Update()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.label1.Text = "当前版本：" + GlobalParams.VERSION;
        }

        public void SetVersionInfo(String version, String url)
        {
            this.label2.Text = "最新版本：" + version;
            this.textBox1.Text = url;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(this.textBox1.Text);
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
