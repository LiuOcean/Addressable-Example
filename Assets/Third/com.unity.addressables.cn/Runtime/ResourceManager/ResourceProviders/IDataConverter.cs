using System.IO;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Interface for converting data.  This can be used to provide a custom encryption strategy for asset bundles.
    /// </summary>
    public interface IDataConverter
    {
        /// <summary>
        /// Create a stream for converting raw data into a format to be consumed by the game.  This method will be called by the build process when preparing data.
        /// </summary>
        /// <param name="input">The raw data to convert.</param>
        /// <param name="id">The id of the stream, useful for debugging.</param>
        /// <returns>Stream that converts the input data.</returns>
        Stream CreateWriteStream(Stream input, string id);
        /// <summary>
        /// Create a stream for transforming converted data back into the format that is expected by the player.  This method will be called at runtime.
        /// </summary>
        /// <param name="input">The converted data to transform.</param>
        /// <param name="id">The id of the stream, useful for debugging..</param>
        /// <returns>Stream that transforms the input data.  For best performance, this stream should support seeking.  If not, a full memory copy of the data must be made.</returns>
        Stream CreateReadStream(Stream input, string id);
    }
}