/********************************************************************
	created:	2016/04/27
	created:	27:4:2016   19:13
	filename: 	E:\ssk\client_proj\UnityProj\Assets\Scripts\Framework\AssetService\AssetDeclare.cs
	file path:	E:\ssk\client_proj\UnityProj\Assets\Scripts\Framework\AssetService
	file base:	AssetDeclare
	file ext:	cs
	author:		benzhou
	
	purpose:	资源结构申明
*********************************************************************/

using System.Collections.Generic;
using Assets.Plugins.Common;
using UnityEngine;
using AGE;
using Yarp;
using PigeonCoopToolkit.Effects.Trails;
using Spine.Unity;
using Assets.Scripts.Framework.Actor;
using System;
using Assets.Scripts.Framework.AssetService.Asset;
using Assets.Plugins.Common.CNS;
using UGCDom;
using System.Reflection;
using CinemaDirector;
using Effect;
using NOAH.VFX;

namespace Assets.Scripts.Framework.AssetService
{
    public static class LoadPriority
    {
        // 意图在进度条或者转菊花的时候进行资源加载
        // 该优先级会打断其他的所有加载，等到该优先级加载完后才开始其他加载
        public const int Wait = 0;

        // 意图以异步的形式加载迫切想要显示的资源
        // 该优先级是异步加载中的最高优先级，在异步过程中会最优先处理它
        // 推荐应用在想以异步的形式加载一些具有交互的资源（UI、模型）上
        public const int ImmediateShow = 1;

        // 意图以异步的形式加载想要显示的资源
        // 该优先级是异步加载中的中优先级
        // 推荐应用在想以异步的形式加载一些不具有交互的资源（特效）上
        public const int SpareShow = 10000;

        // 意图以异步的形式加载当前不显示的资源
        // 该优先级是异步加载的低优先级
        // 推荐应用在想以异步的形式加载一些当前不显示的资源（下一级的UI等）上
        public const int Silent = 20000;

        // 意图以异步的形式加载预处理的资源
        // 该优先级是异步加载中的最低优先级，加载速度是最慢，它只会在当前没有任何加载的时候才会进行一个该类别的加载处理
        // 推荐应用在想以异步的形式加载一些需要预处理的资源（AGE，预解压Bundle等）上，这些资源可能在很久以后会用到的
        public const int Preprocess = 30000;
    }

    public static class AssetLoadResult
    {
        // 装载成功
        public const int Success = 0;

        // 装载失败 - 资源装载失败
        public const int ResourceFail = 2;
    }

    public interface IAssetLoadCallback
    {
        /// <summary>
        /// 资产加载完成
        /// </summary>
        /// <param name="assetName">资产名</param>
        /// <param name="result">加载结果，取值为AssetLoadResult</param>
        /// <param name="resource">资源（当资产类型为External时有效）</param>
        /// <param name="passThroughArg">透传参数</param>
        void OnLoadAssetCompleted(string assetName, int result, CResource resource, object passThroughArg = null);
    }

    public enum AssetType
    {
        // UI资源
        UI = 0,

        // 特效
        Particle = 1,

        // 角色
        Actor = 2,

        // 纹理
        Texture = 3,

        // 模型
        Mesh = 4,

        // 材质
        Material = 5,

        // 实例化资源
        Instantiatable = 6,

        // 不实例化资源
        Primitive = 7,

        // AGE
        Age = 8,

        // 场景策划节点
        DesignScene = 9,

        // RawAssets目录的资源
        Raw = 10,

        // 通过Texture转Sprite会丢失九宫等信息，所以单独一个类型
        Sprite = 11,

        // 王者场景策划节点
        WZDesignScene = 12,

        // 王者场景美术节点
        WZArtistScene = 13,

        BaseAssetTypeCount = 14,

        // 对象，当前包括C#的UIPrefabClass
        Object = 50,

        // Unity场景，主要由于导航网格只能保存在Unity场景里面
        //UnityScene = 99,
    }

    public enum LifeType
    {
        // 跟随别的资产
        FollowAsset = -1,

        // 常驻
        Resident = 0,

        // 角色状态(随着局内某个角色生命周期走)
        ActorState = 1,

        // 跟随游戏状态
        GameState = 2,

        // 跟随UI状态
        UIState = 3,

        // 不用时立即销毁
        Immediate = 4,
    }

    public struct AssetData
    {
        public int Priority;
        public IAssetLoadCallback Callback;
        public int CallbackID;

        public AssetType Type;
        public bool Raw;
        public System.Type ContentType { get { return AssetProcessor.GetContentType(Type); } }

        public string Name;
        public string Filename;
        public object AssetParma;
        public IObjectFactory<BaseAsset> Factory;

        public LifeType Life;
        public string LifeName;
        public int LifeFrames;//0则不生效
        public int ElapseFrames;//在缓存池之后流逝的帧数

        public int Count;
        public object passThroughArg; //回调的透传参数
    }




    public class BaseAsset : BindingObject
    {
        public AssetData BaseData;  // 内部使用的数据结构，外部不要修改也不要访问
        public CResource Resource;
        public ListView<BaseAsset> loadedAssociateAssets;
        public virtual GameObject Go { get { return null; } }
        public virtual Transform Tf { get { return null; } }

        public virtual UnityEngine.Object Asset { get { return null; } }

        public int InstanceID
        {
            get
            {
                return Go != null ? Go.GetInstanceID() : 0;
            }
        }

        public virtual bool IsValid()
        {
            return Resource != null && (Resource.Content != null);
        }

        public virtual void OnDestroy()
        {
            Resource = null;
            BaseData.Callback = null;
            if (BaseData.Factory != null)
            {
                BaseData.Factory.DestroyObject(this);
            }
        }

