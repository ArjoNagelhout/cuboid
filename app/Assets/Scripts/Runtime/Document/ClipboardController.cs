//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Cuboid.Utils;

namespace Cuboid
{
    /// <summary>
    /// Support for cutting, copying and pasting reality objects
    /// </summary>
    public class ClipboardController : MonoBehaviour
    {
        private static ClipboardController _instance;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private List<RealityObjectData> _clipboard;

        public static bool CanPaste => _instance._clipboard != null && _instance._clipboard.Count > 0;

        /// <summary>
        /// Cuts the currently selected objects and adds this to the undo stack
        /// </summary>
        public static void Cut()
        {
            // make sure the selection controller has objects
            Selection selection = SelectionController.Instance.Selection.Value;

            if (!selection.ContainsObjects) { return; }
            _instance.StoreSelectionInClipboard();

            RemoveCommand removeCommand = new RemoveCommand(
                RealityDocumentController.Instance,
                RealitySceneController.Instance,
                SelectionController.Instance,
                selection.SelectedRealityObjects,
                RealitySceneController.Instance.OpenedRealitySceneIndex);

            UndoRedoController.Instance.Execute(removeCommand);
        }

        /// <summary>
        /// Pastes the given objects into the scene. 
        /// </summary>
        /// <param name="objects"></param>
        private static void PasteObjectsIntoScene(List<RealityObjectData> objects, Vector3? pressedPosition = null)
        {
            foreach (RealityObjectData realityObjectData in objects)
            {
                realityObjectData.Guid = System.Guid.NewGuid();
            }

            if (pressedPosition.HasValue)
            {
                Debug.Log($"pasted at {pressedPosition.Value}");
                // offset the transforms of all objects to be at the position of where the user has pressed
            }

            AddCommand addCommand = new AddCommand(
                RealityDocumentController.Instance,
                RealitySceneController.Instance,
                RealitySceneController.Instance.OpenedRealitySceneIndex,
                objects);

            SelectCommand selectCommand = new SelectCommand(
                SelectionController.Instance,
                objects,
                SelectCommand.SelectOperation.SetTo);

            // children are executed after the parent, so we add the select command as a child. 
            addCommand.Children.Add(selectCommand);

            UndoRedoController.Instance.Execute(addCommand);

            // make sure to deep copy again so as to not change the variables of the previously pasted objects into the scene
            _instance._clipboard = DeepCopy(_instance._clipboard);
        }

        public static void Duplicate()
        {
            List<RealityObjectData> objects = DeepCopyFromSelection();
            PasteObjectsIntoScene(objects);
        }

        /// <summary>
        /// Copies
        /// </summary>
        /// <param name="objects"></param>
        public static void Copy()
        {
            // make sure the selection controller has objects
            Selection selection = SelectionController.Instance.Selection.Value;

            if (!selection.ContainsObjects) { return; }
            _instance.StoreSelectionInClipboard();
        }

        /// <summary>
        /// Pastes the objects that are in the 
        /// </summary>
        public static void Paste(Vector3? pressedPosition = null)
        {
            if (!CanPaste) { return; } // make sure that there are actually objects in the clipboard.
            PasteObjectsIntoScene(_instance._clipboard, pressedPosition);
        }

        /// <summary>
        /// Performs a deep copy via serialization
        /// </summary>
        private void StoreSelectionInClipboard()
        {
            _clipboard = DeepCopyFromSelection();
        }

        private static List<RealityObjectData> DeepCopyFromSelection()
        {
            List<RealityObjectData> selectedObjects = SelectionController.Instance.Selection.Value.SelectedRealityObjects.ToList();
            return DeepCopy(selectedObjects);
        }

        private static List<RealityObjectData> DeepCopy(List<RealityObjectData> objects)
        {
            // TODO: This could be made more elegant by not relying on Json serializer, but for now it suffices
            // because we don't have to deal with properly copying all Bindings (since those are reference types)
            // 
            // see https://stackoverflow.com/questions/129389/how-do-you-do-a-deep-copy-of-an-object-in-net
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            string json = JsonConvert.SerializeObject(objects, Formatting.Indented, settings);
            //Debug.Log(json);
            List<RealityObjectData> copiedObjects = JsonConvert.DeserializeObject<List<RealityObjectData>>(json, settings);

            return copiedObjects;
        }
    }
}
