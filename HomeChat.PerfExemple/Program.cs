using HomeChat.PerfExemple;
var monitor = new PerfMonitor();
int previousLines = 0; // Track the number of lines printed in the previous iteration

await foreach (var report in monitor.GetPerfSamples())
{
    Console.SetCursorPosition(0, 0); // Set cursor position to the top-left corner

    // Print performance information
    Console.WriteLine($"CPU {report.CpuPercentage}%\tRAM available {report.RamAvailableInMb}Mb\tRAM used {report.RamCommitedInMb}Mb");

    // Print CPU history chart
    string cpuChart = PrintChart.Generate(report.CpuHistory);
    int newLines = cpuChart.Split('\n').Length; // Count number of lines in the chart
    int diff = Math.Max(0, newLines - previousLines); // Calculate the difference in lines
    previousLines = newLines; // Update the previous line count

    Console.WriteLine(cpuChart);

    // Clear remaining lines from the previous iteration
    for (int i = 0; i < diff; i++)
    {
        Console.WriteLine(new string(' ', Console.WindowWidth - 1)); // Clear the line
        Console.CursorLeft = 0; // Move cursor to the beginning of the line
    }

    await Task.Delay(1000); // Adjust the delay as needed to control refresh rate
}
/*
var monitor = new PerfMonitor();
await foreach (var report in monitor.GetPerfSamples())
{
    Console.Clear();
    Console.WriteLine($"CPU {report.CpuPercentage}%\tRAM available {report.RamAvailableInMb}Mb\tRAM used {report.RamCommitedInMb}Mb");
    Console.WriteLine(PrintChart.Show2(report.CpuHistory));
}

*/