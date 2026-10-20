namespace Backpack;

public enum EquipmentSlot { Weapon, Protection }

public interface IEquipment
{
    EquipmentSlot Slot { get; }
    int AttackBonus { get; }
    int DefenseBonus { get; }
}

public interface IConsumable
{
    bool IsUsed { get; }
    void Use(PlayerStats player);
}

public interface IUpgradeable
{
    int Level { get; }
    void Improve();
}
