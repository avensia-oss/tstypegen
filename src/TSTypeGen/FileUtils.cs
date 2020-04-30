using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TSTypeGen
{
    public static class FileUtils
    {
        public static byte[] ReadAllBytesSafe(string filePath)
        {
            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        public static string ReadAllTextSafe(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static void Delete(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            {
                Thread.Sleep(100);
                if (File.Exists(file))
                    throw;
            }
        }
    }
}
