//-----------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Sirenix.Serialization
{
#pragma warning disable

    using Sirenix.Serialization.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Serializes and deserializes a given type, and wraps serialization and deserialization with all the proper calls to free formatters from tedious boilerplate.
    /// <para />
    /// Whenever serializing or deserializing anything, it is *strongly recommended* to use <see cref="Serializer.Get{T}"/> to get a proper wrapping serializer for that type.
    /// <para />
    /// NOTE: This class should NOT be inherited from; it is hard-coded into the system.
    /// <para />
    /// To extend the serialization system, instead create custom formatters, which are used by the <see cref="ComplexTypeSerializer{T}"/> class.
    /// </summary>
    public abstract class Serializer
    {
        private static readonly Dictionary<Type, Type> PrimitiveReaderWriterTypes = new Dictionary<Type, Type>()
        {
            { typeof(char), typeof(CharSerializer) },
            { typeof(string), typeof(StringSerializer) },
            { typeof(sbyte), typeof(SByteSerializer) },
            { typeof(short), typeof(Int16Serializer) },
            { typeof(int), typeof(Int32Serializer) },
            { typeof(long), typeof(Int64Serializer) },
            { typeof(byte), typeof(ByteSerializer) },
            { typeof(ushort), typeof(UInt16Serializer) },
            { typeof(uint),   typeof(UInt32Serializer) },
            { typeof(ulong),  typeof(UInt64Serializer) },
            { typeof(decimal),   typeof(DecimalSerializer) },
            { typeof(bool),  typeof(BooleanSerializer) },
            { typeof(float),   typeof(SingleSerializer) },
            { typeof(double),  typeof(DoubleSerializer) },
            { typeof(IntPtr),   typeof(IntPtrSerializer) },
            { typeof(UIntPtr),  typeof(UIntPtrSerializer) },
            { typeof(Guid),  typeof(GuidSerializer) }
        };

        private static readonly object LOCK = new object();

        private static readonly Dictionary<Type, Serializer> ReaderWriterCache = new Dictionary<Type, Serializer>(FastTypeComparer.Instance);

#if UNITY_EDITOR

        /// <summary>
        /// Editor-only event that fires whenever a serializer serializes a type.
        /// </summary>
        public static event Action<Type> OnSerializedType;

#endif

        /// <summary>
        /// Fires the <see cref="OnSerializedType"/> event.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        protected static void FireOnSerializedType(Type type)
        {
#if UNITY_EDITOR
            if (OnSerializedType != null)
            {
                OnSerializedType(type);
            }
#endif
        }

        /// <summary>
        /// Gets a <see cref="Serializer"/> for the given value. If the value is null, it will be treated as a value of type <see cref="object"/>.
        /// </summary>
        /// <param name="value">The value to get a <see cref="Serializer"/> for.</param>
        /// <returns>A <see cref="Serializer"/> for the given value.</returns>
        public static Serializer GetForValue(object value)
        {
            if (object.ReferenceEquals(value, null))
            {
                return Get(typeof(object));
            }
            else
            {
                return Get(value.GetType());
            }
        }

        /// <summary>
        /// Gets a <see cref="Serializer"/> for type T.
        /// </summary>
        /// <typeparam name="T">The type to get a <see cref="Serializer"/> for.</typeparam>
        /// <returns>A <see cref="Serializer"/> for type T.</returns>
        public static Serializer<T> Get<T>()
        {
            return (Serializer<T>)Serializer.Get(typeof(T));
        }

        /// <summary>
        /// Gets a <see cref="Serializer"/> for the given type.
        /// </summary>
        /// <param name="type">The type to get a <see cref="Serializer"/> for.</param>
        /// <returns>A <see cref="Serializer"/> for the given type.</returns>
        /// <exception cref="System.ArgumentNullException">The type argument is null.</exception>
        public static Serializer Get(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Serializer result;

            lock (LOCK)
            {
                if (ReaderWriterCache.TryGetValue(type, out result) == false)
                {
                    result = Create(type);
                    ReaderWriterCache.Add(type, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Reads a value weakly, casting it into object. Use this method if you don't know what type you're going to be working with at compile time.
        /// </summary>
        /// <param name="reader">The reader to use.</param>
        /// <returns>The value which has been read.</returns>
        public abstract object ReadValueWeak(IDataReader reader);

        /// <summary>
        /// Writes a weakly typed value. Use this method if you don't know what type you're going to be working with at compile time.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="writer">The writer to use.</param>
        public void WriteValueWeak(object value, IDataWriter writer)
        {
            this.WriteValueWeak(null, value, writer);
        }

        /// <summary>
        /// Writes a weakly typed value with a given name. Use this method if you don't know what type you're going to be working with at compile time.
        /// </summary>
        /// <param name="name">The name of the value to write.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="writer">The writer to use.</param>
        public abstract void WriteValueWeak(string name, object value, IDataWriter writer);

        private static Serializer Create(Type type)
        {
            try
            {
                Type resultType = null;

                if (type.IsEnum)
                {
                    resultType = typeof(EnumSerializer<>).MakeGenericType(type);
                }
                else if (FormatterUtilities.IsPrimitiveType(type))
                {
                    try
                    {
                        resultType = PrimitiveReaderWriterTypes[type];
                    }
                    catch (KeyNotFoundException)
                    {
                        UnityEngine.Debug.LogError("Failed to find primitive serializer for " + type.Name);
                    }
                }
                else
                {
                    resultType = typeof(ComplexTypeSerializer<>).MakeGenericType(type);
                }

                return (Serializer)Activator.CreateInstance(resultType);
            }
            // System.ExecutionEngineException is marked obsolete in .NET 4.6
            // That's all very good for .NET, but Unity still uses it!
#pragma warning disable 618
            catch (TargetInvocationException ex)
            {
                if (ex.GetBaseException() is ExecutionEngineException)
                {
                    LogAOTError(type, ex.GetBaseException() as ExecutionEngineException);
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
            catch (TypeInitializationException ex)
            {
                if (ex.GetBaseException() is ExecutionEngineException)
                {
                    LogAOTError(type, ex.GetBaseException() as ExecutionEngineException);
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
            catch (ExecutionEngineException ex)
            {
                LogAOTError(type, ex);
                return null;
            }
        }

        private static void LogAOTError(Type type, ExecutionEngineException ex)
        {
            UnityEngine.Debug.LogError("No AOT serializer was pre-generated for the type '" + type.GetNiceFullName() + "'. " +
                "Please use Odin's AOT generation feature to generate an AOT dll before building, and ensure that '" +
                type.GetNiceFullName() + "' is in the list of supported types after a scan. If it is not, please " +
                "report an issue and add it to the list manually.");

            throw new SerializationAbortException("AOT serializer was missing for type '" + type.GetNiceFullName() + "'.");
        }
    }

#pragma warning restore 618

    /// <summary>
    /// Serializes and deserializes the type <see cref="T"/>, and wraps serialization and deserialization with all the proper calls to free formatters from tedious boilerplate.
    /// <para />
    /// Whenever serializing or deserializing anything, it is *strongly recommended* to use <see cref="Serializer.Get{T}"/> to get a proper wrapping serializer for that type.
    /// <para />
    /// NOTE: This class should NOT be inherited from; it is hard-coded into the system.
    /// <para />
    /// To extend the serialization system, instead create custom formatters, which are used by the <see cref="ComplexTypeSerializer{T}"/> class.
    /// </summary>
    /// <typeparam name="T">The type which the <see cref="Serializer{T}"/> can serialize and deserialize.</typeparam>
    public abstract class Serializer<T> : Serializer
    {
        /// <summary>
        /// Reads a value of type <see cref="T"/> weakly, casting it into object. Use this method if you don't know what type you're going to be working with at compile time.
        /// </summary>
        /// <param name="reader">The reader to use.</param>
        /// <returns>
        /// The value which has been read.
        /// </returns>
        public override object ReadValueWeak(IDataReader reader)
        {
            return this.ReadValue(reader);
        }

        /// <summary>
        /// Writes a weakly typed value of type <see cref="T"/> with a given name. Use this method if you don't know what type you're going to be working with at compile time.
        /// </summary>
        /// <param name="name">The name of the value to write.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="writer">The writer to use.</param>
        public override void WriteValueWeak(string name, object value, IDataWriter writer)
        {
            this.WriteValue(name, (T)value, writer);
        }

        /// <summary>
        /// Reads a value of type <see cref="T"/>.
        /// </summary>
        /// <param name="reader">The reader to use.</param>
        /// <returns>
        /// The value which has been read.
        /// </returns>
        public abstract T ReadValue(IDataReader reader);

        /// <summary>
        /// Writes a value of type <see cref="T"/>.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="writer">The writer to use.</param>
        public void WriteValue(T value, IDataWriter writer)
        {
            this.WriteValue(null, value, writer);
        }

        /// <summary>
        /// Writes a value of type <see cref="T"/> with a given name.
        /// </summary>
        /// <param name="name">The name of the value to write.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="writer">The writer to use.</param>
        public abstract void WriteValue(string name, T value, IDataWriter writer);

        /// <summary>
        /// Fires the <see cref="OnSerializedType"/> event with the T generic argument of the serializer.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        protected static void FireOnSerializedType()
        {
            FireOnSerializedType(typeof(T));
        }
    }
}