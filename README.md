# DirectoryDuplicator
[[日本語](https://qiita.com/KyoheiOkawa/items/b53ba29a29436078b9d8)]

[![openupm](https://img.shields.io/npm/v/com.littlebigfun.addressable-importer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.littlebigfun.addressable-importer/)

This package is possible to duplicate files in directory with guid dependencies.

When duplicate directory normally, guid in duplicated file refer original directory files.

For example, such a directory below:

```
Original
-image.prefab
-sp.png
```

image.prefab has image component that set sp.png.

duplicated directory is

```
Original(copy)
-image.prefab
-sp.png
```

sprite field of image component in Original(copy)/image.prefab refer Original/sp.png.

This is inconvenient for asset template directory.

## Supported UnityVersion
2020.2 or higher

## Installation

### Install via git url
Open Window/Package Manager, and add package from git URL...

```
https://github.com/k-okawa/DirectoryDuplicator.git?path=Assets/Bg/DirectoryDuplicator
```

### Install via OpenUPM

```
openupm add com.bg.directoryduplicator
```

## How to use

Select a directory and click right mouse button to show context menu.

And then select "Bg/DuplicateDirectoryWithDependencies" menu.

That's all.

## Utility functions

[DirectoryDuplicator.cs](https://github.com/k-okawa/DirectoryDuplicator/blob/master/Assets/Bg/DirectoryDuplicator/Editor/DirectoryDuplicator.cs)

If you need to create custom editor use this package function, you can use utility functions.

```c#
/// <summary>
/// Copy directory and change guid dependencies in target directory
/// </summary>
/// <param name="originDirectory">original directory absolute path</param>
/// <param name="targetDirectory">copy destination directory absolute path</param>
/// <param name="copyExcludeDirectories">exclude sub directories that included in origin directory from copy</param>
/// <param name="progressCallback">callback of progress. returns progress and total count of file count</param>
public static Task CopyDirectoryWithDependencies(string originDirectory, string targetDirectory, string[] copyExcludeDirectories = null, Action<(int progress, int total)> progressCallback = null);

/// <summary>
/// Copy directory(sub directories included)
/// </summary>
/// <param name="originDirectory">original directory absolute path</param>
/// <param name="targetDirectory">copy destination directory absolute path</param>
/// <param name="copyExcludeDirectories">exclude sub directories that included in origin directory from copy</param>
public static void CopyDirectory(string originDirectory, string targetDirectory, string[] copyExcludeDirectories = null);

/// <summary>
/// Change guid dependencies in target directory
/// </summary>
/// <param name="originDirectory">original directory absolute path</param>
/// <param name="targetDirectory">copy destination directory absolute path</param>
/// <param name="progressCallback">callback of progress. returns progress and total count of file count</param>
public static Task ChangeGuidToNewFile(string originDirectory, string targetDirectory, Action<(int progress, int total)> progressCallback = null);
```

## Supported asset file types

```
anim, controller, overrideContoroller, prefab, mat, material, playable, asset, unity
```

This package changes guid in yaml file.

So it supports all asset yaml files.

But it needs to specify file extensions.

If short asset type, please add file extension to [DirectoryDuplicator.cs line 69](https://github.com/k-okawa/DirectoryDuplicator/blob/master/Assets/Bg/DirectoryDuplicator/Editor/DirectoryDuplicator.cs#L69).

I would appreciate you to create pull request or issue if you could.

## External Library

This package includes customized [YamlDotNet for Unity](https://assetstore.unity.com/packages/tools/integration/yamldotnet-for-unity-36292?locale=ja-JP).

Customization is change namespace, so if your project include YamlDotNet for Unity, it is not affects namespace conflict.
