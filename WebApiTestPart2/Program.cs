using System.Text.Json.Nodes;
using WebApiTestPart2;

public class Program
{
    private static readonly string? ApiToken = Environment.GetEnvironmentVariable("DISCOGS_API_TOKEN");
    //private static readonly string SearchUrl = "https://api.discogs.com/artists/22217/releases";
    private static readonly string SearchUrl = "https://api.discogs.com/artistName/releases";
    private static readonly HttpClient Client = new HttpClient();
    private static string? url;
    private static string? recordLabelUrl;
    private static string year;
    private static List<ReleaseData> releaseData = new List<ReleaseData>();
    private static readonly string FolderPath = "resources";
    private static readonly string FilePath = "resources/releaseData.csv";
    private static string? artistName;
    private static string? recordLabelName;
	private static int fileOptionChoice;

    public static async Task Main(string[] args)
    {
	    int choice;
	    do
	    {
		    OptionsMenu();
		    choice = GetSelectionChoice();
		    SetRequestHeaders();
		    await MakeSelection(choice);

	    } while (choice != 0);
    }

	private static async Task EntryPoint()
	{
		int choice;
		do
		{
			OptionsMenu();
			choice = GetSelectionChoice();
			SetRequestHeaders();
			await MakeSelection(choice);
		} while (choice != 0);
	}

    private static int GetSelectionChoice()
    {
	    Console.WriteLine("Please choose an option: ");
	    int choice = Convert.ToInt32(Console.ReadLine());
	    return choice;
    }

    private static int GetArtistId(JsonArray results, int artIndex)
    {
	    JsonObject? res = results[artIndex]?.AsObject();
	    int num = res!["id"].AsValue().GetValue<int>();
	    return num;
    }

    private static int GetNumberOfPages(JsonObject results)
    {
	    JsonObject? res = results["pagination"]?.AsObject();
	    int numOfPages = res!["pages"].AsValue().GetValue<int>();
	    return numOfPages;
    }
    
    private static async Task<JsonObject> GetJsonObjectFromResponse(HttpResponseMessage response)
    {
	    string responseBody = await response.Content.ReadAsStringAsync();
	    JsonNode? json = JsonNode.Parse(responseBody);
	    return json!.AsObject();
    }

    private static void PrintResults(JsonArray json)
    {
	    for (int i = 0; i < json.Count; i++)
	    {
		    JsonObject? art = json[i]?.AsObject();
		    Console.WriteLine(( i + 1) + ".)" + art!["title"] );
	    }
    }
    
    private static async Task FetchAndProcessReleases(int artistId)
	{
		BuildPageUrl(artistId, 1);
		HttpResponseMessage firstResponse = await SendRequest();

		JsonObject firstJson = await GetJsonObjectFromResponse(firstResponse);
		int numberOfPages = GetNumberOfPages(firstJson);
		JsonArray firstPageReleases = firstJson["releases"]!.AsArray();
		AddReleasesToList(firstPageReleases);
		for (int i = 2; i <= numberOfPages; i++)
		{
			BuildPageUrl(artistId,i);
			HttpResponseMessage res = await SendRequest();
			JsonArray json = await GetReleasesFromResponse(res,"releases");
			AddReleasesToList(json);
		}	
		SortReleasesByYear();
		//WriteDataToFile();
		PrintReleases();
	}
    
	private static async Task FetchAndProcessLabelReleases(int labelId)
	{
		BuildLabelPageUrl(labelId, 1);
		HttpResponseMessage firstResponse = await SendRequest();

		JsonObject firstJson = await GetJsonObjectFromResponse(firstResponse);
		int numberOfPages = GetNumberOfPages(firstJson);
		JsonArray firstPageReleases = firstJson["releases"]!.AsArray();
		AddReleasesToList(firstPageReleases);

		for (int i = 2; i <= numberOfPages; i++)
		{
			BuildLabelPageUrl(labelId, i);
			HttpResponseMessage res = await SendRequest();
			JsonArray json = await GetReleasesFromResponse(res, "releases");
			AddReleasesToList(json);
		}

		SortReleasesByYear();
		PrintReleases();
	}

	private static void OptionsMenu()
	{
		Console.WriteLine("*************************");
		Console.WriteLine("1.) Search Artist.       ");
		Console.WriteLine("2.) Search Record Label. ");
		Console.WriteLine("0.) Exit                 ");
		Console.WriteLine("*************************");
	}

	private static async Task MakeSelection(int choice)
	{
		switch (choice)
		{
			case 1:
				artistName = GetArtistName();
				BuildArtistSearchUrl();
				HttpResponseMessage artistResponse = await SendRequest();
				JsonArray results = await GetReleasesFromResponse(artistResponse, "results");
				PrintResults(results);
				int artIndex = getArtistIndex();
				int artistId = GetArtistId(results, artIndex);
				await FetchAndProcessReleases(artistId);
                GetFileOptionChoiceFromUser();
                GetFileOption();
                break;
				
			case 2:
				recordLabelName = GetRecordLabelName();
				BuildRecordLabelSearchUrl();
				HttpResponseMessage labelResponse = await SendRequest();
				JsonArray labelResults = await GetReleasesFromResponse(labelResponse, "results");
				PrintResults(labelResults);
				int recordLabelIndex = getArtistIndex();
				int recordLabelId = GetArtistId(labelResults, recordLabelIndex);
				await FetchAndProcessLabelReleases(recordLabelId);
				break;
			
			case 0:
				break;
				
			default:
				Console.WriteLine("SOMETHING WENT WRONG!");
				break;
		}
	}

