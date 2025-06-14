//-----------------------------------------------------------------------
// <copyright file="AOTSupportUtilities.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
#if UNITY_EDITOR

namespace Sirenix.Serialization.Editor
{
#pragma warning disable

    using Sirenix.Serialization.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Scripting;

    public static class AOTSupportUtilities
    {
        /// <summary>
        /// Scans the project's build scenes and resources, plus their dependencies, for serialized types to support. Progress bars are shown during the scan.
        /// </summary>
        /// <param name="serializedTypes">The serialized types to support.</param>
        /// <param name="scanBuildScenes">Whether to scan the project's build scenes.</param>
        /// <param name="scanAllAssetBundles">Whether to scan all the project's asset bundles.</param>
        /// <param name="scanPreloadedAssets">Whether to scan the project's preloaded assets.</param>
        /// <param name="scanResources">Whether to scan the project's resources.</param>
        /// <param name="resourcesToScan">An optional list of the resource paths to scan. Only has an effect if the scanResources argument is true. All the resources will be scanned if null.</param>
        /// <returns>true if the scan succeeded, false if the scan failed or was cancelled</returns>
        public static bool ScanProjectForSerializedTypes(out List<Type> serializedTypes, bool scanBuildScenes = true, bool scanAllAssetBundles = true, bool scanPreloadedAssets = true, bool scanResources = true, List<string> resourcesToScan = null, bool scanAddressables = true)
        {
            using (var scanner = new AOTSupportScanner())
            {
                scanner.BeginScan();

                if (scanBuildScenes && !scanner.ScanBuildScenes(includeSceneDependencies: true, showProgressBar: true))
                {
                    Debug.Log("Project scan canceled while scanning scenes and their dependencies.");
                    serializedTypes = null;
                    return false;
                }

                if (scanResources && !scanner.ScanAllResources(includeResourceDependencies: true, showProgressBar: true, resourcesPaths: resourcesToScan))
                {
                    Debug.Log("Project scan canceled while scanning resources and their dependencies.");
                    serializedTypes = null;
                    return false;
                }

                if (scanAllAssetBundles && !scanner.ScanAllAssetBundles(showProgressBar: true))
                {
                    Debug.Log("Project scan canceled while scanning asset bundles and their dependencies.");
                    serializedTypes = null;
                    return false;
                }

                if (scanPreloadedAssets && !scanner.ScanPreloadedAssets(showProgressBar: true))
                {
                    Debug.Log("Project scan canceled while scanning preloaded assets and their dependencies.");
                    serializedTypes = null;
                    return false;
                }

                if (scanAddressables && !scanner.ScanAllAddressables(includeAssetDependencies: true, showProgressBar: true))
                {
                    Debug.Log("Project scan canceled while scanning addressable assets and their dependencies.");
                    serializedTypes = null;
                    return false;
                }

                serializedTypes = scanner.EndScan();
            }
            return true;
        }

        /// <summary>
        /// Generates an AOT DLL, using the given parameters.
        /// </summary>
        public static void GenerateDLL(string dirPath, string assemblyName, List<Type> supportSerializedTypes, bool generateLinkXml = true)
        {
            if (!dirPath.EndsWith("/")) dirPath += "/";

            var newDllPath = dirPath + assemblyName;
            var fullDllPath = newDllPath + ".dll";

            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName() { Name = assemblyName }, AssemblyBuilderAccess.Save, dirPath);
            var module = assembly.DefineDynamicModule(assemblyName);

            assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(EmittedAssemblyAttribute).GetConstructor(new Type[0]), new object[0]));

            // The following is a fix for Unity's crappy Mono runtime that doesn't know how to do this sort
            //  of stuff properly
            //
            // We must manually remove the "Default Dynamic Assembly" module that is automatically defined,
            //  otherwise a reference to that non-existent module will be saved into the assembly's IL, and
            //  that will cause a multitude of issues.
            //
            // We do this by forcing there to be only one module - the one we just defined, and we set the
            //   manifest module to be that module as well.
            {
                var modulesField = assembly.GetType().GetField("modules", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var manifestModuleField = assembly.GetType().GetField("manifest_module", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (modulesField != null)
                {
                    modulesField.SetValue(assembly, new ModuleBuilder[] { module });
                }

                if (manifestModuleField != null)
                {
                    manifestModuleField.SetValue(assembly, module);
                }
            }

            var type = module.DefineType(assemblyName + ".PreventCodeStrippingViaReferences", TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NotPublic);

            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(typeof(PreserveAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
            type.SetCustomAttribute(attributeBuilder);

            var staticConstructor = type.DefineTypeInitializer();
            var il = staticConstructor.GetILGenerator();

            var falseLocal = il.DeclareLocal(typeof(bool));

            il.Emit(OpCodes.Ldc_I4_0);                  // Load false
            il.Emit(OpCodes.Stloc, falseLocal);         // Set to falseLocal

            HashSet<Type> seenTypes = new HashSet<Type>();

            if (UnityVersion.Major == 2019 && UnityVersion.Minor == 2)
            {
                // This here is a hack that fixes Unity's assembly updater triggering faultily in Unity 2019.2
                // (and in early 2019.3 alphas/betas, but we're not handling those). When it triggers, it edits
                // the generated AOT assembly such that it immediately causes Unity to hard crash. Having this 
                // type reference present in the assembly prevents that from happening. (Any concrete type in
                // the serialization assembly would work, this one is just a random pick.)
                // 
                // Unity should have fixed this in 2019.3, but said that a backport to 2019.2 is not guaranteed
                // to occur, though it might.

                supportSerializedTypes.Add(typeof(DateTimeFormatter));
            }

            //var endPoint = il.DefineLabel();
            //il.Emit(OpCodes.Br, endPoint);

            foreach (var serializedType in supportSerializedTypes)
            {
                if (serializedType == null) continue;

                bool isAbstract = serializedType.IsAbstract || serializedType.IsInterface;
                
                if (serializedType.IsGenericType && (serializedType.IsGenericTypeDefinition || !serializedType.IsFullyConstructedGenericType()))
                {
                    Debug.LogError("Skipping type '" + serializedType.GetNiceFullName() + "'! Type is a generic type definition, or its arguments contain generic parameters; type must be a fully constructed generic type.");
                    continue;
                }

                if (seenTypes.Contains(serializedType)) continue;

                seenTypes.Add(serializedType);

                // Reference serialized type
                {
                    if (serializedType.IsValueType)
                    {
                        var local = il.DeclareLocal(serializedType);

                        il.Emit(OpCodes.Ldloca, local);
                        il.Emit(OpCodes.Initobj, serializedType);
                    }
                    else if (!isAbstract)
                    {
                        var constructor = serializedType.GetConstructor(Type.EmptyTypes);

                        if (constructor != null)
                        {
                            il.Emit(OpCodes.Newobj, constructor);
                            il.Emit(OpCodes.Pop);
                        }
                    }
                }

                // Reference and/or create formatter type
                if (!FormatterUtilities.IsPrimitiveType(serializedType) && !typeof(UnityEngine.Object).IsAssignableFrom(serializedType) && !isAbstract)
                {
                    var actualFormatter = FormatterLocator.GetFormatter(serializedType, SerializationPolicies.Unity);

                    if (actualFormatter.GetType().IsDefined<EmittedFormatterAttribute>())
                    {
                        //TODO: Make emitted formatter code compatible with IL2CPP
                        //// Emit an actual AOT formatter into the generated assembly

                        //if (this.emitAOTFormatters)
                        //{
                        //    var emittedFormatter = FormatterEmitter.EmitAOTFormatter(typeEntry.Type, module, SerializationPolicies.Unity);
                        //    var emittedFormatterConstructor = emittedFormatter.GetConstructor(Type.EmptyTypes);

                        //    il.Emit(OpCodes.Newobj, emittedFormatterConstructor);
                        //    il.Emit(OpCodes.Pop);
                        //}
                    }

                    var formatters = FormatterLocator.GetAllCompatiblePredefinedFormatters(serializedType, SerializationPolicies.Unity);

                    foreach (var formatter in formatters)
                    {
                        // Reference the pre-existing formatter

                        var formatterConstructor = formatter.GetType().GetConstructor(Type.EmptyTypes);

                        if (formatterConstructor != null)
                        {
                            il.Emit(OpCodes.Newobj, formatterConstructor);
                            il.Emit(OpCodes.Pop);
                        }
                    }

                    //// Make sure we have a proper reflection formatter variant if all else goes wrong
                    //il.Emit(OpCodes.Newobj, typeof(ReflectionFormatter<>).MakeGenericType(serializedType).GetConstructor(Type.EmptyTypes));
                    //il.Emit(OpCodes.Pop);
                }

                ConstructorInfo serializerConstructor;

                // Reference serializer variant
                if (serializedType.IsValueType)
                {
                    serializerConstructor = Serializer.Get(serializedType).GetType().GetConstructor(Type.EmptyTypes);

                    il.Emit(OpCodes.Newobj, serializerConstructor);

                    // The following section is a fix for an issue on IL2CPP for PS4, where sometimes bytecode isn't
                    //   generated for methods in base types of needed types - FX, Serializer<T>.ReadValueWeak()
                    //   may be missing. This only seems to happen in a relevant way for value types.
                    {
                        var endLabel = il.DefineLabel();

                        // Load a false local value, then jump to the end of this segment of code due to that
                        //   false value. This is an attempt to trick any potential code flow analysis made
                        //   by IL2CPP that checks whether this segment of code is actually run.
                        //
                        // We don't run the code because if we did, that would actually throw a bunch of
                        //   exceptions from invalid calls to ReadValueWeak and WriteValueWeak.
                        il.Emit(OpCodes.Ldloc, falseLocal);
                        il.Emit(OpCodes.Brfalse, endLabel);

                        var baseSerializerType = typeof(Serializer<>).MakeGenericType(serializedType);

                        var readValueWeakMethod = baseSerializerType.GetMethod("ReadValueWeak", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { typeof(IDataReader) }, null);
                        var writeValueWeakMethod = baseSerializerType.GetMethod("WriteValueWeak", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { typeof(string), typeof(object), typeof(IDataWriter) }, null);

                        il.Emit(OpCodes.Dup);                               // Duplicate serializer instance
                        il.Emit(OpCodes.Ldnull);                            // Load null argument for IDataReader reader
                        il.Emit(OpCodes.Callvirt, readValueWeakMethod);     // Call ReadValueWeak on serializer instance
                        il.Emit(OpCodes.Pop);                               // Pop result of ReadValueWeak

                        il.Emit(OpCodes.Dup);                               // Duplicate serializer instance
                        il.Emit(OpCodes.Ldnull);                            // Load null argument for string name
                        il.Emit(OpCodes.Ldnull);                            // Load null argument for object value
                        il.Emit(OpCodes.Ldnull);                            // Load null argument for IDataWriter writer
                        il.Emit(OpCodes.Callvirt, writeValueWeakMethod);    // Call WriteValueWeak on serializer instance

                        il.MarkLabel(endLabel);                             // This is where the code always jumps to, skipping the above
                    }

                    il.Emit(OpCodes.Pop);       // Pop the serializer instance
                }
                else
                {
                    serializerConstructor = typeof(ComplexTypeSerializer<>).MakeGenericType(serializedType).GetConstructor(Type.EmptyTypes);
                    il.Emit(OpCodes.Newobj, serializerConstructor);
                    il.Emit(OpCodes.Pop);
                }

            }

            //il.MarkLabel(endPoint);
            il.Emit(OpCodes.Ret);

            type.CreateType();

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            if (File.Exists(fullDllPath))
            {
                File.Delete(fullDllPath);
            }

            if (File.Exists(fullDllPath + ".meta"))
            {
                File.Delete(fullDllPath + ".meta");
            }

            try
            {
                AssetDatabase.Refresh();
            }
            catch (Exception)
            {
                // Sigh, Unity 5.3.0
            }

            assembly.Save(assemblyName);

            File.Move(newDllPath, fullDllPath);

            if (generateLinkXml)
            {
                File.WriteAllText(dirPath + "link.xml",
    @"<linker>
       <assembly fullname=""" + assemblyName + @""" preserve=""all""/>
</linker>");
            }

            try
            {
                AssetDatabase.Refresh();
            }
            catch (Exception)
            {
                // Sigh, Unity 5.3.0
            }

            var pluginImporter = PluginImporter.GetAtPath(fullDllPath) as PluginImporter;

            if (pluginImporter != null)
            {
                //pluginImporter.ClearSettings();

                pluginImporter.SetCompatibleWithEditor(false);
                pluginImporter.SetCompatibleWithAnyPlatform(true);

                // Disable for all standalones
                pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
                
                if (!UnityVersion.IsVersionOrGreater(2019, 2))
                {
                    pluginImporter.SetCompatibleWithPlatform((BuildTarget)17, false);       // StandaloneLinux
                    pluginImporter.SetCompatibleWithPlatform((BuildTarget)25, false);       // StandaloneLinuxUniversal
                }

                // StandaloneOSXUniversal (<= 2017.2) / StandaloneOSX (>= 2017.3)
                pluginImporter.SetCompatibleWithPlatform((BuildTarget)2, false);

                if (!UnityVersion.IsVersionOrGreater(2017, 3))
                {
                    pluginImporter.SetCompatibleWithPlatform((BuildTarget)4, false);        // StandaloneOSXIntel
                    pluginImporter.SetCompatibleWithPlatform((BuildTarget)27, false);       // StandaloneOSXIntel64
                }

                pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
                pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);

                //pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, false);

                pluginImporter.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
        }
    }
}

#endif