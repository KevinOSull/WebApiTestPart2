namespace WebApiTestPart2;

public class ReleaseData
{
    public string Artist { get; private set; }
    public string Title { get; private set; }
    public string Year { get; private set; }

    public ReleaseData(string artist, string title, string year)
    {
        Artist = artist;
        Title = title;
        Year = year;
    }
    
}