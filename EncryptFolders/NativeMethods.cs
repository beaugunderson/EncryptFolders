using System.Runtime.InteropServices;

namespace EncryptFolders
{
    public static class NativeMethods
    {
        [DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EncryptFile(
            string fileName);
    }
}