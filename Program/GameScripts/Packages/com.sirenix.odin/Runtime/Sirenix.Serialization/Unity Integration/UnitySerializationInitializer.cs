//-----------------------------------------------------------------------
// <copyright file="UnitySerializationInitializer.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Sirenix.Serialization
{
#pragma warning disable

    using UnityEngine;

    /// <summary>
    /// Utility class which initializes the Sirenix serialization system to be compatible with Unity.
    /// </summary>
    public static class UnitySerializationInitializer
    {
        private static readonly object LOCK = new object();
        private static bool initialized = false;

        public static bool Initialized { get { return initialized; } }

        public static RuntimePlatform CurrentPlatform { get; private set; }

        /// <summary>
        /// Initializes the Sirenix serialization system to be compatible with Unity.
        /// </summary>
        public static void Initialize()
        {
            if (!initialized)
            {
                GlobalSerializationConfig.LoadInstanceIfAssetExists();

                CurrentPlatform = Application.platform;

                if (Application.isEditor) return;

                ArchitectureInfo.SetRuntimePlatform(CurrentPlatform);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeRuntime()
        {
            Initialize();
        }

#if UNITY_EDITOR

        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            Initialize();
        }
#endif
    }
}
