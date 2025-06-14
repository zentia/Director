//-----------------------------------------------------------------------
// <copyright file="StackFormatter.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using Sirenix.Serialization;

[assembly: RegisterFormatter(typeof(StackFormatter<,>))]

namespace Sirenix.Serialization
{
#pragma warning disable

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Custom generic formatter for the generic type definition <see cref="Stack{T}"/> and types derived from it.
    /// </summary>
    /// <typeparam name="T">The element type of the formatted stack.</typeparam>
    /// <seealso cref="BaseFormatter{System.Collections.Generic.Stack{T}}" />
    public class StackFormatter<TStack, TValue> : BaseFormatter<TStack>
        where TStack : Stack<TValue>, new()
    {
        private static readonly Serializer<TValue> TSerializer = Serializer.Get<TValue>();
        private static readonly object List_LOCK = new object();
        private static readonly List<TValue> List = new List<TValue>();
        private static readonly bool IsPlainStack = typeof(TStack) == typeof(Stack<TValue>);

        static StackFormatter()
        {
            // This exists solely to prevent IL2CPP code stripping from removing the generic type's instance constructor
            // which it otherwise seems prone to do, regardless of what might be defined in any link.xml file.

            new StackFormatter<Stack<int>, int>();
        }

        public StackFormatter()
        {
        }

        /// <summary>
        /// Returns null.
        /// </summary>
        /// <returns>
        /// A null value.
        /// </returns>
        protected override TStack GetUninitializedObject()
        {
            return null;
        }

        /// <summary>
        /// Provides the actual implementation for deserializing a value of type <see cref="T" />.
        /// </summary>
        /// <param name="value">The uninitialized value to serialize into. This value will have been created earlier using <see cref="BaseFormatter{T}.GetUninitializedObject" />.</param>
        /// <param name="reader">The reader to deserialize with.</param>
        protected override void DeserializeImplementation(ref TStack value, IDataReader reader)
        {
            string name;
            var entry = reader.PeekEntry(out name);

            if (entry == EntryType.StartOfArray)
            {
                try
                {
                    long length;
                    reader.EnterArray(out length);

                    if (IsPlainStack)
                    {
                        value = (TStack)new Stack<TValue>((int)length);
                    }
                    else
                    {
                        value = new TStack();
                    }

                    // We must remember to register the stack reference ourselves, since we return null in GetUninitializedObject
                    this.RegisterReferenceID(value, reader);

                    // There aren't any OnDeserializing callbacks on stacks.
                    // Hence we don't invoke this.InvokeOnDeserializingCallbacks(value, reader, context);
                    for (int i = 0; i < length; i++)
                    {
                        if (reader.PeekEntry(out name) == EntryType.EndOfArray)
                        {
                            reader.Context.Config.DebugContext.LogError("Reached end of array after " + i + " elements, when " + length + " elements were expected.");
                            break;
                        }

                        value.Push(TSerializer.ReadValue(reader));

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
        protected override void SerializeImplementation(ref TStack value, IDataWriter writer)
        {
            try
            {
                writer.BeginArrayNode(value.Count);

                lock (List_LOCK)
                {
                    List.Clear();

                    foreach (var element in value)
                    {
                        List.Add(element);
                    }

                    for (int i = List.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            TSerializer.WriteValue(List[i], writer);
                        }
                        catch (Exception ex)
                        {
                            writer.Context.Config.DebugContext.LogException(ex);
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