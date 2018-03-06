using System.IO;

namespace HttpBatchHandler.Tests
{
    public static class TestUtilities
    {
        public static Stream GetNormalizedContentStream(string file)
        {
            var originalStream =
                typeof(TestUtilities).Assembly.GetManifestResourceStream(typeof(MultipartParserTests), file);

            // It's possible that git (or other tools) will automatically convert crlf line endings from the 
            // text files to lf on linux/mac machines.  The Multipart spec is looking for crlf and thus line endings
            // of lf are invalid.  Therefore, for these tests to be valid we need to make sure all line endings are
            // crlf and not just lf.
            var reader = new StreamReader(originalStream);
            var content = reader.ReadToEnd();

            content = content.Replace("\r\n", "\n").Replace("\n", "\r\n");

            var normalizedStream = new MemoryStream();
            var writer = new StreamWriter(normalizedStream);
            writer.Write(content);
            writer.Flush();
            normalizedStream.Position = 0;

            return normalizedStream;
        }
    }
}