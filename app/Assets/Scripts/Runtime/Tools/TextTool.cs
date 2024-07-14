// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using Cuboid.Input;
using UnityEngine;
using Cuboid.UI;

namespace Cuboid
{
    [PrettyTypeName(Name = "Text Tool")]
    public class TextTool : OutsideUIBehaviour,
        IToolHasProperties
    {
        [RuntimeSerializedPropertyInt(Label = "Font Size", Min = 8, Max = 112, InputField = true, Slider = true)]
        public StoredBinding<int> FontSize;

        [PrettyTypeName(Name = "Font Style")]
        public enum FontStyle
        {
            [EnumDataAttribute(Name = "Regular", Icon = "Title")]
            Regular = 0,

            [EnumDataAttribute(Name = "Italic", Icon = "FormatItalic")]
            Italic,

            [EnumDataAttribute(Name = "Bold", Icon = "FormatBold")]
            Bold,

            [EnumDataAttribute(Name = "Bold Italic", Icon = "FormatItalic")]
            BoldItalic
        }

        [PrettyTypeName(Name = "Paragraph Alignment")]
        public enum ParagraphAlignment
        {
            [EnumDataAttribute(Name = "Left Align", Icon = "FormatAlignLeft")]
            LeftAlign = 0,

            [EnumDataAttribute(Name = "Center Align", Icon = "FormatAlignCenter")]
            CenterAlign,

            [EnumDataAttribute(Name = "Right Align", Icon = "FormatAlignRight")]
            RightAlign,

            [EnumDataAttribute(Name = "Justify", Icon = "FormatAlignJustify")]
            JustifyAlign
        }

        [RuntimeSerializedPropertyEnum(Label = "Font Style")]
        public StoredBinding<FontStyle> ActiveFontStyle;

        [RuntimeSerializedPropertyEnum(Label = "Alignment")]
        public StoredBinding<ParagraphAlignment> ActiveParagraphAlignment;

        private void Awake()
        {
            FontSize = new("TextTool_FontSize", 12);
            ActiveFontStyle = new("TextTool_FontStyle", FontStyle.Regular);
            ActiveParagraphAlignment = new("TextTool_ParagraphAlignment", ParagraphAlignment.LeftAlign);
        }

        protected override void OutsideUIPointerClick(SpatialPointerEventData eventData)
        {
            base.OutsideUIPointerClick(eventData);

            if (eventData.outsideUIValidPointerPosition)
            {
                // place text object at cursor position, open keyboard at text object position
                
            }
        }
    }
}