        public void AppendAssociateAsset(BaseAsset ba)
        {
            if (loadedAssociateAssets == null) loadedAssociateAssets = new ListView<BaseAsset>();
            // TODO: Check duplication
            if (!loadedAssociateAssets.Contains(ba))
            {
                loadedAssociateAssets.Add(ba);
            }
        }

        protected void UnloadAssociateAssets()
        {
            if (loadedAssociateAssets != null)
            {
                for (int i = 0; i < loadedAssociateAssets.Count; i++)
                {
                    loadedAssociateAssets[i].Unload();
                }
                loadedAssociateAssets = null;
            }
        }

        public void Unload()
        {
            UnloadAssociateAssets();
            AssetService.GetInstance().Unload(this);
        }

        public virtual bool OnCreate()
        {
            return true;
        }

        public virtual void OnUnload()
        {

        }

        public virtual void OnCache()
        {
            BaseData.ElapseFrames = 0;
        }

        public virtual void OnReuse()
        {
        }


        public virtual List<AssetData> CollectAssociateAssets()
        {
            return null;
        }

        public virtual void ApplyAssociateAssets()
        {
        }
    }

    // Ext
    public static class ExtBaseAsset
    {
        public static bool Valid(this BaseAsset ba)
        {
            if (ba == null) return false;
            return ba.IsValid();
        }

        public static bool Invalid(this BaseAsset ba)
        {
            if (ba == null) return true;
            return !ba.IsValid();
        }

        //public static void Unload(this BaseAsset ba)
        //{
        //    AssetService.GetInstance().Unload(ba);
        //}
    }

    // 实例化资源的基类
    public class InstantiatableAsset : BaseAsset
    {                                           
        public GameObject RootGo;
        public Transform RootTf;

        public Vector3 OrignalPosition;
        public Quaternion OrignalRotate;
        public Vector3 OrignalScale;

        public override GameObject Go { get { return RootGo; } }
        public override Transform Tf { get { return RootTf; } }

        public int InstanceID
        {
            get
            {
                return RootGo != null ? RootGo.GetInstanceID() : 0;
            }
        }

        public override bool IsValid()
        {
            if (RootGo == null)
            {
                return false;
            }

            return base.IsValid();
        }

        public override void OnCache()
        {
            if (RootTf != null)
            {
                RootTf.localPosition = OrignalPosition;
                RootTf.localRotation = OrignalRotate;
                RootTf.localScale = OrignalScale;
            }

            base.OnCache();
        }

        public override void OnDestroy()
        {
            if (RootGo != null)
            {
                RootGo.ExtSetActive(false);
                RootGo.ExtDestroy();
            }

            base.OnDestroy();
        }
        public override void OnReuse()
        {
            base.OnReuse();
            if (RootGo != null)
            {
                RootGo.ExtSetActive(true);
            }
        }

        public override bool OnCreate()
        {
            base.OnCreate();

            InstantiateInstance();

            var manifest = RootGo != null ? RootGo.GetComponent<AssetDependencyManifest>() : null;
            if (manifest != null) manifest.ownerAsset = this;
            return RootGo != null;
        }

        protected virtual void InstantiateInstance()
        {
            RootGo = Resource.Content.ExtInstantiate() as GameObject;
            if (RootGo != null)
            {
                RootTf = RootGo.transform;
                OrignalPosition = RootTf.localPosition;
                OrignalRotate = RootTf.localRotation;
                OrignalScale = RootTf.localScale;
            }
            else
            {
                Log.LogE("Asset", "AssetProcessor.Instantiate : failed to instantiate - {0}", BaseData.Name);
            }
        }

        public override List<AssetData> CollectAssociateAssets()
        {
            //List<AssetData> associateAssets = null;

            //var manifest = RootGo != null ? RootGo.GetComponent<AssetDependencyManifest>() : null;
            //if (manifest != null)
            //{
            //    associateAssets = new List<AssetData>();

            //    foreach(var entry in manifest.entries)
            //    {
            //        foreach (var detail in entry.details)
            //        {
            //            associateAssets.Add(new AssetData
            //            {
            //                Priority = BaseData.Priority,
            //                Callback = null,
            //                CallbackID = 0,
            //                Type = detail.type,
            //                Raw = BaseData.Raw,
            //                //ContentType = AssetProcessor.GetContentType(detail.type),
            //                Name = detail.path,
            //                Filename = AssetProcessor.ProcessLodFilename(detail.type, detail.path),
            //                Factory = AssetClassFactory.GetFactory(detail.type),
            //                Life = LifeType.FollowAsset,
            //                LifeName = BaseData.Name,
            //                Count = 1,
            //                passThroughArg = null
            //            });
            //        }
            //    }
            //}

            return null;
        }

        public override void ApplyAssociateAssets()
        {
            //var manifest = RootGo != null ? RootGo.GetComponent<AssetDependencyManifest>() : null;
            //if (manifest != null)
            //{
            //    foreach (var entry in manifest.entries)
            //    {
            //        if (entry.component != null)
            //        {
            //            if (entry.mainDetail.IsValid())
            //            {
            //                var detail = entry.mainDetail;
            //                var ba = AssetService.GetInstance().LoadAsset(detail.type, detail.path, LifeType.FollowAsset, BaseData.Name, BaseData.LifeFrames);
            //                if (ba != null)
            //                {
            //                    var asset = ba.Resource.GetContent(detail.subAsset, AssetProcessor.GetContentType(detail.type));
            //                    entry.Apply(asset);
            //                }
            //            }
            //            else
            //            {
            //                var assets = new List<UnityEngine.Object>();
            //                foreach (var detail in entry.details)
            //                {
            //                    if (detail.IsValid())
            //                    {
            //                        var ba = AssetService.GetInstance().LoadAsset(detail.type, detail.path, LifeType.FollowAsset, BaseData.Name, BaseData.LifeFrames);
            //                        if (ba != null)
            //                        {
            //                            assets.Add(ba.Resource.GetContent(detail.subAsset, AssetProcessor.GetContentType(detail.type)));
            //                        }
            //                    }
            //                }
            //                entry.Apply(assets);
            //            }
            //        }
            //    }
            //}
        }
    }

