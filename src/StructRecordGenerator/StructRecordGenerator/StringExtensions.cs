using System;
#nullable enable

namespace StructRecordGenerator
{
    public static class StringExtensions
    {
        public static bool IsNotNullOrEmpty(this string s) => !string.IsNullOrEmpty(s);
    }
}