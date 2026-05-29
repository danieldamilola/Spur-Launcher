namespace Arc.Models;

/// <summary>Search category identifiers used throughout the app.</summary>
public enum Category
{
    Apps,
    Files,
    Clipboard,
    Actions
}

public static class CategoryNames
{
    public const string Apps      = "apps";
    public const string Files     = "files";
    public const string Clipboard = "clipboard";
    public const string Actions   = "actions";

    public static string ToString(Category cat) => cat switch
    {
        Category.Apps      => Apps,
        Category.Files     => Files,
        Category.Clipboard => Clipboard,
        Category.Actions   => Actions,
        _                  => string.Empty,
    };

    public static Category? FromString(string? s) => s?.ToLowerInvariant() switch
    {
        Apps      => Category.Apps,
        Files     => Category.Files,
        Clipboard => Category.Clipboard,
        Actions   => Category.Actions,
        _         => null
    };
}
