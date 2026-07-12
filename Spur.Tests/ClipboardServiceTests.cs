using Spur.Services;

namespace Spur.Tests;

public class ClipboardServiceTests
{
    private static ClipboardServiceImpl CreateService(int maxItems = 50)
    {
        var savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"spur_clip_test_{System.Guid.NewGuid():n}.json");
        var svc = new ClipboardServiceImpl(NullLogger.Instance, savePath);
        svc.MaxItems = maxItems;
        return svc;
    }

    [Fact]
    public void Add_SingleItem_AppearsInHistory()
    {
        var svc = CreateService();
        svc.Add("hello world");

        var history = svc.GetHistory();
        Assert.Single(history);
        Assert.Equal("hello world", history[0].Content);
    }

    [Fact]
    public void Add_MultipleItems_MostRecentFirst()
    {
        var svc = CreateService();
        svc.Add("first");
        svc.Add("second");
        svc.Add("third");

        var history = svc.GetHistory();
        Assert.Equal(3, history.Count);
        Assert.Equal("third", history[0].Content);
        Assert.Equal("second", history[1].Content);
        Assert.Equal("first", history[2].Content);
    }

    [Fact]
    public void Add_Duplicate_MovesToTop()
    {
        var svc = CreateService();
        svc.Add("first");
        svc.Add("second");
        svc.Add("first"); // duplicate

        var history = svc.GetHistory();
        Assert.Equal(2, history.Count);
        Assert.Equal("first", history[0].Content);
        Assert.Equal("second", history[1].Content);
    }

    [Fact]
    public void Add_ConsecutiveDuplicate_DoesNotDuplicate()
    {
        var svc = CreateService();
        svc.Add("same");
        svc.Add("same"); // exact duplicate at top — should be no-op

        var history = svc.GetHistory();
        Assert.Single(history);
    }

    [Fact]
    public void Add_EmptyOrWhitespace_IsIgnored()
    {
        var svc = CreateService();
        svc.Add("");
        svc.Add("   ");
        svc.Add(null!);

        var history = svc.GetHistory();
        Assert.Empty(history);
    }

    [Fact]
    public void MaxItems_EnforcesLimit()
    {
        var svc = CreateService(maxItems: 5);

        for (int i = 0; i < 10; i++)
            svc.Add($"item-{i}");

        var history = svc.GetHistory();
        Assert.Equal(5, history.Count);
        // Most recent should be at the top
        Assert.Equal("item-9", history[0].Content);
    }

    [Fact]
    public void MaxItems_ReducingSizeTrimsList()
    {
        var svc = CreateService(maxItems: 10);

        for (int i = 0; i < 10; i++)
            svc.Add($"item-{i}");

        Assert.Equal(10, svc.GetHistory().Count);

        svc.MaxItems = 5;
        Assert.Equal(5, svc.GetHistory().Count);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var svc = CreateService();
        svc.Add("one");
        svc.Add("two");
        svc.Add("three");

        svc.Clear();

        Assert.Empty(svc.GetHistory());
    }

    [Fact]
    public void RemoveById_RemovesSpecificItem()
    {
        var svc = CreateService();
        svc.Add("keep this");
        svc.Add("remove this");

        var history = svc.GetHistory();
        var removeId = history[0].Id; // "remove this" is at top
        svc.RemoveById(removeId);

        history = svc.GetHistory();
        Assert.Single(history);
        Assert.Equal("keep this", history[0].Content);
    }

    [Fact]
    public void RemoveById_NonExistentId_DoesNothing()
    {
        var svc = CreateService();
        svc.Add("item");

        svc.RemoveById(Guid.NewGuid());

        Assert.Single(svc.GetHistory());
    }

    [Fact]
    public void KeepOnly_RetainsMatchingItems()
    {
        var svc = CreateService();
        svc.Add("keep-a");
        svc.Add("remove-b");
        svc.Add("keep-c");

        var keepSet = new HashSet<string> { "keep-a", "keep-c" };
        svc.KeepOnly(keepSet);

        var history = svc.GetHistory();
        Assert.Equal(2, history.Count);
        Assert.Contains(history, e => e.Content == "keep-a");
        Assert.Contains(history, e => e.Content == "keep-c");
    }

    [Fact]
    public void ClipboardChanged_FiresOnAdd()
    {
        var svc = CreateService();
        bool fired = false;
        svc.ClipboardChanged += () => fired = true;

        svc.Add("test");

        Assert.True(fired);
    }

    [Fact]
    public void ClipboardChanged_FiresOnClear()
    {
        var svc = CreateService();
        svc.Add("test");

        bool fired = false;
        svc.ClipboardChanged += () => fired = true;

        svc.Clear();

        Assert.True(fired);
    }
}
