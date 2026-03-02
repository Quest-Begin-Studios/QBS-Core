using UnityEngine;

namespace QBS.Core
{
    public class TryLogScript
    {
        public void Log(int a, int b)
        {
            Debug.Log($"Log Works? {Add(a, b)}");
        }
        
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}
