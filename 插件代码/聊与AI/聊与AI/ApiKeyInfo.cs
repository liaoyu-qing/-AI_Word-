using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 聊与AI
{
    public partial class ApiKeyInfo : Form
    {
        public ApiKeyInfo()
        {
            InitializeComponent();
            this.textBox1.Text = GlobalParams.apiKey;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12);
            var url = GlobalParams.BASE_URL + "/api/v1/query_api_key_info?api_key=" + GlobalParams.apiKey;
            var client = new HttpClient();

            var response = client.GetAsync(url);
            String res = response.Result.Content.ReadAsStringAsync().Result;
            if (response.Result.StatusCode != HttpStatusCode.OK)
            {
                var errJs = JObject.Parse(res);

                MessageBox.Show(errJs["detail"].ToString());
                return;
            }

            var recvJs = JObject.Parse(res);

            String usedTokens = recvJs["used_tokens"].ToString();
            String tokens = recvJs["tokens"].ToString();
            String datediff = recvJs["datediff"].ToString();
            client.Dispose();
            this.textBox2.Text = usedTokens;
            this.textBox3.Text = tokens;
            this.textBox4.Text = datediff + '天';
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
