using PrintOptions.Dto;
using System;
using System.Drawing;

namespace PrintOptions
{
    public class FontInfoService
    {
        private FontInfo mainFont { get; set; }

        private FontInfo defaults { get; set; }

        public StringFormat stringFormat { get; set; }

        public Font font { get; set; }

        public Color color { get; set; }

        public FontInfoService(FontInfo _defaults)
        {
            defaults = _defaults;
            mainFont = defaults;
            SetFontAndFormat(defaults);
        }
        public void SetFontAndFormat(FontInfo specialLineFont)
        {
            CreateActiveFont(specialLineFont);


            StringFormat sformat = new StringFormat();

            if (!String.IsNullOrEmpty(defaults.Align))
            {
                string alignment = defaults.Align;
                switch (alignment.ToLower())
                {
                    case "center":
                        sformat.Alignment = StringAlignment.Center;
                        sformat.LineAlignment = StringAlignment.Center;
                        break;
                    case "right":
                        sformat.Alignment = StringAlignment.Far;
                        sformat.LineAlignment = StringAlignment.Near;
                        break;
                    case "left":
                        sformat.Alignment = StringAlignment.Near;
                        sformat.LineAlignment = StringAlignment.Near;
                        break;
                }
            }

            stringFormat = sformat;


            string fontName = "Arial";
            int fontSize = 10;
            FontStyle fontStyle = FontStyle.Regular;

            if (!String.IsNullOrEmpty(defaults.FontName))
                fontName = defaults.FontName;
            if (defaults.FontSize != null && int.TryParse(defaults.FontSize, out int parsedFontSize) && parsedFontSize != 0)
                fontSize = parsedFontSize;
            if (!String.IsNullOrEmpty(defaults.FontStyle))
            {
                string strFontStyle = defaults.FontStyle;
                switch (strFontStyle.ToString())
                {
                    case "regular":
                        fontStyle = FontStyle.Regular;
                        break;
                    case "bold":
                        fontStyle = FontStyle.Bold;
                        break;
                    case "italic":
                        fontStyle = FontStyle.Italic;
                        break;
                    case "strikeout":
                        fontStyle = FontStyle.Strikeout;
                        break;
                    case "underline":
                        fontStyle = FontStyle.Underline;
                        break;
                }
            }
            Font SlipFont = new Font(fontName, fontSize, fontStyle);
            font = SlipFont;

            switch (defaults.Color?.ToLower())
            {
                case "red":
                    color = Color.Red;
                    break;
                case "gray":
                    color = Color.Gray;
                    break;
                default:
                    color = Color.Black;
                    break;
            }
        }
        public void Reset()
        {
            SetFontAndFormat(defaults);
        }
        private void CreateActiveFont(FontInfo _fontInfo)
        {
            if (_fontInfo == null)
            {
                defaults = mainFont;
                return;
            }

            var newFont = new FontInfo();
            newFont.Align = !String.IsNullOrEmpty(_fontInfo.Align) ? _fontInfo.Align : mainFont.Align;
            newFont.FontSize = !string.IsNullOrEmpty(_fontInfo.FontSize) && _fontInfo.FontSize != "0" ? _fontInfo.FontSize : mainFont.FontSize;
            newFont.FontStyle = !String.IsNullOrEmpty(_fontInfo.FontStyle) ? _fontInfo.FontStyle : mainFont.FontStyle;
            newFont.FontName = !String.IsNullOrEmpty(_fontInfo.FontName) ? _fontInfo.FontName : mainFont.FontName;
            newFont.Width = !string.IsNullOrEmpty(_fontInfo.Width) && _fontInfo.Width != "0" ? _fontInfo.Width : mainFont.Width;
            newFont.Height = !string.IsNullOrEmpty(_fontInfo.Height) && _fontInfo.Height != "0" ? _fontInfo.Height : mainFont.Height;
            newFont.Color = !String.IsNullOrEmpty(_fontInfo.Color) ? _fontInfo.Color : mainFont.Color;
            defaults = newFont;
        }
    }
}
