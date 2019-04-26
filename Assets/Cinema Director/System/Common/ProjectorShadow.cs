using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CinemaDirector
{
    public class ProjectorShadow : MonoBehaviour
    {
        public float mProjectorSize = 23;
        public int mRenderTexSize = 2048;
        public LayerMask mLayerCaster;
        public LayerMask mLayerIgnoreReceiver;
        private bool mUseCommandBuf = false;
        private Projector mProjector;
        private Camera mShadowCam;
        private RenderTexture mShadowRT;
        private CommandBuffer mCommandBuf;
        private Material mReplaceMat;
        static List<Renderer> m_RendererList = new List<Renderer>();
        void Start ()
        {
            mShadowRT = new RenderTexture(mRenderTexSize, mRenderTexSize, 0, RenderTextureFormat.R8);
            mShadowRT.name = "ShadowRT";
            mShadowRT.antiAliasing = 1;
            mShadowRT.filterMode = FilterMode.Bilinear;
            mShadowRT.wrapMode = TextureWrapMode.Clamp;     
            mProjector = GetComponent<Projector>();
            mProjector.orthographic = true;
            mProjector.orthographicSize = mProjectorSize;
            mProjector.ignoreLayers = mLayerIgnoreReceiver;
            mProjector.material.SetTexture("_ShadowTex", mShadowRT);
            mShadowCam = gameObject.AddComponent<Camera>();
            mShadowCam.clearFlags = CameraClearFlags.Color;
            mShadowCam.backgroundColor = Color.black;
            mShadowCam.orthographic = true;
            mShadowCam.orthographicSize = mProjectorSize;
            mShadowCam.depth = -100.0f;
            mShadowCam.nearClipPlane = mProjector.nearClipPlane;
            mShadowCam.farClipPlane = mProjector.farClipPlane;
            mShadowCam.targetTexture = mShadowRT;
            InitCommandBuffer();
        }
        void Update ()
        {
            FillCommandBuffer();
        }
        private void LateUpdate()
        {
        }
        private void InitCommandBuffer()
        {
            Shader replaceshader = Shader.Find("ProjectorShadow/ShadowCaster");
            mShadowCam.cullingMask = 0;
            mShadowCam.RemoveAllCommandBuffers();
            if (mCommandBuf != null)
            {
                mCommandBuf.Dispose();
                mCommandBuf = null;
            }
            mCommandBuf = new CommandBuffer();
            mShadowCam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, mCommandBuf);
            if (mReplaceMat == null)
            {
                mReplaceMat = new Material(replaceshader);
                mReplaceMat.hideFlags = HideFlags.HideAndDontSave;
            }
        }
        private void FillCommandBuffer()
        {
            mCommandBuf.Clear();
            for (int i = 0; i < m_RendererList.Count; i++)
            {
                mCommandBuf.DrawRenderer(m_RendererList[i], mReplaceMat);
            }           
        }
        static ProjectorShadow m_Instance;
        public static void AddRenderer(Transform actor)
        {
            var renderers = actor.GetComponentsInChildren<Renderer>();
            if (renderers == null)
            {
                return;
            }
            if (m_Instance == null)
            {
                m_Instance = DirectorRuntimeHelper.Root.GetComponent<ProjectorShadow>();
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                m_RendererList.Add(renderers[i]);
            }
        }
    }
}
