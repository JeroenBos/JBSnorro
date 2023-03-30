#nullable enable
using JBSnorro.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace JBSnorro.Algorithms;

public interface ISHAThatCanContinue : IDisposable
{
    public static ISHAThatCanContinue CreateOneShot()
    {
        return new SHA1Wrapper();
    }
    public static ISHAThatCanContinue Create()
    {
        return new SHA1CryptoServiceProvider(); // SHA1ImplementationThatCanContinue.Create();
    }

    void AppendHashData(ReadOnlySpan<byte> source);
    string AppendFinalHashData(ReadOnlySpan<byte> source)
    {
        this.AppendHashData(source);
        return this.ToString();
    }
    string ToString();

    public sealed class SHA1CryptoServiceProvider : SHA1, ISHAThatCanContinue
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
    private sealed class SHA1ImplementationThatCanContinue : SHA1, ISHAThatCanContinue
    {
        public new static SHA1ImplementationThatCanContinue Create() => new();

        private readonly byte[] digest = new byte[20];
        private SHA1ImplementationThatCanContinue()
        {
            _hashProvider = new HashProvider("SHA1");
        }
        sealed class HashProvider : IDisposable
        {
            private delegate void AppendHashDataROS(ReadOnlySpan<byte> source);
            private static List<Type> x = Assembly.GetAssembly(typeof(SHA1))!.GetTypes().Where(t => t.FullName.Contains("SHA")).ToList();
            private static readonly Type t = Assembly.GetAssembly(typeof(SHA1))!.GetTypes().Where(t => t.FullName == "System.Security.Cryptography.SHAManagedHashProvider").First()!;
            private static readonly ConstructorInfo createHashProvider = t.GetConstructor(new[] { typeof(string) })!;
            private static readonly MethodInfo _appendHashDataAII = t.GetMethod("AppendHashData", new Type[] { typeof(byte[]), typeof(int), typeof(int) })!;
            private static readonly PropertyInfo hashSizeInBytes = t.GetProperty("hashSizeInBytes")!;
            private readonly MethodInfo finalizeHashAndReset = t.GetMethod("FinalizeHashAndReset", Array.Empty<Type>())!;

            private readonly AppendHashDataROS _appendHashDataROS;
            private readonly object obj;
            public HashProvider(string shaId)
            {
                this.obj = createHashProvider.Invoke(new object[] { shaId });
                this._appendHashDataROS = t.GetMethod("AppendHashData", new Type[] { typeof(ReadOnlySpan<byte>) })!.CreateDelegate<AppendHashDataROS>(this.obj);
            }

            public void AppendHashData(ReadOnlySpan<byte> source)
            {
                _appendHashDataROS(source);
            }
            public void AppendHashData(byte[] a, int i, int c)
            {
                _appendHashDataAII.Invoke(obj, new object[] { a, i, c });
            }
            public int HashSizeInBytes
            {
                get => (int)hashSizeInBytes.GetValue(this.obj)!;
            }
            public byte[] FinalizeHashAndReset()
            {
                return (byte[])finalizeHashAndReset.Invoke(this.obj, Array.Empty<object>())!;
            }
            public void Dispose()
            {
                ((IDisposable)this.obj).Dispose();
            }
        }

        private readonly HashProvider _hashProvider;
        private bool disposed = false;
        protected sealed override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (disposed) throw new ObjectDisposedException(typeof(SHA1ImplementationThatCanContinue).Name);

            _hashProvider.AppendHashData(array, ibStart, cbSize);
        }
        protected sealed override void HashCore(ReadOnlySpan<byte> source)
        {
            if (disposed) throw new ObjectDisposedException(typeof(SHA1ImplementationThatCanContinue).Name);
            _hashProvider.AppendHashData(source);
        }
        protected sealed override byte[] HashFinal()
        {
            if (disposed) throw new ObjectDisposedException(typeof(SHA1ImplementationThatCanContinue).Name);

            disposed = true;
            return _hashProvider.FinalizeHashAndReset();
        }
        protected sealed override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = -1;
            return true; // we must make the base implementation do what we want in ISHAThatCanContinue.AppendHashData
        }
        public sealed override void Initialize() { }

        public new void Dispose()
        {
            _hashProvider.Dispose();
            base.Dispose();
        }

        public override string ToString()
        {
            var digest = HashFinal();
            var result = BitConverter.ToString(digest);
            return result;
        }

        void ISHAThatCanContinue.AppendHashData(ReadOnlySpan<byte> source)
        {
            if (!this.TryComputeHash(source, this.digest, out int _))
                throw new Exception("TryComputeHash returned false");
        }
    }
    internal class SHA1Wrapper : ISHAThatCanContinue
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