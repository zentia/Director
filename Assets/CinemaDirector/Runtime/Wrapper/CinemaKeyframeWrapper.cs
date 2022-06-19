using UnityEngine;

namespace CinemaDirector
{
    public class CinemaKeyframeWrapper
    {
        public Vector2 ScreenPosition;
        public Vector2 InTangentControlPointPosition;
        public Vector2 OutTangentControlPointPosition;
        public float m_InTangent;
        public float m_OutTanger;
        public float m_InWeight;
        public float m_OutWeight;
        public int m_TangentMode;
        public int m_TimeHash;
        private int m_Hash;

        private float m_time;
        private object m_value;
        private CinemaAnimationCurveWrapper m_curve;

        public float time
        {
            get { return m_time; }
            set
            {
                m_time = value;
                m_Hash = 0;
                m_TimeHash = value.GetHashCode();
            }
        }

        public CinemaKeyframeWrapper(CinemaAnimationCurveWrapper curve, Keyframe key)
        {
            time = key.time;
            m_value = key.value;
            this.curve = curve;
            m_InTangent = key.inTangent;
            m_OutTanger = key.outTangent;
            m_TangentMode = key.tangentMode;
        }
        public CinemaAnimationCurveWrapper curve
        {
            get { return m_curve; }
            set
            {
                m_curve = value;
                m_Hash = 0;
            }
        }
        public int GetHash()
        {
            if (m_Hash == 0)
            {
                unchecked
                {
                    m_Hash = curve.GetHashCode();
                    m_Hash = 33 * m_Hash + time.GetHashCode();
                }
            }

            return m_Hash;
        }
    }    
}
