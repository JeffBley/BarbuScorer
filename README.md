# Barbu Scorer

A Windows desktop application for scoring the classic card game **Barbu** (also known as "King of Hearts"). Built with WPF and .NET 10.

![Barbu Scorer](Assets/BarbuPhoto.png)

## Features

- **Full Barbu scoring** for 4 players with all 7 standard contracts:
  - Nullo (No Tricks)
  - No Queens
  - Hearts (No Hearts)
  - No Last Two
  - Barbu (No King of Hearts)
  - Trumps
  - Fan Tan / Domino

- **Optional contracts**:
  - Ravage City (8th contract)
  - Chinese Poker (9th contract, requires Ravage City)

- **Doubling system**:
  - Players can double other players' scores
  - Redouble option when doubled
  - Tracks mandatory doubles (each player must double the dealer twice)

- **Game management**:
  - Auto-save after each hand
  - Load/save games
  - Score history with full scorecard
  - Edit past scores if needed

- **Customization**:
  - Classic or Modern contract names
  - Adjustable text size (Small/Medium/Large)
  - Configurable Fan Tan scoring values
  - Option to allow/disallow dealer doubling

## Requirements

- Windows 10/11
- .NET 10 Runtime (or use the self-contained executable)

## Installation

### Option 1: Download Release
Download the latest release from the [Releases](../../releases) page. The `publish` folder contains a self-contained executable that doesn't require .NET to be installed.

### Option 2: Build from Source
```powershell
# Clone the repository
git clone https://github.com/yourusername/Barbu.git
cd Barbu

# Build and run
dotnet build
dotnet run

# Or publish a self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
```

## How to Play

1. **Start a new game**: Enter player names for West, North, East, and South positions
2. **Choose a contract**: The dealer (starting with West) selects which contract to play
3. **Doubling phase**: Players can double other players (and be redoubled)
4. **Enter scores**: After the hand, enter the results
5. **Repeat**: Play continues clockwise through all players until all contracts are dealt

## Game Rules

### Standard Contracts
| Contract | Scoring |
|----------|---------|
| Nullo | -2 per trick taken |
| No Queens | -6 per queen taken |
| Hearts | -2 per heart, -6 for Ace of Hearts |
| No Last Two | -10 for 2nd last trick, -20 for last trick |
| Barbu | -20 for taking King of Hearts |
| Trumps | +5 per trick won |
| Fan Tan | 40/25/10/-10 by finish order |

### Doubling
- Any player can double any other player
- The doubled player can redouble
- Score difference is transferred between players (doubled if redoubled)

## Screenshots

*Coming soon*

## License

MIT License - Feel free to use, modify, and distribute.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
