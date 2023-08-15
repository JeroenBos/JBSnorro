using System.Reflection;
using System.Text;

namespace JBSnorro.Text;

// TODO: the usage of this class has a stringbuilder, but ideally that should be incorporated into this class rather than in its usage

/// <summary>
/// A wrapper around <see cref="StreamReader"/> that exposes whether the last `ReadLine()` or `ReadLineAsync()` read a completed line (i.e. ending on '\n' or '\r') or just until the end of the file.
/// </summary>
public class StringReaderThatYieldsWholeLines : StreamReader
{
    private static readonly FieldInfo _charPosField = typeof(StreamReader).GetField(nameof(_charPos), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo _charLenField = typeof(StreamReader).GetField(nameof(_charLen), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo _charBufferField = typeof(StreamReader).GetField(nameof(_charBuffer), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo ThrowIfDisposedMethodInfo = typeof(StreamReader).GetMethod(nameof(ThrowIfDisposed), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo CheckAsyncTaskInProgressMethodInfo = typeof(StreamReader).GetMethod(nameof(CheckAsyncTaskInProgress), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo ReadBufferMethodInfo = typeof(StreamReader).GetMethod(nameof(ReadBuffer), BindingFlags.NonPublic | BindingFlags.Instance, Array.Empty<Type>())!;
    public StringReaderThatYieldsWholeLines(Stream stream) : base(stream)
    {
        // other constructors not implemented because not necessary
    }
    /// <summary>
    /// Gets whether the last character read by <see cref="ReadLine"/> or <see cref="ReadLineAsync(CancellationToken)"/> was a newline character (which is excluded from the result value).
    /// Alternatively, the last character could have been anything else and the line terminates because of the file terminating.
    /// </summary>
    public bool LastCharacterWasNewLine { get; private set; }

    int _charPos { get => (int)_charPosField.GetValue(this)!; set => _charPosField.SetValue(this, value); }
    int _charLen => (int)_charLenField.GetValue(this)!;
    char[] _charBuffer => (char[])_charBufferField.GetValue(this)!;
    void ThrowIfDisposed()
    {
        ThrowIfDisposedMethodInfo.Invoke(this, Array.Empty<object>());
    }
    void CheckAsyncTaskInProgress()
    {
        CheckAsyncTaskInProgressMethodInfo.Invoke(this, Array.Empty<object>());
    }
    int ReadBuffer()
    {
        return (int)ReadBufferMethodInfo.Invoke(this, Array.Empty<object>())!;
    }
    public override string? ReadLine()
    {
        this.LastCharacterWasNewLine = false;
        ThrowIfDisposed();
        CheckAsyncTaskInProgress();

        if (_charPos == _charLen)
        {
            if (ReadBuffer() == 0)
            {
                return null;
            }
        }

        StringBuilder? sb = null;
        do
        {
            int i = _charPos;
            do
            {
                char ch = _charBuffer[i];
                // Note the following common line feed chars:
                // \n - UNIX   \r\n - DOS   \r - Mac
                if (ch == '\r' || ch == '\n')
                {
                    string s;
                    if (sb != null)
                    {
                        sb.Append(_charBuffer, _charPos, i - _charPos);
                        s = sb.ToString();
                    }
                    else
                    {
                        s = new string(_charBuffer, _charPos, i - _charPos);
                    }
                    _charPos = i + 1;
                    if (ch == '\r' && (_charPos < _charLen || ReadBuffer() > 0))
                    {
                        if (_charBuffer[_charPos] == '\n')
                        {
                            _charPos++;
                        }
                        this.LastCharacterWasNewLine = true; // ONLY THIS MINI CHANGE HERE COMPARED TO BASE CLASS IMPLEMENTATION
                    }
                    return s;  
                }
                i++;
            } while (i < _charLen);

            i = _charLen - _charPos;
            sb ??= new StringBuilder(i + 80);
            sb.Append(_charBuffer, _charPos, i);
        } while (ReadBuffer() > 0);
        return sb.ToString();
    }
    public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        // overriding implementation not necessary because for types != typeof(StreamReader), the implementation delegates to 'string? ReadLine()'
        return base.ReadLineAsync(cancellationToken); 
    }
}