    public class ParticleAsset : InstantiatableAsset
    {
        public List<ParticleSystem> ParticleCpt;
        public List<Renderer> RenderCpt;
        public PoolObjectComponentsCache.ParticleInitState[] ParticleInitStates;
        public List<int> ParticleRendererInitSortingOrders;
        public Animator AnimatorCpt;
        public List<Material> AllMaterials;
        public List<Animator> AnimatorCpts;
        public List<ParticleScaler> ParticleScalers;
        public List<MeshRenderer> MeshRenderers;
        public List<LineRenderer> LineRenderers;
        public List<TrailRenderer> TrailRenderers;
        public ulong RenderValueCacheKey = 0x03E803E803E80000;   //1000 1000 1000 0

        public void PlayAllParticles()
        {
            var effectHub = Go.GetComponent<VFXEffectHub>();
            if (effectHub != null)
            {
                effectHub.Reactivate();
            }
            else
            {
                if (null == ParticleCpt)
                {
                    return;
                }

                for (int i = 0; i < ParticleCpt.Count; i++)
                {
                    ParticleCpt[i].Play(false);
                }
            }
        }

        public override bool OnCreate()
        {
            var succeed = base.OnCreate();

            if (succeed)
            {
                ParticleCpt = RootGo.ExtGetComponentsInChildren<ParticleSystem>(ListPool<ParticleSystem>.Get(), true);
                RenderCpt = RootGo.ExtGetComponentsInChildren<Renderer>(ListPool<Renderer>.Get(), true);
                ParticleInitStates = SimpleStructArrayPool<PoolObjectComponentsCache.ParticleInitState>.Get(ParticleCpt.Count);
                AnimatorCpt = RootGo.GetComponent<Animator>();
                AnimatorCpts = RootGo.ExtGetComponentsInChildren<Animator>(ListPool<Animator>.Get(), true);
                ParticleScalers = RootGo.ExtGetComponentsInChildren<ParticleScaler>(ListPool<ParticleScaler>.Get(), true);
                MeshRenderers = RootGo.ExtGetComponentsInChildren<MeshRenderer>(ListPool<MeshRenderer>.Get(), true);
                LineRenderers = RootGo.ExtGetComponentsInChildren<LineRenderer>(ListPool<LineRenderer>.Get(), true);
                TrailRenderers = RootGo.ExtGetComponentsInChildren<TrailRenderer>(ListPool<TrailRenderer>.Get(), true);

                // 初始化粒子状态
                if (ParticleCpt.Count != 0)
                {
                    for (int i = 0; i < ParticleCpt.Count; i++)
                    {
                        ParticleInitStates[i].EmmitState = ParticleCpt[i].emission.enabled;
                        ParticleInitStates[i].StartSize = ParticleCpt[i].main.startSizeMultiplier;
                        ParticleInitStates[i].StartLifeTime = ParticleCpt[i].main.startLifetimeMultiplier;
                        ParticleInitStates[i].StartSpeed = ParticleCpt[i].main.startSpeedMultiplier;
                        ParticleInitStates[i].Trans = ParticleCpt[i].transform;
                        ParticleInitStates[i].LocalScale = ParticleInitStates[i].Trans.localScale;
                    }
                }

                if (RenderCpt != null && RenderCpt.Count > 0)
                {
                    ParticleRendererInitSortingOrders = ListPool<int>.Get();
                    AllMaterials = ListPool<Material>.Get();
                    for (int i = 0; i < RenderCpt.Count; ++i)
                    {
                        if (RenderCpt[i] != null)
                        {
                            ParticleRendererInitSortingOrders.Add(RenderCpt[i].sortingOrder);
                            Material[] arrayMaterial = RenderCpt[i].materials;
                            for (int j = 0, jCount = arrayMaterial.Length; j < jCount; ++j)
                            {
                                Material kMtl = arrayMaterial[j];
                                if (!AllMaterials.Contains(kMtl))
                                {
                                    AllMaterials.Add(kMtl);
                                }
                            }
                        }
                    }

                    var fxRootNode = RootGo.GetOrAddComponent<FXRootNode>();
                    fxRootNode.materialCount = AllMaterials.Count;
                    RootGo.SetLayer("PartialSceneObject");
                }
            }

            return succeed;
        }


        public override void OnUnload()
        {
            ListPool<ParticleSystem>.ReleaseRef(ref ParticleCpt);
            ListPool<Renderer>.ReleaseRef(ref RenderCpt);
            SimpleStructArrayPool<PoolObjectComponentsCache.ParticleInitState>.RecycleRef(ref ParticleInitStates);
            if (ParticleRendererInitSortingOrders != null)
            {
                ListPool<int>.ReleaseRef(ref ParticleRendererInitSortingOrders);
            }
            ListPool<Animator>.ReleaseRef(ref AnimatorCpts);
            ListPool<ParticleScaler>.ReleaseRef(ref ParticleScalers);
            ListPool<MeshRenderer>.ReleaseRef(ref MeshRenderers);
            ListPool<LineRenderer>.ReleaseRef(ref LineRenderers);
            ListPool<TrailRenderer>.ReleaseRef(ref TrailRenderers);
            if (AllMaterials != null)
            {
                ListPool<Material>.ReleaseRef(ref AllMaterials);
            }
        }

