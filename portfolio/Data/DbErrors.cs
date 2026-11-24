using Npgsql;

namespace portfolio.Data;

// Turns a refusal from the database into a sentence a visitor can read.
public static class DbErrors
{
    // True for the three refusals the site explains: foreign key (23503), unique (23505) and check (23514).
    public static bool IsRefusal(PostgresException ex) =>
        ex.SqlState == PostgresErrorCodes.ForeignKeyViolation ||
        ex.SqlState == PostgresErrorCodes.UniqueViolation ||
        ex.SqlState == PostgresErrorCodes.CheckViolation;

    public static string Explain(PostgresException ex, string foreignKeyMessage = "Another record still depends on this one, so the database refused the change.")
    {
        if (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return foreignKeyMessage;
        }

        if (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return ex.ConstraintName switch
            {
                "uq_course_code" => "Another course already uses this code.",
                "uq_project_course_title" => "This course already has a project with that title.",
                "uq_image_project_url" => "The same picture address is listed twice for this project. Nothing was saved.",
                _ => "That value is already used."
            };
        }

        return ex.ConstraintName switch
        {
            "ck_course_code_format" => "The code must be capital letters followed by a number, such as ALGCOM1.",
            "ck_project_year" => "The year must be from 2020 to 2100.",
            "ck_image_position" => "The picture position must be 1 or more.",
            _ => "A value broke a rule of the database."
        };
    }

    // Which box a refusal belongs under. An empty name means the message goes at the top of the form.
    public static string FieldFor(PostgresException ex) => ex.ConstraintName switch
    {
        "uq_course_code" => "Code",
        "ck_course_code_format" => "Code",
        "uq_project_course_title" => "Title",
        "ck_project_year" => "YearMade",
        "uq_image_project_url" => "ImageUrlsText",
        _ => ""
    };
}
