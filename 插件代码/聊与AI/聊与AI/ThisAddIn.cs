using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Word = Microsoft.Office.Interop.Word;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Interop.Word;
using System.Windows.Forms;

namespace 聊与AI
{
    public partial class ThisAddIn
    {
        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
        }

        public Tuple<String, Word.Document> GetSelectionTxt()
        {
            Selection sec = this.Application.Selection;
            return new Tuple<String, Word.Document>(sec.Text, this.Application.Selection.Document);
        }

        public void GoToEnd()
        {
            this.Application.Selection.GoTo(WdGoToItem.wdGoToLine, WdGoToDirection.wdGoToLast);
        }

        public void WriteAtEnd(String text, String color, Word.Document doc)
        {
            Word.Range rng = doc.Range(this.Application.ActiveDocument.Content.End - 1, this.Application.ActiveDocument.Content.End);

            Word.WdColor wdColor = new Word.WdColor();
            if (color == "gray")
            {
                wdColor = WdColor.wdColorGray125;
            }
            else
            {
                wdColor = WdColor.wdColorDarkBlue;
            }

            rng.Text = text;
            rng.Font.Color = wdColor;
            rng.Font.Size = 11;
        }

        public Font GetSelectionFont()
        {
            Selection sec = this.Application.Selection;
            return sec.Font;
        }
        public void ClearSelection()
        {
            if (this.Application.ActiveDocument != null)
            {
                Word.Selection selection = this.Application.Selection;
                string selectedText = selection.Text;
                // 清空选择区域
                selection.Text = string.Empty;
            }
        }
        public void SetEndFont(Font font)
        {
            Word.Range rng = this.Application.ActiveDocument.Range(this.Application.ActiveDocument.Content.End - 1, this.Application.ActiveDocument.Content.End);
            rng.Text = "\r\n";
            if (font.Color == WdColor.wdColorDarkBlue)
            {
                font.Color = WdColor.wdColorBlack;
            }
            rng.Font.Color = font.Color;
        }
        public List<InlineShape> GetSelectedImages()
        {
            List<InlineShape> selectedImages = new List<InlineShape>();

            // 获取当前选择范围
            Selection selection = Globals.ThisAddIn.Application.Selection;

            // 遍历选择范围内的所有内联形状
            foreach (InlineShape shape in selection.InlineShapes)
            {
                if (shape.Type == WdInlineShapeType.wdInlineShapePicture)
                {
                    selectedImages.Add(shape);
                }
            }

            return selectedImages;
        }
        #region VSTO 生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }

        #endregion
    }
}
