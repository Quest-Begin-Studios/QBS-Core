using System;
using System.Text;

namespace QBS.Core.Editor
{
	[Flags]
	public enum EnumAttributeFlags
	{
		None = 0,
		Flags = 1 << 0,

		//TODO: Create EnumToStringNoBox Generators
		//StringEnum = 1 << 1,
	}

	public class EnumSourceGenerator : IDisposable
	{
		private const string NamespaceTemplate =
			@"namespace {0}
			{{
				{1}
			}}";
		
		private const string EnumTemplate =
			@"public enum {0}
			{{
			
			}}";
		
		private StringBuilder _sBuilder;

		public EnumSourceGenerator()
		{
			_sBuilder = new StringBuilder();
		}

		public string CreateEnumSource<T>(EnumGenParams<T> genParams) where T : struct, IEquatable<T>, IComparable<T>
		{
			_sBuilder.Clear();
			return "";
		}

		public void Dispose()
		{
			_sBuilder = null;
		}
	}

}