using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Word;
using Word = Microsoft.Office.Interop.Word;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;

namespace 聊与AI
{
    public partial class Ribbon1
    {
        private bool mutex = false;
        private bool tipFlag = false;
        private void Ribbon1_Load(object sender, RibbonUIEventArgs e)
        {

        }

        private void button1_Click(object sender, RibbonControlEventArgs e)
        {
            ApiKeyInputBox f1 = new ApiKeyInputBox();
            f1.Show();
        }

        private void CheckAndInputApiKey()
        {
            if (GlobalParams.apiKey == "")
            {
                ApiKeyInputBox f1 = new ApiKeyInputBox();
                f1.ShowDialog();
                if (GlobalParams.apiKey == "")
                {
                    MessageBox.Show("请输入正确的KEY");
                    return;
                }
            }
        }
        public static string RemovePunctuation(string input)
        {
            char[] punctuations = {  '#', '*', '`', '-', '|' };

            return new string(input.Where(c => !punctuations.Contains(c)).ToArray());
        }
        public class PunctuationRemover
        {
            /// <summary>
            /// 移除字符串中的常见标点符号
            /// </summary>
            /// <param name="text">原始文本</param>
            /// <returns>不含标点符号的文本</returns>
            public static string RemovePunctuationBasic(String text)
            {
                // 定义中英文常见标点符号
                char[] punctuations = { 
            // 中文标点
           '#', '*', '`','-'
        };

                // 逐个替换标点符号为空字符串
                foreach (char punctuation in punctuations)
                {
                    text = text.Replace(punctuation.ToString(), "");
                }

                return text;
            }
        }
        // 使用示例


        async private void HttpForward(String messages, Document document)
        {
            if (mutex)
            {
                MessageBox.Show("请耐心等待前面任务执行");
                return;
            }
            else
            {
                mutex = true;
            }    

            NlpSendJson sendJs = new NlpSendJson();
            sendJs.model = GlobalParams.modelName;
            sendJs.messages = messages;
            sendJs.api_key = GlobalParams.apiKey;
            // String sendText = "{\"model\":\"" + GlobalParams.modelName + "\", \"messages\":[{ \"role\":\"system\", \"content\":\"你是Word文档助手\"}, { \"role\":\"user\", \"content\": \"" + text + "\"}]}";
            String sendText = JsonConvert.SerializeObject(sendJs);
            // MessageBox.Show(sendText);

            ServicePointManager.SecurityProtocol = (SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12);

            var url = GlobalParams.BASE_URL + "/api/v1/forward_msgs/v1/chat/completions/text/stream";
            var client = new HttpClient();

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequestMessage.Content = new StringContent(sendText);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
            } catch
            {
                MessageBox.Show("网络连接异常");
                mutex = false;
                return;
            }

            try
            {
                if (!tipFlag)
                {
                    MessageBox.Show("提示：AI生成中请勿切换至其他word窗口，否则会导致生成内容不连贯！");
                    tipFlag = true;
                }
                var stream = await response.Content.ReadAsStreamAsync();
                var streamReader = new StreamReader(stream);
                var buffer = new byte[1024];
                int writeLength = 0;
                bool lineBreakFlag = false;
                bool midLineBreakFlag = false;

                var font = Globals.ThisAddIn.GetSelectionFont();
                Globals.ThisAddIn.GoToEnd();

                while ((writeLength = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    if (writeLength > 5 && writeLength < buffer.Length)
                    {
                        String recvStr = System.Text.Encoding.UTF8.GetString(buffer).TrimStart("data:".ToCharArray());
                        if (recvStr.Substring(0, 3) == "ERR")
                        {
                            MessageBox.Show(recvStr);
                            break;
                        }
                        if (recvStr.Substring(0, 5) == " ping")
                        {
                            continue;
                        }

                        JObject recvJs = JObject.Parse(recvStr);

                        JToken messagesJToken = recvJs["choices"][0]["delta"];
                        String reasoningContent = "";
                        if (messagesJToken.SelectToken("reasoning_content") != null)
                        {
                            reasoningContent = recvJs["choices"][0]["delta"]["reasoning_content"].ToString();
                        }

                        String content = "";
                        if (messagesJToken.SelectToken("content") != null)
                        {
                            content = recvJs["choices"][0]["delta"]["content"].ToString();
                        }

                        if (reasoningContent != "")
                        {
                            // MessageBox.Show("reasoningContent: " + reasoningContent);
                            if (!lineBreakFlag)
                            {
                                Globals.ThisAddIn.WriteAtEnd("\n", "gray", document);
                                lineBreakFlag = true;
                            }
                            string cleanText = PunctuationRemover.RemovePunctuationBasic(reasoningContent);
                            Globals.ThisAddIn.WriteAtEnd(cleanText, "gray", document);
                        }

                        if (content != "")
                        {
                            if (!midLineBreakFlag)
                            {
                              
                                Globals.ThisAddIn.WriteAtEnd("\n", "blue", document);
                                midLineBreakFlag = true;
                            }
                            string cleanText=PunctuationRemover.RemovePunctuationBasic(content);
                            // MessageBox.Show("content: " + content);
                            Globals.ThisAddIn.WriteAtEnd(cleanText, "blue", document);
                        }

                    }
                    Array.Clear(buffer, 0, buffer.Length);
                }
                Globals.ThisAddIn.SetEndFont(font);
            } catch
            {
                MessageBox.Show("处理失败，可能原因：\n1、网络中断。\n2、文本过长或存在非法字符。不同模型支持最大长度不同，也可以尝试选择其他模型");
            } finally { mutex = false; }
        }

