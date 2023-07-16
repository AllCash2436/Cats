using Microsoft.AspNetCore.Mvc; 
using System.Net.Mime;
using Microsoft.Extensions.Caching.Memory;


//http://localhost:5230/StatusCat/GetImage?url=https://ya.ru/ - works
//http://localhost:5230/StatusCat/GetImage?url=https://dabudidabudai.ru/ - don't work

namespace StatusCat.Controllers;
//route to the controller
[ApiController] 
[Route("[controller]")] 

public class StatusCatController : ControllerBase // get request = create StatusCatController
{
    private readonly IMemoryCache _cash;

    public StatusCatController(IMemoryCache cash)
    {
        _cash = cash;
    }

    [HttpGet("GetImage")] 

    public async Task<IResult> GetImage(string url)
    //IResult - results api
    {
        using var httpClient = new HttpClient(); //HttpClient sends HTTP(s) requests

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return Results.BadRequest($"url is wrong ({url})");
        }

        int statusCode;
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(uri);
            statusCode = Convert.ToInt32(response.StatusCode);
        }
        catch (HttpRequestException hre)
        {
            if (hre.StatusCode is null)
                return Results.BadRequest($"error by {url}");

            statusCode = Convert.ToInt32(hre.StatusCode);
        }
        catch
        {
            return Results.BadRequest($"error by {url}");
        }

    
        //get pics from cash
        if (_cash.TryGetValue<byte[]?>(statusCode, out byte[]? imageBytes) && imageBytes is not null)
        {
            return Results.File(imageBytes, MediaTypeNames.Image.Jpeg);
        }

     
        imageBytes = await GetImageByStatusCode(statusCode);

        await Task.Run
        (
            () =>
            {
                _cash.Set
                (
                    statusCode,
                    imageBytes, //cash pic
                    TimeSpan.FromMinutes(1) //delete-pic-time from cash
                ); 
            }
        );

        return Results.File(imageBytes, MediaTypeNames.Image.Jpeg);
    }

    public async Task<byte[]> GetImageByStatusCode(int statusCode)
    {
        using HttpClient httpClient = new HttpClient();
        
        using HttpResponseMessage responseMessage = await httpClient.GetAsync(new Uri($"https://http.cat/{statusCode}.jpg"));
        
        var bytes = await responseMessage.Content.ReadAsByteArrayAsync();

        return bytes;
    }
}
