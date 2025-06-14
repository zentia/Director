//-----------------------------------------------------------------------
// <copyright file="DictionaryFormatter.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using Sirenix.Serialization;

[assembly: RegisterFormatter(typeof(DictionaryFormatter<,>))]

namespace Sirenix.Serialization
{
#pragma warning disable

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Custom generic formatter for the generic type definition <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
    /// <seealso cref="BaseFormatter{System.Collections.Generic.Dictionary{TKey, TValue}}" />
    public sealed class DictionaryFormatter<TKey, TValue> : BaseFormatter<Dictionary<TKey, TValue>>
    {
        private static readonly bool KeyIsValueType = typeof(TKey).IsValueType;

        private static readonly Serializer<IEqualityComparer<TKey>> EqualityComparerSerializer = Serializer.Get<IEqualityComparer<TKey>>();
        private static readonly Serializer<TKey> KeyReaderWriter = Serializer.Get<TKey>();
        private static readonly Serializer<TValue> ValueReaderWriter = Serializer.Get<TValue>();

        static DictionaryFormatter()
        {
            // This exists solely to prevent IL2CPP code stripping from removing the generic type's instance constructor
            // which it otherwise seems prone to do, regardless of what might be defined in any link.xml file.

            new DictionaryFormatter<int, string>();
        }

        /// <summary>
        /// Creates a new instance of <see cref="DictionaryFormatter{TKey, TValue}"/>.
        /// </summary>
        public DictionaryFormatter()
        {
        }

        /// <summary>
        /// Returns null.
        /// </summary>
        /// <returns>
        /// A value of null.
        /// </returns>
        protected override Dictionary<TKey, TValue> GetUninitializedObject()
        {
            return null;
        }

        /// <summary>
        /// Provides the actual implementation for deserializing a value of type <see cref="T" />.
        /// </summary>
        /// <param name="value">The uninitialized value to serialize into. This value will have been created earlier using <see cref="BaseFormatter{T}.GetUninitializedObject" />.</param>
        /// <param name="reader">The reader to deserialize with.</param>
        protected override void DeserializeImplementation(ref Dictionary<TKey, TValue> value, IDataReader reader)
        {
            string name;
            var entry = reader.PeekEntry(out name);

            IEqualityComparer<TKey> comparer = null;

            //                        TODO: Remove this clause in patch 1.1 or later, when it has had time to take effect in people's serialized data
            //                              Clause was introduced in the patch released after 1.0.5.3
            if (name == "comparer" || entry != EntryType.StartOfArray)
            {
                // There is a comparer serialized
                comparer = EqualityComparerSerializer.ReadValue(reader);
                entry = reader.PeekEntry(out name);
            }

            if (entry == EntryType.StartOfArray)
            {
                try
                {
                    long length;
                    reader.EnterArray(out length);
                    Type type;

                    value = object.ReferenceEquals(comparer, null) ?
                        new Dictionary<TKey, TValue>((int)length) :
                        new Dictionary<TKey, TValue>((int)length, comparer);

                    // We must remember to register the dictionary reference ourselves, since we return null in GetUninitializedObject
                    this.RegisterReferenceID(value, reader);

                    // There aren't any OnDeserializing callbacks on dictionaries that we're interested in.
                    // Hence we don't invoke this.InvokeOnDeserializingCallbacks(value, reader, context);
                    for (int i = 0; i < length; i++)
                    {
                        if (reader.PeekEntry(out name) == EntryType.EndOfArray)
                        {
                            reader.Context.Config.DebugContext.LogError("Reached end of array after " + i + " elements, when " + length + " elements were expected.");
                            break;
                        }

                        bool exitNode = true;

                        try
                        {
                            reader.EnterNode(out type);
                            TKey key = KeyReaderWriter.ReadValue(reader);
                            TValue val = ValueReaderWriter.ReadValue(reader);

                            if (!KeyIsValueType && object.ReferenceEquals(key, null))
                            {
                                reader.Context.Config.DebugContext.LogWarning("Dictionary key of type '" + typeof(TKey).FullName + "' was null upon deserialization. A key has gone missing.");
                                continue;
                            }

                            value[key] = val;
                        }
                        catch (SerializationAbortException ex)
                        {
                            exitNode = false;
                            throw ex;
                        }
                        catch (Exception ex)
                        {
                            reader.Context.Config.DebugContext.LogException(ex);
                        }
                        finally
                        {
                            if (exitNode)
                            {
                                reader.ExitNode();
                            }
                        }

                        if (reader.IsInArrayNode == false)
                        {
                            reader.Context.Config.DebugContext.LogError("Reading array went wrong. Data dump: " + reader.GetDataDump());
                            break;
                        }
                    }
                }
                finally
                {
                    reader.ExitArray();
                }
            }
            else
            {
                reader.SkipEntry();
            }
        }

        /// <summary>
        /// Provides the actual implementation for serializing a value of type <see cref="T" />.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="writer">The writer to serialize with.</param>
        protected override void SerializeImplementation(ref Dictionary<TKey, TValue> value, IDataWriter writer)
        {
            try
            {
                if (value.Comparer != null)
                {
                    EqualityComparerSerializer.WriteValue("comparer", value.Comparer, writer);
                }

                writer.BeginArrayNode(value.Count);

                foreach (var pair in value)
                {
                    bool endNode = true;

                    try
                    {
                        writer.BeginStructNode(null, null);
                        KeyReaderWriter.WriteValue("$k", pair.Key, writer);
                        ValueReaderWriter.WriteValue("$v", pair.Value, writer);
                    }
                    catch (SerializationAbortException ex)
                    {
                        endNode = false;
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        writer.Context.Config.DebugContext.LogException(ex);
                    }
                    finally
                    {
                        if (endNode)
                        {
                            writer.EndNode(null);
                        }
                    }
                }
            }
            finally
            {
                writer.EndArrayNode();
            }
        }
    }
}