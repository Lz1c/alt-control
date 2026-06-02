using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

[DisallowMultipleComponent]
public class CAMCOLArduinoSerialReader : MonoBehaviour
{
    [Serializable]
    public class SerialLineEvent : UnityEvent<string>
    {
    }

    [Header("Serial")]
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 115200;
    [SerializeField] private int readTimeoutMilliseconds = 25;
    [SerializeField] private bool connectOnStart = true;
    [SerializeField] private bool reconnectIfDisconnected = true;
    [SerializeField] private float reconnectIntervalSeconds = 1f;

    [Header("Debug")]
    [SerializeField] private bool logReceivedLines = true;
    [SerializeField] private string lastReceivedLine;

    [Header("Events")]
    [SerializeField] private SerialLineEvent onLineReceived = new SerialLineEvent();

    private ISerialConnection connection;
    private readonly StringBuilder lineBuffer = new StringBuilder(128);
    private readonly byte[] readBuffer = new byte[256];
    private float nextReconnectTime;

    public string PortName
    {
        get => portName;
        set => portName = value;
    }

    public int BaudRate
    {
        get => baudRate;
        set => baudRate = Mathf.Max(1, value);
    }

    public bool IsConnected => connection != null && connection.IsOpen;
    public string LastReceivedLine => lastReceivedLine;
    public SerialLineEvent OnLineReceived => onLineReceived;

