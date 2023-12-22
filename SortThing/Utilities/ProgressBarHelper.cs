using System;
using ShellProgressBar;

namespace SortThing.Utilities;

public static class ProgressBarHelper
{
    public static ProgressBar CreateProgressBar(int maxTicks, string message)
    {
        ProgressBarOptions options = new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.Green,
            ForegroundColorError = ConsoleColor.Red,
            BackgroundColor = ConsoleColor.DarkGray,
            /*ProgressBarOnBottom = true,*/
            BackgroundCharacter = '\u2593'
        };

        return new ProgressBar(maxTicks, message, options);
    }
}