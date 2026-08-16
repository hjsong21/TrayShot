using System;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TrayShot.Infrastructure;
using TrayShot.Models;

namespace TrayShot.Convert;

public sealed class ScreenshotConverter
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpszProgressTitle;
    }

    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_SILENT = 0x0004;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

    public bool ConvertAndVerify(string sourcePngPath, out string convertedWebpPath)
    {
        convertedWebpPath = ScreenshotFile.ConvertedPath(sourcePngPath);

        try
        {
            if (!File.Exists(sourcePngPath)) return false;

            // 1. 변환 전 원본 파일 상태 기록 (파일 변경 감지용)
            var sourceInfo = new FileInfo(sourcePngPath);
            DateTime initialCreationTime = sourceInfo.CreationTimeUtc;
            DateTime initialMTime = sourceInfo.LastWriteTimeUtc;
            long initialSize = sourceInfo.Length;

            // 2. WebP 무손실 인코딩
            WebPEncoder.EncodeLossless(sourcePngPath, convertedWebpPath);

            // 3. 파일 변경 감지: 변환 중 원본 파일이 수정되거나 크기가 변경되었는지 확인
            sourceInfo.Refresh();
            if (!sourceInfo.Exists || sourceInfo.LastWriteTimeUtc != initialMTime || sourceInfo.Length != initialSize)
            {
                Log.Convert.Warn($"Source file was modified during conversion: {sourcePngPath}");
                CleanupWebp(convertedWebpPath);
                return false;
            }

            // 4. 용량 절감 검증: WebP 용량이 원본 PNG보다 실제로 작을 때만 진행 (outputBytes < inputBytes)
            var webpInfo = new FileInfo(convertedWebpPath);
            if (!webpInfo.Exists || webpInfo.Length >= initialSize)
            {
                Log.Convert.Info($"WebP size ({webpInfo.Length} bytes) is not smaller than PNG ({initialSize} bytes). Aborting conversion for {sourcePngPath}");
                CleanupWebp(convertedWebpPath);
                return false;
            }

            // 5. 1:1 픽셀 무결성 비교 검증 (Pixel-by-pixel RGB & Alpha 1:1 비교)
            if (!VerifyPixelMatch(sourcePngPath, convertedWebpPath))
            {
                Log.Convert.Error($"1:1 Pixel verification failed for {sourcePngPath}");
                CleanupWebp(convertedWebpPath);
                return false;
            }

            // 6. 생성/수정 타임스탬프 이식 (원본 PNG의 메타데이터 유지)
            File.SetCreationTimeUtc(convertedWebpPath, initialCreationTime);
            File.SetLastWriteTimeUtc(convertedWebpPath, initialMTime);

            // 7. 필수 검증을 모두 통과한 경우에만 설정된 처분 정책(trash/delete/keep) 수행
            DisposeSourceFile(sourcePngPath);

            Log.Convert.Info($"Successfully converted PNG to WebP with full verification: {convertedWebpPath} (Saved {initialSize - webpInfo.Length} bytes)");
            return true;
        }
        catch (Exception ex)
        {
            Log.Convert.Error($"Conversion error for {sourcePngPath}: {ex.Message}");
            CleanupWebp(convertedWebpPath);
            return false;
        }
    }

    private static void CleanupWebp(string webpPath)
    {
        if (File.Exists(webpPath))
        {
            try { File.Delete(webpPath); } catch { }
        }
    }

    private static bool VerifyPixelMatch(string pngPath, string webpPath)
    {
        try
        {
            using var imgA = Image.Load<Rgba32>(pngPath);
            using var imgB = Image.Load<Rgba32>(webpPath);

            if (imgA.Width != imgB.Width || imgA.Height != imgB.Height)
                return false;

            bool isIdentical = true;
            imgA.ProcessPixelRows(imgB, (accessorA, accessorB) =>
            {
                for (int y = 0; y < accessorA.Height; y++)
                {
                    var rowA = accessorA.GetRowSpan(y);
                    var rowB = accessorB.GetRowSpan(y);
                    if (!rowA.SequenceEqual(rowB))
                    {
                        isIdentical = false;
                        break;
                    }
                }
            });

            return isIdentical;
        }
        catch (Exception ex)
        {
            Log.Convert.Error($"Error during pixel verification: {ex.Message}");
            return false;
        }
    }

    private static void DisposeSourceFile(string sourcePngPath)
    {
        var disposal = AppSettings.Shared.WebpDisposal;
        switch (disposal)
        {
            case WebPDisposal.Delete:
                File.Delete(sourcePngPath);
                Log.Convert.Info($"Deleted original PNG: {sourcePngPath}");
                break;
            case WebPDisposal.Trash:
                MoveToRecycleBin(sourcePngPath);
                Log.Convert.Info($"Moved original PNG to Windows Recycle Bin: {sourcePngPath}");
                break;
            case WebPDisposal.Keep:
                Log.Convert.Info($"Kept original PNG: {sourcePngPath}");
                break;
        }
    }

    private static bool MoveToRecycleBin(string filePath)
    {
        try
        {
            var op = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = filePath + '\0' + '\0', // Double null terminated string
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
            };
            int result = SHFileOperation(ref op);
            if (result != 0 && File.Exists(filePath))
            {
                // Fallback to File.Delete if recycle bin operation failed
                File.Delete(filePath);
            }
            return result == 0;
        }
        catch (Exception ex)
        {
            Log.Convert.Error($"Recycle bin move error, falling back to delete: {ex.Message}");
            if (File.Exists(filePath)) File.Delete(filePath);
            return false;
        }
    }
}
