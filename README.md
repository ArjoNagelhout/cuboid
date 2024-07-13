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

## Architecture

All code is located in the following directories:

```
📁 app/
└── 📁 Scripts/
    ├── 📁 Editor/
    └── 📁 Runtime/
        ├── 📁 Commands/
        ├── 📁 Document/
        ├── 📁 Input/
        ├── 📁 Rendering/
        ├── 📁 SpatialUI/
        ├── 📁 Tools/
        ├── 📁 UI/
        └── 📁 Utils/
```

### `Commands`
For storing the editing history to enable fully undoing and redoing all edits made by the user.

- AddCommand
- RemoveCommand
- SelectCommand
- SetPropertyCommand
- TransformCommand
- UndoRedoController

### `Document`
Serializable and editable data model of the 3D scene

- 📁 RealityAsset
    - RealityAsset `A 3D model`
    - RealityAssetCollection `A collection of 3D models`
    - RealityAssetsController `Logic for loading 3D models from disk`
- 📁 RealityShape `A primitive shape with editable properties`
    - RoundedCuboidRenderer `Renders a cuboid`
- ClipboardController
- PropertiesController `Logic for rendering reflected property fields for objects that are selected in the scene`
- RealityDocument `Main entrypoint for the data the user can create and store (contains a scene of objects)`
- RealityDocumentController `Storing and loading a RealityDocument from disk`
- RealityObject `A selectable object inside the RealityDocument`
- RealitySceneController `Rendering a scene and instantiating RealityObjects when loaded`
- Selection `A simple hashset`
- SelectionController `Selection, transform updates and bounds of selected objects`
- ThumbnailProvider `Cache layer to avoid retrieving thumbnails from the AssetBundle each time`
- TransformData

### `Input`
Handling of spatial input events from XR controllers. 

- 📁 Core
    - Handedness `Handle right and left handedness`
    - RayInteractor
    - SpatialGraphicRaycaster `Raycasting with UI`
    - SpatialInputModule `Handling of input events that retains focus for either UI interactions or 3D scene interactions. Handles stabilization and smoothing of the spatial pointer`
    - SpatialPhysicsRaycaster `Raycasting with 3D scene`
    - SpatialPointerConfiguration `Dictates how the spatial pointer should be moved by the user`
    - SpatialPointerEvents `Events that spatial UI can listen to to create interactable spatial UI`
    - SpatialPointerReticle `Rendering of the spatial pointer`
- 📁 Keyboard `Custom VR keyboard implementation with numeric support`
- 📁 XRController `Handling buttons and rendering of controller`
- InputController `Mapping button to high level actions`

### `Rendering`
- PassthroughController `Turning Passthrough on or off`
- ScreenshotController `For capturing thumbnails of the scene when saving a document`
- SelectionOutlineRendererFeature

### `SpatialUI`

### `Tools`

### `UI`

### `Utils`