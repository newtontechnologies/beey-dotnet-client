YTtoBeey
===

# Usage
Just run YTtoBeey.exe and you will see.

# Quick help

* **YTtoBeey is not working anymore!** Try issuing `youtube-dl.exe -U` to update the downloader.

# Using on Linux
1. Compile for linux using `dotnet publish -c release -r ubuntu.18.04-x64`
2. In the output directory run:
	1. `rm youtube-dl.exe`
	2. `curl -L https://yt-dl.org/downloads/latest/youtube-dl -o youtube-dl`
	3. `chmod a+rx ./youtube-dl`
4. In `Settings.xml`, change `youtube-dl.exe` to `youtube-dl`
5. *Python installation might be required. (any version)*