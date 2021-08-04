﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.Paths;

namespace Wabbajack.Paths.IO
{
    public static class AbsolutePathExtensions
    {
        public static Stream Open(this AbsolutePath file, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.ReadWrite)
        {
            return File.Open(file.ToNativePath(), mode, access, share);
        }
        
        public static void Delete(this AbsolutePath file)
        {
            File.Delete(file.ToNativePath());
        }

        public static long Size(this AbsolutePath file)
        {
            return new FileInfo(file.ToNativePath()).Length;
        }
        
        public static DateTime LastModifiedUtc(this AbsolutePath file)
        {
            return new FileInfo(file.ToNativePath()).LastWriteTimeUtc;
        }
        
        public static byte[] ReadAllBytes(this AbsolutePath file)
        {
            using var s = File.Open(file.ToNativePath(), FileMode.Open, FileAccess.Read, FileShare.Read);
            var remain = s.Length;
            var length = remain;
            var bytes = new byte[length];
            
            while (remain > 0)
            {
                remain -= s.Read(bytes, (int)Math.Min(length - remain, 1024 * 1024), bytes.Length);
            }

            return bytes;
        }

        public static string ReadAllText(this AbsolutePath file)
        {
            return Encoding.UTF8.GetString(file.ReadAllBytes());
        }
        
        public static async Task<string> ReadAllTextAsync(this AbsolutePath file)
        {
            return Encoding.UTF8.GetString(await file.ReadAllBytesAsync());
        }
        
        public static async ValueTask<byte[]> ReadAllBytesAsync(this AbsolutePath file, CancellationToken token = default)
        {
            await using var s = File.Open(file.ToNativePath(), FileMode.Open, FileAccess.Read, FileShare.Read);
            var remain = s.Length;
            var length = remain;
            var bytes = new byte[length];
            
            while (remain > 0)
            {
                remain -= await s.ReadAsync(bytes.AsMemory((int)Math.Min(length - remain, 1024 * 1024), bytes.Length), token);
            }

            return bytes;
        }

        public static void WriteAllBytes(this AbsolutePath file, ReadOnlySpan<byte> data)
        {
            using var s = file.Open(FileMode.Create, FileAccess.Write, FileShare.None);
            s.Write(data);
        }
        
        public static async ValueTask WriteAllBytesAsync(this AbsolutePath file, Memory<byte> data, CancellationToken token = default)
        {
            await using var s = file.Open(FileMode.Create, FileAccess.Write, FileShare.None);
            await s.WriteAsync(data, token);
        }

        public static void WriteAllText(this AbsolutePath file, string str)
        {
            file.WriteAllBytes(Encoding.UTF8.GetBytes(str));
        }
        
        public static async Task WriteAllTextAsync(this AbsolutePath file, string str, CancellationToken token = default)
        {
            await file.WriteAllBytesAsync(Encoding.UTF8.GetBytes(str), token);
        }

        private static string ToNativePath(this AbsolutePath file)
        {
            return file.ToString();
        }

        #region Directories

        public static void CreateDirectory(this AbsolutePath path)
        {
            Directory.CreateDirectory(ToNativePath(path));
        }

        public static void DeleteDirectory(this AbsolutePath path)
        {
            if (!path.DirectoryExists()) return;
            Directory.Delete(ToNativePath(path), true);
        }

        public static bool DirectoryExists(this AbsolutePath path)
        {
            return Directory.Exists(path.ToNativePath());
        }
        #endregion
    }
}