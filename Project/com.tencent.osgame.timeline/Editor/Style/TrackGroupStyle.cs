using UnityEngine;

namespace TimelineEditor
{
    public class TrackGroupStyles
    {
        private GUIStyle inspectorIcon;
        private GUIStyle actorGroupIcon;
        private GUIStyle addIcon;
        private GUIStyle backgroundContentSelected;
        private GUIStyle backgroundSelected;
        private GUIStyle characterGroupIcon;
        private GUIStyle directorGroupIcon;
        private GUIStyle lockIconLRG;
        private GUIStyle lockIconSM;
        private GUIStyle pickerStyle;
        private GUIStyle trackGroupArea;
        private GUIStyle unlockIconLRG;
        private GUIStyle unlockIconSM;

        public TrackGroupStyles(GUISkin skin)
        {
            if (skin != null)
            {
                addIcon = skin.FindStyle("Add");
                lockIconLRG = skin.FindStyle("LockItemLRG");
                unlockIconLRG = skin.FindStyle("UnlockItemLRG");
                lockIconSM = skin.FindStyle("LockItemSM");
                unlockIconSM = skin.FindStyle("UnlockItemSM");
                inspectorIcon = skin.FindStyle("InspectorIcon");
                trackGroupArea = skin.FindStyle("Track Group Area");
                directorGroupIcon = skin.FindStyle("DirectorGroupIcon");
                actorGroupIcon = skin.FindStyle("ActorGroupIcon");
                characterGroupIcon = skin.FindStyle("CharacterGroupIcon");
                pickerStyle = skin.FindStyle("Picker");
                backgroundSelected = skin.FindStyle("TrackGroupFocused");
                backgroundContentSelected = skin.FindStyle("TrackGroupContentFocused");
            }
        }

        public GUIStyle AddIcon
        {
            get
            {
                if (addIcon == null) 
                    addIcon = "box";
                return addIcon;
            }
        }

        public GUIStyle LockIconLRG
        {
            get
            {
                if (lockIconLRG == null) 
                    lockIconLRG = "box";
                return lockIconLRG;
            }
        }

        public GUIStyle UnlockIconLRG
        {
            get
            {
                if (unlockIconLRG == null) 
                    unlockIconLRG = "box";
                return unlockIconLRG;
            }
        }

        public GUIStyle LockIconSM
        {
            get
            {
                if (lockIconSM == null) 
                    lockIconSM = "box";
                return lockIconSM;
            }
        }

        public GUIStyle UnlockIconSM
        {
            get
            {
                if (unlockIconSM == null) 
                    unlockIconSM = "box";
                return unlockIconSM;
            }
        }

        public GUIStyle TrackGroupArea
        {
            get
            {
                if (trackGroupArea == null) 
                    trackGroupArea = "box";
                return trackGroupArea;
            }
        }

        public GUIStyle PickerStyle
        {
            get
            {
                if (pickerStyle == null) 
                    pickerStyle = "box";
                return pickerStyle;
            }
        }

        public GUIStyle BackgroundSelected
        {
            get
            {
                if (backgroundSelected == null) 
                    backgroundSelected = "box";
                return backgroundSelected;
            }
        }

        public GUIStyle BackgroundContentSelected
        {
            get
            {
                if (backgroundContentSelected == null) 
                    backgroundContentSelected = "box";
                return backgroundContentSelected;
            }
        }

        public GUIStyle DirectorGroupIcon
        {
            get
            {
                if (directorGroupIcon == null) 
                    directorGroupIcon = "box";
                return directorGroupIcon;
            }
        }

        public GUIStyle ActorGroupIcon
        {
            get
            {
                if (actorGroupIcon == null) 
                    actorGroupIcon = "box";
                return actorGroupIcon;
            }
        }
    }
}
