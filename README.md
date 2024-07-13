## Discontinuation notice

Due to Unity's changes to their plans and pricing, I sadly had to make the decision to discontinue the development of this app, as it has a hard dependency on the Unity game engine. 
Some of the code and design decisions inside this repository might help someone develop something similar, which is why I have decided to make the source available. 

# Cuboid

![Screenshot 2](https://github.com/user-attachments/assets/897dc105-f319-4de7-a672-b6be4fac494c)

![Screenshot 1](https://github.com/user-attachments/assets/d39d3117-c2c5-4e40-8374-f561e879813b)

Cuboid allows you to design for the real world, in the real world. Import high quality assets, and place and manipulate them at world-scale in your environment. Reimagine your living room, your house, or an entire city! 

## Key Features

- Import any Unity Prefab or 3D model into the application via our Unity Plugin (custom MonoBehaviour scripts excluded)
- Place 3D objects in your environment on world-scale augmented reality using Passthrough.
- Built-in asset library.
- Cuboid is a local, standalone application for Meta Quest 2 and Meta Quest Pro. It does not require an internet connection or creating an account.
- Translate, rotate and scale objects using gizmos and intuitive scale bounds handles, from a distance!
- Select, cut, copy, paste, duplicate and delete objects via a context menu.
- Full undo / redo support.
- Save and load scenes to and from a local .json file, name files.
- Draw primitive shapes and change corner radius or their color via a full RGB / HSV color picker.

## Dependencies

Unity Editor version: **2021.3.27f1**

### Packages

Notable packages:
- `com.unity.nuget.newtonsoft-json` `3.2.1`
- [`com.gwiazdorrr.betterstreamingassets`](https://github.com/gwiazdorrr/BetterStreamingAssets.git)

See `Packages/manifest.json` for all packages. Other than those above, it only contains packages created and maintained by Unity. 

### Plugins

These are free and paid plugins retrieved from the **Unity AssetStore** and should be installed to the `Plugins` folder. 

- [DOTween by Demigiant](https://dotween.demigiant.com/download.php) `> 1.2.000`
- [Shapes by Freya Holmér](https://acegikmo.com/shapes/) `4.2.1`

### Oculus Package

The project depends on the [`Oculus Integration`](https://assetstore.unity.com/packages/tools/integration/oculus-integration-deprecated-82022) package `> 57.0.0`

Only the `VR` subdirectory of the package needs to be installed to the `Oculus` subdirectory. 

*Do note that this package has since been deprecated, please refer to Meta's guide for upgrading your project to the new packages.*

### Architecture

High level overview of the codebase. 

- 📁 Scripts
    - **📁 Editor**
    - **📁 Runtime**
        - **📁 Commands** `For storing the editing history to enable fully undoing and redoing all edits made by the user`
            - AddCommand
            - RemoveCommand
            - SelectCommand
            - SetPropertyCommand
            - TransformCommand
            - UndoRedoController
        - **📁 Document** `Serializable and editable data model of the 3D scene`
            - 📁 RealityAsset
                - RealityAsset `A 3D model`
                - RealityAssetCollection `A collection of 3D models`
                - RealityAssetsController `Logic for loading 3D models from disk`
            - 📁 RealityShape `A primitive shape with editable properties`
                - RoundedCuboidRenderer `The only shape currently implemented: renders a cuboid with rounded corners`
            - ClipboardController `Storing cut or copied objects for pasting into the scene`
            - PropertiesController `Logic for rendering property fields for a given RealityObject`
        - **📁 Input**
        - **📁 Rendering**
        - **📁 SpatialUI**
        - **📁 Tools**
        - **📁 UI**
        - **📁 Utils**