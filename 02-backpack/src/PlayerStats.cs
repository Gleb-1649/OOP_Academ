namespace Backpack;

public sealed class PlayerStats
{
    public int MaxHealth { get; }
    public int Health { get; private set; }
    public int BaseAttack { get; }
    public int BaseDefense { get; }

    public PlayerStats(int maxHealth, int baseAttack = 1, int baseDefense = 0)
    {
        if (maxHealth <= 0 || baseAttack < 0 || baseDefense < 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth));
        MaxHealth = maxHealth;
        Health = maxHealth;
        BaseAttack = baseAttack;
        BaseDefense = baseDefense;
    }

    public void TakeDamage(int damage)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        Health = Math.Max(0, Health - damage);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Health = (int)Math.Min(MaxHealth, (long)Health + amount);
    }
}