    private void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }

    private void Update()
    {
        if (!IsConnected)
        {
            TryReconnect();
            return;
        }

        ReadAvailableData();
    }

    private void OnValidate()
    {
        baudRate = Mathf.Max(1, baudRate);
        readTimeoutMilliseconds = Mathf.Max(1, readTimeoutMilliseconds);
        reconnectIntervalSeconds = Mathf.Max(0.1f, reconnectIntervalSeconds);
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public void Connect()
    {
        Disconnect();

        try
        {
            connection = CreateConnection();
            connection.Open(portName, baudRate, readTimeoutMilliseconds);
            Debug.Log($"Arduino connected on {portName} at {baudRate} baud.", this);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Arduino connection failed on {portName}: {exception.Message}", this);
            Disconnect();
            nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
        }
    }

    public void Disconnect()
    {
        if (connection == null)
        {
            return;
        }

        try
        {
            connection.Close();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Arduino disconnect failed: {exception.Message}", this);
        }
        finally
        {
            connection = null;
            lineBuffer.Clear();
        }
    }

    private ISerialConnection CreateConnection()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return new Win32SerialConnection();
#else
        return new ReflectionSerialConnection();
#endif
    }

    private void TryReconnect()
    {
        if (!reconnectIfDisconnected || Time.unscaledTime < nextReconnectTime)
        {
            return;
        }

        nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
        Connect();
    }

    private void ReadAvailableData()
    {
        try
        {
            int bytesRead = connection.Read(readBuffer, 0, readBuffer.Length);
            for (int i = 0; i < bytesRead; i++)
            {
                char c = (char)readBuffer[i];
                if (c == '\n')
                {
                    DispatchBufferedLine();
                }
                else if (c != '\r')
                {
                    lineBuffer.Append(c);
                }
            }
        }
        catch (TimeoutException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Arduino read failed: {exception.Message}", this);
            Disconnect();
            nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
        }
    }

    private void DispatchBufferedLine()
    {
        if (lineBuffer.Length == 0)
        {
            return;
        }

        string line = lineBuffer.ToString().Trim();
        lineBuffer.Clear();
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        lastReceivedLine = line;
        if (logReceivedLines)
        {
            Debug.Log($"Arduino: {line}", this);
        }

        onLineReceived.Invoke(line);
    }

    private interface ISerialConnection
    {
        bool IsOpen { get; }
        void Open(string port, int baud, int timeoutMilliseconds);
        int Read(byte[] buffer, int offset, int count);
        void Close();
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private sealed class Win32SerialConnection : ISerialConnection
    {
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint OpenExisting = 3;
        private const int InvalidHandleValue = -1;
        private const uint PurgeRxClear = 0x0008;
        private const uint PurgeTxClear = 0x0004;

        private IntPtr handle = new IntPtr(InvalidHandleValue);

        public bool IsOpen => handle != IntPtr.Zero && handle != new IntPtr(InvalidHandleValue);

        public void Open(string port, int baud, int timeoutMilliseconds)
        {
            string path = port.StartsWith(@"\\.\", StringComparison.Ordinal) ? port : @"\\.\" + port;
            handle = CreateFile(path, GenericRead | GenericWrite, 0, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
            if (!IsOpen)
            {
                throw new InvalidOperationException($"Cannot open {port}. Close Arduino Serial Monitor and check the COM port.");
            }

            ConfigurePort(baud, timeoutMilliseconds);
            PurgeComm(handle, PurgeRxClear | PurgeTxClear);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!IsOpen)
            {
                return 0;
            }

            if (offset != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Win32 serial reader expects offset 0.");
            }

            if (!ReadFile(handle, buffer, count, out int bytesRead, IntPtr.Zero))
            {
                throw new InvalidOperationException("ReadFile failed.");
            }

            return bytesRead;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            CloseHandle(handle);
            handle = new IntPtr(InvalidHandleValue);
        }

        private void ConfigurePort(int baud, int timeoutMilliseconds)
        {
            Dcb dcb = new Dcb();
            dcb.DCBlength = Marshal.SizeOf(typeof(Dcb));

            if (!GetCommState(handle, ref dcb))
            {
                throw new InvalidOperationException("GetCommState failed.");
            }

            dcb.BaudRate = baud;
            dcb.Flags = 0x00000001 | 0x00000010 | 0x00001000;
            dcb.ByteSize = 8;
            dcb.Parity = 0;
            dcb.StopBits = 0;

            if (!SetCommState(handle, ref dcb))
            {
                throw new InvalidOperationException("SetCommState failed.");
            }

            CommTimeouts timeouts = new CommTimeouts
            {
                ReadIntervalTimeout = 1,
                ReadTotalTimeoutMultiplier = 0,
                ReadTotalTimeoutConstant = Mathf.Max(1, timeoutMilliseconds),
                WriteTotalTimeoutMultiplier = 0,
                WriteTotalTimeoutConstant = Mathf.Max(1, timeoutMilliseconds)
            };

            if (!SetCommTimeouts(handle, ref timeouts))
            {
                throw new InvalidOperationException("SetCommTimeouts failed.");
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr objectHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetCommState(IntPtr file, ref Dcb dcb);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetCommState(IntPtr file, ref Dcb dcb);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetCommTimeouts(IntPtr file, ref CommTimeouts commTimeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PurgeComm(IntPtr file, uint flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(IntPtr file, byte[] buffer, int bytesToRead, out int bytesRead, IntPtr overlapped);

        [StructLayout(LayoutKind.Sequential)]
        private struct Dcb
        {
            public int DCBlength;
            public int BaudRate;
            public int Flags;
            public ushort wReserved;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public byte XonChar;
            public byte XoffChar;
            public byte ErrorChar;
            public byte EofChar;
            public byte EvtChar;
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CommTimeouts
        {
            public int ReadIntervalTimeout;
            public int ReadTotalTimeoutMultiplier;
            public int ReadTotalTimeoutConstant;
            public int WriteTotalTimeoutMultiplier;
            public int WriteTotalTimeoutConstant;
        }
    }
#endif

    private sealed class ReflectionSerialConnection : ISerialConnection
    {
        private object serialPort;
        private Type serialPortType;
        private PropertyInfo isOpenProperty;
        private MethodInfo openMethod;
        private MethodInfo closeMethod;
        private MethodInfo disposeMethod;
        private MethodInfo readMethod;

        public bool IsOpen => serialPort != null && isOpenProperty != null && (bool)isOpenProperty.GetValue(serialPort);

        public void Open(string port, int baud, int timeoutMilliseconds)
        {
            EnsureSerialPortType();
            if (serialPortType == null)
            {
                throw new InvalidOperationException("System.IO.Ports.SerialPort is not available in this Unity runtime.");
            }

            serialPort = Activator.CreateInstance(serialPortType, port, baud);
            serialPortType.GetProperty("ReadTimeout")?.SetValue(serialPort, timeoutMilliseconds);
            serialPortType.GetProperty("DtrEnable")?.SetValue(serialPort, true);
            serialPortType.GetProperty("RtsEnable")?.SetValue(serialPort, true);
            openMethod.Invoke(serialPort, null);
            serialPortType.GetMethod("DiscardInBuffer")?.Invoke(serialPort, null);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return (int)readMethod.Invoke(serialPort, new object[] { buffer, offset, count });
            }
            catch (TargetInvocationException exception) when (exception.InnerException is TimeoutException timeoutException)
            {
                throw timeoutException;
            }
        }

        public void Close()
        {
            if (IsOpen)
            {
                closeMethod?.Invoke(serialPort, null);
            }

            disposeMethod?.Invoke(serialPort, null);
            serialPort = null;
        }

        private void EnsureSerialPortType()
        {
            if (serialPortType != null)
            {
                return;
            }

            serialPortType = Type.GetType("System.IO.Ports.SerialPort, System.IO.Ports")
                ?? Type.GetType("System.IO.Ports.SerialPort")
                ?? FindSerialPortTypeInLoadedAssemblies();

            if (serialPortType == null)
            {
                return;
            }

            isOpenProperty = serialPortType.GetProperty("IsOpen");
            openMethod = serialPortType.GetMethod("Open");
            closeMethod = serialPortType.GetMethod("Close");
            disposeMethod = serialPortType.GetMethod("Dispose");
            readMethod = serialPortType.GetMethod("Read", new[] { typeof(byte[]), typeof(int), typeof(int) });
        }

        private static Type FindSerialPortTypeInLoadedAssemblies()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType("System.IO.Ports.SerialPort", false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
