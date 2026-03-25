using System.Collections.Generic;
namespace ErrorSimulatorAPI;


public static class MemoryManager
{
    private static List<byte[]> _memory = new List<byte[]>();

    public static void Allocate2GB()
    {
        for (int i = 0; i < 4; i++)
        {
            byte[] chunk = new byte[512 * 1024 * 1024]; // 512 MB

            
            for (int j = 0; j < chunk.Length; j += 4096) // touch every 4KB page
            {
                chunk[j] = 1;
            }

            _memory.Add(chunk);
        }
    }

    public static void ReleaseAll()
    {
        _memory.Clear();
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
    }

    public static int GetAllocatedChunks()
    {
        return _memory.Count;
    }
}