using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.OpenApi.Tests.Controllers;

[ApiController]
public sealed class PetStoreController : ControllerBase
{
    private readonly List<Pet> _pets =
    [
        new Pet(1, "Chopper", "dog"),
        new Pet(2, "Rex", "dog"),
        new Pet(3, "Polly", "bird"),
    ];

    [HttpPost]
    [Route("/pets")]
    public IActionResult AddPet([FromBody] NewPet newPet)
    {
        if (newPet.Tag is "")
        {
            return new JsonResult(
                new Error((int)HttpStatusCode.BadRequest, "Pet tag must not be empty."))
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        var pet = new Pet(
            Id: _pets.Max(p => p.Id) + 1,
            newPet.Name,
            newPet.Tag);

        _pets.Add(pet);

        return new JsonResult(pet);
    }

    [HttpGet]
    [Route("/pets/{id:long}")]
    public IActionResult FindPetById([FromRoute][Required] long id)
    {
        var pet = _pets.FirstOrDefault(p => p.Id == id);

        if (pet is null)
        {
            return new JsonResult(
                new Error((int)HttpStatusCode.NotFound, $"Pet with ID '{id}' not found."))
            {
                StatusCode = (int)HttpStatusCode.NotFound,
            };
        }

        return new JsonResult(pet);
    }

    [HttpGet]
    [Route("/pets")]
    public IActionResult FindPets([FromQuery] List<string> tags, [FromQuery] int? limit)
    {
        if (limit > 10)
        {
            return new JsonResult(
                new Error((int)HttpStatusCode.BadRequest, "Limit must be 10 or less."))
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        var pets = tags.Count is 0
            ? _pets
            : _pets.Where(p => p.Tag is not null && tags.Contains(p.Tag));

        return new JsonResult(pets.Take(limit ?? 10));
    }

    [HttpDelete]
    [Route("/pets/{id:long}")]
    public IActionResult DeletePet([FromRoute][Required] long id)
    {
        var toDelete = _pets.FirstOrDefault(p => p.Id == id);

        if (toDelete is null)
        {
            return new JsonResult(
                new Error((int)HttpStatusCode.NotFound, $"Pet with ID '{id}' not found."))
            {
                StatusCode = (int)HttpStatusCode.NotFound,
            };
        }

        _pets.Remove(toDelete);

        return NoContent();
    }
}

public record NewPet([Required] string Name, string? Tag);

public sealed record Pet([Required] long Id, [Required] string Name, string? Tag)
    : NewPet(Name, Tag);

// ReSharper disable NotAccessedPositionalProperty.Global
public sealed record Error([Required] int Code, [Required] string Message);
// ReSharper restore NotAccessedPositionalProperty.Global
