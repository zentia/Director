using UnityEngine;

namespace TimelineEditor
{
    public class TrackStyles
    {
        public GUIStyle ActorTrackItemSelectedStyle;
        public GUIStyle ActorTrackItemStyle;
        public GUIStyle AudioTrackItemSelectedStyle;
        public GUIStyle AudioTrackItemStyle;
        public GUIStyle backgroundContentSelected;
        public GUIStyle backgroundSelected;
        public GUIStyle compressStyle;
        public GUIStyle curveCanvasStyle;
        public GUIStyle curveStyle;
        private GUIStyle curveTrackItemSelectedStyle;
        private GUIStyle curveTrackItemStyle;
        public GUIStyle editCurveItemStyle;
        public GUIStyle EventItemBottomStyle;
        public GUIStyle EventItemStyle;
        public GUIStyle expandStyle;
        public GUIStyle GlobalTrackItemSelectedStyle;
        public GUIStyle GlobalTrackItemStyle;
        public GUIStyle keyframeContextStyle;
        public GUIStyle keyframeStyle;
        public GUIStyle ShotTrackItemSelectedStyle;
        public GUIStyle ShotTrackItemStyle;
        public GUIStyle tangentStyle;
        public GUIStyle TrackAreaStyle;
        private GUIStyle trackItemSelectedStyle;
        private GUIStyle trackItemStyle;
        public GUIStyle TrackSidebarBG1;
        public GUIStyle TrackSidebarBG2;

        public TrackStyles(GUISkin skin)
        {
            if (skin != null)
            {
                TrackAreaStyle = skin.FindStyle("Track Area");
                TrackItemStyle = skin.FindStyle("Track Item");
                TrackItemSelectedStyle = skin.FindStyle("TrackItemSelected");
                ShotTrackItemStyle = skin.FindStyle("ShotTrackItem");
                ShotTrackItemSelectedStyle = skin.FindStyle("ShotTrackItemSelected");
                AudioTrackItemStyle = skin.FindStyle("AudioTrackItem");
                AudioTrackItemSelectedStyle = skin.FindStyle("AudioTrackItemSelected");
                GlobalTrackItemStyle = skin.FindStyle("GlobalTrackItem");
                GlobalTrackItemSelectedStyle = skin.FindStyle("GlobalTrackItemSelected");
                ActorTrackItemStyle = skin.FindStyle("ActorTrackItem");
                ActorTrackItemSelectedStyle = skin.FindStyle("ActorTrackItemSelected");
                CurveTrackItemStyle = skin.FindStyle("CurveTrackItem");
                CurveTrackItemSelectedStyle = skin.FindStyle("CurveTrackItemSelected");
                keyframeStyle = skin.FindStyle("Keyframe");
                curveStyle = skin.FindStyle("Curve");
                tangentStyle = skin.FindStyle("TangentHandle");
                curveCanvasStyle = skin.FindStyle("CurveCanvas");
                compressStyle = skin.FindStyle("CompressVertical");
                expandStyle = skin.FindStyle("ExpandVertical");
                editCurveItemStyle = skin.FindStyle("EditCurveItem");
                EventItemStyle = skin.FindStyle("EventItem");
                EventItemBottomStyle = skin.FindStyle("EventItemBottom");
                keyframeContextStyle = skin.FindStyle("KeyframeContext");
                TrackSidebarBG1 = skin.FindStyle("TrackSidebarBG");
                TrackSidebarBG2 = skin.FindStyle("TrackSidebarBGAlt");
                backgroundSelected = skin.FindStyle("TrackGroupFocused");
                backgroundContentSelected = skin.FindStyle("TrackGroupContentFocused");
            }
            else
            {
                GUI.skin = new GUISkin();
                TrackAreaStyle = "box";
                TrackItemStyle = "flow node 0";
                TrackItemSelectedStyle = "flow node 0 on";
                ShotTrackItemStyle = "flow node 1";
                ShotTrackItemSelectedStyle = "flow node 1 on";
                AudioTrackItemStyle = "flow node 2";
                AudioTrackItemSelectedStyle = "flow node 2 on";
                GlobalTrackItemStyle = "flow node 3";
                GlobalTrackItemSelectedStyle = "flow node 3 on";
                ActorTrackItemStyle = "flow node 4";
                ActorTrackItemSelectedStyle = "flow node 4 on";
                CurveTrackItemStyle = "flow node 0";
                CurveTrackItemSelectedStyle = "flow node 0 on";
                keyframeStyle = "Dopesheetkeyframe";
                curveStyle = "box";
                tangentStyle = "box";
                curveCanvasStyle = "box";
                compressStyle = "box";
                expandStyle = "box";
                editCurveItemStyle = "box";
                EventItemStyle = "box";
                EventItemBottomStyle = "box";
                keyframeContextStyle = "box";
                TrackSidebarBG1 = "box";
                TrackSidebarBG2 = "box";
                backgroundSelected = "box";
                backgroundContentSelected = "box";
            }
        }

        public GUIStyle TrackItemStyle
        {
            get
            {
                if (trackItemStyle == null) trackItemStyle = "box";
                return trackItemStyle;
            }
            set { trackItemStyle = value; }
        }

        public GUIStyle CurveTrackItemStyle
        {
            get
            {
                if (curveTrackItemStyle == null) curveTrackItemStyle = "box";
                return curveTrackItemStyle;
            }
            set { curveTrackItemStyle = value; }
        }

        public GUIStyle TrackItemSelectedStyle
        {
            get
            {
                if (trackItemSelectedStyle == null) trackItemSelectedStyle = "box";
                return trackItemSelectedStyle;
            }
            set { trackItemSelectedStyle = value; }
        }

        public GUIStyle CurveTrackItemSelectedStyle
        {
            get
            {
                if (curveTrackItemSelectedStyle == null) curveTrackItemSelectedStyle = "box";
                return curveTrackItemSelectedStyle;
            }
            set { curveTrackItemSelectedStyle = value; }
        }
    }
}
