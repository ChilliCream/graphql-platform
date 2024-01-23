using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.OpenApi.Tests.Controller;

public record NewPet([Required]string Name, string? Tag);

public record Pet([Required]string Name, [Required] long Id, string? Tag) : NewPet(Name, Tag);

[ApiController]
public class PetStoreController : ControllerBase
{
    private static readonly List<Pet> _pets =
    [
        new Pet("Chopper", 1, null),
        new Pet("Rex", 2, null),
    ];

    [HttpPost]
    [Route("/pets")]
    public IActionResult AddPet([FromBody] NewPet body)
    {
        var newPet = new Pet(body.Name, _pets.Max(p => p.Id) + 1, body.Tag);

        _pets.Add(newPet);
        return new ObjectResult(newPet);
    }

    [HttpDelete]
    [Route("/pets/{id}")]
    public IActionResult DeletePet([FromRoute] [Required] long? id)
    {
        var toDelete = _pets.FirstOrDefault(p => p.Id == id);

        if (toDelete is null)
        {
            return BadRequest("Pet not found");
        }

        _pets.Remove(toDelete);
        return Ok();

    }

    [HttpGet]
    [Route("/pets/{id}")]
    public IActionResult FindPetById([FromRoute] [Required] long? id)
    {
        return new ObjectResult(_pets.FirstOrDefault(p => p.Id == id));
    }

    [HttpGet]
    [Route("/pets")]
    public IActionResult FindPets([FromQuery] List<string> tags, [FromQuery] int? limit)
    {
        return new ObjectResult(_pets);
    }
}
