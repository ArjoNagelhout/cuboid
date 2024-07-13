## Discontinuation notice

Due to Unity's pricing changes, I made the decision to discontinue the development of this app, as rewriting it using a different 3D graphics framework or game engine would be too costly and time intensive. 

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

- [DOTween by Demigiant](https://dotween.demigiant.com/download.php) `> 1.2.000` (free)
- [Shapes by Freya Holmér](https://acegikmo.com/shapes/) `4.2.1` (⚠️ paid)

### Oculus Package

The project depends on the [`Oculus Integration`](https://assetstore.unity.com/packages/tools/integration/oculus-integration-deprecated-82022) package `> 57.0.0`

Only the `VR` subdirectory of the package needs to be installed to the `Oculus` subdirectory. 

*⚠️ Note that the Oculus Integration package has since been deprecated, please refer to Meta's guide for upgrading your project to the new packages.*

## Codebase Architecture

### Unity Scenes and Prefabs

There are two main Unity Scenes that compose the app. These are the main entrypoints of the application: 

1. [`AppEditor`](app/Assets/Scenes/AppEditor.unity) The main scene for testing the application directly inside the Editor with the `XR Device Simulator`. 
2. [`AppRuntime`](app/Assets/Scenes/AppRuntime.unity) The main scene that gets run on the XR device, which does *not* contain the simulator. 

Both scenes contain the [`App`](app/Assets/Prefabs/App.prefab) prefab, which contain prefabs for each *controller* described below (e.g. `UndoRedoController`, `RealitySceneController` and `ToolController`). 

The only difference is that the `AppEditor` scene contains the [`EditorXRRig`](app/Assets/Prefabs/Input/EditorXRRig.prefab), and `AppRuntime` contains the [`RuntimeXRRig`](app/Assets/Prefabs/Input/RuntimeXRRig.prefab). 


### Scripts

All code is located in the following directories:

```
📁 app/
└── 📁 Assets/
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

Top level scripts inside `📁 Runtime`:

- [`App.cs`](app/Assets/Scripts/Runtime/App.cs) Main 
- [`ApplicationData.cs`](app/Assets/Scripts/Runtime/ApplicationData.cs)
- [`Binding.cs`](app/Assets/Scripts/Runtime/Binding.cs)
- [`CacheController.cs`](app/Assets/Scripts/Runtime/CacheController.cs)
- [`ColorsController.cs`](app/Assets/Scripts/Runtime/ColorsController.cs)
- [`Constants.cs`](app/Assets/Scripts/Runtime/Constants.cs)
- [`Layers.cs`](app/Assets/Scripts/Runtime/Layers.cs)
- [`UserData.cs`](app/Assets/Scripts/Runtime/UserData.cs)

### `📁 Commands`
For storing the editing history to enable fully undoing and redoing all edits made by the user. This employs the command pattern. Commands can be nested and/or combined to create compound commands, e.g. for selecting and moving objects on click and drag. 

- [`UndoRedoController.cs`](app/Assets/Scripts/Runtime/Commands/UndoRedoController.cs)

#### Commands

- [`AddCommand.cs`](app/Assets/Scripts/Runtime/Commands/AddCommand.cs) Add RealityObject to the scene
- [`RemoveCommand.cs`](app/Assets/Scripts/Runtime/Commands/RemoveCommand.cs) Remove RealityObject from the scene
- [`SelectCommand.cs`](app/Assets/Scripts/Runtime/Commands/SelectCommand.cs) Select or deselect a set of RealityObjects
- [`SetPropertyCommand.cs`](app/Assets/Scripts/Runtime/Commands/SetPropertyCommand.cs) Set a property of a set of RealityObjects with the same type
- [`TransformCommand.cs`](app/Assets/Scripts/Runtime/Commands/TransformCommand.cs) Transform a set of objects using a TRS matrix transform

### `📁 Document`

Serializable and editable data model of a 3D scene. A `RealityDocument` is the data model that gets saved and loaded to and from disk. A `RealityDocument` contains a `RealityScene`, which in its turn contains a set of `RealityObject`s. These `RealityObject`s can have different types, such as a 3D asset, or a primitive shape. 

3D assets are not stored inside the `RealityDocument` but stored as a reference to a `RealityAssetCollection`, which wraps a Unity AssetBundle. These `RealityAssetCollection`s are created with [`com.cuboid.unity-plugin`](https://github.com/ShapeReality/com.cuboid.unity-plugin). 

#### Data model

- [`RealityDocument.cs`](app/Assets/Scripts/Runtime/Document/RealityDocument.cs) Main data model
- [`TransformData.cs`](app/Assets/Scripts/Runtime/Document/TransformData.cs)
- [`RealityObject.cs`](app/Assets/Scripts/Runtime/Document/RealityObject.cs) A selectable object inside the RealityDocument
- `📁 RealityAsset`
    - [`RealityAsset.cs`](app/Assets/Scripts/Runtime/Document/RealityAsset/RealityAsset.cs) A 3D model
    - [`RealityAssetCollection`](app/Assets/Scripts/Runtime/Document/RealityAsset/RealityAssetCollection.cs) A collection of 3D models`
- `📁 RealityShape` A primitive shape with editable properties
    - [`RoundedCuboidRenderer.cs`](app/Assets/Scripts/Runtime/Document/RealityShape/RoundedCuboidRenderer.cs) Renders a cuboid
- [`Selection.cs`](app/Assets/Scripts/Runtime/Document/Selection.cs) A simple hashset of objects

#### Controllers

- [`📁 RealityAsset/RealityAssetsController.cs`](app/Assets/Scripts/Runtime/Document/RealityAsset/RealityAssetsController.cs) Logic for loading 3D models from disk
- [`ClipboardController.cs`](app/Assets/Scripts/Runtime/Document/ClipboardController.cs) Stores cut or copied objects
- [`PropertiesController.cs`](app/Assets/Scripts/Runtime/Document/PropertiesController.cs) Logic for rendering reflected property fields for objects that are selected in the scene
- [`RealityDocumentController.cs`](app/Assets/Scripts/Runtime/Document/RealityDocumentController.cs) Storing and loading a RealityDocument from disk
- [`RealitySceneController`](app/Assets/Scripts/Runtime/Document/RealitySceneController.cs) Rendering a scene and instantiating RealityObjects when loaded
- [`SelectionController.cs`](app/Assets/Scripts/Runtime/Document/SelectionController.cs) Selection, transform updates and bounds of selected objects
- [`ThumbnailProvider.cs`](app/Assets/Scripts/Runtime/Document/ThumbnailProvider.cs) Cache layer to avoid retrieving thumbnails from the AssetBundle each time

### `📁 Input`

Handling of spatial input events from XR controllers. Part of this is adopted and modified from the XR Interaction Toolkit, as the XR Interaction Toolkit proved insufficient for achieving the exact interactions expected in a design application. 

- `📁 Core`
    - [`Handedness.cs`](app/Assets/Scripts/Runtime/Input/Core/Handedness.cs) Handle left- and right-handedness
    - [`RayInteractor.cs`](app/Assets/Scripts/Runtime/Input/Core/RayInteractor.cs)
    - [`SpatialGraphicRaycaster.cs`](app/Assets/Scripts/Runtime/Input/Core/SpatialGraphicRaycaster.cs) Raycasting with UI
    - [`SpatialInputModule.cs`](app/Assets/Scripts/Runtime/Input/Core/SpatialInputModule.cs) Handling of input events that retains focus for either UI interactions or 3D scene interactions. Handles stabilization and smoothing of the spatial pointer
    - [`SpatialPhysicsRaycaster.cs`](app/Assets/Scripts/Runtime/Input/Core/SpatialPhysicsRaycaster.cs) Raycasting with 3D scene
    - [`SpatialPointerConfiguration.cs`](app/Assets/Scripts/Runtime/Input/Core/SpatialPointerConfiguration.cs) Dictates how the spatial pointer should be moved by the user
    - [`SpatialPointerEvents.cs`](app/Assets/Scripts/Runtime/Input/Core/SpatialPointerEvents.cs) Events that spatial UI can listen to to create interactable spatial UI
    - [`SpatialPointerReticle.cs`](app/Assets/Scripts/Runtime/Input/Core/SpatialPointerReticle.cs) Rendering of the spatial pointer
- `📁 Keyboard` Custom VR keyboard implementation with numeric support
- `📁 XRController` Handling buttons and rendering of controller
- [`InputController.cs`](app/Assets/Scripts/Runtime/Input/InputController.cs) Mapping button to high level actions

### `📁 Rendering`

Not much to see here, as Unity handles all rendering. 

- [`PassthroughController.cs`](app/Assets/Scripts/Runtime/Rendering/PassthroughController.cs) Turning Passthrough on or off
- [`ScreenshotController.cs`](app/Assets/Scripts/Runtime/Rendering/ScreenshotController.cs) For capturing thumbnails of the scene when saving a document
- [`SelectionOutlineRendererFeature.cs`](app/Assets/Scripts/Runtime/Rendering/SelectionOutlineRendererFeature.cs) Custom URP render feature that renders selected objects' outlines

### `📁 SpatialUI`

- [`SpatialContextMenu.cs`](app/Assets/Scripts/Runtime/SpatialUI/SpatialContextMenu.cs) A simple menu that moves in front of the view of the user
- [`Visuals.cs`](app/Assets/Scripts/Runtime/SpatialUI/Visuals.cs) Show origin and grid in scene

#### Handles

The handles defined in SpatialUI purely contain data and implement the interfaces defined in [`SpatialPointerEvents.cs`](app/Assets/Scripts/Runtime/Input/Core/SpatialPointerEvents.cs) in `📁 Input`. 

Calculating the new position of the *handle* on moving the *spatial pointer* is performed by the tools in `📁 Tools`. These tools are also responsible for instantiating these handles. 

- [`Handle.cs`](app/Assets/Scripts/Runtime/SpatialUI/Handle.cs) Base class that implements the interfaces defined in [`SpatialPointerEvents.cs`](app/Assets/Scripts/Runtime/Input/Core/SpatialPointerEvents.cs)
- [`AxisHandle.cs`](app/Assets/Scripts/Runtime/SpatialUI/AxisHandle.cs) Contains additional data for which axis (x, y or z) this handle would edit
- [`TranslateHandle.cs`](app/Assets/Scripts/Runtime/SpatialUI/TranslateHandle.cs) Contains additional data for whether the handle is a plane or axis handle
- [`SelectionBoundsHandle.cs`](app/Assets/Scripts/Runtime/SpatialUI/SelectionBoundsHandle.cs) Handle for the corner, edge or face of a selection bounds

### `📁 Tools`

- [`ToolController.cs`](app/Assets/Scripts/Runtime/Tools/ToolController.cs) Instantiates the tool prefab based on the selected tool
- [`ToolSwitcher.cs`](app/Assets/Scripts/Runtime/Tools/ToolSwitcher.cs) A quick switcher with the joystick between the most commonly used tools. 
- [`ModifiersController.cs`](app/Assets/Scripts/Runtime/Tools/ModifiersController.cs) Listens to buttons on the non-dominant hand to activate the `Shift` or `Option` modifiers
- [`OutsideUIBehaviour.cs`](app/Assets/Scripts/Runtime/Tools/OutsideUIBehaviour.cs) Registers listening to events (e.g. when the user clicks or drags) for outside the UI *and* outside spatial UI. 

#### Selection

- [`DefaultSelectBehaviour.cs`](app/Assets/Scripts/Runtime/Tools/DefaultSelectBehaviour.cs) Behaviour for selecting and moving objects when clicking and dragging an object. Depends on `OutsideUIBehaviour`. All handle tools use this behaviour. 
- [`SelectTool.cs`](app/Assets/Scripts/Runtime/Tools/SelectTool.cs) Uses `DefaultSelectBehaviour`. 

#### Handle tools

- [`AxisHandleTool.cs`](app/Assets/Scripts/Runtime/Tools/AxisHandleTool.cs) Base class for a tool that instantiates handles. Uses `DefaultSelectBehaviour`
- [`TranslateTool.cs`](app/Assets/Scripts/Runtime/Tools/TranslateTool.cs) Translate the selected objects along their local x, y or z axis, or their x, y or z *plane*. 
- [`RotateTool.cs`](app/Assets/Scripts/Runtime/Tools/RotateTool.cs) Rotate the selected objects around their local x, y or z axis. 
- [`ScaleTool.cs`](app/Assets/Scripts/Runtime/Tools/ScaleTool.cs) Scale the selected objects along their local x, y or z axis
- [`SelectionBoundsTool.cs`](app/Assets/Scripts/Runtime/Tools/SelectionBoundsTool.cs) Scale the selected objects by dragging the corners, edges or faces of the selection bounds, similar to how Adobe Photoshop or Adobe Illustrator has a selection box around the selected items. 

#### Draw tools

- [`DrawShapeTool.cs`](app/Assets/Scripts/Runtime/Tools/DrawShapeTool.cs) Depends on `OutsideUIBehaviour`. For drawing a primitive shape (see [`RealityShape.cs`](app/Assets/Scripts/Runtime/Document/RealityShape/RealityShape.cs) in `📁 Document`) in 3D space. 

### `📁 UI`

### `📁 Utils`