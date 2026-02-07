# Fortnite Replay Parser GUI

## Description

A web-based GUI application that parses Fortnite `.replay` files and extracts match statistics. Built with ASP.NET Core (C#) on the backend and vanilla HTML/CSS/JavaScript on the frontend, it runs locally at `http://localhost:12345`.

### Features

- Upload and parse Fortnite `.replay` files via a browser-based interface
- View match start/end times and duration
- Browse all players in a match (with bot/human distinction)
- Display player eliminations with timestamps
- Show player cosmetics (skin names) retrieved from [Fortnite-API](https://fortnite-api.com/)
- Adjust elimination timestamps with a time offset for video recording synchronization
- Display local system information (OS, CPU, GPU, RAM, resolution)
- Export full replay data as JSON

## Dependencies

### Runtime

- [.NET 9.0 SDK](https://dotnet.microsoft.com/) or later

### NuGet Packages

| Package | Version | Description |
|---------|---------|-------------|
| [FortniteReplayReader](https://github.com/Shiqan/FortniteReplayReader) | 2.4.0 | Reads and parses Fortnite `.replay` binary files |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | 13.0.1 | JSON serialization/deserialization |
| [Scriban](https://github.com/scriban/scriban) | 6.3.0 | Lightweight text templating engine |

### External APIs

- [Fortnite-API](https://fortnite-api.com/) — Used to retrieve cosmetic/skin display names

### Development / Testing

| Package | Version | Description |
|---------|---------|-------------|
| [xUnit](https://xunit.net/) | 2.9.3 | Unit testing framework |
| [RichardSzalay.MockHttp](https://github.com/richardszalay/mockhttp) | 7.0.0 | Mock HTTP handler for unit tests |

## Setup

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later
- Windows (system information retrieval uses PowerShell)

### Build and Run

Clone the repository and publish:

```bash
git clone https://github.com/Kumapapa2012/Fortnite_Replay_Parser_GUI.git
cd Fortnite_Replay_Parser_GUI
dotnet publish
```

Run the executable:

```bash
.\bin\Release\net9.0\publish\Fortnite_Replay_Parser_GUI.exe
```

Alternatively, open `Fortnite_Replay_Parser_GUI.sln` in Visual Studio 2022 or later and build/run from the IDE.

### Usage

1. Open `http://localhost:12345` in your browser.
2. Click the file selector and choose a `.replay` file.
3. Select a player from the dropdown list.
4. View the parsed match result including eliminations, placement, and system info.
5. Adjust the time offset (positive or negative seconds) to shift elimination timestamps for video editing.
6. Click **Save Replay Data as JSON** to export the full replay data.

### Running Tests

```bash
dotnet test
```

## Licence

This project is licensed under the [GNU Affero General Public License v3.0 (AGPL-3.0)](LICENSE.txt).
