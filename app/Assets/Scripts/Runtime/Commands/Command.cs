// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Models;
using System.Linq;

namespace Cuboid
{
    /// <summary>
    /// <para>
    /// The <see cref="Command"/> should be inherited from by all Commands.
    /// </para>
    ///
    /// <para>
    /// Allows adding children to Commands that can be used to compose complex
    /// commands from base building blocks.
    /// </para>
    ///
    /// TODO: Implement merge support for merging multiple commands. 
    /// </summary>
    public class Command
    {
        public List<Command> Children = new List<Command>();

        /// <summary>
        /// Use this method to add a child and set the NeedsSaving and Changes properties
        ///
        /// Used for example by the SelectTool to first execute the Select command, then
        /// add the Transform command as a child. The Select command doesn't set NeedsSaving to true,
        /// so we need to set this property from the child Transform command.
        ///
        /// When undoing and redoing the commands it automatically gets the
        /// needs saving property, but when adding an already executed command it doesn't.
        /// </summary>
        public void AddChild(Command command)
        {
            if (command.NeedsSaving)
            {
                NeedsSaving = true;
            }

            if (command.Changes)
            {
                Changes = true;
            }
            Children.Add(command);
        }

        /// <summary>
        /// Whether the command changes anything.
        /// e.g. for the Selection command, sometimes the selection doesn't change.
        /// We could try merging commands, but easier is just to check with a simple boolean.
        ///
        /// The checks can then be performed inside the commands themselves. 
        /// </summary>
        public bool Changes;

        public bool NeedsSaving;

        /// <summary>
        /// The constructor should not mutate scene state.
        /// It should only be used to capture scene state and store arguments. 
        /// </summary>
        public Command() { }

        /// <summary>
        /// Execute this entire command tree.
        /// Parent is always redone before all children.
        /// Children are redone in order.
        ///
        /// Note: It checks whether it actually changes anything, if not, it gets
        /// discarded by the UndoRedoController. 
        /// </summary>
        internal void InternalDo(out bool changes, out bool needsSaving)
        {
            OnDo(out changes, out needsSaving);

            foreach (Command command in Children)
            {
                command.InternalDo(out bool childChanges, out bool childNeedsSaving);

                if (childChanges) { changes = true; }
                if (childNeedsSaving) { needsSaving = true; }
            }
            Changes = changes;
            NeedsSaving = needsSaving;
        }

        public void Do()
        {
            InternalDo(out bool changes, out bool needsSaving);
        }

        /// <summary>
        /// Undo the entire command tree.
        /// Parent is always undone after all children. 
        /// Children are undone in reverse order.
        /// </summary>
        internal void Undo()
        {
            foreach (Command command in Children.AsEnumerable().Reverse())
            {
                command.Undo();
            }
            OnUndo();
        }

        virtual protected void OnDo(out bool changes, out bool needsSaving)
        {
            changes = false;
            needsSaving = false;
        }

        virtual protected void OnUndo() { }
    }
}

