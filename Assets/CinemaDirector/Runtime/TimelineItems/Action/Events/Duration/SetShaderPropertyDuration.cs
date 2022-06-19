using System;
using UnityEngine;
using CinemaDirector;

namespace AGE
{
    [Serializable]
    [EventCategory("ChangeSpeed")]
    public class SetShaderPropertyDuration : DurationEvent
    {
        public enum PropertyType
        {
            Float = 0,
            Color = 1,
            Vector = 2,
            Int = 3,
        };

        [Template]
        public int targetId = -1;
        public string ShaderKeyWordsName = "";
        public string ShaderPropertyName = "";
        public PropertyType propertyType = PropertyType.Float;
        public float StartColorR = 0;
        public float StartColorG = 0;
        public float StartColorB = 0;
        public float StartColorA = 0;
        public float EndColorR = 0;
        public float EndColorG = 0;
        public float EndColorB = 0;
        public float EndColorA = 0;

        public float StartFloat = 0f;
        public float EndFloat = 0f;

        public float StartX = 0;
        public float StartY = 0;
        public float StartZ = 0;
        public float StartW = 0;
        public float EndX = 0;
        public float EndY = 0;
        public float EndZ = 0;
        public float EndW = 0;

        public int EndInt = 0;

        public bool leaveRestore = false;
        private object restoreValue = null;

        private Renderer[] _renderers = null;

        // Renderer逐渐换成CharacterRenderAdapter控制角色的材质表现
        //private CharacterRenderAdapter[] _adpaters = null;
        private bool _isCharacterRender = false;
        private bool _restoreKeyWordsEnable = false;

        public override bool SupportEditMode()
        {
            return true;
        }


        public override void Enter(Action _action, Track _track)
        {
            GameObject targetObject = _action.GetGameObject(targetId);
            if (targetObject == null)
            {
                return;
            }

            // 默认使用CharacterRenderAdapter,如果获取不到，就换成Renderer

            if (_isCharacterRender == false)
            {
                _renderers = targetObject.GetComponentsInChildren<Renderer>();
            }

            if(leaveRestore)
            {
                if(_renderers == null)
                {
                    _renderers = targetObject.GetComponentsInChildren<Renderer>();
                }

                for (int i = 0, imax = _renderers.Length; i < imax; i++)
                {
                    if (_renderers[i] != null && _renderers[i].material != null)
                    {
                        if (_renderers[i].material.HasProperty(ShaderPropertyName))
                        {
                            if (propertyType == PropertyType.Color)
                            {
                                restoreValue = _renderers[i].material.GetColor(ShaderPropertyName);
                            }
                            else if (propertyType == PropertyType.Float)
                            {
                                restoreValue = _renderers[i].material.GetFloat(ShaderPropertyName);
                            }
                            else if (propertyType == PropertyType.Vector)
                            {
                                restoreValue = _renderers[i].material.GetVector(ShaderPropertyName);
                            }
                            else if(propertyType == PropertyType.Int)
                            {
                                restoreValue = _renderers[i].material.GetInt(ShaderPropertyName);
                            }

                            if (!string.IsNullOrEmpty(ShaderKeyWordsName))
                            {
                                _restoreKeyWordsEnable = _renderers[i].material.IsKeywordEnabled(ShaderKeyWordsName);
                            }
                            break;
                        }
                    }
                }
            }

            if (_renderers != null)
            {
                for (int i = 0, imax = _renderers.Length; i < imax; i++)
                {
                    if (_renderers[i] != null && _renderers[i].material != null)
                    {
                        if (!string.IsNullOrEmpty(ShaderKeyWordsName))
                        {
                            _renderers[i].material.EnableKeyword(ShaderKeyWordsName);
                        }
                    }
                }
            }

            Process(_action, _track, 0);
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            if (string.IsNullOrEmpty(ShaderPropertyName))
            {
                return;
            }

            float percent = _localTime / length;
            if(propertyType == PropertyType.Color)
            {
                Color startColor = new Color(StartColorR, StartColorG, StartColorB, StartColorA);
                Color endColor = new Color(EndColorR, EndColorG, EndColorB, EndColorA);
                Color curColor = (startColor + (endColor - startColor) * percent) / 255;

                if ( _renderers != null)
                {
                    for (int i = 0, imax = _renderers.Length; i < imax; i++)
                    {
                        if (_renderers[i] != null && _renderers[i].material != null)
                        {
                            if (_renderers[i].material.HasProperty(ShaderPropertyName))
                            {
                                _renderers[i].material.SetColor(ShaderPropertyName, curColor);
                            }
                        }
                    }
                }


            }
            else if(propertyType == PropertyType.Float)
            {
                float curFloat = StartFloat + (EndFloat - StartFloat) * percent;
                if (_isCharacterRender)
                {
                    //for (int i = 0, imax = _adpaters.Length; i < imax; ++i)
                    //{
                    //    if (_adpaters[i] != null )
                    //    {
                    //        _adpaters[i].SetFloat(ShaderPropertyName, curFloat);
                    //    }
                    //}
                }
                else if (_renderers != null)
                {
                    for (int i = 0, imax = _renderers.Length; i < imax; i++)
                    {
                        if (_renderers[i] != null && _renderers[i].material != null)
                        {
                            if (_renderers[i].material.HasProperty(ShaderPropertyName))
                            {
                                _renderers[i].material.SetFloat(ShaderPropertyName, curFloat);
                            }
                        }
                    }
                }
            }
            else if(propertyType == PropertyType.Vector)
            {
                Vector4 startColor = new Vector4(StartX, StartY, StartZ, StartW);
                Vector4 endColor = new Vector4(EndX, EndY, EndZ, EndW);
                Vector4 curColor = startColor + (endColor - startColor) * percent;
                if (_isCharacterRender)
                {
                    //for (int i = 0, imax = _adpaters.Length; i < imax; ++i)
                    //{
                    //    if (_adpaters[i] != null)
                    //    {
                    //        _adpaters[i].ApplyMaterialBlock(ShaderPropertyName, curColor);
                    //    }
                    //}
                }
                else if (_renderers != null)
                {
                    for (int i = 0, imax = _renderers.Length; i < imax; i++)
                    {
                        if (_renderers[i] != null && _renderers[i].material != null)
                        {
                            if (_renderers[i].material.HasProperty(ShaderPropertyName))
                            {
                                _renderers[i].material.SetVector(ShaderPropertyName, curColor);
                            }
                        }
                    }
                }
            }
            else if (propertyType == PropertyType.Int)
            {
                if (_isCharacterRender)
                {
                    //for (int i = 0, imax = _adpaters.Length; i < imax; ++i)
                    //{
                    //    if (_adpaters[i] != null)
                    //    {
                    //        _adpaters[i].ApplyMaterialBlock(ShaderPropertyName, curColor);
                    //    }
                    //}
                }
                else if (_renderers != null)
                {
                    for (int i = 0, imax = _renderers.Length; i < imax; i++)
                    {
                        if (_renderers[i] != null && _renderers[i].material != null)
                        {
                            if (_renderers[i].material.HasProperty(ShaderPropertyName))
                            {
                                _renderers[i].material.SetInt(ShaderPropertyName, EndInt);
                            }
                        }
                    }
                }
            }
        }