        async private void button3_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }
            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;

            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行对话");
                return;
            }

            // MessageBox.Show(text);
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, "+ msg.ToString() +"]";
            HttpForward(messages, document);
        }

        private void button5_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行对话");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我续写下面这段内容：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button4_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行对话");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我翻译下面这段内容：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text }
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button2_Click(object sender, RibbonControlEventArgs e)
        {
            SelectModel f1 = new SelectModel();
            f1.ShowDialog();
        }

        private void button6_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }
            ApiKeyInfo f = new ApiKeyInfo();
            f.ShowDialog();
        }

        private void button7_Click(object sender, RibbonControlEventArgs e)
        {
            Buy f = new Buy();
            f.Show();
        }

        private void button8_Click(object sender, RibbonControlEventArgs e)
        {
            LiaoyuInfo f = new LiaoyuInfo();
            f.Show();
        }

        private void button9_Click(object sender, RibbonControlEventArgs e)
        {
            if (!GlobalParams.CheckVersion())
            {
                MessageBox.Show("已是最新版本");
            }
        }

        private void button10_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行总结");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我简短总结以下内容：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button11_Click(object sender, RibbonControlEventArgs e)
        {

            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行重写");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我重写以下内容：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button12_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行缩写");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我缩写以下内容：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button13_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行全文润色");
                return;
            }

            // MessageBox.Show(text);
            String message = "作为[语言学专家]， 需要：\r\n 1. 查找所有语法错误。\r\n 2. 提供正确的拼写和语法修正。\r\n 3. 建议更合适的词汇以提高可读性。\r\n4.并返回修改后的文章，请检查以下段落的语法和拼写错误：";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button14_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行对话");
                return;
            }

            // MessageBox.Show(text);
            String message = "调整以下文本的语言风格，使其更加符合正式场合的要求：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button15_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行对话");
                return;
            }

            // MessageBox.Show(text);
            String message = "将以下文本转换为党政风格的语气：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button16_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我分析以下内容生成3-5个标题：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button17_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选会议内容");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我根据以下会议内容，生成会议纪要：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button18_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本进行扩写");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我扩写下面这段内容：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button21_Click(object sender, RibbonControlEventArgs e)
        {
            
                CheckAndInputApiKey();
                if (GlobalParams.apiKey == "")
                {
                    return;
                }

                var tuple = Globals.ThisAddIn.GetSelectionTxt();
                String text = tuple.Item1;
                Document document = tuple.Item2;
                if (text.Length == 1)
                {
                    MessageBox.Show("请扩选文本");
                    return;
                }

                // MessageBox.Show(text);
                String message = "帮我根据以下案件详情生成起诉书：\n";
                JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
                String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
                HttpForward(messages, document);
            
        }

        private void button19_Click(object sender, RibbonControlEventArgs e)
        {
            
                CheckAndInputApiKey();
                if (GlobalParams.apiKey == "")
                {
                    return;
                }

                var tuple = Globals.ThisAddIn.GetSelectionTxt();
                String text = tuple.Item1;
                Document document = tuple.Item2;
                if (text.Length == 1)
                {
                    MessageBox.Show("请扩选文本");
                    return;
                }

                // MessageBox.Show(text);
                String message = "帮我对以下内容进行合规性检查，并给出解决方案：\n";
                JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
                String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
                HttpForward(messages, document);
            
        }

        private void button20_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本");
                return;
            }

            // MessageBox.Show(text);
            String message = "帮我根据以下协商内容，起草新的合同文本：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
        }

        private void button22_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本");
                return;
            }
            // MessageBox.Show(text);
            String message = "帮我分段，不用加标题，不要说多余的话：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);
           
        }

        private void button23_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选文本");
                return;
            }
            // MessageBox.Show(text);
            String message = "帮我分段，加标题，不要说多余的话：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);

        }

        private void button25_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
          
            // MessageBox.Show(text);
            String message = "帮我生成一个入院诊断建议模板：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);

        }

        private void button26_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选研究主题");
                return;
            }
            // MessageBox.Show(text);
            String message = "帮我获取国内外与以下研究主题相关的权威文献：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);

        }

        private void button24_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
           
            // MessageBox.Show(text);
            String message = "帮我生成一个病例模板：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);

        }

        private void button28_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选优化内容");
                return;
            }
            // MessageBox.Show(text);
            String message = "帮我对以下内容进行学术语言优化：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);

        }

        private void button27_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选课程主题");
                return;
            }
            // MessageBox.Show(text);
            String message = "帮我对以下主题生成教案：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);

        }

        private void button29_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选需要查询的药品名称");
                return;
            }
            // MessageBox.Show(text);
            String message = "帮我查询服用以下药品的禁忌事项：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);

        }

        private void button30_Click(object sender, RibbonControlEventArgs e)
        {
            CheckAndInputApiKey();
            if (GlobalParams.apiKey == "")
            {
                return;
            }

            var tuple = Globals.ThisAddIn.GetSelectionTxt();
            String text = tuple.Item1;
            Document document = tuple.Item2;
            if (text.Length == 1)
            {
                MessageBox.Show("请扩选课程主题");
                return;
            }
            // MessageBox.Show(text);
            String message = "帮我对以下课程主题设计课堂互动内容：\n";
            JObject msg = new JObject
            {
                {"role", "user"},
                {"content", message + text}
            };
            String messages = "[{ \"role\":\"system\", \"content\":\"我是聊与AI助手，有什么可以帮助您的？\"}, " + msg.ToString() + "]";
            HttpForward(messages, document);

        }
        bool IsTransLate = true;

        private void button31_Click(object sender, RibbonControlEventArgs e)
        {
            string[] EN = {
        "Stay hungry, stay foolish.",
        "Simple is better than complex.",
        "Code is poetry."
    };
            if (IsTransLate)
            {
               
                group1.Label = "Model Settings";
                group2.Label = "Reasoning";
                group3.Label = "Industry Assistant";
                group4.Label = "Other";
                menu1.Label = "Legal assistant";
                menu2.Label = "Segmentation";
                menu3.Label = "Medical assistant";
                menu4.Label = "Paper support";
                menu5.Label = "Instructional Aides";
                button2.Label = "Model Selection";
                button3.Label = "Dialogue";
                button4.Label = "Translate";
                button5.Label = "Continued writing";
                button6.Label = "Query";
                button7.Label = "Buy Key";
                button8.Label = "About";
                button9.Label = "Update";
                button10.Label = "Summarize";
                button11.Label = "Rewrite";
                button12.Label = "Abbreviation";
                button13.Label = "Polish";
                button14.Label = "Formal Style";
                button15.Label ="Party and government style";
                button16.Label = "Title Generation";
                button17.Label = "Meeting Minutes";
                button18.Label = "Elaborate On";
                button19.Label = "Compliance check";
                button20.Label = "Contract drafting";
                button21.Label = "Generate indictment";
                button22.Label = "Untitled segmentation";
                button23.Label = "Segmented with title"; 
                button24.Label = "Automatic generation of case templates";
                button25.Label = " Admission Diagnosis Suggestion Template";
                button26.Label = "Literature screening and retrieval"; 
                button27.Label = "Automatic generation of lesson plans";
                button28.Label = "Academic language optimization";
                button29.Label = "Drug taboo inquiry";
                button30.Label = " Design classroom interaction";
                button31.Label = "中文";
                IsTransLate = false;
            }
            else {
                group1.Label = "模型设置";
                group2.Label = "推理";
                group3.Label = "行业助手";
                group4.Label = "其他";
                menu1.Label = "法律助手";
                menu2.Label = "分段";
                menu3.Label = "医疗助手";
                menu4.Label = "论文辅助"; 
                menu5.Label = "教培助手";
                button2.Label = "模型选择";
                button3.Label = "对话";
                button4.Label = "翻译";
                button5.Label = "续写";
                button6.Label = "查询";
                button7.Label = "购买key";
                button8.Label = "关于";
                button9.Label = "更新";
                button10.Label = "总结";
                button11.Label = "重写";
                button12.Label = "缩写";
                button13.Label = "润色";
                button15.Label = "党政风";
                button16.Label = "标题生成";
                button17.Label = "会议纪要";
                button18.Label = "扩写";
                button19.Label = "合规性检查";
                button20.Label = "合同拟定";
                button21.Label = "生成起诉书";
                button22.Label = "无标题分段";
                button23.Label = "带标题分段";
                button24.Label = "带标题分段";
                button25.Label = "带标题分段";
                button26.Label = "文献筛选与检索";
                button27.Label = "教案自动生成";
                button28.Label = "学术语言优化";
                button31.Label = "EN";
                IsTransLate = true;
            }
        }
    }

    public class NlpSendJson
    {
        public String model { get; set; }
        public String messages { get; set; }
        public String api_key { get; set; }
    }
}