        public override void OnCache()
        {
            base.OnCache();

            if (ParticleCpt.Count != 0)
            {
                for (int i = 0; i < ParticleCpt.Count; i++)
                {
                    ParticleCpt[i].startSize = ParticleInitStates[i].StartSize;
                    ParticleCpt[i].startLifetime = ParticleInitStates[i].StartLifeTime;
                    ParticleCpt[i].startSpeed = ParticleInitStates[i].StartSpeed;
                    ParticleCpt[i].transform.localScale = ParticleInitStates[i].LocalScale;

                    if (ParticleCpt[i].enableEmission != ParticleInitStates[i].EmmitState)
                    {
                        ParticleCpt[i].enableEmission = ParticleInitStates[i].EmmitState;
                    }
                }
            }

            if (RenderCpt.Count != 0)
            {
                for (int i = 0; i < RenderCpt.Count; i++)
                {
                    RenderCpt[i].sortingOrder = ParticleRendererInitSortingOrders[i];
                }
            }
        }
    }

    public class ActorAsset : InstantiatableAsset
    {
        public Animation AnimationCpt;
        public Animator AnimatorCpt;
        public IPooledMonoBehaviour[] CachedIPooledMonos;//其实可以替换掉，先保留sgame的逻辑吧
        public MeshRenderer[] MeshRender;
        public SkinnedMeshRenderer[] SkinMeshRender;
        public ParticleSystem[] ParticleSystems;
        public ParticleSystemRenderer[] ParticleRenders;
        public AraTrail[] AraTrails;
        public Renderer[] Renderers;
        public DictionaryView<ulong, Transform> SubTrans = new DictionaryView<ulong, Transform>();
        public ActorResInfo ResInfo;
        private Bounds _aabb = new Bounds(Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f));
        private Vector3 _aabbOffset = Vector3.zero;
        public bool IsOldStyleAnimation = false;//是否以前为分离角色动画的方式，先兼容，后面要拔掉
        public Transform Trans { get; set; }
        private static List<MonoBehaviour> _monoList = new List<MonoBehaviour>();
        public override void OnCache()
        {
            RemoveShadowActor();
            UnbindAnimation();
            base.OnCache();
        }

        public override void OnDestroy()
        {
            RemoveShadowActor();
            UnbindAnimation();
            base.OnDestroy();
        }

        public override bool OnCreate()
        {
            var succeed = base.OnCreate();

            if (succeed)
            {
                AnimationCpt = RootGo.ExtGetComponentInChildren<Animation>();
                AnimatorCpt = RootGo.ExtGetComponentInChildren<Animator>();
                MeshRender = RootGo.ExtGetComponentsInChildren<MeshRenderer>(true);
                SkinMeshRender = RootGo.ExtGetComponentsInChildren<SkinnedMeshRenderer>(true);
                ParticleSystems = RootGo.ExtGetComponentsInChildren<ParticleSystem>(true);
                ParticleRenders = RootGo.ExtGetComponentsInChildren<ParticleSystemRenderer>(true);
                AraTrails = RootGo.ExtGetComponentsInChildren<AraTrail>(true);
                Renderers = RootGo.ExtGetComponentsInChildren<Renderer>(true);
                ResInfo = RootGo.ExtGetComponent<ActorResInfo>();
                Trans = RootGo.transform;
                _monoList.Clear();
                RootGo.GetComponentsInChildren<MonoBehaviour>(true, _monoList);
                InitAABB();
                if (AnimationCpt != null || AnimatorCpt != null)
                {
                    IsOldStyleAnimation = true;
                }

                int count = _monoList.Count;
                if (count > 0)
                {
                    int pooledMonoBehavioursCount = 0;

                    for (int i = 0; i < count; i++)
                    {
                        if (_monoList[i] is IPooledMonoBehaviour)
                        {
                            pooledMonoBehavioursCount++;
                        }
                    }

                    CachedIPooledMonos = new IPooledMonoBehaviour[pooledMonoBehavioursCount];

                    int cachedIPooledMonoIndex = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (_monoList[i] is IPooledMonoBehaviour)
                        {
                            CachedIPooledMonos[cachedIPooledMonoIndex] = _monoList[i] as IPooledMonoBehaviour;
                            cachedIPooledMonoIndex++;
                        }
                    }
                }
                else
                {
                    CachedIPooledMonos = new IPooledMonoBehaviour[0];
                }
            }

