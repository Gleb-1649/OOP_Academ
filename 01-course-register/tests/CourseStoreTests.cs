using StudyRegister;
using Xunit;

namespace StudyRegister.Checks;

public class CourseStoreTests
{
    [Fact]
    public void CatalogKeepsCoursesOfDifferentTypes()
    {
        ICourseCatalog catalog = CreateStore();
        Assert.IsType<RemoteCourse>(catalog.GetCourse(11));
        Assert.IsType<ClassroomCourse>(catalog.GetCourse(12));
        Assert.Equal(2, catalog.GetAllCourses().Count);
        Assert.Contains("Teams", catalog.GetCourse(11).Location);
        Assert.Contains("Б-204", catalog.GetCourse(12).Location);
    }

    [Fact]
    public void DuplicateIdIsRejectedWithoutReplacement()
    {
        var store = CreateStore();
        Assert.Throws<ArgumentException>(() => store.AddCourse(new ClassroomCourse(11, "Другой", "101", 5)));
        Assert.Equal("Основы C#", store.GetCourse(11).Title);
    }

    [Fact]
    public void TeacherFilterUsesTeacherIdAndReassignment()
    {
        var store = CreateStore();
        var teacher = new Teacher(4, "Ирина Климова", "Программирование");
        store.AssignTeacher(11, teacher);
        store.AssignTeacher(12, teacher);
        Assert.Equal(new[] { 11, 12 }, store.GetTeacherCourses(4).Select(c => c.Id));
        store.AssignTeacher(12, new Teacher(5, "Олег Лебедев", "Математика"));
        Assert.Equal(11, Assert.Single(store.GetTeacherCourses(4)).Id);
        Assert.Equal(12, Assert.Single(store.GetTeacherCourses(5)).Id);
        Assert.Empty(store.GetTeacherCourses(99));
    }

    [Fact]
    public void EnrollmentStoresStudentInformationAndIsCourseSpecific()
    {
        var store = CreateStore();
        var student = new Student(21, "Анна Орлова", "ИС-23");
        store.EnrollStudent(11, student);
        store.EnrollStudent(12, student);
        Student registered = Assert.Single(store.GetStudents(11));
        Assert.Equal("Анна Орлова", registered.FullName);
        Assert.Equal("ИС-23", registered.Group);
        Assert.Single(store.GetStudents(12));
    }

    [Fact]
    public void DuplicateEnrollmentChecksId()
    {
        var store = CreateStore();
        store.EnrollStudent(11, new Student(21, "Анна", "ИС-23"));
        Assert.Throws<InvalidOperationException>(() => store.EnrollStudent(11, new Student(21, "Анна Орлова", "ИС-23")));
        Assert.Single(store.GetStudents(11));
    }

    [Fact]
    public void CapacityIsCheckedAndWithdrawalFreesAPlace()
    {
        var store = new CourseStore();
        store.AddCourse(new ClassroomCourse(1, "Практика", "201", 1));
        store.EnrollStudent(1, new Student(1, "Анна", "А"));
        Assert.Throws<InvalidOperationException>(() => store.EnrollStudent(1, new Student(2, "Борис", "А")));
        Assert.True(store.WithdrawStudent(1, 1));
        Assert.False(store.WithdrawStudent(1, 1));
        store.EnrollStudent(1, new Student(2, "Борис", "А"));
        Assert.Equal(2, Assert.Single(store.GetStudents(1)).Id);
    }

    [Fact]
    public void DeletionClearsEnrollmentsEvenIfIdIsReused()
    {
        var store = CreateStore();
        store.EnrollStudent(11, new Student(21, "Анна", "ИС-23"));
        store.RemoveCourse(11);
        Assert.Throws<KeyNotFoundException>(() => store.GetCourse(11));
        store.AddCourse(new RemoteCourse(11, "Новый курс", "Meet", "https://example.org/new"));
        Assert.Empty(store.GetStudents(11));
    }

    [Fact]
    public void UnknownCourseRejectsAllCourseOperations()
    {
        var store = CreateStore();
        Assert.Throws<KeyNotFoundException>(() => store.RemoveCourse(999));
        Assert.Throws<KeyNotFoundException>(() => store.GetStudents(999));
        Assert.Throws<KeyNotFoundException>(() => store.EnrollStudent(999, new Student(1, "Анна", "А")));
        Assert.Throws<KeyNotFoundException>(() => store.AssignTeacher(999, new Teacher(1, "Ирина", "П")));
    }

    [Theory]
    [InlineData("room123")]
    [InlineData("file:///tmp/course")]
    [InlineData("")]
    public void RemoteCourseRejectsInvalidMeetingLink(string link) =>
        Assert.Throws<ArgumentException>(() => new RemoteCourse(1, "Курс", "Meet", link));

    [Fact]
    public void EntitiesRejectInvalidRequiredValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClassroomCourse(1, "Курс", "201", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Student(0, "Анна", "А"));
        Assert.Throws<ArgumentException>(() => new Teacher(1, " ", "Кафедра"));
    }

    private static CourseStore CreateStore()
    {
        var store = new CourseStore();
        store.AddCourse(new RemoteCourse(11, "Основы C#", "Teams", "https://example.org/csharp"));
        store.AddCourse(new ClassroomCourse(12, "Компьютерный практикум", "Б-204", 2));
        return store;
    }
}
