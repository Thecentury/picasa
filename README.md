# Remake of Picasa image viewer in F#

Built using [Avalonia](https://avaloniaui.net/) and [FuncUI](https://github.com/fsprojects/Avalonia.FuncUI).

Application consists of two parts:
- PicasaLauncher — a thin macOS .app-like wrapper built using [Mono](https://www.mono-project.com/) to be able to associate image file types with the application;
- Picasa itself — a .net 6.0 Avalonia application.

![Application screenshot](./images/Picasa-Geese.jpg)

## Keyboard shortcuts

- `Left`/`Right` — navigate between images;
- `Ctrl+Left`/`Ctrl+Right` — navigate to the first/last image in the folder;
- `[`/`]` — rotate image left/right.

## Planned features

- Ability to delete a currently displayed image.

## TODO

* [ ] Write an instruction on how to build the application and what its dependencies are.
* [ ] Create a redistributable package.

