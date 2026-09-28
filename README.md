<h1 align="center">Subtitle Extract for Jellyfin Plugin</h1>
<h3 align="center">
Part of the <a href="https://jellyfin.org">Jellyfin Project</a>
</h3>

<p align="center">
<img alt="Plugin Banner"
src="https://raw.githubusercontent.com/jellyfin/jellyfin-ux/master/plugins/SVG/jellyfin-plugin-subtitleextract.svg?sanitize=true"/>
<br/>
<br/>
<a href="https://github.com/jellyfin/jellyfin-plugin-subtitleextract/actions/workflows/build.yaml">
<img alt="Build Status"
src="https://img.shields.io/github/actions/workflow/status/jellyfin/jellyfin-plugin-subtitleextract/build.yaml?branch=master"/>
</a>
<a href="./LICENSE">
<img alt="GPL-3.0 License"
src="https://img.shields.io/github/license/jellyfin/jellyfin-plugin-subtitleextract.svg"/>
</a>
<a href="https://github.com/jellyfin/jellyfin-plugin-subtitleextract/releases">
<img alt="Current Release"
src="https://img.shields.io/github/release/jellyfin/jellyfin-plugin-subtitleextract.svg"/>
</a>
</p>

## About

Jellyfin extracts embedded subtitles and attachments (such as the fonts
used by ASS/SSA subtitles) from a media file the first time they are
needed during playback. For large files this can delay the start of
playback or the appearance of subtitles by a noticeable amount of time.

This plugin extracts them proactively instead, so they are already cached
when playback starts:

- **Scheduled tasks**: *Extract Subtitles* and *Extract Attachments* can be
  run manually or on a schedule from the dashboard.
- **Library scan**: optionally extract subtitles and attachments for new or
  changed media while the library is scanned.
- **Library selection**: limit subtitle and attachment extraction to
  specific libraries.
- **Subtitle selection**: extract only text subtitles, only graphical
  subtitles, or only the subtitle streams using specific codecs (for
  example only ASS/SSA).

Attachments are only extracted for media containing ASS/SSA subtitles, as
those are the only subtitles using them. Extraction runs in parallel based
on the server's library scan concurrency setting.

## Installation

[See the official documentation for install instructions][install].

## Build

1. To build this plugin you will need [.NET 10.x][dotnet].

1. Build the plugin with the following command:

   ```sh
   dotnet publish --configuration Release --output bin
   ```

1. Place the dll-file in the `plugins/subtitleextract` folder (you might
   need to create the folders) of your Jellyfin install.

## Releasing

To release the plugin we recommend [JPRM][jprm] that will build and package
the plugin. For additional context and for how to add the packaged plugin
zip to a plugin manifest see the [JPRM documentation][jprm] for more info.

## Contributing

We welcome all contributions and pull requests! If you have a larger
feature in mind please open an issue so we can discuss the implementation
before you start. In general refer to our
[contributing guidelines][contributing] for further information.

## License

This plugin's code and packages are distributed under the GNU General
Public License v3.0. See [LICENSE](./LICENSE) for more information.

[install]: https://jellyfin.org/docs/general/server/plugins/index.html#installing
[dotnet]: https://dotnet.microsoft.com/download/dotnet/10.0
[jprm]: https://github.com/oddstr13/jellyfin-plugin-repository-manager
[contributing]: https://github.com/jellyfin/.github/blob/master/CONTRIBUTING.md
