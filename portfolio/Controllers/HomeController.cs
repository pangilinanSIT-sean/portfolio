using Microsoft.AspNetCore.Mvc;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

// The viewer side: what a visitor sees. Read-only.
public class HomeController : Controller
{
    private readonly CourseRepository _courses;
    private readonly ProjectRepository _projects;
    private readonly ProjectImageRepository _images;

    public HomeController(CourseRepository courses, ProjectRepository projects, ProjectImageRepository images)
    {
        _courses = courses;
        _projects = projects;
        _images = images;
    }

    // GET / : every course with its projects. With ?q=word only the matching projects are shown.
    public async Task<IActionResult> Index(string? q)
    {
        var query = (q ?? "").Trim();
        var page = new HomePage
        {
            Query = query,
            Courses = await _courses.GetAllAsync(),
            Projects = query.Length == 0 ? await _projects.GetAllAsync() : await _projects.SearchAsync(query)
        };
        return View(page);
    }

    // GET /Home/Project/5 : one project with its pictures and its video.
    public async Task<IActionResult> Project(long id)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();                          // a 404 for an id that does not exist
        }

        var page = new ProjectPage
        {
            Project = project,
            Images = await _images.GetByProjectAsync(id)
        };
        return View(page);
    }

    // The friendly page for anything unexpected (Program.cs sends exceptions here).
    public IActionResult Error()
    {
        return View();
    }

    // A status code such as 404 (Program.cs sends them here).
    public IActionResult Status(int code)
    {
        return code == 404 ? View("NotFound") : View("Error");
    }
}