namespace MergeTool.Text.Profiles;

/// <summary>Matches a file path to a built-in profile by extension.</summary>
public static class FileTypeProfileMatcher
{
    public static FileTypeProfile Match(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return FileTypeProfileRegistry.PlainText;
        }

        string extension = Path.GetExtension(path);
        if (extension.Length == 0)
        {
            return FileTypeProfileRegistry.PlainText;
        }

        foreach (FileTypeProfile profile in FileTypeProfileRegistry.Profiles)
        {
            foreach (string mask in profile.ExtensionMasks)
            {
                if (extension.Equals(mask, StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }
        }

        return FileTypeProfileRegistry.PlainText;
    }
}
