using System.ComponentModel.DataAnnotations;

namespace portfolio.Models;

// One Project object holds one row of portfolio.project.
public class Project
{
    public long ProjectId { get; set; }                 // project_id  BIGINT, the primary key

    [Required(ErrorMessage = "Choose a course.")]
    public long? CourseId { get; set; }                 // course_id   BIGINT NOT NULL, the foreign key

    [StringLength(120, ErrorMessage = "The title can be at most 120 characters.")]
    public string Title { get; set; } = "";             // title       VARCHAR(120) NOT NULL

    [StringLength(300, ErrorMessage = "The summary can be at most 300 characters.")]
    public string Summary { get; set; } = "";           // summary     VARCHAR(300) NOT NULL

    public string? Details { get; set; }                // details     TEXT, may be NULL

    [StringLength(150, ErrorMessage = "The tools can be at most 150 characters.")]
    public string? Tools { get; set; }                  // tools       VARCHAR(150), may be NULL

    [Range(2020, 2100, ErrorMessage = "The year must be from 2020 to 2100.")]
    public int YearMade { get; set; } = DateTime.Today.Year;   // year_made   SMALLINT NOT NULL, 2020 to 2100

    [StringLength(200, ErrorMessage = "The video link can be at most 200 characters.")]
    [RegularExpression(Rules.VideoUrl, ErrorMessage = "Paste a YouTube link, such as https://youtu.be/dQw4w9WgXcQ.")]
    public string? VideoUrl { get; set; }               // video_url   VARCHAR(200), may be NULL

    [StringLength(200, ErrorMessage = "The repository link can be at most 200 characters.")]
    [RegularExpression(Rules.RepoUrl, ErrorMessage = "Use a GitHub address, such as https://github.com/name/portfolio.")]
    public string? RepoUrl { get; set; }                // repo_url    VARCHAR(200), may be NULL

    // Used by the project form only; not a column. One picture address per line.
    public string? ImageUrlsText { get; set; }

    // Filled by the SELECTs that join the course and the pictures; not columns of this table.
    // They are nullable on purpose: a form never posts them, so they must not be required.
    public string? CourseCode { get; set; }
    public string? CourseTitle { get; set; }
    public string? CoverUrl { get; set; }               // the first picture, for the cards
    public long ImageCount { get; set; }

    public int Hue => Course.HueFor(CourseCode);
}