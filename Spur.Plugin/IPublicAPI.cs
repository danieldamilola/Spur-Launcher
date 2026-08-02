namespace Spur.Plugin;

public interface IPublicAPI
{
    void HideApp();
    void ShowApp();
    void OpenResult(string id);
    string GetTranslation(string key);
    Task ChangeQueryAsync(string query, bool requery = false);
}
