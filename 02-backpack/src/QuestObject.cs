namespace Backpack;

public sealed class QuestObject : InventoryItem
{
    public string Quest { get; }

    public QuestObject(int id, string name, string quest) : base(id, name)
    {
        if (string.IsNullOrWhiteSpace(quest)) throw new ArgumentException("Укажите квест.");
        Quest = quest.Trim();
    }

    public override string Describe() => $"Квестовый предмет {Name}: {Quest}";
}
