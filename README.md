# System Resource Monitor

System Resource Monitor is a Windows desktop dashboard written in C# and WPF. It shows live CPU usage, RAM usage, primary-disk capacity, and the processes using the most memory.

## Features

- Updates CPU, RAM, disk, and process data every two seconds.
- Searches processes by name or PID.
- Sorts every process column; memory starts sorted from highest to lowest.
- Offers manual refresh and pause/resume controls.
- Shows a details dialog when a process row is double-clicked.

## Run it

Needed .NET 9 SDK on Windows. Open PowerShell in this project folder and run:

```powershell
dotnet restore
dotnet run
```

If `dotnet` is not in your PATH, use:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" restore
& "C:\Program Files\dotnet\dotnet.exe" run
```

## Project layout

- `MainWindow.xaml` is the dashboard layout.
- `MainWindow.xaml.cs` coordinates the timer, filtering, sorting, and buttons.
- `Services/ResourceMonitorService.cs` safely reads Windows resource data.
- `Models` contains the small data objects displayed by the UI.
- `docs/INTERVIEW-NOTES.md` contains a plain-language walkthrough for interviews.

The CPU value starts at zero briefly because Windows needs two system-time samples to calculate a percentage. Some protected processes can expose only limited information which is shown as `Limited` instead of causing the monitor to fail.
