using System.ComponentModel.DataAnnotations;

namespace portfolio.Models;

// Small classes that carry what one page needs. They are not tables.

public class HomePage
{
    public string Query { get; set; } = "";
    public List<Course> Courses { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}

public class ProjectPage
{
    public Project Project { get; set; } = new();
    public List<ProjectImage> Images { get; set; } = new();
}

public class LoginForm
{
    [Required(ErrorMessage = "Enter the editor password.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
