namespace StudyRegister;

public abstract class Person
{
    public int Id { get; }
    public string FullName { get; }

    protected Person(int id, string fullName)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Имя обязательно.");
        Id = id;
        FullName = fullName.Trim();
    }
}

public sealed class Teacher : Person
{
    public string Department { get; }

    public Teacher(int id, string fullName, string department) : base(id, fullName)
    {
        if (string.IsNullOrWhiteSpace(department)) throw new ArgumentException("Укажите кафедру.");
        Department = department.Trim();
    }
}

public sealed class Student : Person
{
    public string Group { get; }

    public Student(int id, string fullName, string group) : base(id, fullName)
    {
        if (string.IsNullOrWhiteSpace(group)) throw new ArgumentException("Укажите группу.");
        Group = group.Trim();
    }
}
