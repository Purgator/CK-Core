using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Simple <see cref="SerializeSimple(ICKSimpleBinarySerializable)"/> and <see cref="DeserializeSimple{T}(byte[])"/>.
    /// </summary>
    public static class SimpleSerializable
    {
        /// <summary>
        /// Serializes a <see cref="ICKSimpleBinarySerializable"/> as a byte array.
        /// </summary>
        /// <param name="this">This object.</param>
        /// <returns>The serialized object.</returns>
        public static byte[] SerializeSimple( this ICKSimpleBinarySerializable @this )
        {
            Throw.CheckNotNullArgument( @this );
            using( var s = Util.RecyclableStreamManager.GetStream() )
            using( var w = new CKBinaryWriter( s, Encoding.UTF8, true ) )
            {
                @this.Write( w );
                w.Flush();
                return s.ToArray();
            }
        }

        /// <summary>
        /// Serializes a <see cref="ICKVersionedBinarySerializable"/> as a byte array.
        /// </summary>
        /// <param name="this">This object.</param>
        /// <returns>The serialized object.</returns>
        public static byte[] SerializeVersioned( this ICKVersionedBinarySerializable @this )
        {
            Throw.CheckNotNullArgument( @this );
            using( var s = Util.RecyclableStreamManager.GetStream() )
            using( var w = new CKBinaryWriter( s, Encoding.UTF8, true ) )
            {
                w.WriteNonNegativeSmallInt32( SerializationVersionAttribute.GetRequiredVersion( @this.GetType() ) );
                @this.WriteData( w );
                w.Flush();
                return s.ToArray();
            }
        }

        /// <summary>
        /// Deserializes a <see cref="ICKSimpleBinarySerializable"/> from serialized bytes.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="bytes">Serialized bytes.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeSimple<T>( byte[] bytes ) where T : ICKSimpleBinarySerializable
        {
            using var m = new MemoryStream( bytes );
            return DeserializeSimple<T>( m );
        }

        /// <summary>
        /// Deserializes a <see cref="ICKSimpleBinarySerializable"/> from serialized bytes.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="bytes">Serialized bytes.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeSimple<T>( ReadOnlyMemory<byte> bytes ) where T : ICKSimpleBinarySerializable
        {
            return DeserializeSimple<T>( bytes.AsStream() );
        }

        /// <summary>
        /// Deserializes a <see cref="ICKSimpleBinarySerializable"/> from serialized bytes.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="bytes">The stream.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeSimple<T>( Stream bytes ) where T : ICKSimpleBinarySerializable
        {
            using( var r = new CKBinaryReader( bytes, Encoding.UTF8, true ) )
            {
                return (T)Activator.CreateInstance( typeof( T ), r )!;
            }
        }

        /// <summary>
        /// Deserializes a <see cref="ICKVersionedBinarySerializable"/> from serialized bytes.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="bytes">Serialized bytes.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeVersioned<T>( ReadOnlyMemory<byte> bytes ) where T : ICKVersionedBinarySerializable
        {
            return DeserializeVersioned<T>( bytes.AsStream() );
        }

        /// <summary>
        /// Deserializes a <see cref="ICKVersionedBinarySerializable"/> from serialized bytes.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="bytes">Serialized bytes.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeVersioned<T>( byte[] bytes ) where T : ICKVersionedBinarySerializable
        {
            using var m = new MemoryStream( bytes );
            return DeserializeVersioned<T>( m );
        }

        /// <summary>
        /// Deserializes a <see cref="ICKVersionedBinarySerializable"/> from serialized bytes.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="bytes">Serialized bytes.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeVersioned<T>( Stream bytes ) where T : ICKVersionedBinarySerializable
        {
            using( var r = new CKBinaryReader( bytes, Encoding.UTF8, true ) )
            {
                return (T)Activator.CreateInstance( typeof( T ), r, r.ReadNonNegativeSmallInt32() )!;
            }
        }

        /// <summary>
        /// Deep clones a <see cref="ICKSimpleBinarySerializable"/> by serializing/deserializing it.
        /// When this instance is null, null is returned.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="this">This object .</param>
        /// <returns>A cloned instance.</returns>
        [return: NotNullIfNotNull( "this" )]
        public static T? DeepClone<T>( this T? @this ) where T : ICKSimpleBinarySerializable => DeepCloneSimple( @this );

        /// <summary>
        /// Deep clones a <see cref="ICKSimpleBinarySerializable"/> by serializing/deserializing it.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="o">The object to clone.</param>
        /// <returns>The clone or null if the object to clone is null.</returns>
        [return: NotNullIfNotNull( "o" )]
        public static T? DeepCloneSimple<T>( T? o ) where T : ICKSimpleBinarySerializable
        {
            if( o is null ) return default;
            using( var s = Util.RecyclableStreamManager.GetStream() )
            using( var w = new CKBinaryWriter( s, Encoding.UTF8, true ) )
            {
                o.Write( w );
                w.Flush();
                s.Position = 0;
                using( var r = new CKBinaryReader( s, Encoding.UTF8, true ) )
                {
                    return (T)Activator.CreateInstance( o.GetType(), r )!;
                }
            }
        }

        /// <summary>
        /// Deep clones a <see cref="ICKVersionedBinarySerializable"/> by serializing/deserializing it.
        /// The <typeparamref name="T"/> must be the runtime type of <paramref name="o"/>
        /// otherwise an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        /// <typeparam name="T">The object's type.</typeparam>
        /// <param name="o">The object to clone.</param>
        /// <returns>The clone or null if the object to clone is null.</returns>
        [return: NotNullIfNotNull( "o" )]
        public static T? DeepCloneVersioned<T>( T? o ) where T : ICKVersionedBinarySerializable
        {
            if( o is null ) return default;
            if( typeof( T ) != o.GetType() ) Throw.ArgumentException( $"Type parameter '{typeof( T )}' must be the same as the runtime type '{o.GetType()}'." );
            using( var s = Util.RecyclableStreamManager.GetStream() )
            using( var w = new CKBinaryWriter( s, Encoding.UTF8, true ) )
            {
                o.WriteData( w );
                w.Flush();
                s.Position = 0;
                using( var r = new CKBinaryReader( s, Encoding.UTF8, true ) )
                {
                    return (T)Activator.CreateInstance( typeof( T ), r, SerializationVersionAttribute.GetRequiredVersion( o.GetType() ) )!;
                }
            }
        }


    }
}
