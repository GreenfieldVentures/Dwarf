using System;
using System.IO;
using System.Linq;

namespace Dwarf.Extensions
{
    /// <summary>
    /// Static class for holding extension methods for Streams
    /// </summary>
    public static class StreamExtensions
    {
        #region ToByteArray

        /// <summary>
        /// Returns the stream as a byte array
        /// </summary>
        public static byte[] ToByteArray(this Stream stream)
        {
            stream.Position = 0;

            var buffer = new byte[32768];

            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);

                    if (read <= 0)
                        return ms.ToArray();

                    ms.Write(buffer, 0, read);
                }
            }
        }

        #endregion ToByteArray

        #region SaveAs

        /// <summary>
        /// Save the current stream to the specified file path
        /// </summary>
        public static void SaveAs(this MemoryStream stream, string path)
        {
            if (Path.GetInvalidPathChars().Any(path.Contains))
                throw new InvalidOperationException("Path contains invalid characters");

            File.WriteAllBytes(path, stream.ToArray());
        }

        /// <summary>
        /// Save the current stream to the specified file path
        /// </summary>
        public static void SaveAs(this Stream stream, string path)
        {
            if (Path.GetInvalidPathChars().Any(path.Contains))
                throw new InvalidOperationException("Path contains invalid characters");

            File.WriteAllBytes(path, stream.ToByteArray());
        }

        #endregion SaveAs

        #region CopyStream

        /// <summary>
        /// Returns a copy of the current stream
        /// </summary>
        public static MemoryStream CopyStream(this Stream stream)
        {
            var newStream = new MemoryStream();
            var buffer = new byte[32768];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                newStream.Write(buffer, 0, read);
            }

            return newStream;
        }

        #endregion CopyStream

        #region Rewind

        /// <summary>
        /// Rewinds the stream to position 0
        /// </summary>
        public static MemoryStream Rewind(this MemoryStream memoryStream)
        {
            memoryStream.Position = 0;
            return memoryStream;
        }

        #endregion Rewind
    }
}
