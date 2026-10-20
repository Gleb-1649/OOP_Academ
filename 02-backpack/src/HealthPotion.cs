namespace Backpack;

public sealed class HealthPotion : InventoryItem, IConsumable
{
    public int Healing { get; }
    public bool IsUsed { get; private set; }

    public HealthPotion(int id, string name, int healing) : base(id, name)
    {
        if (healing <= 0) throw new ArgumentOutOfRangeException(nameof(healing));
        Healing = healing;
    }

    public void Use(PlayerStats player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (IsUsed) throw new InvalidOperationException("Зелье уже израсходовано.");
        if (Container is not null && !ReferenceEquals(Container.Player, player))
            throw new InvalidOperationException("Зелье находится у другого игрока.");
        if (player.Health == player.MaxHealth)
            throw new InvalidOperationException("Здоровье уже полное.");
        player.Heal(Healing);
        IsUsed = true;
        Container?.Remove(Id);
    }

    public override string Describe() => $"Зелье {Name}: восстановление {Healing} HP";
}