	private static int GetFileOptionChoiceFromUser()
	{
		Console.WriteLine("Press 1 to save search to csv file or 2 to return to main menu: ");
		fileOptionChoice = Convert.ToInt32(Console.ReadLine());
		return fileOptionChoice;
	}

	private static void GetFileOption()
	{
		switch (fileOptionChoice)
		{
			case 1:
				Console.WriteLine("File stuff");
				break;

			case 2:
				break;

			default:
				Console.WriteLine("ERROR!");
				break;
		}
	}

	private static void BuildArtistSearchUrl()
	{
		url = "https://api.discogs.com/database/search?q=" + artistName + "&type=artist";
	}

	private static void BuildRecordLabelSearchUrl()
	{
		url = "https://api.discogs.com/database/search?q=" + recordLabelName + "&type=label";
	}

	private static void SetRequestHeaders()
	{
		Client.DefaultRequestHeaders.Remove("Authorization");
		Client.DefaultRequestHeaders.UserAgent.ParseAdd(
			"WebAPITest/1.0"
		);
		Client.DefaultRequestHeaders.Add(
			"Authorization",
			"Discogs token=" + ApiToken
		);
	}

	private static void BuildPageUrl(int artistId,int pageNumber)
	{
		url = "https://api.discogs.com/artists/" + artistId + "/releases?page=" + pageNumber;
	}
	
	private static void BuildLabelPageUrl(int labelId, int pageNumber)
	{
		url = "https://api.discogs.com/labels/" + labelId + "/releases?page=" + pageNumber;
	}

	private static async Task<HttpResponseMessage> SendRequest()
	{
		HttpResponseMessage response =
			await Client.GetAsync(url);
		response.EnsureSuccessStatusCode();
		return response;
	}

	private static async Task<JsonArray> GetReleasesFromResponse(HttpResponseMessage response,string responseInput)
	{
		string responseFromInput = await response.Content.ReadAsStringAsync();
		JsonNode? json = JsonNode.Parse(responseFromInput);
		JsonArray artist = json![responseInput]!.AsArray();
		return artist;
	}

	private static String GetArtistName()
	{
		Console.WriteLine("Please enter the name of the artist:");
		string? artist = Console.ReadLine();
		return artist;
	}

	private static String GetRecordLabelName()
	{
		Console.WriteLine("Please enter the name of the record label: ");
		string recordLabelName = Console.ReadLine();
		return recordLabelName;
	}

	private static int getArtistIndex()
	{
		Console.WriteLine("Enter Artist Number: ");
		int artistIndex = int.Parse(Console.ReadLine())-1;
		return artistIndex;
	}

	/*private static async Task<JsonArray> GetReleasesFromResponse(HttpResponseMessage response)
	{
		string responseBody = await response.Content.ReadAsStringAsync();
		JsonNode? json = JsonNode.Parse(responseBody);
		JsonArray releases = json!["releases"]!.AsArray();
		return releases;
	}*/

	private static void SortReleasesByYear()
	{
		releaseData = releaseData.OrderBy(r => r.Year).ToList();
	}

	private static void PrintReleases()
	{
		foreach (ReleaseData obj in releaseData)
		{
			Console.WriteLine("Artist: " + obj.Artist + " Title: " + obj.Title + " Year: " + obj.Year);
		}
	}
	
	private static void AddReleasesToList(JsonArray releases)
	{
		for (int i = 0; i < releases.Count; i++)
		{
			JsonObject? rel = releases[i]?.AsObject();
			string artist = rel!["artist"]!.ToString();
			string title = rel!["title"]!.ToString();
			if (rel.ContainsKey("year"))
			{
				year = rel!["year"]!.ToString();
			}
			else
			{
				year = "Unknown";
			}

			ReleaseData r = new ReleaseData(artist, title, year);
			releaseData.Add(r);
		}
	}

	private static void CheckIfFileExists()
	{
		if(!Directory.Exists(FolderPath))
		{
			Directory.CreateDirectory(FolderPath);
		}

		if (!File.Exists(FilePath))
		{
			File.Create(FilePath).Close();
		}
	}

	private static void WriteDataToFile()
	{
		using StreamWriter writer = new StreamWriter(FilePath, true);
		for (int i = 0; i < releaseData.Count; i++)
		{
			writer.WriteLine(
				"\"" + releaseData[i].Artist + "\"," +
				"\"" + releaseData[i].Title + "\"," +
				"\"" + releaseData[i].Year + "\""
			);
		}
	}
}