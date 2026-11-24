using Npgsql;
using portfolio.Models;

namespace portfolio.Data;

// Every SQL statement about projects lives in this one class.
public class ProjectRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ProjectRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // The columns every project SELECT returns, in this order: 0 to 12.
    // The last two are worked out per project: its first picture, and how many pictures it has.
    private const string Select =
        "SELECT p.project_id, p.course_id, p.title, p.summary, p.details, p.tools, p.year_made, p.video_url, p.repo_url, " +
        "       c.code, c.title, " +
        "       (SELECT i.image_url FROM portfolio.project_image i WHERE i.project_id = p.project_id ORDER BY i.position, i.image_id LIMIT 1), " +
        "       (SELECT COUNT(*) FROM portfolio.project_image i WHERE i.project_id = p.project_id) " +
        "FROM portfolio.project p " +
        "JOIN portfolio.course c ON c.course_id = p.course_id ";

    private static Project ReadProject(NpgsqlDataReader reader)
    {
        return new Project
        {
            ProjectId = reader.GetInt64(0),
            CourseId = reader.GetInt64(1),
            Title = reader.GetString(2),
            Summary = reader.GetString(3),
            Details = reader.IsDBNull(4) ? null : reader.GetString(4),
            Tools = reader.IsDBNull(5) ? null : reader.GetString(5),
            YearMade = reader.GetInt16(6),
            VideoUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
            RepoUrl = reader.IsDBNull(8) ? null : reader.GetString(8),
            CourseCode = reader.GetString(9),
            CourseTitle = reader.GetString(10),
            CoverUrl = reader.IsDBNull(11) ? null : reader.GetString(11),
            ImageCount = reader.GetInt64(12)
        };
    }

    private async Task<List<Project>> RunListAsync(string sql, Action<NpgsqlCommand>? addParameters = null)
    {
        var projects = new List<Project>();

        await using var command = _dataSource.CreateCommand(sql);
        addParameters?.Invoke(command);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            projects.Add(ReadProject(reader));
        }

        return projects;
    }

    // READ: every project, grouped by course.
    public Task<List<Project>> GetAllAsync() =>
        RunListAsync(Select + "ORDER BY c.code, p.year_made DESC, p.title;");

    // READ: search the title, the summary, the tools and the course code.
    public Task<List<Project>> SearchAsync(string term)
    {
        // % and _ mean "anything" in a LIKE pattern, so a visitor's own % and _ are escaped first.
        var safe = term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

        return RunListAsync(
            Select + "WHERE p.title ILIKE @q OR p.summary ILIKE @q OR p.tools ILIKE @q OR c.code ILIKE @q " +
                     "ORDER BY c.code, p.title;",
            command => command.Parameters.AddWithValue("q", "%" + safe + "%"));
    }

    // READ: one project, or null when there is no such project.
    public async Task<Project?> GetByIdAsync(long id)
    {
        await using var command = _dataSource.CreateCommand(Select + "WHERE p.project_id = @id;");
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadProject(reader) : null;
    }

    private static void AddProjectParameters(NpgsqlCommand command, Project project)
    {
        command.Parameters.AddWithValue("course_id", project.CourseId!.Value);
        command.Parameters.AddWithValue("title", project.Title);
        command.Parameters.AddWithValue("summary", project.Summary);
        command.Parameters.AddWithValue("details", (object?)project.Details ?? DBNull.Value);
        command.Parameters.AddWithValue("tools", (object?)project.Tools ?? DBNull.Value);
        command.Parameters.AddWithValue("year_made", (short)project.YearMade);
        command.Parameters.AddWithValue("video_url", (object?)project.VideoUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("repo_url", (object?)project.RepoUrl ?? DBNull.Value);
    }

    // Inserts the pictures of one project, numbering them 1, 2, 3 in the order given.
    private static async Task InsertImagesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
                                                long projectId, List<string> imageUrls)
    {
        for (int i = 0; i < imageUrls.Count; i++)
        {
            await using var insert = new NpgsqlCommand(
                "INSERT INTO portfolio.project_image (project_id, image_url, position) " +
                "VALUES (@project_id, @image_url, @position);", connection, transaction);
            insert.Parameters.AddWithValue("project_id", projectId);
            insert.Parameters.AddWithValue("image_url", imageUrls[i]);
            insert.Parameters.AddWithValue("position", (short)(i + 1));
            await insert.ExecuteNonQueryAsync();
        }
    }

    // CREATE a project together with its pictures, in ONE transaction (the same idea as BorrowAsync).
    // If any picture is refused (for example the same address twice) nothing is saved, not even the project.
    public async Task<long> AddWithImagesAsync(Project project, List<string> imageUrls)
    {
        // 1. One connection, and a transaction on it.
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 2. The project first. RETURNING gives back the id the database chose.
            long newId;
            await using (var insert = new NpgsqlCommand(
                "INSERT INTO portfolio.project (course_id, title, summary, details, tools, year_made, video_url, repo_url) " +
                "VALUES (@course_id, @title, @summary, @details, @tools, @year_made, @video_url, @repo_url) " +
                "RETURNING project_id;", connection, transaction))
            {
                AddProjectParameters(insert, project);
                newId = Convert.ToInt64(await insert.ExecuteScalarAsync());
            }

            // 3. Then each picture. A refusal here jumps to the catch below.
            await InsertImagesAsync(connection, transaction, newId, imageUrls);

            // 4. Everything worked: make it permanent.
            await transaction.CommitAsync();
            return newId;
        }
        catch
        {
            // 5. Something was refused: undo all of it, then let the controller show the message.
            await transaction.RollbackAsync();
            throw;
        }
    }

    // UPDATE a project and REPLACE its pictures, in ONE transaction.
    // If any picture is refused, the project and its old pictures stay exactly as they were.
    public async Task UpdateWithImagesAsync(Project project, List<string> imageUrls)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await using (var update = new NpgsqlCommand(
                "UPDATE portfolio.project SET course_id = @course_id, title = @title, summary = @summary, " +
                "details = @details, tools = @tools, year_made = @year_made, video_url = @video_url, repo_url = @repo_url " +
                "WHERE project_id = @id;", connection, transaction))
            {
                AddProjectParameters(update, project);
                update.Parameters.AddWithValue("id", project.ProjectId);
                await update.ExecuteNonQueryAsync();
            }

            await using (var clear = new NpgsqlCommand(
                "DELETE FROM portfolio.project_image WHERE project_id = @id;", connection, transaction))
            {
                clear.Parameters.AddWithValue("id", project.ProjectId);
                await clear.ExecuteNonQueryAsync();
            }

            await InsertImagesAsync(connection, transaction, project.ProjectId, imageUrls);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // DELETE: the project's pictures go with it (ON DELETE CASCADE).
    public async Task DeleteAsync(long id)
    {
        await using var command = _dataSource.CreateCommand("DELETE FROM portfolio.project WHERE project_id = @id;");
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }
}
