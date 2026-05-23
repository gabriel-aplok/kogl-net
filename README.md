<div align="center">
 <a href="https://kolpa-engine.github.io/kogl">
  <img alt="Kolpa" width="200px" src="assets/icon.png">
 </a>
 <h1>KoGL (Kolpa Graphics Library)</h1>
</div>

<p align="center">
 a backend-agnostic cross-platform lightweight graphics framework for C#.
</p>

<p align="center">
 <img alt="Part of Kolpa Engine" src="https://img.shields.io/badge/Part%20of-Kolpa%20Engine-black?style=for-the-badge">
 <!-- <img alt="Latest Release" src="https://img.shields.io/github/v/release/gabriel-aplok/kogl-net?color=black&label=Latest%20Release&style=for-the-badge">
 <img alt="Downloads" src="https://img.shields.io/github/downloads/gabriel-aplok/kogl-net/total?color=black&style=for-the-badge"> -->
 <img alt="Repository size" src="https://img.shields.io/github/repo-size/gabriel-aplok/kogl-net?color=black&style=for-the-badge">
 <img alt="License" src="https://img.shields.io/github/license/gabriel-aplok/kogl-net?color=black&style=for-the-badge">
</p>

this was heavily inspired by libraries such as [RGL](https://github.com/ColleagueRiley/RGL) and [RLGL](https://github.com/raysan5/raylib/blob/master/src/rlgl.h), and is especially suitable for prototyping, tools, graphical applications, and education.

> WARNING:
> **status:** API is subject to change.

</div>

this stills a work in progress, if you want to contribute or report any issue, feel free for open a pull request.

this is currently a port of my library I made in pure C to C#, so:

- maybe at some point I'll reset all the commits in the repository, cause there are some pretty disorganized messages.
- I'm thinking of changing the name to kgdx or kgfx to keep the old kogl-c public.

## Features

- OpenGL immediate-mode style but with modern systems behind the scenes.
- agnostic backend architecture
- simple and easy to use.

### License

This project is licensed under the unmodified zlib/libpng license. See [LICENSE](LICENSE.txt) for details.

This project uses third-party libraries via NuGet packages and project configuration files, including [Silk.NET](https://github.com/dotnet/Silk.NET), [StbImageSharp](https://github.com/StbSharp/StbImageSharp), and others for windowing, graphics, input, and file format support. Check the dependencies licenses in the project documentation for more information.
