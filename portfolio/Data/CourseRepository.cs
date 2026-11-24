using Npgsql;
using portfolio.Models;

namespace portfolio.Data;

// Every SQL statement about courses lives in this one class.
public class CourseRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CourseRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Columns 0 to 4 match the column order in every SELECT below.
    private static Course ReadCourse(NpgsqlDataReader reader)
    {
        return new Course
        {
            CourseId = reader.GetInt64(0),
            Code = reader.GetString(1),
            Title = reader.GetString(2),
            Term = reader.IsDBNull(3) ? null : reader.GetString(3),
            Description = reader.IsDBNull(4) ? null : reader.GetString(4)
        };
    }

    // READ: every course, with how many projects it has. A LEFT JOIN keeps courses with no projects.
    public async Task<List<Course>> GetAllAsync()
    {
        const string sql =
            "SELECT c.course_id, c.code, c.title, c.term, c.description, COUNT(p.project_id) " +
            "FROM portfolio.course c " +
            "LEFT JOIN portfolio.project p ON p.course_id = c.course_id " +
            "GROUP BY c.course_id " +
            "ORDER BY c.code;";

        var courses = new List<Course>();

        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var course = ReadCourse(reader);
            course.ProjectCount = reader.GetInt64(5);
            courses.Add(course);
        }

        return courses;
    }

    // READ: one course, or null when there is no such course.
    public async Task<Course?> GetByIdAsync(long id)
    {
        const string sql = "SELECT course_id, code, title, term, description FROM portfolio.course WHERE course_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadCourse(reader) : null;
    }

    // CREATE
    public async Task AddAsync(Course course)
    {
        const string sql = "INSERT INTO portfolio.course (code, title, term, description) " +
                           "VALUES (@code, @title, @term, @description);";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("code", course.Code);
        command.Parameters.AddWithValue("title", course.Title);
        command.Parameters.AddWithValue("term", (object?)course.Term ?? DBNull.Value);
        command.Parameters.AddWithValue("description", (object?)course.Description ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    // UPDATE
    public async Task UpdateAsync(Course course)
    {
        const string sql = "UPDATE portfolio.course " +
                           "SET code = @code, title = @title, term = @term, description = @description " +
                           "WHERE course_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("code", course.Code);
        command.Parameters.AddWithValue("title", course.Title);
        command.Parameters.AddWithValue("term", (object?)course.Term ?? DBNull.Value);
        command.Parameters.AddWithValue("description", (object?)course.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("id", course.CourseId);

        await command.ExecuteNonQueryAsync();
    }

    // DELETE: the database refuses (23503) when the course still has projects.
    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM portfolio.course WHERE course_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }
}
