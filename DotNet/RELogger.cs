using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class RELogger
{
    [DllImport("kernel32")]
    public static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool WriteConsole(IntPtr hConsoleOutput, string lpBuffer, uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten, IntPtr lpReserved);

    static bool writeToConsole;
    static IntPtr consoleHandle;
    static HashSet<string> seenLogs;
    static readonly StreamWriter logWriter;
    static readonly StreamWriter methodsLog;
    static Dictionary<string, DateTime> logCooldowns;

    static RELogger()
    {
        var gameDirectory = AppDomain.CurrentDomain.BaseDirectory;
        logWriter = new StreamWriter($"{gameDirectory}\\Trace.log");
        methodsLog = new StreamWriter($"{gameDirectory}\\Methods.log");
        logCooldowns = new Dictionary<string, DateTime>();
        seenLogs = new HashSet<string>();
    }

    public static void EnableConsoleLog()
    {
        const int STD_OUTPUT_HANDLE = -11;
        if (!writeToConsole)
        {
            AllocConsole();
            consoleHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            writeToConsole = consoleHandle != IntPtr.Zero;
        }
    }

    public static void LogOnce(string text, string key)
    {
        if (seenLogs.Add(key))
        {
            Log(text);
        }
    }

    public static void LogWithCooldown(string text, string key, int cooldownSeconds)
    {
        if (!HasLogCooldown(key))
        {
            logCooldowns[key] = DateTime.Now.AddSeconds(cooldownSeconds);
            Log(text);
        }
    }

    private static bool HasLogCooldown(string key)
    {
        if (!logCooldowns.TryGetValue(key, out DateTime nextLogtime))
        {
            return false;
        }
        return DateTime.Now < nextLogtime;
    }

    public static void Log(string text)
    {
        LogWithTimestamp(logWriter, text);
    }

    public static void TraceCallingMethods(int skip = 0)
    {
        var prefix = "";
        var result = new StringBuilder();
        var stackTrace = new StackTrace(2 + skip);
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            var stackFrame = stackTrace.GetFrame(i);
            var method = stackFrame.GetMethod();
            var methodName = MethodToString(method);
            result.Append(prefix);
            result.Append(methodName);
            prefix = " <- ";
        }
        LogWithTimestamp(methodsLog, result.ToString());
    }

    private static string MethodToString(MethodBase method)
    {
        var prefix = "";
        var result = new StringBuilder();
        result.Append(method.DeclaringType.ToString());
        result.Append(".");
        result.Append(method.Name);
        result.Append("(");
        foreach (ParameterInfo parameterInfo in method.GetParameters())
        {
            result.Append(prefix);
            result.Append(parameterInfo.ParameterType.ToString());
            prefix = ", ";
        }
        result.Append(")");
        return result.ToString();
    }

    public static void LogException(Exception ex)
    {
        var message = BuildExceptionMessage(ex);
        Log(message);
    }

    public static void LogExceptionWithCooldown(Exception ex, string key, int cooldownSeconds = 10)
    {
        if (HasLogCooldown(key))
        {
            return;
        }
        var message = BuildExceptionMessage(ex);
        LogWithCooldown(message, key, cooldownSeconds);
    }

    private static string BuildExceptionMessage(Exception ex)
    {
        var currException = ex;
        var result = new StringBuilder();
        while (currException != null)
        {
            result.AppendLine($"{currException.ToString()}: {currException.Message}\n");
            result.AppendLine(currException.StackTrace);
            currException = currException.InnerException;
            if (currException != null)
            {
                result.AppendLine("\n===========================================================================");
                result.AppendLine("                              INNER EXCEPTION");
                result.AppendLine("===========================================================================");
            }
        }
        return result.ToString();
    }

    private static void LogWithTimestamp(StreamWriter streamWriter, string text)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logText = $"[{timestamp}] {text}\n";
        streamWriter.Write(logText);
        streamWriter.Flush();
        if (writeToConsole)
        {
            WriteConsole(consoleHandle, logText, (uint)logText.Length, out _, IntPtr.Zero);
        }
    }
}
