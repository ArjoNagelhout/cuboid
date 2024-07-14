## Discontinuation notice

Due to Unity's pricing changes, I made the decision to discontinue the development of this app, as rewriting it using a different 3D graphics framework or game engine would be too costly and time intensive. 

Some of the code and design decisions inside this repository might help someone develop something similar, which is why I have decided to publish the source code under the MIT license. 

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

These are free and paid plugins retrieved from the **Unity AssetStore** and should be installed to the `Plugins` directory. 

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

- [`App.cs`](app/Assets/Scripts/Runtime/App.cs) Main class for certain properties that should be global and bindable
- [`ApplicationData.cs`](app/Assets/Scripts/Runtime/ApplicationData.cs) Bundled metadata such as build number on build
- [`Binding.cs`](app/Assets/Scripts/Runtime/Binding.cs) Utility class for binding a value to a UI element to keep the UI and data model in sync. 
- [`CacheController.cs`](app/Assets/Scripts/Runtime/CacheController.cs) Enables the user to clear the cache and to inspect its size
- [`ColorsController.cs`](app/Assets/Scripts/Runtime/ColorsController.cs) Global controller for changing the color of all selected objects
- [`Constants.cs`](app/Assets/Scripts/Runtime/Constants.cs)
- [`Layers.cs`](app/Assets/Scripts/Runtime/Layers.cs) Layers that exist in the project: 1) Default, 2) UI, 3) SpatialUI, 4) Controllers and 5) Selected
- [`UserData.cs`](app/Assets/Scripts/Runtime/UserData.cs) Alternative to Unity's built in UserSettings that serializes to Json. 

### `📁 Commands`
For storing the editing history to enable fully undoing and redoing all edits made by the user. This employs the command pattern. Commands can be nested and/or combined to create compound commands, e.g. for selecting and moving objects on click and drag. 

- [`UndoRedoController.cs`](app/Assets/Scripts/Runtime/Commands/UndoRedoController.cs)

#### Commands

- [`AddCommand.cs`](app/Assets/Scripts/Runtime/Commands/AddCommand.cs) Add RealityObject to the scene
- [`RemoveCommand.cs`](app/Assets/Scripts/Runtime/Commands/RemoveCommand.cs) Remove RealityObject from the scene
- [`SelectCommand.cs`](app/Assets/Scripts/Runtime/Commands/SelectCommand.cs) Select or deselect a set of RealityObjects
- [`SetPropertyCommand.cs`](app/Assets/Scripts/Runtime/Commands/SetPropertyCommand.cs) Set a property of a set of RealityObjects that have the same type
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
- [`PropertiesController.cs`](app/Assets/Scripts/Runtime/Document/PropertiesController.cs) Logic for rendering reflected property fields in the UI for objects that are selected in the scene
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
- [`ToolSwitcher.cs`](app/Assets/Scripts/Runtime/Tools/ToolSwitcher.cs) A quick switcher that enables the user to switch with the joystick between the most commonly used tools. 
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

Custom UI

#### `📁 Binding`

Contains UI components that are bound to specific data or data types inside the application. Ideally, only the generic versions of these UI Binding classes exist, but as UI is still sometimes defined inside Prefabs, specialized versions need to exist. These are ommitted for brevity. 

- [`BindingButton.cs`](app/Assets/Scripts/Runtime/UI/Binding/BindingButton.cs)
- [`BindingPopupButton.cs`](app/Assets/Scripts/Runtime/UI/Binding/BindingPopupButton.cs)
- [`BindingSlider.cs`](app/Assets/Scripts/Runtime/UI/Binding/BindingSlider.cs)
- [`BindingToggle.cs`](app/Assets/Scripts/Runtime/UI/Binding/BindingSlider.cs)

#### `📁 Core`

Contains custom UI components that are data driven and better than Unity UI's built in components, such as scroll views, sliders and more. 

##### Elements

- [`Button.cs`](app/Assets/Scripts/Runtime/UI/Core/Button.cs) A data driven button
- [`PopupButton.cs`](app/Assets/Scripts/Runtime/UI/Core/PopupButton.cs) A button for an enum value that on press shows a list of all enum values to pick from. 
- [`Slider.cs`](app/Assets/Scripts/Runtime/UI/Core/Slider.cs) A data driven slider
- [`Slider2D.cs`](app/Assets/Scripts/Runtime/UI/Core/Slider2D.cs) A data driven 2D slider (used by the color picker)
- [`Toggle.cs`](app/Assets/Scripts/Runtime/UI/Core/Toggle.cs) A data driven toggle
- [`InputField.cs`](app/Assets/Scripts/Runtime/UI/Core/InputField.cs) An input field, adapted from TextMeshPro as that one was broken with text selection and editing in VR. Instantiates the correct keyboard popup based on its value type (e.g. numeric or text). 
- [`ValueField.cs`](app/Assets/Scripts/Runtime/UI/Core/ValueField.cs) Depends on InputField, binds to a binding that contains a float value. 
- [`Vector2Field.cs`](app/Assets/Scripts/Runtime/UI/Core/Vector2Field.cs) Contains two `ValueField`s
- [`Vector3Field.cs`](app/Assets/Scripts/Runtime/UI/Core/Vector3Field.cs) Contains three `ValueField`s

