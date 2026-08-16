using System;

namespace TrayShot.Core;

public class RecursiveFolderWatcher : FolderWatcher
{
    public RecursiveFolderWatcher(
        string folderPath,
        TimeSpan debounceInterval,
        Action onChange,
        Action onFolderLost)
        : base(folderPath, debounceInterval, onChange, onFolderLost, includeSubdirectories: true)
    {
    }
}
