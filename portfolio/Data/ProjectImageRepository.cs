using Npgsql;
using portfolio.Models;

namespace portfolio.Data;

// Every SQL statement that only reads pictures lives here.
// Adding and replacing pictures happens inside ProjectRepository, in the same transaction as the project.
public class ProjectImageRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ProjectImageRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // READ: the pictures of one project, in their order.
    public async Task<List<ProjectImage>> GetByProjectAsync(long projectId)
    {
        const string sql = "SELECT image_id, project_id, image_url, caption, position " +
                           "FROM portfolio.project_image WHERE project_id = @project_id ORDER BY position, image_id;";

        var images = new List<ProjectImage>();

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("project_id", projectId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            images.Add(new ProjectImage
            {
                ImageId = reader.GetInt64(0),
                ProjectId = reader.GetInt64(1),
                ImageUrl = reader.GetString(2),
                Caption = reader.IsDBNull(3) ? null : reader.GetString(3),
                Position = reader.GetInt16(4)
            });
        }

        return images;
    }
}
