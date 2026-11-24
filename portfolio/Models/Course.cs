using System.ComponentModel.DataAnnotations;

namespace portfolio.Models;

// One Course object holds one row of portfolio.course.
public class Course
{
    public long CourseId { get; set; }                  // course_id    BIGINT, the primary key

    [StringLength(10, ErrorMessage = "The code can be at most 10 characters.")]
    [RegularExpression(Rules.CourseCode, ErrorMessage = "Use capital letters followed by a number, such as ALGCOM1.")]
    public string Code { get; set; } = "";              // code         VARCHAR(10) NOT NULL, UNIQUE

    [StringLength(100, ErrorMessage = "The title can be at most 100 characters.")]
    public string Title { get; set; } = "";             // title        VARCHAR(100) NOT NULL

    [StringLength(30, ErrorMessage = "The term can be at most 30 characters.")]
    public string? Term { get; set; }                   // term         VARCHAR(30), may be NULL

    public string? Description { get; set; }            // description  TEXT, may be NULL

    public long ProjectCount { get; set; }              // filled by the course list only; not a column

    // Text for the course dropdown on the project form.
    public string Label => Code + " : " + Title;

    // Every course gets its own colour, worked out from its code, so it is always the same one.
    public int Hue => HueFor(Code);
    public static int HueFor(string? code) => (code ?? "").Aggregate(7, (h, c) => (h * 31 + c) % 360);
}