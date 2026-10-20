namespace StudyRegister;

public abstract class StudyCourse
{
    public int Id { get; }
    public string Title { get; }
    public Teacher? Teacher { get; private set; }
    public abstract string Location { get; }
    public virtual int? StudentLimit => null;

    protected StudyCourse(int id, string title)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Название курса обязательно.");
        Id = id;
        Title = title.Trim();
    }

    internal void Assign(Teacher teacher) => Teacher = teacher;
}

public sealed class RemoteCourse : StudyCourse
{
    public string Platform { get; }
    public Uri MeetingLink { get; }
    public override string Location => $"{Platform}: {MeetingLink}";

    public RemoteCourse(int id, string title, string platform, string meetingLink) : base(id, title)
    {
        if (string.IsNullOrWhiteSpace(platform)) throw new ArgumentException("Укажите платформу.");
        if (!Uri.TryCreate(meetingLink, UriKind.Absolute, out var link) ||
            (link.Scheme != Uri.UriSchemeHttps && link.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("Нужна абсолютная HTTP/HTTPS-ссылка.", nameof(meetingLink));
        Platform = platform.Trim();
        MeetingLink = link;
    }
}

public sealed class ClassroomCourse : StudyCourse
{
    public string Room { get; }
    public int Capacity { get; }
    public override int? StudentLimit => Capacity;
    public override string Location => $"Аудитория {Room}, мест: {Capacity}";

    public ClassroomCourse(int id, string title, string room, int capacity) : base(id, title)
    {
        if (string.IsNullOrWhiteSpace(room)) throw new ArgumentException("Укажите аудиторию.");
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Room = room.Trim();
        Capacity = capacity;
    }
}
