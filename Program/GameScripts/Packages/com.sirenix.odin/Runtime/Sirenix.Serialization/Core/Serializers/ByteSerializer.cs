//-----------------------------------------------------------------------
// <copyright file="ByteSerializer.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Sirenix.Serialization
{
#pragma warning disable

    /// <summary>
    /// Serializer for the <see cref="byte"/> type.
    /// </summary>
    /// <seealso cref="Serializer{System.Byte}" />
    public sealed class ByteSerializer : Serializer<byte>
    {
        /// <summary>
        /// Reads a value of type <see cref="byte" />.
        /// </summary>
        /// <param name="reader">The reader to use.</param>
        /// <returns>
        /// The value which has been read.
        /// </returns>
        public override byte ReadValue(IDataReader reader)
        {
            string name;
            var entry = reader.PeekEntry(out name);

            if (entry == EntryType.Integer)
            {
                byte value;
                if (reader.ReadByte(out value) == false)
                {
                    reader.Context.Config.DebugContext.LogWarning("Failed to read entry '" + name + "' of type " + entry.ToString());
                }
                return value;
            }
            else
            {
                reader.Context.Config.DebugContext.LogWarning("Expected entry of type " + EntryType.Integer.ToString() + ", but got entry '" + name + "' of type " + entry.ToString());
                reader.SkipEntry();
                return default(byte);
            }
        }

        /// <summary>
        /// Writes a value of type <see cref="byte" />.
        /// </summary>
        /// <param name="name">The name of the value to write.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="writer">The writer to use.</param>
        public override void WriteValue(string name, byte value, IDataWriter writer)
        {
            FireOnSerializedType();
            writer.WriteByte(name, value);
        }
    }
}