            return succeed;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            AnimationCpt = null;
            AnimatorCpt = null;
            MeshRender = null;
            SkinMeshRender = null;
            ParticleSystems = null;
            ParticleRenders = null;
            AraTrails = null;
            Renderers = null;
            ResInfo = null;
            IsOldStyleAnimation = false;
        }

        //预载用，仅收集依赖，预载后的资源最终以asset的形式放在cache池中
        public override List<AssetData> CollectAssociateAssets()
        {
            //ListView<string> materialPath = new ListView<string>();
            //ListView<string> meshPath = new ListView<string>();
            //GetAssociate(materialPath, meshPath);

            //var associateAssetData = new List<AssetData>();

            //if (materialPath.Count > 0)
            //{
            //    var materialService = MaterialService.GetInstance();
            //    for (int i = 0; i < materialPath.Count; i++)
            //    {
            //        if (string.IsNullOrEmpty(materialPath[i]))
            //        {
            //            continue;
            //        }

            //        AssetData data = MakeAssociateAssetData(AssetType.Material, materialPath[i], true);
            //        associateAssetData.Add(data);

            //        SingleMaterialConfig singleMaterialConfig = materialService.GetSingleMaterialConfig(materialPath[i]);

            //        if (singleMaterialConfig == null)
            //        {
            //            continue;
            //        }
            //        Dictionary<string, string> materialTextureDic = singleMaterialConfig.ToDictionary();

            //        var enumer = materialTextureDic.GetEnumerator();
            //        while (enumer.MoveNext())
            //        {
            //            string materialTexturePath = enumer.Current.Value;
            //            if (!string.IsNullOrEmpty(materialTexturePath))
            //            {
            //                AssetData textureAssetData = MakeAssociateAssetData(AssetType.Texture, materialTexturePath, true);
            //                associateAssetData.Add(textureAssetData);
            //            }
            //        }
            //    }
            //}

            //if (meshPath.Count > 0)
            //{
            //    for (int i = 0; i < meshPath.Count; i++)
            //    {
            //        if (string.IsNullOrEmpty(meshPath[i]))
            //        {
            //            continue;
            //        }

            //        AssetData data = MakeAssociateAssetData(AssetType.Mesh, meshPath[i], true);
            //        associateAssetData.Add(data);
            //    }
            //}

            return null;
        }

        private void GetAssociate(ListView<string> materialPaths, ListView<string> meshPaths)
        {
            if (ResInfo != null)
            {
                if (ResInfo.ActorRenders != null)
                {
                    ActorRenderElement[] actorRenders = ResInfo.ActorRenders;
                    for (int i = 0; i < actorRenders.Length; i++)
                    {
                        string[] originMaterialPaths = actorRenders[i].MaterialPaths;
                        for (int matIndex = 0; matIndex < originMaterialPaths.Length; matIndex++)
                        {
                            if (!string.IsNullOrEmpty(originMaterialPaths[matIndex]))
                            {
                                materialPaths.Add(originMaterialPaths[matIndex]);
                            }
                        }
                    }
                }

            }
        }

        //有cache则直接取asset，没有则加载。一边取asset，一边还原依赖
        public override void ApplyAssociateAssets()
        {
            //AssetService assetService = AssetService.GetInstance();
            //var materialService = MaterialService.GetInstance();
            //ActorResInfo actorResInfo = ResInfo;
            //if (actorResInfo != null && actorResInfo.ActorRenders != null)
            //{
            //    ActorRenderElement[] actorRenders = actorResInfo.ActorRenders;
            //    for (int i = 0; i < actorRenders.Length; i++)
            //    {
            //        string[] originMaterialPaths = actorRenders[i].MaterialPaths;
            //        Material[] materials = new Material[originMaterialPaths.Length];

            //        for (int matIndex = 0; matIndex < originMaterialPaths.Length; matIndex++)
            //        {
            //            MaterialAsset mast = assetService.LoadMaterialAsset(originMaterialPaths[matIndex], LifeType.FollowAsset);
            //            if (mast == null)
            //            {
            //                continue;
            //            }

            //            materials[matIndex] = mast.Material;
            //            AppendAssociateAsset(mast);

            //            SingleMaterialConfig singleMaterialConfig = materialService.GetSingleMaterialConfig(originMaterialPaths[matIndex]);
            //            if (singleMaterialConfig == null)
            //            {
            //                continue;
            //            }

            //            Dictionary<string, string> materialTextureDic = singleMaterialConfig.ToDictionary();
            //            var enumer = materialTextureDic.GetEnumerator();
            //            while (enumer.MoveNext())
            //            {
            //                string materialProperty = enumer.Current.Key;
            //                string materialTexturePath = enumer.Current.Value;
            //                if (!string.IsNullOrEmpty(materialTexturePath))
            //                {
            //                    TextureAsset tast = assetService.LoadTextureAsset(materialTexturePath, LifeType.FollowAsset);
            //                    if (tast != null)
            //                    {
            //                        materials[matIndex].SetTexture(materialProperty, tast.Texture);
            //                        AppendAssociateAsset(tast);
            //                    }
            //                }

            //            }
            //            actorRenders[i].RendererComponent.sharedMaterials = materials;
            //        }
            //    }
            //}
        }

        public void AddShadowActor(bool important)
        {
            RemoveShadowActor();

            // 如果身高小于1.2米的，也作为不重要角色
            important = important && _aabb.size.y > 1.2;
            //ShadowManager.GetInstance().AddShadowActor(this, important);
        }

        public void RemoveShadowActor()
        {
            //ShadowManager.GetInstance().RemoveShadowActor(this);
        }

        public void InitAABB()
        {
            int cnt = 0;
            for (int i = 0; i < MeshRender.Length; i++)
            {
                if (cnt == 0)
                {
                    _aabb = MeshRender[i].bounds;
                }
                else
                {
                    _aabb.Encapsulate(MeshRender[i].bounds);
                }
                cnt++;
            }
            for (int i = 0; i < SkinMeshRender.Length; i++)
            {
                if (cnt == 0)
                {
                    _aabb = SkinMeshRender[i].bounds;
                }
                else
                {
                    _aabb.Encapsulate(SkinMeshRender[i].bounds);
                }
                cnt++;
            }
            _aabbOffset = (_aabb.center - RootTf.position);
        }

        public Bounds GetAABB()
        {
            _aabb.center = RootTf.position + _aabbOffset;
            return _aabb;
        }

        public IPooledMonoBehaviour GetCachedMonobehaviourByType(System.Type type, bool canBeSubClass = true)
        {
            for (int i = 0; i < CachedIPooledMonos.Length; ++i)
            {
                if (CachedIPooledMonos[i] != null)
                {
                    if (CachedIPooledMonos[i].GetType() == type
                     || (canBeSubClass && CachedIPooledMonos[i].GetType().IsSubclassOf(type)))
                        return CachedIPooledMonos[i];
                }
            }
            return null;
        }

        public void VisitPooledMonos(PooledMonoVisitor visitor)
        {
#if USE_OPTIMIZE_POOL
            //what？？？
        //Handle Mono
        if (m_cachedMonos != null && m_cachedMonos.Length > 0)
        {
            for (int i = 0; i < m_cachedMonos.Length; i++)
            {
                if (m_cachedMonos[i] != null && m_cachedMonos[i] is IPooledMonoBehaviour)
                {
                    visitor(m_cachedMonos[i] as IPooledMonoBehaviour);
                }
            }
        }
#else
            //Handle IPooledMono
            if (CachedIPooledMonos != null && CachedIPooledMonos.Length > 0)
            {
                for (int i = 0; i < CachedIPooledMonos.Length; i++)
                {
                    if (CachedIPooledMonos[i] != null)
                    {
                        visitor(CachedIPooledMonos[i]);
                    }
                }
            }
#endif

#if UNITY_EDITOR && !SGAME_PROFILE && !SGAME_PROFILE_GC && !PERFORMANCE_SPECIAL
            // 检查是否存在Default材质
            Renderer[] renderers = Go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                foreach (Renderer render in renderers)
                {
                    foreach (Material mat in render.sharedMaterials)
                    {
                        if (CommonTools.IsMaterialUseDefaultShader(mat))
                        {
                            Log.LogE("Asset", string.Format("Load Object With Default Material! GoName = {0}", BaseData.Name));
                        }
                    }
                }
            }
