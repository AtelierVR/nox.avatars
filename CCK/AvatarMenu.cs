using System;
using UnityEngine;

namespace Nox.CCK.Avatars
{
    [CreateAssetMenu(fileName = "AvatarDescriptor", menuName = "Nox/Avatars/Avatar Menu")]
    public class AvatarMenu : ScriptableObject
    {
        public Element[] elements;
        
        [Serializable]
        public class Element
        {
            public string label;
            public LocalizedLabel[] localizedLabels;
            public Texture2D icon;
            
            public ElementType type;
            
            public string[] parameters;
            public AvatarMenu menu;
            
            public ElementAxis[] axes;
            public ElementDropdown[] dropdowns;
        }

        [Serializable]
        public class ElementAxis
        {
            public float min;
            public float max;
            public float step;
        }
        
        [Serializable]
        public class ElementDropdown
        {
            public string label;
            public LocalizedLabel[] localizedLabels;
        }
        
        [Serializable]
        public class LocalizedLabel
        {
            public string language;
            public string text;
        }

        public enum ElementType
        {
            Trigger = 0,
            Toggle = 1,
            TextInput = 2,
            Menu = 3,
            SliderOneAxis = 4,
            SliderTwoAxis = 5,
            ColorPicker = 6,
            Dropdown = 7
        }
    }
}