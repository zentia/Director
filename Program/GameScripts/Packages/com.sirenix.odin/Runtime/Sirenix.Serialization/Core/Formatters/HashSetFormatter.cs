//-----------------------------------------------------------------------
// <copyright file="HashSetFormatter.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using Sirenix.Serialization;

[assembly: RegisterFormatter(typeof(HashSetFormatter<>))]

namespace Sirenix.Serialization
{
#pragma warning disable

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Custom generic formatter for the generic type definition <see cref="HashSet{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the formatted list.</typeparam>
    /// <seealso cref="BaseFormatter{System.Collections.Generic.HashSet{T}}" />
    public class HashSetFormatter<T> : BaseFormatter<HashSet<T>>
    {
        private static readonly Serializer<T> TSerializer = Serializer.Get<T>();

        static HashSetFormatter()
        {
            // This exists solely to prevent IL2CPP code stripping from removing the generic type's instance constructor
            // which it otherwise seems prone to do, regardless of what might be defined in any link.xml file.

            new HashSetFormatter<int>();
        }

        public HashSetFormatter()
        {
        }

        /// <summary>
        /// Returns null.
        /// </summary>
        /// <returns>
        /// A null value.
        /// </returns>
        protected override HashSet<T> GetUninitializedObject()
        {
            return null;
        }

        /// <summary>
        /// Provides the actual implementation for deserializing a value of type <see cref="T" />.
        /// </summary>
        /// <param name="value">The uninitialized value to serialize into. This value will have been created earlier using <see cref="BaseFormatter{T}.GetUninitializedObject" />.</param>
        /// <param name="reader">The reader to deserialize with.</param>
        protected override void DeserializeImplementation(ref HashSet<T> value, IDataReader reader)
        {
            string name;
            var entry = reader.PeekEntry(out name);

            if (entry == EntryType.StartOfArray)
            {
                try
                {
                    long length;
                    reader.EnterArray(out length);
                    value = new HashSet<T>();

                    // We must remember to register the hashset reference ourselves, since we return null in GetUninitializedObject
                    this.RegisterReferenceID(value, reader);

                    // There aren't any relevant OnDeserializing callbacks on hash sets.
                    // Hence we don't invoke this.InvokeOnDeserializingCallbacks(value, reader, context);
                    for (int i = 0; i < length; i++)
                    {
                        if (reader.PeekEntry(out name) == EntryType.EndOfArray)
                        {
                            reader.Context.Config.DebugContext.LogError("Reached end of array after " + i + " elements, when " + length + " elements were expected.");
                            break;
                        }

                        value.Add(TSerializer.ReadValue(reader));

                        if (reader.IsInArrayNode == false)
                        {
                            // Something has gone wrong
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
        protected override void SerializeImplementation(ref HashSet<T> value, IDataWriter writer)
        {
            try
            {
                writer.BeginArrayNode(value.Count);

                foreach (T item in value)
                {
                    try
                    {
                        TSerializer.WriteValue(item, writer);
                    }
                    catch (Exception ex)
                    {
                        writer.Context.Config.DebugContext.LogException(ex);
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