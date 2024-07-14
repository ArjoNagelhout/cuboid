//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class UndoRedoButton : BindingButton<UndoRedoData>
    {
        public enum AssociatedAction
        {
            Undo,
            Redo
        }

        [SerializeField] private AssociatedAction _associatedAction;

        protected override void OnPressed()
        {
            switch (_associatedAction)
            {
                case AssociatedAction.Undo:
                    UndoRedoController.Instance.Undo();
                    break;
                case AssociatedAction.Redo:
                    UndoRedoController.Instance.Redo();
                    break;
            }
        }

        protected override IBinding<UndoRedoData> GetBinding() => UndoRedoController.Instance.UndoRedoData;

        protected override void OnValueChanged(UndoRedoData data)
        {
            switch (_associatedAction)
            {
                case AssociatedAction.Undo:
                    _button.Disabled = !data.CanUndo;
                    break;
                case AssociatedAction.Redo:
                    _button.Disabled = !data.CanRedo;
                    break;
            }
        }
    }
}
