using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QBS.Core
{
	public enum LogLevel
	{
		Trace,
		Debug,
		Info,
		Warning,
		Error,
		Fatal,
	}

	public static partial class Log
	{
		[ThreadStatic]
		private static StringBuilder _sb;
		
		private static StringBuilder Builder
		{
			get
			{
				if (_sb == null)
				{
					_sb = new StringBuilder(256);
				}
				// Cap capacity to avoid holding huge buffers
				else if (_sb.Capacity > 4096)
				{
					_sb.Capacity = 4096;
				}
				return _sb;
			}
		}

		private const string BracketsFormat = "[{0}] ";
		private const string AssertionFailedTag = "ASSERTION FAILED";
		private const string ColorStartTag = "<color=#";
		private const string ColorEndTag = "</color>";
		
		public static LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
		public static LogChannel EnabledChannels { get; set; } = LogChannel.All;

		public static void SetChannelEnabled(LogChannel channel, bool enabled)
		{
			if (enabled)
			{
				EnabledChannels |= channel;
			}
			else
			{
				EnabledChannels &= ~channel;
			}
		}

		public static bool IsChannelEnabled(LogChannel channel) => (EnabledChannels & channel) != 0;

		[Conditional("ENABLE_LOGS")]
		public static void Trace(string message, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessage(message, tag, color, LogLevel.Trace, channel, context);

		[Conditional("ENABLE_LOGS")]
		public static void Debug(string message, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessage(message, tag, color, LogLevel.Debug, channel, context);

		[Conditional("ENABLE_LOGS")]
		public static void Info(string message, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessage(message, tag, color, LogLevel.Info, channel, context);
		
		// String allocation
		[Conditional("ENABLE_LOGS")]
		public static void InfoSnipe(string message, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessage($"( -_•)▄︻デ══━一 {message}", tag, color, LogLevel.Info, channel, context);

		[Conditional("ENABLE_LOGS")]
		public static void Warning(string message, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessage(message, tag, color, LogLevel.Warning, channel, context);

		[Conditional("ENABLE_LOGS")]
		public static void Error(string message, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessage(message, tag, color, LogLevel.Error, channel, context);

		[Conditional("ENABLE_LOGS")]
		public static void Error(string message, Exception exception, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessageWithException(message, exception, tag, color, LogLevel.Error, channel, context);

		[Conditional("ENABLE_LOGS")]
		public static void Fatal(string message, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessage(message, tag, color, LogLevel.Fatal, channel, context);

		[Conditional("ENABLE_LOGS")]
		public static void Fatal(string message, Exception exception, string tag = null, Color color = default,
			LogChannel channel = LogChannel.None, Object context = null) =>
			LogMessageWithException(message, exception, tag, color, LogLevel.Fatal, channel, context);

		private static void LogMessage(string message, string tag, Color color,
			LogLevel level, LogChannel channel, Object context)
		{
			if (level < MinimumLevel)
			{
				return;
			}

			if (!IsChannelEnabled(channel) && channel != LogChannel.None)
			{
				return;
			}

			// Append level first.
			var levelStr = level.ToString();
			Builder.Clear();
			Builder.AppendFormat(BracketsFormat, levelStr);

			// Append channel if specified.
			if (channel != LogChannel.None)
			{
				var channelStr = channel.ToString();
				Builder.AppendFormat(BracketsFormat, channelStr);
			}

			// Append tag if specified.
			if (tag != null)
			{
				Builder.AppendFormat(BracketsFormat, tag);
			}

			if (color != default)
			{
				Builder.Append(ColorStartTag);
				Builder.Append(((byte)(color.r * 255)).ToString("X2"));
				Builder.Append(((byte)(color.g * 255)).ToString("X2"));
				Builder.Append(((byte)(color.b * 255)).ToString("X2"));
				Builder.Append(">");
				Builder.Append(message);
				Builder.Append(ColorEndTag);
			}
			else
			{
				Builder.Append(message);
			}

			if (context != null)
			{
				LogToUnityWithReference(level, Builder.ToString(), context);
			}
			else
			{
				LogToUnity(level, Builder.ToString());
			}
		}

		private static void LogMessageWithException(string message, Exception exception, string tag, Color color,
			LogLevel level, LogChannel channel, Object context)
		{
			if (level < MinimumLevel)
			{
				return;
			}

			if (!IsChannelEnabled(channel) && channel != LogChannel.None)
			{
				return;
			}

			// Append level first.
			var levelStr = level.ToString();
			Builder.Clear();
			Builder.AppendFormat(BracketsFormat, levelStr);

			// Append channel if specified.
			if (channel != LogChannel.None)
			{
				var channelStr = channel.ToString();
				Builder.AppendFormat(BracketsFormat, channelStr);
			}

			// Append tag if specified.
			if (tag != null)
			{
				Builder.AppendFormat(BracketsFormat, tag);
			}

			if (color != default)
			{
				Builder.Append(ColorStartTag);
				Builder.Append(((byte)(color.r * 255)).ToString("X2"));
				Builder.Append(((byte)(color.g * 255)).ToString("X2"));
				Builder.Append(((byte)(color.b * 255)).ToString("X2"));
				Builder.Append(">");
				Builder.Append(message);
				Builder.Append(ColorEndTag);
			}
			else
			{
				Builder.Append(message);
			}

			// Append exception details
			Builder.Append("\n");
			Builder.Append(exception.GetType().Name);
			Builder.Append(": ");
			Builder.Append(exception.Message);
			Builder.Append("\n");
			Builder.Append(exception.StackTrace);

			if (context != null)
			{
				LogToUnityWithReference(level, Builder.ToString(), context);
			}
			else
			{
				LogToUnity(level, Builder.ToString());
			}
		}

		private static void LogToUnity(LogLevel level, string toString)
		{
			switch (level)
			{
				case LogLevel.Trace:
				case LogLevel.Debug:
				case LogLevel.Info:
				{
					UnityEngine.Debug.Log(toString);
					break;
				}
				case LogLevel.Warning:
				{
					UnityEngine.Debug.LogWarning(toString);
					break;
				}
				case LogLevel.Error:
				case LogLevel.Fatal:
				{
					UnityEngine.Debug.LogError(toString);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(level), level, null);
			}
		}

		private static void LogToUnityWithReference(LogLevel level, string toString, Object context)
		{
			switch (level)
			{
				case LogLevel.Trace:
				case LogLevel.Debug:
				case LogLevel.Info:
				{
					UnityEngine.Debug.Log(toString, context);
					break;
				}
				case LogLevel.Warning:
				{
					UnityEngine.Debug.LogWarning(toString, context);
					break;
				}
				case LogLevel.Error:
				case LogLevel.Fatal:
				{
					UnityEngine.Debug.LogError(toString, context);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(level), level, null);
			}
		}

		// Assert methods
		[Conditional("ENABLE_LOGS")]
		public static void Assert(bool condition, string message, Object context = null)
		{
			if (!condition)
			{
				LogMessage(message, AssertionFailedTag, Color.red, LogLevel.Error, LogChannel.None, context);
			}
		}
		
		public static void AssertThrow(bool condition, string message,
			LogChannel channel = LogChannel.None, Object context = null)
		{
			if (!condition)
			{
				LogMessage($"[ASSERTION FAILED] {message}", AssertionFailedTag, Color.red, LogLevel.Fatal, LogChannel.None, context);
				throw new Exception($"Assertion failed: {message}");
			}
		}
		
	}

	
}