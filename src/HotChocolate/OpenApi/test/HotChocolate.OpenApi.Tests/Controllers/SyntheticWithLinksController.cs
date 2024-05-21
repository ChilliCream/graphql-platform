using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.OpenApi.Tests.Controllers;

[ApiController]
public sealed class SyntheticWithLinksController : ControllerBase
{
    [HttpGet]
    [Route("/articles")]
    public IActionResult GetArticles()
    {
        return new JsonResult(new List<Article>
        {
            new(1, "title", AuthorUserId: 1),
            new(2, "title", AuthorUserId: 1),
        });
    }

    [HttpGet]
    [Route("/articles/{id:int}")]
    public IActionResult GetArticleById([FromRoute][Required] int id)
    {
        return new JsonResult(new Article(id, "title", AuthorUserId: 1));
    }

    [HttpGet]
    [Route("/users/{id:int}")]
    public IActionResult GetUserById([FromRoute][Required] int id)
    {
        return new JsonResult(new User(id, "username"));
    }
}

// ReSharper disable NotAccessedPositionalProperty.Global
public sealed record Article(int Id, string Title, int AuthorUserId);
public sealed record User(int Id, string Username);
// ReSharper restore NotAccessedPositionalProperty.Global
