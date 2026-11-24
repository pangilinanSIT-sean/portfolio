namespace portfolio.Models;

// One ProjectImage object holds one row of portfolio.project_image.
public class ProjectImage
{
    public long ImageId { get; set; }                   // image_id    BIGINT, the primary key
    public long ProjectId { get; set; }                 // project_id  BIGINT NOT NULL, the foreign key
    public string ImageUrl { get; set; } = "";          // image_url   VARCHAR(500) NOT NULL, unique for one project
    public string? Caption { get; set; }                // caption     VARCHAR(150), may be NULL
    public int Position { get; set; } = 1;              // position    SMALLINT NOT NULL, 1 or more
}
