using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace 聊与AI
{
    public partial class SelectModel : Form
    {
        public SelectModel()
        {
            InitializeComponent();
            //顺序不能乱改
            comboBox1.Items.Add("deepseek_r1");

            comboBox1.Items.Add("deepseek_v3");

            comboBox1.Items.Add("通义千问2.5_plus");

            comboBox1.Items.Add("文心一言4.0");

            comboBox1.SelectedIndex = GlobalParams.modelNum;
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex) 
            {
                case 0:
                    GlobalParams.modelNum = comboBox1.SelectedIndex;
                    GlobalParams.modelName = "deepseek_r1";
                    break;
                case 1:
                    GlobalParams.modelNum = comboBox1.SelectedIndex;
                    GlobalParams.modelName = "deepseek_v3";
                    break;
                case 2:
                    GlobalParams.modelNum = comboBox1.SelectedIndex;
                    GlobalParams.modelName = "qwen2.5";
                    break;
                case 3:
                    GlobalParams.modelNum = comboBox1.SelectedIndex;
                    GlobalParams.modelName = "ernie_bot4.0";
                    break;
            }
            Config.Default.MODEL_NAME = GlobalParams.modelName;
            Config.Default.Save();
            this.Close();
        }
    }
}
