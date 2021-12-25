using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    public class AESStreamProcessor : IDataConverter
    {
        byte[] Key { get { return System.Text.Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOP"); } }
        SymmetricAlgorithm m_algorithm;
        SymmetricAlgorithm Algorithm
        {
            get
            {
                if (m_algorithm == null)
                {
                    m_algorithm = new AesManaged();
                    m_algorithm.Padding = PaddingMode.Zeros;
                    var initVector = new byte[m_algorithm.BlockSize / 8];
                    for (int i = 0; i < initVector.Length; i++)
                        initVector[i] = (byte)i;
                    m_algorithm.IV = initVector;
                    m_algorithm.Key = Key;
                    m_algorithm.Mode = CipherMode.ECB;
                }
                return m_algorithm;
            }
        }
        public Stream CreateReadStream(Stream input, string id)
        {
            return new CryptoStream(input, Algorithm.CreateDecryptor(Algorithm.Key, Algorithm.IV), CryptoStreamMode.Read);
        }

        public Stream CreateWriteStream(Stream input, string id)
        {
            return new CryptoStream(input, Algorithm.CreateEncryptor(Algorithm.Key, Algorithm.IV), CryptoStreamMode.Write);
        }
    }

    public class SeekableEncryptionStream : Stream
    {
        Stream m_Source;
        public override bool CanRead => m_Source.CanRead;
        public override bool CanSeek => m_Source.CanSeek;
        public override bool CanWrite => m_Source.CanWrite;
        public override long Length => m_Source.Length;
        public override long Position { get => m_Source.Position; set => m_Source.Position = value; }
        public SeekableEncryptionStream(Stream src) { m_Source = src; }
        public override void Flush() => m_Source.Flush();
        public override long Seek(long offset, SeekOrigin origin) => m_Source.Seek(offset, origin);
        public override void SetLength(long value) => m_Source.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count)
        {
            byte s = (byte)'b';
            var transformed = new byte[count];
            var ret = m_Source.Read(transformed, 0, count);
            for(int i = 0; i < ret; i++)
                buffer[offset + i] = (byte)(transformed[i] ^ s);
            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte s = (byte)'b';
            var transformed = new byte[count];
            for (int i = 0; i < count; i++)
                transformed[i] = (byte)(buffer[offset + i] ^ s);
            m_Source.Write(transformed, 0, count);
        }
    }

    public class GZipDataStreamProc : IDataConverter
    {
        public Stream CreateReadStream(Stream input, string id)
        {
            return new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        }

        public Stream CreateWriteStream(Stream input, string id)
        {
            return new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Compress);
        }
    }

    public class AESStreamProcessorWithSeek : IDataConverter
    {
        string password = "password";
        byte[] salt = new byte[16] {0x01, 0x02, 0x01, 0x05, 0x10, 0xAA, 0xBB, 0xCC, 0xDD, 0xF1, 0xF2, 0xF3, 0xF4, 0xF4, 0xE5, 0xE6};
        public Stream CreateReadStream(Stream input, string id)
        {
            Debug.Log(id);
            return new SeekableAesStream(input, password, salt);
        }

        public Stream CreateWriteStream(Stream input, string id)
        {
            Debug.Log(id);
            return new SeekableAesStream(input, password, salt);
        }
    }


    public class SeekableAesStream : Stream
    {
        private Stream baseStream;
        private AesManaged aes;
        private ICryptoTransform encryptor;
        public bool autoDisposeBaseStream { get; set; } = true;

        /// <param name="salt"></param>
        public SeekableAesStream(Stream baseStream, string password, byte[] salt)
        {
            this.baseStream = baseStream;
            using (var key = new PasswordDeriveBytes(password, salt))
            {
                aes = new AesManaged();
                aes.KeySize = 128;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = new byte[16]; //zero buffer is adequate since we have to use new salt for each stream
                encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            }
        }

        private void cipher(byte[] buffer, int offset, int count, long streamPos)
        {
            //find block number
            var blockSizeInByte = aes.BlockSize / 8;
            var blockNumber = (streamPos / blockSizeInByte) + 1;
            var keyPos = streamPos % blockSizeInByte;

            //buffer
            var outBuffer = new byte[blockSizeInByte];
            var nonce = new byte[blockSizeInByte];
            var init = false;

            for (int i = offset; i < count; i++)
            {
                //encrypt the nonce to form next xor buffer (unique key)
                if (!init || (keyPos % blockSizeInByte) == 0)
                {
                    BitConverter.GetBytes(blockNumber).CopyTo(nonce, 0);
                    encryptor.TransformBlock(nonce, 0, nonce.Length, outBuffer, 0);
                    if (init) keyPos = 0;
                    init = true;
                    blockNumber++;
                }
                buffer[i] ^= outBuffer[keyPos]; //simple XOR with generated unique key
                keyPos++;
            }
        }

        public override bool CanRead { get { return baseStream.CanRead; } }
        public override bool CanSeek { get { return baseStream.CanSeek; } }
        public override bool CanWrite { get { return baseStream.CanWrite; } }
        public override long Length { get { return baseStream.Length; } }
        public override long Position { get { return baseStream.Position; } set { baseStream.Position = value; } }
        public override void Flush() { baseStream.Flush(); }
        public override void SetLength(long value) { baseStream.SetLength(value); }
        public override long Seek(long offset, SeekOrigin origin) { return baseStream.Seek(offset, origin); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var streamPos = Position;
            var ret = baseStream.Read(buffer, offset, count);
            cipher(buffer, offset, count, streamPos);
            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            cipher(buffer, offset, count, Position);
            baseStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                encryptor?.Dispose();
                aes?.Dispose();
                if (autoDisposeBaseStream)
                    baseStream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}