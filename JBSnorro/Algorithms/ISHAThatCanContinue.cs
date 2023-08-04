using System.Security.Cryptography;

namespace JBSnorro.Algorithms;

public interface ISHAThatCanContinue : IDisposable
{
    /// <summary>
    /// Creates a <see cref="ISHAThatCanContinue"/> that cannot continue.
    /// </summary>
    public static ISHAThatCanContinue CreateOneShot()
    {
        return new SHA1Wrapper();
    }
    /// <summary>
    /// Creates a <see cref="ISHAThatCanContinue"/> that can continue, i.e. append data multiple times.
    /// </summary>
    public static ISHAThatCanContinue Create()
    {
        return new SHA1CryptoServiceProvider();
    }

    void AppendHashData(ReadOnlySpan<byte> source);
    string AppendFinalHashData(ReadOnlySpan<byte> source)
    {
        this.AppendHashData(source);
        return this.ToString();
    }
    string ToString();

    private sealed class SHA1CryptoServiceProvider : SHA1, ISHAThatCanContinue
    {
        private readonly IncrementalHash _incrementalHash;
        private bool _running;
        private bool _started;
        private byte[] digest = new byte[20];

        public SHA1CryptoServiceProvider()
        {
            _incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
            HashSizeValue = HashSizeInBits;
        }

        public override void Initialize()
        {
            if (_running)
            {
                Span<byte> destination = stackalloc byte[HashSizeInBytes];

                if (!_incrementalHash.TryGetHashAndReset(destination, out _))
                {
                    System.Diagnostics.Debug.Fail("Reset expected a properly sized buffer.");
                    throw new CryptographicException();
                }

                _running = false;
            }
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _running = _started = true;
            _incrementalHash.AppendData(array, ibStart, cbSize);
        }

        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            _running = _started = true;
            _incrementalHash.AppendData(source);
        }

        protected override byte[] HashFinal()
        {
            _running = false;
            return _incrementalHash.GetHashAndReset();
        }

        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            _running = false;
            return _incrementalHash.TryGetHashAndReset(destination, out bytesWritten);
        }

        // The Hash and HashSize properties are not overridden since the correct values are returned from base.

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _incrementalHash.Dispose();
            }
            base.Dispose(disposing);
        }

        void ISHAThatCanContinue.AppendHashData(ReadOnlySpan<byte> source)
        {
            if (!_running && _started) throw new InvalidOperationException("Finished");

            HashCore(source);
        }
        string ISHAThatCanContinue.AppendFinalHashData(ReadOnlySpan<byte> source)
        {
            if (!_running && _started) throw new InvalidOperationException("Finished");

            this.HashCore(source);
            if (!this.TryHashFinal(this.digest, out int bytesWritten))
                throw new Exception("!TryHashFinal");
            return this.ToString()!;
        }

        public override string ToString()
        {
            if (!_started) throw new InvalidOperationException("Not yet started");
            if (_running)
            {
                ((ISHAThatCanContinue)this).AppendFinalHashData(Array.Empty<byte>());
            }

            var result = BitConverter.ToString(digest);
            return result;
        }
    }

    internal sealed class SHA1Wrapper : ISHAThatCanContinue
    {
        private readonly SHA1 sha = SHA1.Create();
        private bool disposed;
        private byte[]? digest;
        public void AppendHashData(ReadOnlySpan<byte> source)
        {
            if (disposed) throw new ObjectDisposedException(typeof(SHA1Wrapper).Name);
            if (digest != null) throw new Exception("Already appended hash data");

            this.digest = new byte[20];
            if (!sha.TryComputeHash(source, digest.AsSpan(), out var _))
                throw new Exception();
        }

        string ISHAThatCanContinue.AppendFinalHashData(ReadOnlySpan<byte> source)
        {
            if (disposed) throw new ObjectDisposedException(typeof(SHA1Wrapper).Name);

            this.AppendHashData(source);
            return this.ToString();
        }
        public void Dispose()
        {
            this.disposed = true;
            this.sha.Dispose();
        }
        public override string ToString()
        {
            if (digest == null) throw new Exception("Not yet appended hash data");

            return BitConverter.ToString(digest);
        }
    }
}