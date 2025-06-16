using System;
using System.IO;
using System.Windows;

namespace ChameleonHub.Helpers
{
    public static class AlignmentChecker
    {
        public static string GetAlignmentInfo(string fileName, long length)
        {
            string result = $"{fileName}: ";

            if (length % 16 == 0)
                result += "Aligned to 16 bytes; ";

            if (length % 32 == 0)
                result += "Aligned to 32 bytes; ";

            if (length % 64 == 0)
                result += "Aligned to 64 bytes; ";

            if (length % 16 != 0 && length % 32 != 0 && length % 64 != 0)
                result += "Not aligned to 16/32/64 bytes.";

            return result;
        }
    }
}
