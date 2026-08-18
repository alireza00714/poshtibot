using Microsoft.Extensions.Configuration;

namespace Namadno.AI.Support.Application.Copy;

public static class SupportCopyFiles
{
    public static IConfigurationBuilder AddSupportCopyFiles(
        this IConfigurationBuilder builder,
        params string?[] searchRoots)
    {
        var directory = ResolveDirectory(searchRoots);
        builder.AddJsonFile(Path.Combine(directory, "texts.json"), optional: false, reloadOnChange: false);
        builder.AddJsonFile(Path.Combine(directory, "lexicon.json"), optional: false, reloadOnChange: false);
        builder.AddJsonFile(Path.Combine(directory, "faq-seed.json"), optional: false, reloadOnChange: false);
        return builder;
    }

    public static T LoadSection<T>(string sectionName, params string?[] searchRoots)
        where T : class =>
        new ConfigurationBuilder()
            .AddSupportCopyFiles(searchRoots)
            .Build()
            .GetSection(sectionName)
            .Get<T>()
        ?? throw new InvalidOperationException($"Configuration section '{sectionName}' is missing.");

    public static string ResolveDirectory(params string?[] searchRoots)
    {
        foreach (var root in searchRoots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var found = FindFrom(root!);
            if (found is not null)
            {
                return found;
            }
        }

        var fromBase = FindFrom(AppContext.BaseDirectory);
        if (fromBase is not null)
        {
            return fromBase;
        }

        throw new InvalidOperationException(
            "Copy JSON files were not found. Expected copy/texts.json next to the app or at the repository root.");
    }

    private static string? FindFrom(string start)
    {
        DirectoryInfo? dir;
        try
        {
            dir = new DirectoryInfo(Path.GetFullPath(start));
        }
        catch (Exception)
        {
            return null;
        }

        if (dir.Exists
            && dir.Name.Equals("copy", StringComparison.OrdinalIgnoreCase)
            && File.Exists(Path.Combine(dir.FullName, "texts.json")))
        {
            return dir.FullName;
        }

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "copy");
            if (File.Exists(Path.Combine(candidate, "texts.json")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
