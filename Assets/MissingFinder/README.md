# Unity Missing Finder

This package provides the menu items for finding missing components and references in your Unity project.

![Demo](https://user-images.githubusercontent.com/5264444/221176837-3d9cef1f-16d9-469b-9e46-7a047ef3ad58.png)

## 日本語による説明 / Explanation in Japanese

[Unity プロジェクト内の Missing を検索するツールを公開しました](https://blog.gigacreation.jp/entry/2023/02/24/225454)

## Usage

After installation, `Tools/GIGA CREATION/Missing Finder/` will be added to the menu. You can execute the following commands:

- `Find Missing in Current Scene`
    - Find missing components and references in the currently open scene.
- `Find Missing in Enabled Scenes`
    - Find missing components and references in the enabled scenes -- added scenes to `Scenes In Build` in the Building Settings window.
- `Find Missing in All Scenes`
    - Find missing components and references in all scenes included in the project.
- `Find Missing in Current Prefab Stage`
    - Find missing components and references in the currently open Prefab. This command is available only in Prefab mode.
- `Find Missing in All Prefab Assets`
    - Find missing components and references in all Prefabs included in the project.

## Installation

### Package Manager

- `https://github.com/gigacreation/UnityMissingFinder.git?path=Assets/MissingFinder`

### Manual

- Copy `Assets/MissingFinder/` to your project.
