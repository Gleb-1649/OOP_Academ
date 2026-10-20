namespace Backpack;

public abstract class Gear : InventoryItem, IEquipment, IUpgradeable
{
    private readonly int basePower;
    public const int MaxLevel = 3;
    public int Level { get; private set; }
    protected abstract int UpgradeStep { get; }
    protected int Power => checked(basePower + Level * UpgradeStep);
    public abstract EquipmentSlot Slot { get; }
    public abstract int AttackBonus { get; }
    public abstract int DefenseBonus { get; }

    protected Gear(int id, string name, int basePower) : base(id, name)
    {
        if (basePower <= 0) throw new ArgumentOutOfRangeException(nameof(basePower));
        this.basePower = basePower;
    }

    public void Improve()
    {
        if (Level == MaxLevel) throw new InvalidOperationException("Достигнут максимальный уровень.");
        _ = checked(basePower + (Level + 1) * UpgradeStep);
        Level++;
    }
}

public sealed class Sword : Gear
{
    protected override int UpgradeStep => 3;
    public override EquipmentSlot Slot => EquipmentSlot.Weapon;
    public override int AttackBonus => Power;
    public override int DefenseBonus => 0;
    public Sword(int id, string name, int damage) : base(id, name, damage) { }
    public override string Describe() => $"Меч {Name}: урон {AttackBonus}, улучшение {Level}/{MaxLevel}";
}

public sealed class BodyArmor : Gear
{
    protected override int UpgradeStep => 2;
    public override EquipmentSlot Slot => EquipmentSlot.Protection;
    public override int AttackBonus => 0;
    public override int DefenseBonus => Power;
    public BodyArmor(int id, string name, int defense) : base(id, name, defense) { }
    public override string Describe() => $"Доспех {Name}: защита {DefenseBonus}, улучшение {Level}/{MaxLevel}";
}