#endif
        }


        public void SetAnimationSpeed(string clip, float spped)
        {
            AnimationState state = AnimationCpt[clip];
            if (state != null)
            {
                state.speed = spped;
            }
        }

        public float GetAnimationLength(string clip)
        {
            AnimationState state = AnimationCpt[clip];
            if (state != null)
            {
                return state.length;
            }
            return 0f;
        }

        public bool ContainsAnimation(string clip)
        {
            return AnimationCpt[clip] != null;
        }

        /// <summary>
        /// 模型和动画绑定(仅支持legency Animation组件)
        /// </summary>
        /// <param name="animation">动画包名</param>
        /// <param name="lifeType">生命周期</param>
        /// <returns></returns>
        public void BindAnimationWraps(string animationWraps, LifeType lifeType)
        {
            if (IsOldStyleAnimation)
            {
                return;
            }

            if (!string.IsNullOrEmpty(animationWraps))
            {
                string[] animations = animationWraps.Split('|');
                bool bind = false;
                for (int i = 0; i < animations.Length; i++)
                {
                    BaseAsset asset = AssetService.GetInstance().LoadPrimitiveAsset(animations[i], lifeType);
                    if (asset == null)
                    {
                        Log.LogE("ActorAsset.BindAnimation : failed to load animation - {0}", animations[i]);
                        continue;
                    }

                    if (!(asset.Resource.Content is AnimationWrap))
                    {
                        Log.LogE("ActorAsset.BindAnimation : animation error - {0}", animations[i]);
                        AssetService.GetInstance().Unload(asset);
                        continue;
                    }

                    bool attach;
                    if (!bind)
                    {
                        bind = BindAnimation(asset);
                        attach = bind;
                    }
                    else
                    {
                        attach = AppendAnimation(asset);
                    }

                    if (attach)
                    {
                        asset.BaseData.Life = LifeType.Immediate;
                    }

                    AssetService.GetInstance().Unload(asset);
                }
            }
        }

        public bool BindAnimation(BaseAsset aniAsset)
        {
            if (this.Go == null || aniAsset == null)
            {
                Log.LogE("Asset", "AnimationHelper - Bind(): bad paramater.");
                return false;
            }

            AnimationWrap group = aniAsset.Resource.Content as AnimationWrap;
            if (group == null)
            {
                Log.LogE("Asset", "AnimationHelper - Bind(): There's a null group in AnimationWrap {0}", aniAsset.Resource.m_relativePath);
                return false;
            }

            GameObject go = this.Go;
            UnityEngine.Animation animation = go.GetComponent<UnityEngine.Animation>();
            if (animation != null)
            {
                UnityEngine.Object.DestroyImmediate(animation);
            }

            UnityEngine.Animator animator = go.GetComponent<UnityEngine.Animator>();
            if (animator != null)
            {
                return false;
            }

            animation = go.AddComponent<UnityEngine.Animation>();
            if (animation == null)
            {
                return false;
            }

            // settings
            animation.playAutomatically = true;
            animation.cullingType = AnimationCullingType.AlwaysAnimate;

            // clips
            animation.clip = group.Clip;
            for (int i = 0; i < group.Clips.Length; i++)
            {
                if (group.Clips[i].Clip == null || string.IsNullOrEmpty(group.Clips[i].Name))
                {
                    continue;
                }

                animation.AddClip(group.Clips[i].Clip, group.Clips[i].Name);
            }

            AnimationCpt = animation;

            // 预播默认动画
            if (animation != null && animation.clip != null)
            {
                AnimationCpt.Play(animation.clip.name);
            }

            return true;
        }

        /// <summary>
        /// 添加动画
        /// </summary>
        /// <param name="aniAsset"></param>
        /// <returns></returns>
        public bool AppendAnimation(BaseAsset aniAsset)
        {
            if (this.Go == null || aniAsset == null)
            {
                Log.LogE("Asset", "AnimationHelper - Append(): bad paramater.");
                return false;
            }

            AnimationWrap group = aniAsset.Resource.Content as AnimationWrap;
            if (group == null)
            {
                Log.LogE("Asset", "AnimationHelper - Append(): There's a null group in AnimationWrap {0}", aniAsset.Resource.m_relativePath);
                return false;
            }

            GameObject go = this.Go;
            UnityEngine.Animation animation = go.GetComponent<UnityEngine.Animation>();
            if (animation == null)
            {
                Log.LogE("Asset", "AnimationHelper - Append(): animation component not exist.");
                return false;
            }

            // clips
            for (int i = 0; i < group.Clips.Length; i++)
            {
                if (group.Clips[i].Clip == null || string.IsNullOrEmpty(group.Clips[i].Name))
                {
                    Log.LogE("Asset", "AnimationHelper - Bind(): There's a null clip in AnimationWrap {0}", aniAsset.Resource.m_relativePath);
                    continue;
                }

                animation.AddClip(group.Clips[i].Clip, group.Clips[i].Name);
            }

            return true;
        }

        /// <summary>
        /// 模型和动画解绑
        /// </summary>
        public void UnbindAnimation()
        {
            if (IsOldStyleAnimation)
            {
                return;
            }

            if (this.Go != null)
            {
                UnityEngine.Animation animation = this.Go.GetComponent<UnityEngine.Animation>();
                animation.ExtDestroyImmediate();
            }

            this.AnimationCpt = null;
        }
    }


    public class UIAsset : InstantiatableAsset
    {
        public UIPrefabBase Prefab;
        public Vector3 OriginalAnchoredPosition3D = Vector3.zero;

        public override void OnCache()
        {
            if (Prefab is UIPrefab2D && RootTf is RectTransform)
            {
                string name = "";
                if (Resource != null && Resource.Content != null)
                {
                    name = Resource.Content.name;
                }
                // UI RectTransform 如果恢复localPosition会有问题，localPosition可能会被更改，但是UI相关的属性都没变
                RootTf.localRotation = OrignalRotate;
                RootTf.localScale = OrignalScale;
                (RootTf as RectTransform).anchoredPosition3D = OriginalAnchoredPosition3D;
                return;
            }

            base.OnCache();
        }


        public override bool OnCreate()
        {
            var succeed = base.OnCreate();

            if (succeed)
            {
                CNSService.BeginFlow(FlowId.All, FlowId.LoadUIAsset_Inst, BaseData.Name);
                Prefab = RootGo.ExtGetComponent<UIPrefabBase>();
                if (Prefab == null)
                {
                    Debug.Log("");
                }
                Prefab.Path = BaseData.Filename;

                if (Prefab is UIPrefab2D)
                {
                    if (RootTf is RectTransform)
                    {
                        OriginalAnchoredPosition3D = (RootTf as RectTransform).anchoredPosition3D;
                    }
                    else
                    {
                        string name = "";
                        if (Resource != null && Resource.Content != null)
                        {
                            name = Resource.Content.name;
                        }
                        Log.LogE("Asset", "AssetType is UI, but RootTf is not RectTransform." + name);
                    }
                }
                CNSService.EndFlow(FlowId.All, FlowId.LoadUIAsset_Inst);
            }

            return succeed;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            Prefab = null;
        }
    }

    public class TextureAsset : BaseAsset
    {
        public Texture2D Texture;

        public override bool OnCreate()
        {
            base.OnCreate();

            Texture = Resource.Content as Texture2D;

            return Texture != null;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            Texture = null;
        }
    }

    public class SpriteAsset : BaseAsset
    {
        public Sprite Sprite;

        public override bool OnCreate()
        {
            base.OnCreate();

            Sprite = Resource.Content as Sprite;

            return Sprite != null;
        }

        public override void OnUnload()
        {
            base.OnUnload();
            Sprite = null;
        }
    }

    public class MeshAsset : BaseAsset
    {
        public Mesh MeshData;

        public override bool OnCreate()
        {
            base.OnCreate();

            MeshData = Resource.Content as Mesh;

            return MeshData != null;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            MeshData = null;
        }
    }

    public class MaterialAsset : BaseAsset
    {
        public Material Material;

        public override bool OnCreate()
        {
            base.OnCreate();

            Material = Resource.Content as Material;
            Material = Material.ExtInstantiate() as Material;

            return Material != null;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            if (Material != null)
            {
                GameObject.Destroy(Material);
                Material = null;
            }
        }
    }

    public class RawAsset : BaseAsset
    {
        public byte[] bytes;

        public override bool OnCreate()
        {
            base.OnCreate();

            var bo = Resource.Content as BinaryObject;
            bytes = bo != null ? bo.bytes : null;

            return bytes != null;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            bytes = null;
        }
    }

    public class AgeAsset : BaseAsset
    {
        public ActionCommonData CommonData;
        public Cutscene Act;

        public override void OnCache()
        {
            CommonData = null;
            Act = null;

            base.OnCache();
        }

        public override void OnReuse()
        {
            base.OnReuse();

            OnCreate();
        }

        public override bool OnCreate()
        {
            base.OnCreate();
            return AssetProcessorAge.InstantiateWithXml(this, out CommonData, out Act);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            CommonData = null;
            Act = null;
        }
    }

    public abstract class ObjectAsset : InstantiatableAsset
    {
        public abstract string GetPath();
        public abstract void OnObjectAssetCreate();
        public abstract void OnObjectAssetDestroy();

        public override bool OnCreate()
        {
            var succeed = base.OnCreate();

            if (succeed)
            {
                OnObjectAssetCreate();
            }

            return succeed;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            OnObjectAssetDestroy();
        }
    }



    public enum SceneShowType {
       MainScene = 1,
       SubScene = 2
    }

    // 王者的Design场景资源
    public class WZDesignSceneAsset : InstantiatableAsset
    {

        private static GameSerializer s_serializer = new GameSerializer();
        protected void TraverseEnumerator(System.Collections.IEnumerator iter)
        {
            while (iter.MoveNext())
            {
                System.Collections.IEnumerator innerIter = iter.Current as System.Collections.IEnumerator;
                if (innerIter != null)
                {
                    TraverseEnumerator(innerIter);
                }
            }
        }
        protected override void InstantiateInstance()
        {
            ObjectHolder holder = new ObjectHolder();
            System.Collections.IEnumerator iter = s_serializer.LoadAsync(Resource.Bytes, holder);

            TraverseEnumerator(iter);

            RootGo = holder.obj as GameObject;
            if (null == RootGo)
            {
                DebugHelper.Assert(false, "策划场景" + "SceneExport/Design/" + BaseData.Filename + ".bytes有错误！请检查！");
                return;
            }

            RootTf = RootGo.transform;

            OrignalPosition = RootTf.localPosition;
            OrignalRotate = RootTf.localRotation;
            OrignalScale = RootTf.localScale;

            Transform staticRoot = RootTf.Find("StaticMesh");
            if (null != staticRoot)
            {
                StaticBatchingUtility.Combine(staticRoot.gameObject);
            }

            Camera camera = RootGo.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                camera.useOcclusionCulling = false;
            }
        }


        public override void OnUnload()
        {
            base.OnUnload();
        }

        public override void OnCache()
        {
            base.OnCache();
        }
    }



    // 王者的Design场景美术资源
    public class WZArtistSceneAsset : InstantiatableAsset
    {
        public class CustomizeLoadParam
        {
            public SceneLevelDefine sceneLevel = SceneLevelDefine.Count;
            public SceneShowType sceneShowType = SceneShowType.SubScene;
        }
        
        public string SceneName;
        private static GameSerializer s_serializer = new GameSerializer();
        protected void TraverseEnumerator(System.Collections.IEnumerator iter)
        {
            while (iter.MoveNext())
            {
                System.Collections.IEnumerator innerIter = iter.Current as System.Collections.IEnumerator;
                if (innerIter != null)
                {
                    TraverseEnumerator(innerIter);
                }
            }
        }
        protected override void InstantiateInstance()
        {
            var levelArtist = Resource.m_content as LevelResAsset;
            if (levelArtist == null || levelArtist.levelDom == null)
            {
                DebugHelper.Assert(false, "artist场景" + "SceneExport/Artist/" + BaseData.Filename + ".bytes有错误！请检查！");
                return;
            }

            UnityObjMgr.GetInstance().IncArtistCount();
            ObjectHolder holder = new ObjectHolder();
            System.Collections.IEnumerator iter = s_serializer.LoadAsync(levelArtist.levelDom.bytes, holder);

            TraverseEnumerator(iter);

            RootGo = holder.obj as GameObject;
            if (null == RootGo)
            {
                DebugHelper.Assert(false, "artist场景" + "SceneExport/Artist/" + BaseData.Filename + ".bytes有错误！请检查！");
                return;
            }

            SceneName = System.IO.Path.GetFileNameWithoutExtension(BaseData.Filename);
            EventRouter.instance.BroadCastEvent(EventID.CSharpLevelLoaded, SceneName, RootGo);
            // add by rogercheng， 根据和garra ，赵磊讨论，此处artist都不用camera。统一用moba_camera
            // 为什么又不能在编辑时不序列化呢。因为camera放到了virtualscene下面，virtualscene又是一个prefab。按
            // 王者老的序列化，prefab上的节点干不掉
            var loadParam = BaseData.AssetParma as CustomizeLoadParam;
            if(loadParam.sceneShowType == SceneShowType.SubScene){
                Yarp.LegacyForwardRenderer forwardRender = RootGo.GetComponentInChildren<Yarp.LegacyForwardRenderer>(true);
                if (forwardRender != null)
                {
                    GameObject.Destroy(forwardRender);
                }

                Yarp.CameraData cameraData = RootGo.GetComponentInChildren<Yarp.CameraData>(true);
                if (cameraData != null)
                {
                    GameObject.Destroy(cameraData);
                }
                Camera camera = RootGo.GetComponentInChildren<Camera>(true);
                if (camera != null)
                {
                    camera.enabled = false;
                    //GameObject.Destroy(camera);
                }
            }
            RootTf = RootGo.transform;

            OrignalPosition = RootTf.localPosition;
            OrignalRotate = RootTf.localRotation;
            OrignalScale = RootTf.localScale;

            Light[] lights = RootGo.GetComponentsInChildren<Light>(true);
            if (lights != null)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type != LightType.Directional)
                    {
                        lights[i].enabled = false;
                    }
                }
            }
        }

        public override void OnUnload()
        {
            base.OnUnload();
        }

        public override void OnCache()
        {
            base.OnCache();
        }
    }

    public class DesignSceneAsset : InstantiatableAsset
    {
        public GameCamera GameCamera;

        protected override void InstantiateInstance()
        {
            DomResource domRes = Resource as DomResource;
            if (domRes == null)
            {
                return;
            }

            RootGo = Level.SceneSerialize.DeserializeDesignNode(domRes.bakedObject as IDomDocument) as GameObject;

            if (RootGo != null)
            {
                RootTf = RootGo.transform;

                OrignalPosition = RootTf.localPosition;
                OrignalRotate = RootTf.localRotation;
                OrignalScale = RootTf.localScale;
            }
        }

        public override bool OnCreate()
        {
            var succeed = base.OnCreate();

            if (succeed)
            {
                GameCamera = new GameCamera();
                GameCamera.AllNode = new List<ObjectNode>();
                for (int i = 0; i < RootGo.transform.childCount; i++)
                {
                    var tempRoot = RootGo.transform.GetChild(i);
                    if (tempRoot.name.ToLower().Contains("camera"))
                    {
                        var AllTransform = tempRoot.GetComponentsInChildren<Transform>(true);
                        if (AllTransform.Length > 20)
                        {
                            Log.LogE("Asset", "AssetProcessor.Instantiate : failed to instantiate camera子节点太多,请检查非相机节点是否放入相机节点 - {0}", BaseData.Name);
                            return false;
                        }
                        for (int j = 0; j < AllTransform.Length; j++)
                        {
                            ObjectNode objectNode = new ObjectNode();
                            objectNode.Node = AllTransform[j];
                            objectNode.NodeInitPosition = AllTransform[j].localPosition;
                            objectNode.NodeInitRotate = AllTransform[j].localEulerAngles;
                            GameCamera.AllNode.Add(objectNode);
                        }
                        break;
                    }
                }
            }

            return succeed;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            GameCamera = null;
        }

        public override void OnCache()
        {
            base.OnCache();

            for (int i = 0; i < GameCamera.AllNode.Count; i++)
            {
                var node = GameCamera.AllNode[i];
                node.Node.localPosition = node.NodeInitPosition;
                node.Node.localEulerAngles = node.NodeInitRotate;
            }
        }
    }
}
