//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Utils
{
    public static class StringUtils
    {
        public static string AppendCopyToEndOfString(string input)
        {
            if (input.EndsWith("copy"))
            {
                return input + " 1";
            }
            else
            {
                int lastSpaceIndex = input.LastIndexOf(' ');
                if (lastSpaceIndex != -1)
                {
                    string firstWord = input.Substring(0, lastSpaceIndex);
                    if (!firstWord.EndsWith("copy"))
                    {
                        return input.TrimEnd() + " copy";
                    }
                    string lastWord = input.Substring(lastSpaceIndex + 1);
                    if (int.TryParse(lastWord, out int n))
                    {
                        return firstWord + " " + (n + 1);
                    }
                }
            }
            return input.TrimEnd() + " copy";
        }
    }
}
