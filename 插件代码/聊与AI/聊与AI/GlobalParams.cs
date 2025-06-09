using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace 聊与AI
{
    public static class GlobalParams
    {
        public static String apiKey = Config.Default.API_KEY;
        public static String modelName = GetModelName();
        public static int modelNum = 0;
        public static String BASE_URL = Config.Default.SERVER_ADRESS;
        //public static String BASE_URL = "https://wstools.liaoyuai.top:54847";
        // public const String BASE_URL = "http://192.168.0.100:54876";
        public const String VERSION = "v0.0.3";
        public const String VERSION_FILE_URL = "https://wstools.liaoyuai.top:9812/office_addin/word_liaoyuai/version.json";

        public static bool CheckVersion()
        {
            var url = GlobalParams.VERSION_FILE_URL;
            var client = new HttpClient();
            ServicePointManager.SecurityProtocol = (SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12);
            var response = client.GetAsync(url);
            String res;
            try
            {
                res = response.Result.Content.ReadAsStringAsync().Result;
            } catch
            {
                MessageBox.Show("网络连接异常");
                return false;
            }
            
            if (response.Result.StatusCode != HttpStatusCode.OK)
            {
                var errJs = JObject.Parse(res);

                MessageBox.Show(errJs["detail"].ToString());
                return false;
            }

            var recvJs = JObject.Parse(res);

            String version = recvJs["version"].ToString();
            String update_url = recvJs["url"].ToString();
            if (GlobalParams.VERSION != version)
            {
                Update f = new Update();
                f.SetVersionInfo(version, update_url);
                f.ShowDialog();
                return true;
            }
            return false;
        }

        private static String GetModelName()
        {
            if (Config.Default.MODEL_NAME == "")
            {
                return "deepseek_r1";
            } else
            {
                return Config.Default.MODEL_NAME;
            }
        }
    }
}