		public override void Leave(Action _action, Track _track)
		{
			GameObject targetObject = _action.GetGameObject(targetId);
			if (targetObject == null)
			{
				return;
			}

            // 默认使用CharacterRenderAdapter,如果获取不到，就换成Renderer
            //_adpaters = targetObject.GetComponentsInChildren<CharacterRenderAdapter>();

            //if (_adpaters != null && _adpaters.Length > 0)
            //{
            //    _isCharacterRender = true;
            //}

            if (_isCharacterRender == false)
            {
                _renderers = targetObject.GetComponentsInChildren<Renderer>();
            }

            if(leaveRestore && restoreValue != null)
            {
                if (propertyType == PropertyType.Color)
                {
                    Color r = (Color)restoreValue * 255;
                    StartColorR = r.r;
                    StartColorG = r.g;
                    StartColorB = r.b;
                    StartColorA = r.a;
                    EndColorR = r.r;
                    EndColorG = r.g;
                    EndColorB = r.b;
                    EndColorA = r.a;
                }
                else if (propertyType == PropertyType.Float)
                {
                    float f = (float)restoreValue;
                    StartFloat = f;
                    EndFloat = f;
                }
                else if (propertyType == PropertyType.Vector)
                {
                    Vector4 r = (Vector4)restoreValue;
                    StartX = r.x;
                    StartY = r.y;
                    StartZ = r.z;
                    StartW = r.w;
                    EndX = r.x;
                    EndY = r.y;
                    EndZ = r.z;
                    EndW = r.w;
                }else if(propertyType == PropertyType.Int)
                {
                    EndInt = (int)restoreValue;
                }
            }

            if (_renderers != null)
            {
                for (int i = 0, imax = _renderers.Length; i < imax; i++)
                {
                    if (_renderers[i] != null && _renderers[i].material != null)
                    {
                        if (!string.IsNullOrEmpty(ShaderKeyWordsName) && leaveRestore && !_restoreKeyWordsEnable)
                        {
                            _renderers[i].material.DisableKeyword(ShaderKeyWordsName);
                        }
                    }
                }
            }
            Process(_action, _track, length);
        }

		protected override void CopyData(BaseEvent src)
		{
			base.CopyData(src);
            SetShaderPropertyDuration r = src as SetShaderPropertyDuration;
            targetId = r.targetId;
            ShaderPropertyName = r.ShaderPropertyName;
            propertyType = r.propertyType;
            StartColorR = r.StartColorR;
            StartColorG = r.StartColorG;
            StartColorB = r.StartColorB;
            StartColorA = r.StartColorA;
            EndColorR = r.EndColorR;
            EndColorG = r.EndColorG;
            EndColorB = r.EndColorB;
            EndColorA = r.EndColorA;

            StartFloat = r.StartFloat;
            EndFloat = r.EndFloat;

            EndInt = r.EndInt;

            StartX = r.StartX;
            StartY = r.StartY;
            StartZ = r.StartZ;
            StartW = r.StartW;
            EndX = r.EndX;
            EndY = r.EndY;
            EndZ = r.EndZ;
            EndW = r.EndW;

            leaveRestore = r.leaveRestore;
        }

        protected override void ClearData()
        {
            base.ClearData();
            targetId = -1;
            ShaderPropertyName = "";
            propertyType = PropertyType.Float;
            StartColorR = 0;
            StartColorG = 0;
            StartColorB = 0;
            StartColorA = 0;
            EndColorR = 0;
            EndColorG = 0;
            EndColorB = 0;
            EndColorA = 0;

            StartFloat = 0f;
            EndFloat = 0f;

            EndInt = 0;

            StartX = 0;
            StartY = 0;
            StartZ = 0;
            StartW = 0;
            EndX = 0;
            EndY = 0;
            EndZ = 0;
            EndW = 0;

            leaveRestore = false;

            _renderers = null;

            //_adpaters = null;
            _isCharacterRender = false;
            restoreValue = null;
        }
    }
}
