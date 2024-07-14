//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// Contains whether the user is left or right handed and contains
    /// convenience methods for setting the dominant hand. 
    /// </summary>
    public class Handedness
    {
        private const string k_DominantHandPreferencesString = "dominant-hand";

        public enum Hand
        {
            RightHand = 0,
            LeftHand,
        }

        public StoredBinding<Hand> DominantHand;

        /// <summary>
        /// Please instantiate in Awake method, not in constructor
        /// </summary>
        public Handedness()
        {
            DominantHand = new("Handedness_DominantHand", Hand.RightHand);
        }

        private bool _bothHandsEnabled = false;
        /// <summary>
        /// Whether or not both hands are enabled. 
        /// </summary>
        public bool BothHandsEnabled
        {
            get => _bothHandsEnabled;
            set
            {
                _bothHandsEnabled = value;
                DominantHand.ValueChanged();
            }
        }

        public bool HandEnabled(Hand hand)
        {
            return BothHandsEnabled || DominantHand.Value == hand;
        }

        public bool IsDominantHand(Hand hand)
        {
            return DominantHand.Value == hand;
        }

        public bool IsLeftHandEnabled => HandEnabled(Hand.LeftHand);
        public bool IsRightHandEnabled => HandEnabled(Hand.RightHand);

        public bool IsRightHanded => DominantHand.Value == Hand.RightHand;
        public bool IsLeftHanded => DominantHand.Value == Hand.LeftHand;

        public Hand NonDominantHand => DominantHand.Value switch
        {
            Hand.LeftHand => Hand.RightHand,
            Hand.RightHand => Hand.LeftHand,
            _ => Hand.LeftHand
        };

        public void SwitchDominantHand()
        {
            DominantHand.Value = NonDominantHand;
        }
    }

    public static class HandExtensions
    {
        public static Handedness.Hand GetOtherHand(this Handedness.Hand hand)
        {
            return hand == Handedness.Hand.LeftHand ? Handedness.Hand.RightHand : Handedness.Hand.LeftHand;
        }
    }
}

