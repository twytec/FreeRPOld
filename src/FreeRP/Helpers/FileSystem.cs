namespace FreeRP.Helpers
{
    public static class FileSystem
    {
        private static readonly string[] _sizeInByte = ["B", "KB", "MB", "GB", "TB"];

        public static bool CopyAll(string sourcePath, string destPath)
        {
            if (Directory.Exists(destPath) == false)
                Directory.CreateDirectory(destPath);

            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destPath));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, destPath), true);
            }

            return true;
        }

        public static void CreateDirectory(string path)
        {
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);
        }

        public static string SizeToString(double size)
        {
            int count = 0;

            while (true)
            {
                if (size > 1024)
                {
                    size /= 1024;
                    count++;
                }
                else
                    break;
            }

            return $"{Math.Round(size, 2)} {_sizeInByte[count]}";
        }
    }
}
