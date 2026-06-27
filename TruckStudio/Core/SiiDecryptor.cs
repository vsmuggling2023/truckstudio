using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TruckStudio.Core
{
    public static class SiiDecryptor
    {
        private const string DllName = "SII_Decrypt.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint GetMemoryFormat(byte[] arr_val, uint leng);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint DecryptAndDecodeMemory(byte[] arr_val, uint leng, byte[] out_buf_ptr, ref uint out_buf_ptr_leng);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint DecodeMemory(byte[] arr_val, uint leng, byte[] out_buf_ptr, ref uint out_buf_ptr_leng);

        public static string DecryptFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            byte[] binFile = File.ReadAllBytes(filePath);
            if (binFile.Length == 0) return null;

            uint memoryFormat = GetMemoryFormat(binFile, (uint)binFile.Length);

            switch (memoryFormat)
            {
                case 1:
                    return Encoding.UTF8.GetString(binFile);
                case 2:
                    return DecryptMemoryFile(binFile);
                case 4:
                    return Decode3nkFile(binFile);
                default:
                    return null;
            }
        }

        private static string DecryptMemoryFile(byte[] binFile)
        {
            uint outBufSize = 0;
            uint response = DecryptAndDecodeMemory(binFile, (uint)binFile.Length, null, ref outBufSize);

            if (response != 0) return null;

            byte[] newFileData = new byte[outBufSize];
            DecryptAndDecodeMemory(binFile, (uint)binFile.Length, newFileData, ref outBufSize);

            return Encoding.UTF8.GetString(newFileData);
        }

        private static string Decode3nkFile(byte[] binFile)
        {
            uint outBufSize = 0;
            uint response = DecodeMemory(binFile, (uint)binFile.Length, null, ref outBufSize);

            if (response != 0) return null;

            byte[] newFileData = new byte[outBufSize];
            DecodeMemory(binFile, (uint)binFile.Length, newFileData, ref outBufSize);

            return Encoding.UTF8.GetString(newFileData);
        }
    }
}
