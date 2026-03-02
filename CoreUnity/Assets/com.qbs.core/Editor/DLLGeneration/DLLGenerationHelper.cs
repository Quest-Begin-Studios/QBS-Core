using QBS.Core;
using System;

namespace QBS.Editor
{
	public static class DLLGenerationHelper
	{
		public static bool TryGeneratingDLL(DLLGenerationParameters parameters, int recursionCount = 1)
		{
			if (recursionCount <= 0)
			{
				throw new ArgumentException("Recursion count must be greater than 0");
			}
			
			
			return true;
		}
	}
}