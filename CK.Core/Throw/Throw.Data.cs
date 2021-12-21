using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CK.Core
{
    public partial class Throw
    {
        /// <summary>
        /// Throws a new <see cref="System.IO.InvalidDataException"/>.
        /// </summary>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <param name="innerException">Optional inner <see cref="Exception"/> to include.</param>
        [DoesNotReturn]
        public static void InvalidDataException( string? message = null, Exception? innerException = null )
        {
            throw new InvalidDataException( message, innerException );
        }

        /// <summary>
        /// Throws a new <see cref="System.IO.EndOfStreamException"/>.
        /// </summary>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <param name="innerException">Optional inner <see cref="Exception"/> to include.</param>
        [DoesNotReturn]
        public static void EndOfStreamException( string? message = null, Exception? innerException = null )
        {
            throw new EndOfStreamException( message, innerException );
        }

        /// <summary>
        /// Throws a new <see cref="System.IO.XmlException"/>.
        /// </summary>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <param name="innerException">Optional inner <see cref="Exception"/> to include.</param>
        [DoesNotReturn]
        public static void XmlException( string? message = null, Exception? innerException = null )
        {
            throw new XmlException( message, innerException );
        }

        /// <summary>
        /// Throws a new <see cref="System.FormatException"/>.
        /// </summary>
        /// <param name="message">Optional message to include in the exception.</param>
        /// <param name="innerException">Optional inner <see cref="Exception"/> to include.</param>
        [DoesNotReturn]
        public static void FormatException( string? message = null, Exception? innerException = null )
        {
            throw new FormatException( message, innerException );
        }

    }
}
