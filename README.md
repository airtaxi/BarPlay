# BarPlay

🌐 [한국어](README.ko.md)

BarPlay is a lightweight Windows taskbar widget that shows media playback information and controls on your Windows 11 taskbar.
It integrates as a deskband, so you can view the currently playing media and control playback without switching windows.

[![Download from Microsoft Store](https://get.microsoft.com/images/en-US%20dark.svg)](https://apps.microsoft.com/detail/9N841L4XR28R)

![BarPlay Showcase](.github/Showcase.jpg)

## Features

- Integrates into the Windows 11 taskbar as a deskband widget
- Shows the current playing media thumbnail, title, and description
- Click the artwork to jump to the currently playing app
- Previous, Play/Pause, and Next transport controls
- Seek slider in an expanded flyout menu
- Works with any app that supports Windows System Media Transport Controls (Spotify, web browsers, YouTube, etc.)
- Option to run at system startup (auto-start)
- Compiled with NativeAOT for fast startup and low resource use
- Multilingual UI (English, Korean, Japanese, Chinese Simplified, Chinese Traditional)

## Libraries Used

- [Microsoft.WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK) - WinUI 3 app platform and Windows integration
- [Deskband11Lib](https://github.com/airtaxi/Deskband11Lib) - Windows 11 taskbar widget integration
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM architecture and source generators
- [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/runtime) - Dependency injection container

## License

This project is distributed under the [MIT License](LICENSE.txt).

## Author

**Howon Lee** ([airtaxi](https://github.com/airtaxi))
