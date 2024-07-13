// 
// FileUtils.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Cuboid
{
    /// <summary>
    /// 
    /// </summary>
    public static class FileUtils
    {
        // https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
        // for getting human readable file size string from bytes
        public static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        public static Stream GetStream(string filePath, bool streamingAssets)
        {
            if (streamingAssets)
            {
                return BetterStreamingAssets.OpenRead(filePath);
            }
            else
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
        }
    }

}
