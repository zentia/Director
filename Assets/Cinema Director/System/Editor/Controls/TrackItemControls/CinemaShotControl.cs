using UnityEngine;
using UnityEditor;
using CinemaDirector;

[CutsceneItemControlAttribute(typeof(CinemaShot))]
public class CinemaShotControl : CinemaActionControl
{
    private const string MODIFY_CAMERA = "Set Camera/{0}";
    private const string MODIFY_ANIMATION = "Set Animation/{0}";

    public override void Initialize(TimelineItemWrapper wrapper, TimelineTrackWrapper track)
    {
        base.Initialize(wrapper, track);
        actionIcon = Resources.Load<Texture>("Director_ShotIcon");
    }

    public override void Draw(DirectorControlState state)
    {
        CinemaShot shot = Wrapper.Behaviour as CinemaShot;
        if (shot == null) return;

        if (Selection.Contains(shot.gameObject))
        {
            GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.ShotTrackItemSelectedStyle);
        }
        else
        {
            GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.ShotTrackItemStyle);
        }
        

        // Draw Icon
        Color temp = GUI.color;
        GUI.color = (shot.shotCamera != null) ? new Color(0.19f, 0.76f, 0.84f) : Color.red;
        Rect icon = controlPosition;
        icon.x += 4;
        icon.width = 16;
        icon.height = 16;
        //GUI.DrawTexture(icon, shotIcon, ScaleMode.ScaleToFit, true, 0);
        GUI.Box(icon, actionIcon, GUIStyle.none);
        GUI.color = temp;

        Rect labelPosition = controlPosition;
        labelPosition.x = icon.xMax;
        labelPosition.width -= (icon.width + 4);

        if (TrackControl.isExpanded)
        {
            labelPosition.height = TimelineTrackControl.ROW_HEIGHT;

            if (shot.shotCamera != null)
            {
                Rect extraInfo = labelPosition;
                extraInfo.y += TimelineTrackControl.ROW_HEIGHT;
                GUI.Label(extraInfo, string.Format("Camera: {0}", shot.shotCamera.name));
            }
        }
        DrawRenameLabel(shot.name, labelPosition);
    }

    protected override void showContextMenu(Behaviour behaviour)
    {
        CinemaShot shot = behaviour as CinemaShot;
        if (shot == null) return;

        Camera[] cameras = Object.FindObjectsOfType<Camera>();
        
        GenericMenu createMenu = new GenericMenu();
        createMenu.AddItem(new GUIContent("Rename"), false, renameItem, behaviour);
        createMenu.AddItem(new GUIContent("Copy"), false, copyItem, behaviour);
        createMenu.AddItem(new GUIContent("Delete"), false, deleteItem, shot);
        createMenu.AddSeparator(string.Empty);
        createMenu.AddItem(new GUIContent("Focus"), false, focusShot, shot);
        foreach (Camera c in cameras)
        {
            ContextSetCamera arg = new ContextSetCamera
            {
                shot = shot,
                camera = c
            };
            createMenu.AddItem(new GUIContent(string.Format(MODIFY_CAMERA, c.gameObject.name)), false, setCamera, arg);
        }
        Cutscene.ForeachDir(CinemaShot.animDir, path =>
        {
            ContextSetAnimationClip arg = new ContextSetAnimationClip
            {
                shot = shot,
                clipName = path
            };
            createMenu.AddItem(new GUIContent(string.Format(MODIFY_ANIMATION, path)), false, setAnimationClip, arg);
        });
        createMenu.ShowAsContext();
    }

    private void focusShot(object userData)
    {
        CinemaShot shot = userData as CinemaShot;
        if (shot.shotCamera != null)
        {
            if (SceneView.currentDrawingSceneView != null)
            {
                SceneView.currentDrawingSceneView.AlignViewToObject(shot.shotCamera.transform);
            } 
            else
            {
                Debug.Log("Focus is not supported in this version of Unity.");
            }
        }
    }

    private void setCamera(object userData)
    {
        ContextSetCamera arg = userData as ContextSetCamera;
        if (arg != null)
        {
            Undo.RecordObject(arg.shot, "Set Camera");
            arg.shot.shotCamera = arg.camera;
            arg.shot.cameraName = arg.camera.name;
            if (arg.camera != null)
            {

            }
        }
    }

    private void setAnimationClip(object userData)
    {
        ContextSetAnimationClip arg = userData as ContextSetAnimationClip;
        if (arg != null)
        {
            Undo.RecordObject(arg.shot, "Set AnimationClip");
            arg.shot.clipName = arg.clipName;
            arg.shot.LoadAnim();
        }
    }
    private class ContextSetCamera
    {
        public Camera camera;
        public CinemaShot shot;
    }
    private class ContextSetAnimationClip
    {
        public string clipName;
        public CinemaShot shot;
    }
}
