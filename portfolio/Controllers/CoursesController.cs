using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

// The editor side: add, edit and delete courses. [Authorize] sends visitors who are signed out to the login page.
[Authorize]
public class CoursesController : Controller
{
    private readonly CourseRepository _courses;

    public CoursesController(CourseRepository courses)
    {
        _courses = courses;
    }

    // GET /Courses
    public async Task<IActionResult> Index()
    {
        return View(await _courses.GetAllAsync());
    }

    // GET /Courses/Create
    public IActionResult Create()
    {
        return View(new Course());
    }

    // POST /Courses/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course)
    {
        if (!ModelState.IsValid)                        // a rule on the form was broken
        {
            return View(course);
        }

        try
        {
            await _courses.AddAsync(course);
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))   // unique (23505) or check (23514)
        {
            ModelState.AddModelError(DbErrors.FieldFor(ex), DbErrors.Explain(ex));   // a message under the right box
            return View(course);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET /Courses/Edit/5
    public async Task<IActionResult> Edit(long id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound();
        }
        return View(course);
    }

    // POST /Courses/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Course course)
    {
        if (!ModelState.IsValid)
        {
            return View(course);
        }

        try
        {
            await _courses.UpdateAsync(course);
        }
        catch (PostgresException ex) when (DbErrors.IsRefusal(ex))
        {
            ModelState.AddModelError(DbErrors.FieldFor(ex), DbErrors.Explain(ex));
            return View(course);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET /Courses/Delete/5
    public async Task<IActionResult> Delete(long id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound();
        }
        return View(course);
    }

    // POST /Courses/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        try
        {
            await _courses.DeleteAsync(id);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)   // 23503: the course still has projects
        {
            var course = await _courses.GetByIdAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewBag.Error = DbErrors.Explain(ex, "This course still has projects, so the database will not delete it. Delete or move its projects first.");
            return View("Delete", course);
        }

        return RedirectToAction(nameof(Index));
    }
}