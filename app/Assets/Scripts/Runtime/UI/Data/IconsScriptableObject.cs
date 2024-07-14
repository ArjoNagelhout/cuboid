// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using Cuboid.Utils;
using UnityEngine;

namespace Cuboid.UI
{
    public static class Icons
    {
        public static IconsScriptableObject Data => UIController.Instance.Icons;
    }

    [CreateAssetMenu(fileName = "IconsScriptableObject", menuName = "ShapeReality/IconsScriptableObject")]
    public class IconsScriptableObject : ScriptableObject
    {
        public Sprite Backspace;
        public Sprite Cursor;
        public Sprite OutlineDraw;
        public Sprite KeyboardShiftKey;
        public Sprite HandRight;
        public Sprite HandLeft;
        public Sprite BusinessCenter;
        public Sprite Close;
        public Sprite CropSquare;
        public Sprite DeleteForever;
        public Sprite Delete;
        public Sprite Edit;
        public Sprite FileCopy;
        public Sprite FileOpen;
        public Sprite Folder;
        public Sprite Info;
        public Sprite IOSShare;
        public Sprite KeyboardOptionKey;
        public Sprite More;
        public Sprite PointingFinger;
        public Sprite Redo;
        public Sprite Refresh;
        public Sprite RemoveCircle;
        public Sprite RestoreFromTrash;
        public Sprite Settings;
        public Sprite Undo;
        public Sprite Widget;
        public Sprite ContentCut;
        public Sprite ContentCopy;
        public Sprite ContentPaste;
        public Sprite ContentDuplicate;
        public Sprite Language;
        public Sprite KeyboardHide;
        public Sprite Check;
        public Sprite DeleteSweep;

        public Sprite Title;

        public Sprite FormatAlignCenter;
        public Sprite FormatAlignRight;
        public Sprite FormatAlignLeft;
        public Sprite FormatAlignJustify;

        public Sprite FormatBold;
        public Sprite FormatItalic;

        public Sprite Warning;
        public Sprite Error;

        public Sprite Cloud;
        public Sprite Extension;
    }
}

