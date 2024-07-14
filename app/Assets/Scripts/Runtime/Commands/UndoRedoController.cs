//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// <para>Contains Undo and Redo stacks with <see cref="Command"/>s.</para>
    ///
    /// <para>This, to store the history of edits made in the <see cref="Models.OldRealityScene"/></para>
    ///
    /// <para>To execute a <see cref="Command"/>, call <see cref="Execute(Command)"/>.
    /// The <see cref="Command"/> then gets added to the Undo stack.</para>
    /// 
    /// </summary>
    public sealed class UndoRedoController : MonoBehaviour
    {
        // Singleton implementation
        private static UndoRedoController _instance;
        public static UndoRedoController Instance => _instance;

        [NonSerialized] public Binding<UndoRedoData> UndoRedoData = new Binding<UndoRedoData>();

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        /// <summary>
        /// Clear the Undo and Redo stacks.
        /// </summary>
        public void ClearStacks()
        {
            UndoRedoData.Value.Clear();
            UndoRedoData.ValueChanged();
        }

        public void SetLastStackCount()
        {
            UndoRedoData.Value._lastStackCount = UndoRedoData.Value.UndoStack.Count;
            UndoRedoData.ValueChanged();
        }

        /// <summary>
        /// Performs a new command
        /// </summary>
        /// <param name="command"></param>
        public void Execute(Command command)
        {
            //Debug.Log($"Execute {command}");
            command.Do();
            Add(command);
        }

        /// <summary>
        /// Adds the command to the Undo stack.
        /// Should be used only if the command is already executed
        /// (e.g. when having to combine multiple commands). 
        /// </summary>
        /// <param name="command"></param>
        public void Add(Command command)
        {
            //Debug.Log($"Add {command}");

            UndoRedoData data = UndoRedoData.Value;

            if (!command.Changes) { return; }

            data.UndoStack.Push(command);
            data.RedoStack.Clear();

            UndoRedoData.ValueChanged();
        }

        /// <summary>
        /// Undoes the last command on the undo stack
        /// </summary>
        public void Undo()
        {
            UndoRedoData data = UndoRedoData.Value;
            // Safety check: Don't undo if there is nothing to undo
            if (!data.CanUndo) { return; }

            Command command = data.UndoStack.Pop();
            command.Undo();
            data.RedoStack.Push(command);

            UndoRedoData.ValueChanged();
        }

        /// <summary>
        /// Redoes the last command on the redo stack
        /// </summary>
        public void Redo()
        {
            UndoRedoData data = UndoRedoData.Value;
            if (!data.CanRedo) { return; }

            Command command = data.RedoStack.Pop();
            command.Do();
            data.UndoStack.Push(command);

            UndoRedoData.ValueChanged();
        }
    }

    public class UndoRedoData
    {
        public Stack<Command> UndoStack = new Stack<Command>();
        public Stack<Command> RedoStack = new Stack<Command>();

        public bool CanUndo => UndoStack.Count > 0;
        public bool CanRedo => RedoStack.Count > 0;

        internal int _lastStackCount;

        public bool NeedsSaving
        {
            get
            {
                if (UndoStack.Count != _lastStackCount)
                {
                    IEnumerable<Command> newCommands = null;
                    if (UndoStack.Count > _lastStackCount)
                    {
                        newCommands = UndoStack.Take(UndoStack.Count - _lastStackCount);
                    }
                    else
                    {
                        newCommands = RedoStack.Take(_lastStackCount - UndoStack.Count);
                    }
                    return newCommands.Any(command => command.NeedsSaving);
                }
                return false;
            }
        }

        public void Clear()
        {
            UndoStack.Clear();
            RedoStack.Clear();
            _lastStackCount = 0;
        }
    }
}

