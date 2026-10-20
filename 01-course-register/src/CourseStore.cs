namespace StudyRegister;

public interface ICourseCatalog
{
    StudyCourse GetCourse(int courseId);
    IReadOnlyList<StudyCourse> GetAllCourses();
    IReadOnlyList<StudyCourse> GetTeacherCourses(int teacherId);
}

public sealed class Enrollment
{
    public int CourseId { get; }
    public Student Student { get; }

    public Enrollment(int courseId, Student student)
    {
        if (courseId <= 0) throw new ArgumentOutOfRangeException(nameof(courseId));
        ArgumentNullException.ThrowIfNull(student);
        CourseId = courseId;
        Student = student;
    }
}

public sealed class CourseStore : ICourseCatalog
{
    private readonly Dictionary<int, StudyCourse> courses = [];
    private readonly List<Enrollment> enrollments = [];

    public void AddCourse(StudyCourse course)
    {
        ArgumentNullException.ThrowIfNull(course);
        if (!courses.TryAdd(course.Id, course))
            throw new ArgumentException("Курс с таким идентификатором уже существует.");
    }

    public void RemoveCourse(int courseId)
    {
        GetCourse(courseId);
        courses.Remove(courseId);
        enrollments.RemoveAll(e => e.CourseId == courseId);
    }

    public StudyCourse GetCourse(int courseId) => courses.TryGetValue(courseId, out var course)
        ? course : throw new KeyNotFoundException("Курс не найден.");

    public IReadOnlyList<StudyCourse> GetAllCourses() => courses.Values.OrderBy(c => c.Id).ToArray();

    public void AssignTeacher(int courseId, Teacher teacher)
    {
        ArgumentNullException.ThrowIfNull(teacher);
        GetCourse(courseId).Assign(teacher);
    }

    public IReadOnlyList<StudyCourse> GetTeacherCourses(int teacherId)
    {
        if (teacherId <= 0) throw new ArgumentOutOfRangeException(nameof(teacherId));
        return courses.Values.Where(c => c.Teacher?.Id == teacherId).OrderBy(c => c.Id).ToArray();
    }

    public void EnrollStudent(int courseId, Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        StudyCourse course = GetCourse(courseId);
        if (enrollments.Any(e => e.CourseId == courseId && e.Student.Id == student.Id))
            throw new InvalidOperationException("Студент уже записан на этот курс.");
        int count = enrollments.Count(e => e.CourseId == courseId);
        if (course.StudentLimit is int limit && count >= limit)
            throw new InvalidOperationException("На курсе нет свободных мест.");
        enrollments.Add(new Enrollment(courseId, student));
    }

    public bool WithdrawStudent(int courseId, int studentId)
    {
        GetCourse(courseId);
        return enrollments.RemoveAll(e => e.CourseId == courseId && e.Student.Id == studentId) > 0;
    }

    public IReadOnlyList<Student> GetStudents(int courseId)
    {
        GetCourse(courseId);
        return enrollments.Where(e => e.CourseId == courseId)
            .Select(e => e.Student).OrderBy(s => s.Id).ToArray();
    }
}
