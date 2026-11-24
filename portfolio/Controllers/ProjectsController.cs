using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

// The editor side: add, edit and delete projects, together with their pictures.
[Authorize]
public class ProjectsController : Controller
{
    private readonly ProjectRepository _projects;
    private readonly CourseRepository _courses;
    private readonly ProjectImageRepository _images;

    public ProjectsController(ProjectRepository projects, CourseRepository courses, ProjectImageRepository images)
    {
        _projects = projects;
        _courses = courses;
        _images = images;
    }

    // Fills the course dropdown of the form.
    private async Task FillCoursesAsync()
    {
        ViewBag.Courses = new SelectList(await _courses.GetAllAsync(), "CourseId", "Label");
    }

    // The picture addresses typed in the box: one per line, trimmed, blank lines dropped.
    private static List<string> ParseUrls(string? text) =>
        (text ?? "").Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList();

    // Checks every line with a regular expression, and adds a message under the box for each bad one.
    private void CheckUrlLines(List<string> urls)
    {
        for (int i = 0; i < urls.Count; i++)
        {
            if (!Regex.IsMatch(urls[i], Rules.ImageUrl))
            {
                ModelState.AddModelError(nameof(Project.ImageUrlsText),
                    $"Line {i + 1} is not a picture address. It must start with http and end in .png, .jpg, .gif or .webp.");
            }
        }
    }

    // GET /Projects
    public async Task<IActionResult> Index()
    {
        return View(await _projects.GetAllAsync());
    }

    // GET /Projects/Create
    public async Task<IActionResult> Create()
    {
        await FillCoursesAsync();
        return View(new Project());
    }

    // POST /Projects/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project project)
    {
        var urls = ParseUrls(project.ImageUrlsText);    // 1. the pictures, one per line
        CheckUrlLines(urls);                            // 2. every line must look like a picture address

        if (!ModelState.IsValid)                        // 3. a rule on the form was broken
        {
            await FillCoursesAsync();
            return View(project);
        }

        try
        {
            await _projects.AddWithImagesAsync(project, urls);       // 4. the project and its pictures, in one transaction
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))   // 5. the database refused: nothing was saved
        {
            ModelState.AddModelError(DbErrors.FieldFor(ex),
                DbErrors.Explain(ex, "That course does not exist any more."));
            await FillCoursesAsync();
            return View(project);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET /Projects/Edit/5
    public async Task<IActionResult> Edit(long id)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        // Show the current pictures in the box, one per line.
        var images = await _images.GetByProjectAsync(id);
        project.ImageUrlsText = string.Join("\n", images.Select(image => image.ImageUrl));

        await FillCoursesAsync();
        return View(project);
    }

    // POST /Projects/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Project project)
    {
        var urls = ParseUrls(project.ImageUrlsText);
        CheckUrlLines(urls);

        if (!ModelState.IsValid)
        {
            await FillCoursesAsync();
            return View(project);
        }

        try
        {
            await _projects.UpdateWithImagesAsync(project, urls);    // the project and its pictures, in one transaction
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            ModelState.AddModelError(DbErrors.FieldFor(ex),
                DbErrors.Explain(ex, "That course does not exist any more."));
            await FillCoursesAsync();
            return View(project);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET /Projects/Delete/5
    public async Task<IActionResult> Delete(long id)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }
        return View(project);
    }

    // POST /Projects/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        await _projects.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
