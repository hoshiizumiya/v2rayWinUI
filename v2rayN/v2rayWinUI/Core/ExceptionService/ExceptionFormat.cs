// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections;
using System.Globalization;
using System.Text;

namespace v2rayWinUI.Core.ExceptionService;

internal static class ExceptionFormat
{
    private const string SectionSeparator = "----------------------------------------";

    public static string Format(Exception exception)
    {
        return Format(new(), exception).ToString();
    }

    public static StringBuilder Format(StringBuilder builder, Exception exception)
    {
        if (exception.Data.Count > 0)
        {
            builder.AppendLine("Exception Data:");

            foreach (DictionaryEntry entry in exception.Data)
            {
                string typeName = entry.Value?.GetType().FullName ?? "null";
                builder.AppendLine(CultureInfo.CurrentCulture, $"[{typeName}] {entry.Key}:'{entry.Value}'");
            }

            builder.AppendLine(SectionSeparator);
        }

        builder.Append(exception);
        return builder;
    }
}