namespace portfolio.Models;

// The regular expressions, in one place. The browser, the server and the database use the same ideas.
public static class Rules
{
    // A course code: 3 to 8 capital letters and then 1 or 2 digits, such as ALGCOM1.
    public const string CourseCode = @"^[A-Z]{3,8}[0-9]{1,2}$";

    // A picture address: http or https, no spaces, ending in a picture extension (a query string may follow).
    public const string ImageUrl = @"^https?://\S+\.(png|jpe?g|gif|webp|PNG|JPE?G|GIF|WEBP)(\?\S*)?$";

    // A YouTube link in one of the three forms the page can play: watch?v=, youtu.be/ or shorts/.
    public const string VideoUrl = @"^https?://(www\.)?(youtube\.com/watch\?v=|youtu\.be/|youtube\.com/shorts/)[A-Za-z0-9_-]{11}(\S*)?$";

    // A GitHub repository address, such as https://github.com/name/portfolio.
    public const string RepoUrl = @"^https://github\.com/[A-Za-z0-9-]+/[A-Za-z0-9._-]+/?$";
}