##### Containers

- [`NavigationStack.cs`](app/Assets/Scripts/Runtime/UI/Core/NavigationStack.cs) A stack of views similar to UIKit's NavigationStack. 
- [`ScrollView.cs`](app/Assets/Scripts/Runtime/UI/Core/ScrollView.cs)
- [`ScrollViewPool.cs`](app/Assets/Scripts/Runtime/UI/Core/ScrollViewPool.cs)
- [`ScrollViewScaler.cs`](app/Assets/Scripts/Runtime/UI/Core/ScrollViewScaler.cs)

##### Menus

- [`ContextMenu.cs`](app/Assets/Scripts/Runtime/UI/Core/ContextMenu.cs) A data driven context menu that contains a set of actions that can be performed. 
- [`DialogBox.cs`](app/Assets/Scripts/Runtime/UI/Core/DialogBox.cs) Similar to a ContextMenu, but with a description and icon for the actions that are to be performed (used for a save file dialog for example)

##### Miscellaneous

- [`ColorPicker.cs`](app/Assets/Scripts/Runtime/UI/Core/ColorPicker.cs) ColorPicker with HSV and RGB sliders with value field and and a 2D area for setting for example Hue and Value at the same time. 
- [`LoadingSpinner.cs`](app/Assets/Scripts/Runtime/UI/Core/LoadingSpinner.cs) A simple rotating spinner for indicating that something is loading
- [`Notification.cs`](app/Assets/Scripts/Runtime/UI/Core/Notification.cs)
- [`Tooltip.cs`](app/Assets/Scripts/Runtime/UI/Core/Tooltip.cs)
- [`TooltipPopup.cs`](app/Assets/Scripts/Runtime/UI/Core/TooltipPopup.cs)

#### `📁 Properties`

Contains property UI elements. Property UI elements get instantiated by the `PropertiesController` (see [`📁 Document/PropertiesController.cs`](app/Assets/Scripts/Runtime/Document/PropertiesController.cs)). 

The state of the UI elements referenced by the property UI elements reflects the state of the document. Changes are propagated to the document when changed in the UI, and when the document changes, the UI changes (e.g. when performing Undo or Redo). 

Another unique property of properties (he), is that they can update the document live while for example dragging the slider, but only register the set property command on release. 

In addition, it works with multiple objects selected at the same time. 

- [`Property.cs`](app/Assets/Scripts/Runtime/UI/Properties/Property.cs) Base class
- [`BooleanProperty.cs`](app/Assets/Scripts/Runtime/UI/Properties/BooleanProperty.cs)
- [`ColorProperty.cs`](app/Assets/Scripts/Runtime/UI/Properties/ColorProperty.cs) Creates a `ColorPicker` popup on clicking on the property
- [`EnumProperty.cs`](app/Assets/Scripts/Runtime/UI/Properties/EnumProperty.cs) creates a `PopupButton` on clicking on the property
- [`FloatProperty.cs`](app/Assets/Scripts/Runtime/UI/Properties/FloatProperty.cs) contains a slider and a value field
- [`IntProperty.cs`](app/Assets/Scripts/Runtime/UI/Properties/IntProperty.cs)
- [`StringProperty.cs`](app/Assets/Scripts/Runtime/UI/Properties/StringProperty.cs)
- [`Vector2Property.cs`](app/Assets/Scripts/Runtime/UI/Properties/Vector2Property.cs) contains a `Vector2Field`
- [`Vector3Property.cs`](app/Assets/Scripts/Runtime/UI/Properties/Vector3Property.cs) contains a `Vector3Field`

#### `📁 Views`

Contains implementation for the UI for each specific view (i.e. panel). These panels can be selected in the interface via the panel buttons. 

- `📁 AssetsView` Logic for displaying asset collections and dragging and dropping RealityAssets into the scene
- `📁 DocumentsView` Logic for displaying the currently opened document, and other documents that the user has created that they could open, rename or delete. 
- `📁 Elements`
- [`ColorsViewController.cs`](app/Assets/Scripts/Runtime/UI/Views/ColorsViewController.cs) Display a colors panel with the currently active
- [`CreditsViewController.cs`](app/Assets/Scripts/Runtime/UI/Views/CreditsViewController.cs) Display credits and links to license and website
- [`PropertiesViewController.cs`](app/Assets/Scripts/Runtime/UI/Views/PropertiesViewController.cs) Display properties of the currently selected objects
- [`SettingsViewController.cs`](app/Assets/Scripts/Runtime/UI/Views/SettingsViewController.cs) Display settings
- [`ToolsViewController.cs`](app/Assets/Scripts/Runtime/UI/Views/ToolsViewController.cs) Display tools, and the properties of the selected tool, if any. 
- [`TransformProperties.cs`](app/Assets/Scripts/Runtime/UI/Views/TransformProperties.cs) Display transform properties of selected objects (translation, rotation and scale) using Vector3Fields

### `📁 Utils`

General utility functions that extend Unity's classes or mitigate some ommision in Unity. 
