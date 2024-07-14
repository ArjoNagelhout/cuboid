//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Cuboid.Input
{
    /// <summary>
    /// Stack that allows a virtual keyboard to add key input events to this stack,
    ///
    /// an InputField can then read input events from this stack.
    ///
    /// This is quicker than having to interact with the InputSystem, since the key input
    /// events can't be enqueued via the public API since that is done internally in the Unity
    /// runtime (via InputSystem.QueueTextEvent())
    ///
    /// (Keyboard device implements ITextInputReceiver)
    /// </summary>
    /// <example>
    ///
    /// VirtualKeyboard.OnTextInput += (character) =>
    /// {
    ///     // handle input event here, e.g.:
    ///     text += character;
    /// };
    /// 
    /// </example>
    public sealed class VirtualKeyboard : MonoBehaviour
    {
        private static VirtualKeyboard _instance;
        public static VirtualKeyboard Instance => _instance;

        private const string k_VirtualKeyboardName = "VirtualKeyboard";

        public Keyboard Keyboard;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            Keyboard = InputSystem.AddDevice<Keyboard>(k_VirtualKeyboardName);
        }
        
        private void Start()
        {
            Keyboard.onTextInput += (character) =>
            {
                OnTextInput?.Invoke(character);
            };
        }

        public void EnterCharacter(char character)
        {
            InputSystem.QueueTextEvent(Keyboard, character);
        }

        public void PressBackspace()
        {
            OnBackspace?.Invoke();
        }

        public Action<char> OnTextInput;

        public Action OnBackspace;

        private void OnDestroy()
        {
            if (Keyboard != null)
            {
                InputSystem.RemoveDevice(Keyboard);
            }
        }
    }
}
