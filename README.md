# YouTube Live Stream Plugin for InfoPanel

This plugin extracts the HLS stream URL from a YouTube Live Stream link and provides it to InfoPanel's image item. InfoPanel can then play the stream as video inside the display panel.

## Requirements

- **.NET 8.0**
- **InfoPanel 1.3.x** (or version with HttpImageDisplayItem and m3u8 video support)
- **yt-dlp** - Must be installed and on PATH

### Installing yt-dlp

- **Windows (winget):** `winget install yt-dlp.yt-dlp`
- **Manual:** Download from [yt-dlp releases](https://github.com/yt-dlp/yt-dlp/releases) and add to PATH

## Installation

### Option A: Import via InfoPanel (recommended)

1. Run the pack script to create the installable package:
   ```powershell
   cd C:\Users\black\YouTubeLivePlugin
   .\pack.ps1
   ```
2. In InfoPanel, go to **Plugins** and use **Add Plugin from ZIP**
3. Select `InfoPanel.YouTubeLivePlugin.zip` (InfoPanel only processes `.zip` files matching `InfoPanel.*.zip`)

### Option B: Manual copy

1. Build the plugin project (Release mode recommended)
2. Copy the output to InfoPanel's plugins folder:
   - **User plugins:** `%APPDATA%\Roaming\InfoPanel\Plugins\YouTubeLivePlugin\`
   - **Development:** `[InfoPanel Directory]\Plugins\YouTubeLivePlugin\`

3. Copy these files:
   - `YouTubeLivePlugin.dll`
   - `PluginInfo.ini`
   - `config.ini`

## Configuration

1. Open `config.ini` in the plugin folder
2. Add your YouTube Live Stream URL under the `[YouTube]` section:

```ini
[YouTube]
Url=https://youtube.com/live/STREAM_ID
```

Supported URL formats:
- `https://youtube.com/live/STREAM_ID`
- `https://youtube.com/watch?v=VIDEO_ID`
- `https://youtu.be/VIDEO_ID`

3. Restart InfoPanel or reload the plugin

## Display Setup in InfoPanel

To display the stream in your panel:

1. Add an **Image** item to your display profile
2. Set the image source to **URL** (or use **HttpImage** / "Image from sensor" if your InfoPanel version offers it)
3. Set **Sensor Type** to **Plugin**
4. Set **Plugin Sensor Id** to: `/youtube-live/youtube/stream-url`
5. Adjust size and position as desired

The stream will play as video inside the panel. HLS URLs are cached for 30 minutes to avoid excessive yt-dlp calls.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "yt-dlp not found" | Install yt-dlp and ensure it's on your system PATH |
| "Failed to extract stream URL" | Stream may be offline; check the URL in a browser |
| Black/blank display | Verify InfoPanel supports m3u8 (1.3.x+); stream may be starting |
| "Invalid YouTube URL" | Use one of the supported URL formats |

## Plugin Sensor IDs

- **Stream URL:** `/youtube-live/youtube/stream-url`
- **Status:** `/youtube-live/youtube/status`

## License

MIT License

Copyright (c) 2026 fweepa

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
